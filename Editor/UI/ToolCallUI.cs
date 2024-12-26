using UnityEngine;
using UnityEditor;
using UnityLLMAPI.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityAIHelper.Editor.UI
{
    /// <summary>
    /// 工具调用UI控件
    /// </summary>
    public class ToolCallUI
    {
        private class ToolCallCache
        {
            public string Args{ get; set; }
            public string Result{ get; set; }
            public string ShortArgs { get; set; }
            public string DetailedArgs { get; set; }
            public string FormattedResult { get; set; }
        }

        // 使用字典存储每个工具调用的展开状态和缓存
        private Dictionary<string, bool> expandedStates = new Dictionary<string, bool>();
        private Dictionary<string, ToolCallCache> argsCache = new Dictionary<string, ToolCallCache>();
        private GUIStyle toolCallStyle;
        private GUIStyle headerStyle;
        private GUIStyle contentStyle;
        private GUIStyle resultStyle;
        private readonly GUIContent tempContent = new GUIContent();

        /// <summary>
        /// 尝试格式化JSON字符串
        /// </summary>
        private string TryFormatJson(string jsonStr)
        {
            try
            {
                // 尝试解析为JToken（可以是对象或数组）
                var token = JToken.Parse(jsonStr);
                return JsonConvert.SerializeObject(token, Formatting.Indented);
            }
            catch
            {
                return jsonStr;
            }
        }

        /// <summary>
        /// 获取或创建工具调用的参数缓存
        /// </summary>
        private ToolCallCache GetOrCreateCache(ToolCall toolCall, ChatMessage result)
        {
            if (argsCache.TryGetValue(toolCall.id, out var cache))
            {
                // 如果结果发生变化，更新缓存
                if (result?.content == cache.Result&&toolCall.function.arguments==cache.Args)
                {
                    return cache;
                }
            }

            cache ??= new ToolCallCache();
            try
            {
                var obj = JObject.Parse(toolCall.function.arguments);
                
                // 生成简短格式
                var parts = new List<string>();
                foreach (var prop in obj.Properties())
                {
                    var value = prop.Value;
                    string displayValue;
                    
                    if (value.Type == JTokenType.Object || value.Type == JTokenType.Array)
                    {
                        displayValue = value.Type == JTokenType.Object ? "{...}" : "[...]";
                    }
                    else
                    {
                        displayValue = value.ToString();
                        if (displayValue.Length > 20)
                        {
                            displayValue = displayValue.Substring(0, 17) + "...";
                        }
                    }
                    
                    parts.Add($"{prop.Name}: {displayValue}");
                }
                
                var shortResult = string.Join(", ", parts);
                cache.ShortArgs = shortResult.Length > 50 ? shortResult.Substring(0, 47) + "..." : shortResult;
                
                // 生成详细格式
                cache.DetailedArgs = JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch
            {
                // 解析失败时使用原始字符串
                cache.ShortArgs = toolCall.function.arguments.Length > 50 
                    ? toolCall.function.arguments.Substring(0, 47) + "..." 
                    : toolCall.function.arguments;
                cache.DetailedArgs = toolCall.function.arguments;
            }

            // 处理结果
            if (result != null)
            {
                cache.FormattedResult = TryFormatJson(result.content);
            }

            cache.Args = toolCall.function.arguments;
            cache.Result = result?.content;
            argsCache[toolCall.id] = cache;
            return cache;
        }
        
        private void InitStyles()
        {
            if (toolCallStyle != null) return;
            
            // 工具调用区域样式
            toolCallStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(4, 4, 4, 4)
            };
            
            // 头部样式
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.4f, 0.8f, 1f) }
            };
            
            // 内容样式
            contentStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                richText = true,
                padding = new RectOffset(4, 4, 4, 4)
            };
            
            // 结果样式
            resultStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                richText = true,
                padding = new RectOffset(4, 4, 4, 4),
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
        }

        /// <summary>
        /// 绘制工具调用UI
        /// </summary>
        /// <param name="toolCall">工具调用数据</param>
        /// <param name="position">绘制位置</param>
        /// <param name="width">绘制宽度</param>
        /// <param name="result">工具执行结果</param>
        /// <returns>绘制的总高度</returns>
        public float Draw(ToolCall toolCall, float y, float width, ChatMessage result)
        {
            InitStyles();
            
            // 确保工具调用有展开状态
            if (!expandedStates.ContainsKey(toolCall.id))
            {
                expandedStates[toolCall.id] = false; // 默认收起
            }
            
            // 获取参数缓存
            var cache = GetOrCreateCache(toolCall, result);
            
            float startY = y;
            float currentY = y;
            
            // 计算内容高度
            float contentHeight = CalculateContentHeight(toolCall, width, result);
            
            // 绘制背景
            GUI.Box(new Rect(0, currentY, width, contentHeight), "", toolCallStyle);
            currentY += toolCallStyle.padding.top;
            
            // 绘制头部(可折叠)
            Rect headerRect = new Rect(8, currentY, width - 16, 20);
            
            // 在 Foldout 前绘制一个箭头，用于控制是否显示参数
            expandedStates[toolCall.id] = EditorGUI.Foldout(headerRect, expandedStates[toolCall.id], "", true);
            
            // 绘制工具调用名和参数（收起状态下）
            Rect labelRect = new Rect(headerRect.x + 16, headerRect.y, headerRect.width - 16, headerRect.height);
            string headerText = !expandedStates[toolCall.id]
                ? $"工具调用: {toolCall.function.name} ({cache.ShortArgs})"
                : $"工具调用: {toolCall.function.name}";
            GUI.Label(labelRect, headerText, headerStyle);
            
            currentY += 24; // 头部高度 + 间距

            // 如果是收起状态，不显示参数（因为已经在头部显示了）
            if (!expandedStates[toolCall.id])
            {
                // 不需要额外显示参数了
            }
            // 展开时显示详细信息
            else
            {
                // 绘制参数
                GUI.Label(new Rect(16, currentY, width - 32, 20), "参数:", EditorStyles.boldLabel);
                currentY += 20;
                
                tempContent.text = cache.DetailedArgs;
                float argsHeight = contentStyle.CalcHeight(tempContent, width - 32);
                GUI.TextArea(new Rect(16, currentY, width - 32, argsHeight),
                    cache.DetailedArgs, contentStyle);
                currentY += argsHeight + 8;
                
                // 如果有执行结果，显示结果
                if (result != null && !string.IsNullOrEmpty(result.content))
                {
                    GUI.Label(new Rect(16, currentY, width - 32, 20), "执行结果:", EditorStyles.boldLabel);
                    currentY += 20;
                    
                    tempContent.text = cache.FormattedResult;
                    float resultHeight = resultStyle.CalcHeight(tempContent, width - 32);
                    GUI.TextArea(new Rect(16, currentY, width - 32, resultHeight),
                        cache.FormattedResult, resultStyle);
                    currentY += resultHeight + 8;
                }
            }
            return contentHeight;
        }
        
        /// <summary>
        /// 计算工具调用内容的总高度
        /// </summary>
        public float CalculateContentHeight(ToolCall toolCall, float width, ChatMessage result)
        {
            InitStyles();
            
            // 确保工具调用有展开状态
            if (!expandedStates.ContainsKey(toolCall.id))
            {
                expandedStates[toolCall.id] = false;
            }
            
            // 获取参数缓存
            var cache = GetOrCreateCache(toolCall, result);
            
            float height = toolCallStyle.padding.vertical; // 背景padding
            height += 24; // 头部高度 + 间距

            // 如果是收起状态，不需要额外显示参数
            if (!expandedStates[toolCall.id])
            {
                // 不需要额外显示参数了
            }
            else
            {
                height += 20; // "参数:" 标签
                
                // 计算参数文本高度
                tempContent.text = cache.DetailedArgs;
                height += contentStyle.CalcHeight(tempContent, width - 32);
                height += 8; // 参数底部padding
                
                // 如果有执行结果，计算结果高度
                if (result != null && !string.IsNullOrEmpty(result.content))
                {
                    height += 20; // "执行结果:" 标签
                    tempContent.text = cache.FormattedResult;
                    height += resultStyle.CalcHeight(tempContent, width - 32);
                    height += 8; // 结果底部padding
                }
            }
            
            return height;
        }
    }
}
