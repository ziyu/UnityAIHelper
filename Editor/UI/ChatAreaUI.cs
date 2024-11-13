using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor.UI
{
    /// <summary>
    /// 聊天区域UI组件
    /// </summary>
    public class ChatAreaUI
    {
        private readonly AIHelperWindow window;
        private Vector2 scrollPosition;
        private float lastContentHeight = 0f;
        private bool shouldScrollToBottom = false;
        private float cachedViewWidth = 0f;
        
        private GUIStyle messageStyle;
        private readonly GUIContent tempContent;

        public ChatAreaUI(AIHelperWindow window)
        {
            this.window = window;
            tempContent = new GUIContent();
        }
        
        void InitStyles()
        {
            if(messageStyle!=null)return;
            messageStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                richText = true
            };
        }

        public void Draw(float height, IReadOnlyList<ChatMessage> chatHistory, ChatMessage streamingMessage = null, bool isStreaming = false)
        {
            InitStyles();

            // 使用固定的宽度
            float viewWidth = cachedViewWidth;
            float contentWidth = viewWidth - 20; // 预留滚动条宽度

            // 创建固定大小的滚动视图区域
            Rect scrollViewRect = GUILayoutUtility.GetRect(viewWidth, height);
            Rect contentRect = new Rect(0, 0, contentWidth - 20, 0);

            float totalContentHeight = 0;

            // 计算内容总高度
            foreach (var message in chatHistory)
            {
                totalContentHeight += CalculateMessageHeight(message, contentWidth - 40);
                totalContentHeight += 10; // 消息间距
            }

            // 如果有streaming消息，添加其高度
            if (isStreaming && streamingMessage != null)
            {
                totalContentHeight += CalculateMessageHeight(streamingMessage, contentWidth - 40);
                totalContentHeight += 10;
            }

            contentRect.height = totalContentHeight;

            // 检查是否需要滚动到底部
            if (shouldScrollToBottom || Mathf.Abs(totalContentHeight - lastContentHeight) > 1)
            {
                scrollPosition.y = totalContentHeight - height;
                shouldScrollToBottom = false;
                lastContentHeight = totalContentHeight;
            }

            // 绘制滚动视图
            scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, contentRect);

            float currentY = 0;
            foreach (var message in chatHistory)
            {
                float messageHeight = DrawMessage(message, currentY, contentWidth - 40);
                currentY += messageHeight + 5;
            }

            // 绘制streaming消息
            if (isStreaming && streamingMessage != null)
            {
                DrawMessage(streamingMessage, currentY, contentWidth - 40);
            }

            GUI.EndScrollView();
        }

        private float CalculateMessageHeight(ChatMessage message, float width)
        {
            float height = 20; // 发送者标签高度

            // 计算消息内容高度
            tempContent.text = message.content ?? "";
            height += messageStyle.CalcHeight(tempContent, width);

            // 如果有工具调用，计算工具调用信息高度
            if (message.tool_calls != null)
            {
                foreach (var toolCall in message.tool_calls)
                {
                    height += 20; // 工具调用标签高度
                    tempContent.text = $"{toolCall.function.name}: {toolCall.function.arguments}";
                    height += messageStyle.CalcHeight(tempContent, width);
                }
            }

            return height + 20; // 添加边距
        }

        private float DrawMessage(ChatMessage message, float y, float width)
        {
            float startY = y;
            float currentY = y + 5;
            
            // 绘制发送者
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = GetMessageColor(message.role);
            GUI.Label(new Rect(10, currentY, width, 20), GetMessagePrefix(message.role), style);
            currentY += 20;

            // 绘制消息内容
            tempContent.text = message.content ?? "";
            float contentHeight = messageStyle.CalcHeight(tempContent, width);
            GUI.TextArea(new Rect(10, currentY, width, contentHeight), message.content ?? "", messageStyle);
            currentY += contentHeight;

            // 如果有工具调用，绘制工具调用信息
            if (message.tool_calls != null)
            {
                foreach (var toolCall in message.tool_calls)
                {
                    currentY += 5;
                    GUI.Label(new Rect(10, currentY, width, 20), "执行命令:", EditorStyles.boldLabel);
                    currentY += 20;

                    tempContent.text = $"{toolCall.function.name}: {toolCall.function.arguments}";
                    float toolCallHeight = messageStyle.CalcHeight(tempContent, width);
                    GUI.TextArea(new Rect(10, currentY, width, toolCallHeight), 
                        $"{toolCall.function.name}: {toolCall.function.arguments}", messageStyle);
                    currentY += toolCallHeight;
                }
            }

            return currentY - startY + 5;
        }

        private string GetMessagePrefix(string role)
        {
            return role switch
            {
                "system" => "系统:",
                "user" => "你:",
                "assistant" => "AI:",
                "tool" => "工具执行结果:",
                _ => role + ":"
            };
        }

        private Color GetMessageColor(string role)
        {
            return role switch
            {
                "system" => Color.yellow,
                "user" => Color.blue,
                "assistant" => Color.green,
                "tool" => Color.cyan,
                _ => Color.white
            };
        }

        public void SetViewWidth(float width)
        {
            if (Mathf.Abs(cachedViewWidth - width) > 1)
            {
                cachedViewWidth = width;
            }
        }

        public void ScrollToBottom()
        {
            shouldScrollToBottom = true;
        }
    }
}
