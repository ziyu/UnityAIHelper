using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityLLMAPI.Services;
using UnityLLMAPI.Config;
using UnityLLMAPI.Models;
using UnityAIHelper.Editor.Tools;
using UnityLLMAPI.Interfaces;

namespace UnityAIHelper.Editor
{
    public abstract class ChatbotBase : IChatbot
    {
        protected readonly ChatbotService chatbotService;
        protected readonly ChatSessionStorage sessionStorage;
        protected readonly ToolRegistry toolRegistry;
        protected readonly ToolExecutor toolExecutor;

        protected string name;
        protected string description;
        protected string systemPrompt;

        public abstract string Id { get; }
        public virtual string Name => name;
        public virtual string Description => description;
        public virtual string SystemPrompt => systemPrompt;
        
        public event Action<ChatMessage> OnStreamingMessage;

        protected ChatbotBase(string systemPrompt, bool useStreaming = false, Action<ChatMessage> streamingCallback = null, bool useSessionStorage = true)
        {
            this.systemPrompt = systemPrompt;

            // 1. 创建会话存储（如果需要）
            if (useSessionStorage)
            {
                sessionStorage = new ChatSessionStorage(Id);
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
            toolRegistry = new ToolRegistry();
            toolExecutor = new ToolExecutor(toolRegistry);
            
            // 5. 注册工具
            RegisterTools();

            // 6. 配置ChatBot
            var chatbotConfig = new ChatbotConfig
            {
                systemPrompt = GetSystemPrompt(systemPrompt),
                useStreaming = useStreaming,
                defaultModel = openAIConfig.defaultModel,
                toolSet = toolRegistry.LLMToolSet // 使用工具注册表中的LLM工具集
            };

            if (useStreaming)
            {
                chatbotConfig.onStreamingChunk = OnStreamingCallBack;
                if (streamingCallback != null)
                {
                    OnStreamingMessage += streamingCallback;
                }
            }

            // 7. 加载会话（如果需要）
            ChatSession session = null;
            if (useSessionStorage)
            {
                session = LoadSession();
            }
            
            //8. 新建ChatbotService
            chatbotService = session == null ? new ChatbotService(openAIService, chatbotConfig) : new ChatbotService(openAIService, chatbotConfig, session);
            chatbotService.StateChanged += OnChatStateChanged;
        }

        /// <summary>
        /// 获取系统提示词
        /// </summary>
        protected virtual string GetSystemPrompt(string basePrompt)
        {
            return basePrompt;
        }

        void OnStreamingCallBack(ChatMessage chatMessage)
        {
            OnStreamingMessage?.Invoke(chatMessage);
        }

        void OnChatStateChanged(object sender, ChatStateChangedEventArgs e)
        {
            SaveSession();
        }

        public virtual async Task<ChatMessage> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            var response = await chatbotService.SendMessage(message, new ChatParams()
            {
                CancellationToken = cancellationToken
            });
            SaveSession();
            return response;
        }

        /// <summary>
        /// 继续之前的对话
        /// </summary>
        public virtual async Task<ChatMessage> ContinueMessageAsync(CancellationToken cancellationToken = default)
        {
            if (!chatbotService.IsInterrupted)
            {
                throw new InvalidOperationException("没有需要继续的对话");
            }

            var response = await chatbotService.ResumeSession(new ChatParams()
            {
                CancellationToken = cancellationToken
            });
            SaveSession();
            return response;
        }

        public virtual IReadOnlyList<ChatMessage> GetChatHistory()
        {
            return chatbotService.Messages;
        }

        public virtual void ClearHistory()
        {
            chatbotService.ClearHistory();
        }

        /// <summary>
        /// 重新加载会话
        /// </summary>
        public void ReloadSession()
        {
            var session = LoadSession();
            this.chatbotService.SetSession(session);
        }
        
        /// <summary>
        /// 加载会话
        /// </summary>
        protected virtual ChatSession LoadSession()
        {
            if (sessionStorage == null) return null;

            try 
            {
                return sessionStorage.LoadSession();
            }
            catch (Exception ex)
            {
                Debug.LogError($"加载会话失败: {ex.Message}");
            }
            return null;
        }

        public virtual void SaveSession()
        {
            if (sessionStorage == null) return;

            try
            {
                var session = chatbotService.Session;
                sessionStorage.SaveSession(session);
            }
            catch (Exception ex)
            {
                Debug.LogError($"保存会话失败: {ex.Message}");
            }
        }
        
        public void ClearPendingState()
        {
            toolExecutor?.ClearContext();
            chatbotService.ClearPending();
        }

        /// <summary>
        /// 注册工具
        /// </summary>
        protected virtual void RegisterTools()
        {
            toolRegistry.RegisterBuiltInTools(toolExecutor);
        }

        /// <summary>
        /// 注册工具实例
        /// </summary>
        protected void RegisterTool<T>() where T : IUnityTool, new()
        {
            var tool = new T();
            toolRegistry.RegisterTool(tool, toolExecutor);
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

        /// <summary>
        /// 检查是否有未完成的对话
        /// </summary>
        public bool HasPendingMessage => chatbotService.IsInterrupted;

        /// <summary>
        /// 更新设置
        /// </summary>
        public virtual void UpdateSettings(string name, string description, string systemPrompt)
        {
            this.name = name;
            this.description = description;
            this.systemPrompt = systemPrompt;
            
            // 清空历史记录并重新初始化会话
            ClearHistory();
        }
    }
}
