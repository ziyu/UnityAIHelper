using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityLLMAPI.Services;
using UnityLLMAPI.Config;
using UnityLLMAPI.Models;
using UnityAIHelper.Editor.Tools;

namespace UnityAIHelper.Editor
{
    public abstract class ChatbotBase : IChatbot
    {
        protected readonly ChatbotService chatbotService;
        protected readonly ChatHistoryStorage historyStorage;
        protected readonly ToolRegistry toolRegistry;

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

            // 4. 初始化工具系统
            toolRegistry = ToolRegistry.Instance;

            // 5. 注册工具
            RegisterTools();

            // 6. 配置ChatBot
            var chatbotConfig = new ChatbotConfig
            {
                systemPrompt = systemPrompt,
                useStreaming = useStreaming,
                defaultModel = openAIConfig.defaultModel,
                toolSet = toolRegistry.LLMToolSet // 使用工具注册表中的LLM工具集
            };

            if (useStreaming && streamingCallback != null)
            {
                chatbotConfig.onStreamingChunk = streamingCallback;
            }

            // 7. 创建ChatBot服务
            chatbotService = new ChatbotService(openAIService, chatbotConfig);

            // 8. 加载历史聊天记录（如果需要）
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

        /// <summary>
        /// 注册工具
        /// </summary>
        protected virtual void RegisterTools()
        {
        }

        /// <summary>
        /// 注册工具实例
        /// </summary>
        protected void RegisterTool<T>() where T : IUnityTool, new()
        {
            var tool = new T();
            toolRegistry.RegisterTool(tool);
        }

        /// <summary>
        /// 创建临时工具
        /// </summary>
        protected async Task<IUnityTool> CreateTemporaryToolAsync(string name, string scriptContent)
        {
            return await toolRegistry.CreateTemporaryToolAsync(name, scriptContent);
        }

        /// <summary>
        /// 获取工具
        /// </summary>
        protected IUnityTool GetTool(string name)
        {
            return toolRegistry.GetTool(name);
        }

        /// <summary>
        /// 获取指定类型的所有工具
        /// </summary>
        protected IEnumerable<IUnityTool> GetToolsByType(ToolType type)
        {
            return toolRegistry.GetToolsByType(type);
        }
    }
}
