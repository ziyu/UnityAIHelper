using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
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
        public ChatSession Session => chatbotService.Session;
        
        public event Action<ChatMessage> OnStreamingMessage;

        protected ChatbotBase(string systemPrompt, bool useTools = true, bool useStreaming = false, Action<ChatMessage> streamingCallback = null, bool useSessionStorage = true)
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

            if (useTools)
            {
                // 4. 初始化工具系统
                toolRegistry = new ToolRegistry();
                toolExecutor = new ToolExecutor(toolRegistry);
                
                // 5. 注册工具
                RegisterTools();
            }
            
            // 6. 配置ChatBot
            var chatbotConfig = new ChatbotConfig
            {
                systemPrompt = systemPrompt,
                useStreaming = useStreaming,
                defaultModel = openAIConfig.defaultModel,
                toolSet = toolRegistry?.LLMToolSet // 使用工具注册表中的LLM工具集
            };

            if (useStreaming)
            {
                chatbotConfig.onStreamingChunk = OnStreamingCallBack;
                if (streamingCallback != null)
                {
                    OnStreamingMessage += streamingCallback;
                }
            }

            // 7. 初始化会话
            if (useSessionStorage)
            {
                var sessions = sessionStorage.GetSessionList();
                var currentSessionId = sessionStorage.LoadCurrentSessionId();
                if (currentSessionId != null && sessions.Any(s => s.SessionId == currentSessionId))
                {
                    var session = sessionStorage.LoadSession(currentSessionId);
                    chatbotService = new ChatbotService(openAIService, chatbotConfig, session);
                }
                else if(sessions.Count>0)
                {
                    var firstSessionId = sessions.First().SessionId; // 使用第一个会话作为当前会话
                    var session = sessionStorage.LoadSession(firstSessionId);
                    chatbotService = new ChatbotService(openAIService, chatbotConfig, session);
                    sessionStorage.SaveCurrentSessionId(firstSessionId);
                }
                else
                {
                    chatbotService = new ChatbotService(openAIService, chatbotConfig);
                }
            }
            else
            {
                chatbotService = new ChatbotService(openAIService, chatbotConfig);
            }
            
            chatbotService.StateChanged += OnChatStateChanged;
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

        public virtual IReadOnlyList<ChatMessageInfo> GetChatHistory()
        {
            return chatbotService.GetAllMessageInfos();
        }

        public virtual void ClearHistory()
        {
            chatbotService.ClearHistory();
            SaveSession();
        }

        /// <summary>
        /// 更新消息内容
        /// </summary>
        public virtual void UpdateMessage(ChatMessageInfo messageInfo, string newContent)
        {
            if (messageInfo == null)
                throw new ArgumentNullException(nameof(messageInfo));

            if (string.IsNullOrEmpty(newContent))
                throw new ArgumentException("消息内容不能为空", nameof(newContent));

            // 只允许编辑用户消息
            if (messageInfo.message.role != "user")
                throw new InvalidOperationException("只能编辑用户消息");

            var messages = chatbotService.Session.messages;
            var index = messages.FindIndex(m => m.messageId == messageInfo.messageId);
            
            if (index == -1)
                throw new InvalidOperationException("未找到指定消息");

            // 更新消息内容
            messageInfo.message.content = newContent;
            messageInfo.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            chatbotService.ClearPending();
            // 移除该消息之后的所有消息
            if (index < messages.Count - 1)
            {
                messages.RemoveRange(index + 1, messages.Count - index - 1);
            }
            
            SaveSession();
        }

        /// <summary>
        /// 删除消息
        /// </summary>
        public virtual void DeleteMessage(ChatMessageInfo messageInfo)
        {
            if (messageInfo == null)
                throw new ArgumentNullException(nameof(messageInfo));

            chatbotService.DeleteMessage(messageInfo.messageId);
            SaveSession();
        }

        public List<ChatSessionInfo> GetSessionList()
        {
            return sessionStorage?.GetSessionList() ?? new List<ChatSessionInfo>();
        }

        /// <summary>
        /// 切换到指定会话
        /// </summary>
        public virtual void SwitchSession(string sessionId)
        {
            if (sessionStorage == null)
                throw new InvalidOperationException("会话存储未启用");

            // 保存当前会话
            SaveSession();
            
            Debug.Log("SwitchSession:" + sessionId);
            // 加载新会话
            var session = sessionStorage.LoadSession(sessionId);
            if (session == null)
                throw new ArgumentException($"会话 {sessionId} 不存在");

            chatbotService.SetSession(session);
            sessionStorage.SaveCurrentSessionId(sessionId);
        }

        /// <summary>
        /// 创建新会话
        /// </summary>
        public virtual (string sessionId, string title) CreateSession(string sessionName = null)
        {
            if (sessionStorage == null)
                throw new InvalidOperationException("会话存储未启用");

            // 保存当前会话
            SaveSession();

            // 创建新会话
            var result = sessionStorage.CreateSession(sessionName);
            
            // 切换到新会话
            var session = sessionStorage.LoadSession(result.Item1);
            chatbotService.SetSession(session);
            sessionStorage.SaveCurrentSessionId(result.Item1);

            return result;
        }

        /// <summary>
        /// 删除当前会话
        /// </summary>
        public virtual void DeleteSession(string sessionId)
        {
            if (sessionStorage == null)
                throw new InvalidOperationException("会话存储未启用");

            var isCurrent = sessionId == Session.sessionId;
            // 删除会话
            sessionStorage.DeleteSession(sessionId);

            if (isCurrent)
            {
                //如果是当前session, 切换到第一个可用会话
                var sessions = sessionStorage.GetSessionList();
                var firstSessionId = sessions.First().SessionId;
                chatbotService.SetSession(sessionStorage.LoadSession(firstSessionId));
                sessionStorage.SaveCurrentSessionId(firstSessionId);
            }

        }

        /// <summary>
        /// 重命名当前会话
        /// </summary>
        public virtual string RenameSession(string sessionId,string newName)
        {
            if (sessionStorage == null)
                throw new InvalidOperationException("会话存储未启用");

            return sessionStorage.RenameSession(sessionId, newName);
        }

        /// <summary>
        /// 重新加载当前会话
        /// </summary>
        public virtual void ReloadSession()
        {
            if (sessionStorage == null) return;

            var session = sessionStorage.LoadSession(chatbotService.Session.sessionId);
            chatbotService.SetSession(session);
        }

        /// <summary>
        /// 保存当前会话
        /// </summary>
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
            if(toolRegistry==null)return;
            var tool = new T();
            toolRegistry.RegisterTool(tool, toolExecutor);
        }

        /// <summary>
        /// 获取工具
        /// </summary>
        protected IUnityTool GetTool(string name)
        {
            return toolRegistry?.GetTool(name);
        }

        /// <summary>
        /// 获取指定类型的所有工具
        /// </summary>
        protected IEnumerable<IUnityTool> GetToolsByType(ToolType type)
        {
            return toolRegistry?.GetToolsByType(type);
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
