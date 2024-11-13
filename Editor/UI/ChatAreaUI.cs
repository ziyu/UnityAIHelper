using System.Collections.Generic;
using System.Linq;
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
        private readonly ToolCallUI toolCallUI;
        
        // 缓存过滤后的消息列表
        private List<ChatMessage> filteredMessages;
        private List<ChatMessage> toolResults;
        private int lastMessageCount;
        private ChatMessage lastStreamingMessage;
        private bool lastIsStreaming;
        
        // UI常量
        private const float MESSAGE_SPACING = 8f;
        private const float TOOL_CALL_SPACING = 4f;
        private const float CONTENT_PADDING = 8f;

        public ChatAreaUI(AIHelperWindow window)
        {
            this.window = window;
            tempContent = new GUIContent();
            toolCallUI = new ToolCallUI();
            filteredMessages = new List<ChatMessage>();
            toolResults = new List<ChatMessage>();
        }
        
        void InitStyles()
        {
            if(messageStyle!=null)return;
            messageStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                richText = true,
                padding = new RectOffset(4, 4, 4, 4)
            };
        }
        
        /// <summary>
        /// 检查并更新过滤后的消息列表和工具调用结果
        /// </summary>
        private void UpdateFilteredMessages(IReadOnlyList<ChatMessage> chatHistory, ChatMessage streamingMessage, bool isStreaming)
        {
            // 检查是否需要更新
            if (chatHistory.Count == lastMessageCount && 
                streamingMessage == lastStreamingMessage && 
                isStreaming == lastIsStreaming)
            {
                return;
            }
            
            // 更新缓存状态
            lastMessageCount = chatHistory.Count;
            lastStreamingMessage = streamingMessage;
            lastIsStreaming = isStreaming;
            
            // 清空并重新填充列表
            filteredMessages.Clear();
            toolResults.Clear();
            
            // 分类消息
            foreach (var message in chatHistory)
            {
                if (message.role == "tool")
                {
                    toolResults.Add(message);
                }
                else if (message.role != "system")
                {
                    filteredMessages.Add(message);
                }
            }
            
            // 如果有streaming消息且不是系统消息或工具执行结果，添加到过滤列表
            if (isStreaming && streamingMessage != null && 
                streamingMessage.role != "system" && streamingMessage.role != "tool")
            {
                filteredMessages.Add(streamingMessage);
            }
        }
        
        /// <summary>
        /// 获取工具调用对应的执行结果
        /// </summary>
        private ChatMessage GetToolCallResult(ToolCall toolCall)
        {
            return toolResults.FirstOrDefault(r => 
                r.role == "tool" && 
                r.tool_call_id == toolCall.id);
        }

        public void Draw(float height, IReadOnlyList<ChatMessage> chatHistory, ChatMessage streamingMessage = null, bool isStreaming = false)
        {
            InitStyles();
            
            // 更新过滤后的消息列表和工具调用结果
            UpdateFilteredMessages(chatHistory, streamingMessage, isStreaming);

            // 使用固定的宽度
            float viewWidth = cachedViewWidth;
            float contentWidth = viewWidth - 16; // 预留滚动条宽度

            // 计算内容总高度
            float totalContentHeight = CalculateTotalContentHeight(contentWidth);

            // 确保内容高度至少等于视图高度
            totalContentHeight = Mathf.Max(totalContentHeight, height);

            // 创建滚动视图区域
            Rect scrollViewRect = EditorGUILayout.GetControlRect(false, height);
            Rect contentRect = new Rect(0, 0, contentWidth, totalContentHeight);

            // 检查是否需要滚动到底部
            if (shouldScrollToBottom)
            {
                scrollPosition.y = totalContentHeight - height;
                shouldScrollToBottom = false;
                lastContentHeight = totalContentHeight;
            }

            // 绘制滚动视图
            scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, contentRect, false, true);

            float currentY = 0;
            for (int i = 0; i < filteredMessages.Count; i++)
            {
                float messageHeight = DrawMessage(filteredMessages[i], currentY, contentWidth);
                currentY += messageHeight;
                
                if (i < filteredMessages.Count - 1)
                {
                    currentY += MESSAGE_SPACING;
                }
            }

            GUI.EndScrollView();
            
            // 处理滚轮事件
            if (Event.current.type == EventType.ScrollWheel && scrollViewRect.Contains(Event.current.mousePosition))
            {
                scrollPosition.y += Event.current.delta.y * 20f;
                scrollPosition.y = Mathf.Clamp(scrollPosition.y, 0, totalContentHeight - height);
                Event.current.Use();
                window.Repaint();
            }
        }
        
        /// <summary>
        /// 计算内容总高度
        /// </summary>
        private float CalculateTotalContentHeight(float width)
        {
            if (filteredMessages.Count == 0) return 0;
            
            float totalHeight = 0;
            
            // 计算所有消息的高度和间距
            for (int i = 0; i < filteredMessages.Count; i++)
            {
                totalHeight += CalculateMessageHeight(filteredMessages[i], width);
                
                // 除了最后一条消息，每条消息后都添加间距
                if (i < filteredMessages.Count - 1)
                {
                    totalHeight += MESSAGE_SPACING;
                }
            }
            
            return totalHeight;
        }

        private float CalculateMessageHeight(ChatMessage message, float width)
        {
            float height = 20; // 发送者标签高度
            height += CONTENT_PADDING; // 顶部padding

            // 计算消息内容高度
            if (!string.IsNullOrEmpty(message.content))
            {
                tempContent.text = message.content;
                height += messageStyle.CalcHeight(tempContent, width - 16);
            }

            // 如果有工具调用，计算工具调用信息高度
            if (message.tool_calls != null && message.tool_calls.Length > 0)
            {
                for (int i = 0; i < message.tool_calls.Length; i++)
                {
                    if (i > 0) height += TOOL_CALL_SPACING;
                    var result = GetToolCallResult(message.tool_calls[i]);
                    height += toolCallUI.CalculateContentHeight(message.tool_calls[i], width - 16, result);
                }
            }

            height += CONTENT_PADDING; // 底部padding
            return height;
        }

        private float DrawMessage(ChatMessage message, float y, float width)
        {
            float startY = y;
            float currentY = y + CONTENT_PADDING;
            
            // 绘制发送者
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = GetMessageColor(message.role);
            GUI.Label(new Rect(8, currentY, width - 16, 20), GetMessagePrefix(message.role), style);
            currentY += 20;

            // 绘制消息内容
            if (!string.IsNullOrEmpty(message.content))
            {
                tempContent.text = message.content;
                float contentHeight = messageStyle.CalcHeight(tempContent, width - 16);
                GUI.TextArea(new Rect(8, currentY, width - 16, contentHeight), message.content, messageStyle);
                currentY += contentHeight;
            }

            // 如果有工具调用，使用ToolCallUI绘制
            if (message.tool_calls != null && message.tool_calls.Length > 0)
            {
                for (int i = 0; i < message.tool_calls.Length; i++)
                {
                    if (i > 0) currentY += TOOL_CALL_SPACING;
                    var result = GetToolCallResult(message.tool_calls[i]);
                    float toolCallHeight = toolCallUI.Draw(message.tool_calls[i], currentY, width - 16, result);
                    currentY += toolCallHeight;
                }
            }

            return currentY - startY + CONTENT_PADDING;
        }

        private string GetMessagePrefix(string role)
        {
            return role switch
            {
                "user" => "你:",
                "assistant" => "AI:",
                _ => role + ":"
            };
        }

        private Color GetMessageColor(string role)
        {
            return role switch
            {
                "user" => Color.blue,
                "assistant" => Color.green,
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
