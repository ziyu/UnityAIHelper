using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityLLMAPI.Models;
using UnityLLMAPI.Utils.Json;

namespace UnityAIHelper.Editor
{
    /// <summary>
    /// 管理聊天会话的持久化存储
    /// </summary>
    public class ChatSessionStorage
    {
        private const string BASE_DIR = "Library/AIHelper/Sessions";
        private const string FILE_EXTENSION = ".session";
        private static readonly object fileLock = new object();

        private readonly string chatbotId;
        private readonly string storageDir;
        private readonly string sessionPath;
        
        // 缓存当前会话
        private ChatSession currentSession;
        private string savedJson;

        public ChatSessionStorage(string chatbotId)
        {
            this.chatbotId = chatbotId;
            this.storageDir = Path.Combine(Application.dataPath, "..", BASE_DIR, chatbotId);
            this.sessionPath = Path.Combine(storageDir, $"session{FILE_EXTENSION}");
            
            Directory.CreateDirectory(storageDir);
        }

        /// <summary>
        /// 保存当前会话
        /// </summary>
        public void SaveSession(ChatSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            
            try
            {
                currentSession = session;
                var json = session.ToJson();

                if(json==savedJson)
                    return;
                lock (fileLock)
                {
                    File.WriteAllText(sessionPath, json);
                    savedJson = json;
                }

                Debug.Log($"已保存会话到: {sessionPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"保存会话失败 (Chatbot: {chatbotId}): {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 加载会话
        /// </summary>
        public ChatSession LoadSession()
        {
            if (currentSession != null)
            {
                return currentSession;
            }

            try
            {
                if (!File.Exists(sessionPath))
                {
                    currentSession = CreateNewSession();
                    return currentSession;
                }

                string json;
                lock (fileLock)
                {
                    json = File.ReadAllText(sessionPath);
                }

                // 尝试加载新格式
                try 
                {
                    currentSession = ChatSession.FromJson(json);
                    return currentSession;
                }
                catch
                {
                    // 尝试加载旧格式并转换
                    try
                    {
                        var oldHistory = JsonConverter.DeserializeObject<OldChatHistory>(json);
                        if (oldHistory.chatbot_id != chatbotId)
                        {
                            Debug.LogWarning($"聊天记录ID不匹配 (Expected: {chatbotId}, Found: {oldHistory.chatbot_id})");
                            currentSession = CreateNewSession();
                            return currentSession;
                        }

                        currentSession = ConvertOldHistoryToSession(oldHistory);
                        // 立即保存为新格式
                        SaveSession(currentSession);
                        return currentSession;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"加载旧格式会话失败 (Chatbot: {chatbotId}): {e.Message}");
                        currentSession = CreateNewSession();
                        return currentSession;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载会话失败 (Chatbot: {chatbotId}): {e.Message}");
                currentSession = CreateNewSession();
                return currentSession;
            }
        }

        /// <summary>
        /// 清除会话
        /// </summary>
        public void ClearSession()
        {
            try
            {
                if (File.Exists(sessionPath))
                {
                    lock (fileLock)
                    {
                        File.Delete(sessionPath);
                    }
                    Debug.Log($"已清除会话: {sessionPath}");
                }
                currentSession = null;
                savedJson = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"清除会话失败 (Chatbot: {chatbotId}): {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 创建新的会话
        /// </summary>
        private ChatSession CreateNewSession()
        {
            var session = new ChatSession
            {
                title = $"Chat with {chatbotId}",
                metadata = new Dictionary<string, string>
                {
                    { "chatbot_id", chatbotId }
                }
            };
            return session;
        }

        /// <summary>
        /// 旧版本的数据结构(用于向后兼容)
        /// </summary>
        [Serializable]
        private class OldChatHistory
        {
            public string chatbot_id;
            public List<ChatMessage> messages = new List<ChatMessage>();
            public long last_modified;
        }

        /// <summary>
        /// 将旧格式转换为新的ChatSession
        /// </summary>
        private ChatSession ConvertOldHistoryToSession(OldChatHistory oldHistory)
        {
            var session = new ChatSession
            {
                title = $"Chat with {oldHistory.chatbot_id}",
                updatedAt = oldHistory.last_modified,
                metadata = new Dictionary<string, string>
                {
                    { "chatbot_id", oldHistory.chatbot_id },
                    { "converted_from", "old_format" }
                }
            };

            // 转换消息
            foreach (var msg in oldHistory.messages)
            {
                session.messages.Add(new ChatMessageInfo(msg));
            }

            return session;
        }
    }
}
