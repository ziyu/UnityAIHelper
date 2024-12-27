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
    /// 会话信息，用于会话列表展示
    /// </summary>
    public class ChatSessionInfo
    {
        public string SessionId { get; set; }
        public string Title { get; set; }
        public long LastMessageTime { get; set; }
    }

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
        private const string CURRENT_SESSION_FILE = "current_session";
        
        // 缓存
        private Dictionary<string, (ChatSession session, string json)> sessionCache 
            = new Dictionary<string, (ChatSession session, string json)>();
        private Dictionary<string, string> sessionListCache = null;
        private DateTime lastSessionListCheck = DateTime.MinValue;
        private const int SESSION_LIST_CACHE_SECONDS = 5; // 会话列表缓存5秒

        public ChatSessionStorage(string chatbotId)
        {
            this.chatbotId = chatbotId;
            this.storageDir = Path.Combine(Application.dataPath, "..", BASE_DIR, chatbotId);
            Directory.CreateDirectory(storageDir);
        }

        private string GetSessionPath(string sessionId)
        {
            return Path.Combine(storageDir, $"{sessionId}{FILE_EXTENSION}");
        }

        private string GetCurrentSessionPath()
        {
            return Path.Combine(storageDir, CURRENT_SESSION_FILE);
        }

        private bool IsSessionListCacheValid()
        {
            if (sessionListCache == null) return false;
            return (DateTime.Now - lastSessionListCheck).TotalSeconds < SESSION_LIST_CACHE_SECONDS;
        }

        /// <summary>
        /// 获取所有会话列表，按最后一条消息的时间排序
        /// </summary>
        public List<ChatSessionInfo> GetSessionList()
        {
            var sessions = new List<ChatSessionInfo>();
            if (Directory.Exists(storageDir))
            {
                foreach (var file in Directory.GetFiles(storageDir, $"*{FILE_EXTENSION}"))
                {
                    try
                    {
                        var sessionId = Path.GetFileNameWithoutExtension(file);
                        ChatSession session;
                        
                        // 尝试从缓存获取会话信息
                        if (sessionCache.TryGetValue(sessionId, out var cached))
                        {
                            session = cached.session;
                        }
                        else
                        {
                            var json = File.ReadAllText(file);
                            session = ChatSession.FromJson(json);
                            
                            // 更新缓存
                            sessionCache[sessionId] = (session, json);
                        }
                        
                        // 创建新的会话信息对象
                        var sessionInfo = new ChatSessionInfo
                        {
                            SessionId = sessionId,
                            Title = session.title,
                            LastMessageTime = session.messages?.LastOrDefault()?.timestamp ?? 0 
                        };
                        
                        sessions.Add(sessionInfo);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"加载会话失败 {file}: {e.Message}");
                    }
                }
            }
            
            // 按最后一条消息的时间排序
            sessions.Sort((a, b) => b.LastMessageTime.CompareTo(a.LastMessageTime));
            
            return sessions;
        }

        /// <summary>
        /// 创建新会话，返回 (sessionId, title)
        /// </summary>
        public (string, string) CreateSession(string sessionName)
        {
            var session = new ChatSession
            {
                title = sessionName,
                metadata = new Dictionary<string, string>
                {
                    { "chatbot_id", chatbotId }
                }
            };
            
            SaveSession(session);
            return (session.sessionId, session.title);
        }

        /// <summary>
        /// 保存会话
        /// </summary>
        public void SaveSession(ChatSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            
            try
            {
                var sessionId = session.sessionId;
                var json = session.ToJson();
                
                // 检查是否需要写入文件
                if (sessionCache.TryGetValue(sessionId, out var cached) && cached.json == json)
                {
                    return; // 内容没有变化，不需要写入
                }
                
                lock (fileLock)
                {
                    File.WriteAllText(GetSessionPath(sessionId), json);
                }
                
                // 更新缓存
                sessionCache[sessionId] = (session, json);
                
                // 如果标题变了，清除会话列表缓存
                if (!sessionCache.TryGetValue(sessionId, out var oldCached) || 
                    oldCached.session.title != session.title)
                {
                    sessionListCache = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"保存会话失败 (Chatbot: {chatbotId}): {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 加载会话，如果会话不存在则返回 null
        /// </summary>
        public ChatSession LoadSession(string sessionId)
        {
            try
            {
                // 检查缓存
                if (sessionCache.TryGetValue(sessionId, out var cached))
                {
                    return cached.session;
                }
                
                var path = GetSessionPath(sessionId);
                if (!File.Exists(path))
                {
                    return null;
                }

                string json;
                lock (fileLock)
                {
                    json = File.ReadAllText(path);
                }

                var session = ChatSession.FromJson(json);
                
                // 更新缓存
                sessionCache[sessionId] = (session, json);
                
                return session;
            }
            catch (Exception e)
            {
                Debug.LogError($"加载会话失败 (Chatbot: {chatbotId}, Session: {sessionId}): {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 删除会话
        /// </summary>
        public void DeleteSession(string sessionId)
        {
            // 不能删除最后一个会话
            var sessions = GetSessionList();
            if (sessions.Count <= 1)
            {
                throw new InvalidOperationException("Cannot delete the last session");
            }
            
            var sessionPath = GetSessionPath(sessionId);
            if (File.Exists(sessionPath))
            {
                try
                {
                    File.Delete(sessionPath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"删除会话失败 (Chatbot: {chatbotId}, Session: {sessionId}): {e.Message}");
                    throw;
                }
            }
            sessionCache.Remove(sessionId);
            sessionListCache = null; // 清除缓存
        }

        /// <summary>
        /// 重命名会话，返回新的title
        /// </summary>
        public string RenameSession(string sessionId, string newName)
        {
            try
            {
                var session = LoadSession(sessionId);
                if (session == null)
                {
                    throw new ArgumentException($"Session {sessionId} does not exist");
                }

                session.title = newName;
                SaveSession( session);
                return newName;
            }
            catch (Exception e)
            {
                Debug.LogError($"重命名会话失败 (Chatbot: {chatbotId}, Session: {sessionId}): {e.Message}");
                throw;
            }
        }

        public void SaveCurrentSessionId(string sessionId)
        {
            var path = GetCurrentSessionPath();
            File.WriteAllText(path, sessionId);
        }

        public string LoadCurrentSessionId()
        {
            var path = GetCurrentSessionPath();
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            return null;
        }
    }
}
