using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityAIHelper.Editor
{
    public class ChatbotStorage
    {
        private const string BASE_DIR = "Library/AIHelper/Chatbots";
        private const string FILE_EXTENSION = ".chatbot";
        private const string CURRENT_CHATBOT_FILE = "current_chatbot";
        private static readonly object fileLock = new object();

        private readonly string storageDir;
        
        // Cache
        private Dictionary<string, (ChatbotData data, string json)> dataCache 
            = new Dictionary<string, (ChatbotData data, string json)>();
        private Dictionary<string, string> chatbotListCache = null;
        private DateTime lastChatbotListCheck = DateTime.MinValue;
        private const int CHATBOT_LIST_CACHE_SECONDS = 5;

        public ChatbotStorage()
        {
            this.storageDir = Path.Combine(Application.dataPath, "..", BASE_DIR);
            Directory.CreateDirectory(storageDir);
        }

        private string GetChatbotPath(string chatbotId)
        {
            return Path.Combine(storageDir, $"{chatbotId}{FILE_EXTENSION}");
        }

        private string GetCurrentChatbotPath()
        {
            return Path.Combine(storageDir, CURRENT_CHATBOT_FILE);
        }

        private bool IsChatbotListCacheValid()
        {
            if (chatbotListCache == null) return false;
            return (DateTime.Now - lastChatbotListCheck).TotalSeconds < CHATBOT_LIST_CACHE_SECONDS;
        }

        public List<string> GetAllChatbotIds()
        {
            var chatbotIds = new List<string>();
            if (Directory.Exists(storageDir))
            {
                foreach (var file in Directory.GetFiles(storageDir, $"*{FILE_EXTENSION}"))
                {
                    try
                    {
                        var chatbotId = Path.GetFileNameWithoutExtension(file);
                        chatbotIds.Add(chatbotId);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"加载Chatbot失败 {file}: {e.Message}");
                    }
                }
            }
            return chatbotIds;
        }

        public void SaveChatbot(IChatbot chatbot)
        {
            if (chatbot == null) throw new ArgumentNullException(nameof(chatbot));

            try
            {
                var chatbotId = chatbot.Id;
                var data = ConvertToData(chatbot);
                var json = JsonUtility.ToJson(data, true);
                
                // Check if we need to write to file
                if (dataCache.TryGetValue(chatbotId, out var cached) && cached.json == json)
                {
                    return; // No changes, skip writing
                }
                
                lock (fileLock)
                {
                    File.WriteAllText(GetChatbotPath(chatbotId), json);
                }
                
                // Update cache
                dataCache[chatbotId] = (data, json);
                chatbotListCache = null; // Invalidate list cache
            }
            catch (Exception e)
            {
                Debug.LogError($"保存Chatbot失败: {e.Message}");
                throw;
            }
        }

        public IChatbot LoadChatbot(string chatbotId)
        {
            try
            {
                // Check cache
                if (dataCache.TryGetValue(chatbotId, out var cached))
                {
                    return ConvertFromData(cached.data);
                }
                
                var path = GetChatbotPath(chatbotId);
                if (!File.Exists(path))
                {
                    return null;
                }

                string json;
                lock (fileLock)
                {
                    json = File.ReadAllText(path);
                }

                var data = JsonUtility.FromJson<ChatbotData>(json);
                
                // Update cache
                dataCache[chatbotId] = (data, json);
                
                return ConvertFromData(data);
            }
            catch (Exception e)
            {
                Debug.LogError($"加载Chatbot失败 (Chatbot: {chatbotId}): {e.Message}");
                throw;
            }
        }

        public void DeleteChatbot(string chatbotId)
        {
            var chatbotPath = GetChatbotPath(chatbotId);
            if (File.Exists(chatbotPath))
            {
                try
                {
                    File.Delete(chatbotPath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"删除Chatbot失败 (Chatbot: {chatbotId}): {e.Message}");
                    throw;
                }
            }
            dataCache.Remove(chatbotId);
            chatbotListCache = null; // Invalidate list cache
        }

        private ChatbotData ConvertToData(IChatbot chatbot)
        {
            return new ChatbotData
            {
                Id = chatbot.Id,
                Name = chatbot.Name,
                Description = chatbot.Description,
                SystemPrompt = chatbot.SystemPrompt
            };
        }

        private IChatbot ConvertFromData(ChatbotData data)
        {
            return new CustomChatbot(
                id: data.Id,
                name:data.Name,
                description:data.Description,
                systemPrompt: data.SystemPrompt
            );
        }

        public void SaveCurrentChatbot(string chatbotId)
        {
            try
            {
                lock (fileLock)
                {
                    File.WriteAllText(GetCurrentChatbotPath(), chatbotId);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"保存当前Chatbot失败: {e.Message}");
                throw;
            }
        }

        public string LoadCurrentChatbot()
        {
            try
            {
                var path = GetCurrentChatbotPath();
                if (!File.Exists(path))
                {
                    return null;
                }

                lock (fileLock)
                {
                    return File.ReadAllText(path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载当前Chatbot失败: {e.Message}");
                throw;
            }
        }
    }
}