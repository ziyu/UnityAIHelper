using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor
{
    public class ChatHistoryStorage
    {
        private const string BASE_DIR = "Library/AIHelper/ChatHistory";
        private const string FILE_EXTENSION = ".chat";
        private static readonly object fileLock = new object();

        private readonly string chatbotId;
        private readonly string storageDir;
        private readonly string historyPath;

        public ChatHistoryStorage(string chatbotId)
        {
            this.chatbotId = chatbotId;
            this.storageDir = Path.Combine(Application.dataPath, "..", BASE_DIR, chatbotId);
            this.historyPath = Path.Combine(storageDir, $"history{FILE_EXTENSION}");
            
            Directory.CreateDirectory(storageDir);
        }

        [Serializable]
        private class ChatHistory
        {
            public string chatbot_id;
            public List<ChatMessage> messages = new List<ChatMessage>();
            public long last_modified;
        }

        public void SaveHistory(IEnumerable<ChatMessage> messages)
        {
            try
            {
                var history = new ChatHistory
                {
                    chatbot_id = chatbotId,
                    messages = new List<ChatMessage>(messages),
                    last_modified = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                var json = JsonUtility.ToJson(history, true);

                lock (fileLock)
                {
                    File.WriteAllText(historyPath, json);
                }

                Debug.Log($"已保存聊天记录到: {historyPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"保存聊天记录失败 (Chatbot: {chatbotId}): {e.Message}");
            }
        }

        public List<ChatMessage> LoadHistory()
        {
            try
            {
                if (!File.Exists(historyPath))
                {
                    return new List<ChatMessage>();
                }

                string json;
                lock (fileLock)
                {
                    json = File.ReadAllText(historyPath);
                }

                var history = JsonUtility.FromJson<ChatHistory>(json);
                
                // 验证历史记录属于正确的chatbot
                if (history.chatbot_id != chatbotId)
                {
                    Debug.LogError($"聊天记录ID不匹配 (Expected: {chatbotId}, Found: {history.chatbot_id})");
                    return new List<ChatMessage>();
                }

                return history.messages ?? new List<ChatMessage>();
            }
            catch (Exception e)
            {
                Debug.LogError($"加载聊天记录失败 (Chatbot: {chatbotId}): {e.Message}");
                return new List<ChatMessage>();
            }
        }

        public void ClearHistory()
        {
            try
            {
                if (File.Exists(historyPath))
                {
                    lock (fileLock)
                    {
                        File.Delete(historyPath);
                    }
                    Debug.Log($"已清除聊天记录: {historyPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"清除聊天记录失败 (Chatbot: {chatbotId}): {e.Message}");
            }
        }
    }
}
