using UnityEngine;
using UnityEditor;
using UnityLLMAPI.Models;
using System.Collections.Generic;

namespace UnityAIHelper.Editor.UI
{
    /// <summary>
    /// 工具调用UI控件
    /// </summary>
    public class ToolCallUI
    {
        // 使用字典存储每个工具调用的展开状态
        private Dictionary<string, bool> expandedStates = new Dictionary<string, bool>();
        private GUIStyle toolCallStyle;
        private GUIStyle headerStyle;
        private GUIStyle contentStyle;
        private GUIStyle resultStyle;
        private readonly GUIContent tempContent = new GUIContent();
        
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
                expandedStates[toolCall.id] = true; // 默认展开
            }
            
            float startY = y;
            float currentY = y;
            
            // 计算内容高度
            float contentHeight = CalculateContentHeight(toolCall, width, result);
            
            // 绘制背景
            GUI.Box(new Rect(0, currentY, width, contentHeight), "", toolCallStyle);
            currentY += toolCallStyle.padding.top;
            
            // 绘制头部(可折叠)
            Rect headerRect = new Rect(8, currentY, width - 16, 20);
            bool newExpanded = EditorGUI.Foldout(headerRect, expandedStates[toolCall.id], 
                $"工具调用: {toolCall.function.name}", true, headerStyle);
            
            if (newExpanded != expandedStates[toolCall.id])
            {
                expandedStates[toolCall.id] = newExpanded;
            }
            currentY += 24; // 头部高度 + 间距

            // 展开时显示详细信息
            if (expandedStates[toolCall.id])
            {
                // 绘制参数
                GUI.Label(new Rect(16, currentY, width - 32, 20), "参数:", EditorStyles.boldLabel);
                currentY += 20;
                
                tempContent.text = toolCall.function.arguments;
                float argsHeight = contentStyle.CalcHeight(tempContent, width - 32);
                GUI.TextArea(new Rect(16, currentY, width - 32, argsHeight), 
                    toolCall.function.arguments, contentStyle);
                currentY += argsHeight + 8;
                
                // 如果有执行结果，显示结果
                if (result != null && !string.IsNullOrEmpty(result.content))
                {
                    GUI.Label(new Rect(16, currentY, width - 32, 20), "执行结果:", EditorStyles.boldLabel);
                    currentY += 20;
                    
                    tempContent.text = result.content;
                    float resultHeight = resultStyle.CalcHeight(tempContent, width - 32);
                    GUI.TextArea(new Rect(16, currentY, width - 32, resultHeight), 
                        result.content, resultStyle);
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
                expandedStates[toolCall.id] = true; // 默认展开
            }
            
            float height = toolCallStyle.padding.vertical; // 背景padding
            height += 24; // 头部高度 + 间距
            
            if (expandedStates[toolCall.id])
            {
                height += 20; // "参数:" 标签
                
                // 计算参数文本高度
                tempContent.text = toolCall.function.arguments;
                height += contentStyle.CalcHeight(tempContent, width - 32);
                height += 8; // 参数底部padding
                
                // 如果有执行结果，计算结果高度
                if (result != null && !string.IsNullOrEmpty(result.content))
                {
                    height += 20; // "执行结果:" 标签
                    tempContent.text = result.content;
                    height += resultStyle.CalcHeight(tempContent, width - 32);
                    height += 8; // 结果底部padding
                }
            }
            
            return height;
        }
    }
}
