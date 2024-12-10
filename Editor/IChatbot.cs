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
        
        event Action<ChatMessage> OnStreamingMessage;
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
        void ClearPendingState();
        
        void SaveSession();

        void UpdateSettings(string name, string description, string systemPrompt);
    }
}
