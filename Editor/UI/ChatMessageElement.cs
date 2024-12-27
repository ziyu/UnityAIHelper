using UnityEngine;
using UnityEngine.UIElements;
using UnityLLMAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityLLMAPI.Utils.Json;

namespace UnityAIHelper.Editor.UI
{
    public class ChatMessageElement : VisualElement
    {
        private readonly ChatMessageInfo messageInfo;
        private readonly Action<ChatMessageInfo> onDelete;
        private readonly List<ChatMessage> toolResults;
        private Label contentLabel;
        private VisualElement toolCallContainer;
        private List<ToolCallElement> toolCallElements;

        public ChatMessageElement(
            ChatMessageInfo messageInfo, 
            Action<ChatMessageInfo> onDelete = null,
            List<ChatMessage> toolResults = null)
        {
            this.messageInfo = messageInfo;
            this.onDelete = onDelete;
            this.toolResults = toolResults ?? new List<ChatMessage>();

            AddToClassList("message-container");
            name = "message-" + messageInfo.messageId;

            CreateUI();
        }

        private void CreateUI()
        {
            // 创建头部
            var header = CreateHeader();
            Add(header);

            // 创建内容
            contentLabel = new Label(messageInfo.message.content);
            contentLabel.AddToClassList("message-content");
            contentLabel.AddToClassList(messageInfo.message.role);
            Add(contentLabel);

            UpdateToolCalls();
        }

        private void CopyMessageToClipboard()
        {
            GUIUtility.systemCopyBuffer = messageInfo.message.content;
            
            // Temporarily change button text to checkmark
            var copyButton = this.Q<Button>("copy-button");
            if (copyButton != null)
            {
                copyButton.text = "✓";
                copyButton.style.color = new StyleColor(Color.green);
                
                // Schedule to revert back to "Copy" after a short delay
                this.schedule.Execute(() => 
                {
                    if (copyButton != null)
                    {
                        copyButton.text = "Copy";
                        copyButton.style.color = new StyleColor(Color.white);
                    }
                }).StartingIn(1000); // 1 second delay
            }
        }

        private VisualElement CreateHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("message-header");

            // 发送者标签
            var senderLabel = new Label(GetMessagePrefix(messageInfo.message.role));
            senderLabel.AddToClassList("message-sender");
            senderLabel.AddToClassList(messageInfo.message.role);
            header.Add(senderLabel);

            // 时间标签
            var messageDate = DateTimeOffset.FromUnixTimeSeconds(messageInfo.timestamp).LocalDateTime;
            var timeLabel = new Label(messageDate.Date == DateTime.Now.Date ?
                messageDate.ToString("HH:mm:ss") :
                messageDate.ToString("yyyy-MM-dd HH:mm:ss"));
            timeLabel.AddToClassList("message-time");
            header.Add(timeLabel);

            // 操作按钮容器
            var actions = new VisualElement();
            actions.AddToClassList("message-actions");

            // 复制按钮
            var copyButton = new Button(CopyMessageToClipboard) { text = "Copy" };
            copyButton.style.width = new Length(45, LengthUnit.Pixel);
            copyButton.AddToClassList("message-action-button");
            copyButton.name = "copy-button"; // Add a name for easy querying
            actions.Add(copyButton);

            // 删除按钮
            if (onDelete != null)
            {
                var deleteButton = new Button(() => onDelete(messageInfo)) { text = "×" };
                deleteButton.AddToClassList("message-action-button");
                actions.Add(deleteButton);
            }

            header.Add(actions);
            return header;
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

        public void UpdateMessage()
        {
            contentLabel.text = this.messageInfo.message.content;
            UpdateToolCalls();
        }


        void UpdateToolCalls()
        {
            // 如果是助手消息且有工具调用，添加工具调用
            if (messageInfo.message.role == "assistant" && messageInfo.message.tool_calls != null)
            {
                if (toolCallContainer == null)
                {
                    toolCallContainer = new VisualElement();
                    toolCallContainer.AddToClassList("tool-calls-container");
                    Add(toolCallContainer);
                    toolCallElements = new();
                }

                toolCallContainer.Clear();
                foreach (var toolCall in messageInfo.message.tool_calls)
                {
                    var toolResult = toolResults.FirstOrDefault(r => r.tool_call_id == toolCall.id);
                    var toolCallElement = toolCallElements.Find(x => x.ToolCallId == toolCall.id);
                    if (toolCallElement == null)
                    {
                        toolCallElement=new ToolCallElement(toolCall, toolResult);
                        toolCallElements.Add(toolCallElement);
                    }
                    else
                    {
                        toolCallElement.UpdateToolCall(toolCall, toolResult);
                    }
                    toolCallElement.SetCollapse(false);
                    toolCallContainer.Add(toolCallElement);
                }
            }
        }
    }
}
