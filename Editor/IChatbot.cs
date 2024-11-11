using System.Threading.Tasks;
using System.Collections.Generic;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor
{
    public interface IChatbot
    {
        string Id { get; }
        string Name { get; }
        Task<ChatMessage> SendMessageAsync(string message);
        IReadOnlyList<ChatMessage> GetChatHistory();
        void ClearHistory();
    }
}
