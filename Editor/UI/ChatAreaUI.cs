using UnityEngine;
using UnityEngine.UIElements;
using UnityLLMAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnityAIHelper.Editor.UI
{
    /// <summary>
    /// 聊天区域UI组件
    /// </summary>
    public class ChatAreaUI:UIComponentBase
    {
        private readonly AIHelperWindow window;
        private VisualElement root;
        private ScrollView scrollView;
        private bool shouldScrollToBottom = false;

        // 缓存过滤后的消息列表
        private List<ChatMessageInfo> filteredMessages;
        private List<ChatMessage> toolResults;
        private int lastMessageCount;
        private ChatMessage lastStreamingMessage;
        private ChatMessageElement _streamingElement;

        // 消息操作回调
        public Action<ChatMessageInfo> OnDeleteMessage;

        public ChatAreaUI(AIHelperWindow window, VisualElement root)
        {
            this.window = window;
            this.root = root;
            filteredMessages = new List<ChatMessageInfo>();
            toolResults = new List<ChatMessage>();
            Initialize();
        }

        private void Initialize()
        {
            // 加载USS
            var styleSheet = PackageAssetLoader.LoadUIAsset<StyleSheet>("ChatAreaUI.uss");
            root.styleSheets.Add(styleSheet);

            // 获取UI元素引用
            scrollView = root.Q<ScrollView>("chat-scroll");
            shouldScrollToBottom = true;
        }

        /// <summary>
        /// 更新聊天内容
        /// </summary>
        void UpdateMessageList(IReadOnlyList<ChatMessageInfo> chatHistory)
        {
            // 检查并更新过滤后的消息列表
            if (chatHistory.Count == lastMessageCount) return;
            // 更新缓存状态
            lastMessageCount = chatHistory.Count;

            // 清空并重新填充列表
            filteredMessages.Clear();
            toolResults.Clear();

            // 分类消息
            foreach (var messageInfo in chatHistory)
            {
                if (messageInfo.message.role == "tool")
                {
                    toolResults.Add(messageInfo.message);
                }
                else if (messageInfo.message.role != "system")
                {
                    filteredMessages.Add(messageInfo);
                }
            }

            // 清空现有内容
            scrollView.Clear();

            // 添加消息
            foreach (var messageInfo in filteredMessages)
            {
                var messageElement = new ChatMessageElement(
                    messageInfo,
                    OnDeleteMessage,
                    toolResults
                );
                scrollView.Add(messageElement);
            }


            // 滚动到底部
            shouldScrollToBottom = true;

        }

        public override void OnUpdateUI()
        {
            if(!window.IsDirty(AIHelperDirtyFlag.Message))return;
            
            var chatHistory = window.currentChatbot.GetChatHistory();
            UpdateMessageList(chatHistory);
            
            // 添加正在流式传输的消息
            if (window.isStreaming &&  window.currentStreamingMessage != null)
            {
                if (_streamingElement == null|| window.currentStreamingMessage!=lastStreamingMessage)
                {
                    _streamingElement = new ChatMessageElement(
                        new ChatMessageInfo(window.currentStreamingMessage)
                    );
                    scrollView.Add(_streamingElement);
                    lastStreamingMessage = window.currentStreamingMessage;
                }
                else
                {
                    _streamingElement.SetContent( window.currentStreamingMessage.content);
                }

                shouldScrollToBottom = true;
            }
            else
            {
                if (_streamingElement != null)
                {
                    if(scrollView.Contains(_streamingElement))
                        scrollView.Remove(_streamingElement);
                    _streamingElement = null;
                }
            }
            
            
            
            // 处理滚动
            if (shouldScrollToBottom)
            {
                scrollView.schedule.Execute(() =>
                {
                    shouldScrollToBottom = false;
                    scrollView.scrollOffset = new Vector2(0, scrollView.contentContainer.worldBound.height);
                });
            }
        }
    }
}
