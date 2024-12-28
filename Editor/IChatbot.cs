using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor
{
    public interface IChatbot
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        string SystemPrompt { get; }
        ChatSession Session { get; }
        
        event Action<ChatMessage> OnStreamingMessage;
        event Func<ChatMessageInfo,ToolCall, Task<bool>> OnShouldExecuteToolEvent;
        event Action<ChatStateChangedEventArgs> OnChatStateChangedEvent;
        Task<ChatMessage> SendMessageAsync(string message, CancellationToken cancellationToken = default);
        Task<ChatMessage> ContinueMessageAsync(CancellationToken cancellationToken = default);
        
        IReadOnlyList<ChatMessageInfo> GetChatHistory();
        void ClearHistory();
        
        /// <summary>
        /// 更新消息内容
        /// </summary>
        void UpdateMessage(ChatMessageInfo messageInfo, string newContent);
        
        /// <summary>
        /// 删除消息
        /// </summary>
        void DeleteMessage(ChatMessageInfo messageInfo);
        
        bool HasPendingMessage { get; }
        
        /// <summary>
        /// 获取会话存储
        /// </summary>
        List<ChatSessionInfo> GetSessionList();
        
        /// <summary>
        /// 切换到指定会话
        /// </summary>
        void SwitchSession(string sessionId);
        
        /// <summary>
        /// 创建新会话
        /// </summary>
        (string sessionId, string title) CreateSession(string sessionName = null);
        
        /// <summary>
        /// 删除会话
        /// </summary>
        void DeleteSession(string sessionId);
        
        /// <summary>
        /// 重命名会话
        /// </summary>
        string RenameSession(string sessionId,string newName);
        
        /// <summary>
        /// 重新加载当前会话
        /// </summary>
        void ReloadSession();
        
        /// <summary>
        /// 保存当前会话
        /// </summary>
        void SaveSession();
        
        void ClearPendingState();
        
        void UpdateSettings(string name, string description, string systemPrompt);
    }
}
