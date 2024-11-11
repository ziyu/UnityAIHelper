using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityLLMAPI.Services;
using UnityLLMAPI.Config;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor
{
    public abstract class ChatbotBase : IChatbot
    {
        protected readonly ChatbotService chatbotService;
        protected readonly ChatHistoryStorage historyStorage;
        protected readonly ToolSet toolSet;

        public abstract string Id { get; }
        public abstract string Name { get; }

        protected ChatbotBase(string systemPrompt, bool useStreaming = false, Action<ChatMessage, bool> streamingCallback = null, bool useHistoryStorage = true)
        {
            // 1. 创建历史记录存储（如果需要）
            if (useHistoryStorage)
            {
                historyStorage = new ChatHistoryStorage(Id);
            }

            // 2. 获取OpenAI配置
            var openAIConfig = OpenAIConfig.Instance;
            if (openAIConfig == null)
            {
                throw new Exception("请在Resources文件夹中创建OpenAIConfig配置文件");
            }

            // 3. 创建OpenAI服务
            var openAIService = new OpenAIService(openAIConfig);

            // 4. 创建ToolSet
            toolSet = new ToolSet();

            // 5. 配置ChatBot
            var chatbotConfig = new ChatbotConfig
            {
                systemPrompt = systemPrompt,
                useStreaming = useStreaming,
                defaultModel = openAIConfig.defaultModel,
                toolSet = toolSet
            };

            if (useStreaming && streamingCallback != null)
            {
                chatbotConfig.onStreamingChunk = streamingCallback;
            }

            // 6. 创建ChatBot服务
            chatbotService = new ChatbotService(openAIService, chatbotConfig);

            // 7. 加载历史聊天记录（如果需要）
            if (useHistoryStorage)
            {
                LoadChatHistory();
            }
        }

        public virtual async Task<ChatMessage> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await chatbotService.SendMessage(message, cancellationToken: cancellationToken);
                // 如果请求被取消，不保存聊天记录
                if (!cancellationToken.IsCancellationRequested && historyStorage != null)
                {
                    SaveChatHistory();
                }
                return response;
            }
            catch (OperationCanceledException)
            {
                // 重新抛出取消异常，让调用者处理
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in {GetType().Name}: {ex}");
                throw;
            }
        }

        public virtual IReadOnlyList<ChatMessage> GetChatHistory()
        {
            return chatbotService.Messages;
        }

        public virtual void ClearHistory()
        {
            chatbotService.ClearHistory();
            historyStorage?.ClearHistory();
        }

        protected virtual void LoadChatHistory()
        {
            if (historyStorage == null) return;

            var history = historyStorage.LoadHistory();
            if (history.Count > 0)
            {
                IList<ChatMessage> chatMessages = (IList<ChatMessage>)chatbotService.Messages;
                chatMessages.Clear();
                foreach (var message in history)
                {
                    chatMessages.Add(message);
                }
            }
            else
            {
                chatbotService.ClearHistory(true);
            }
        }

        protected virtual void SaveChatHistory()
        {
            historyStorage?.SaveHistory(chatbotService.Messages);
        }

        protected void RegisterTool(Tool tool, Func<ToolCall, Task<string>> handler)
        {
            toolSet.RegisterTool(tool, handler);
        }
    }
}
