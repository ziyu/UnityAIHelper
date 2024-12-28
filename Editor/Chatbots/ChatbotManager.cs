using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor
{
    [InitializeOnLoad]
    public class ChatbotManager
    {
        private static ChatbotManager instance;
        public static ChatbotManager Instance => instance ??= new ChatbotManager();

        private Dictionary<string, IChatbot> chatbots = new Dictionary<string, IChatbot>();
        private string currentChatbotId;
        private ChatbotStorage storage;

        public event Action OnChatbotChanged;
        public event Action OnChatbotListChanged;
        
        public string CurrentChatbotId => currentChatbotId;
        public IReadOnlyDictionary<string, IChatbot> Chatbots => chatbots;

        private ChatbotManager()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (storage!=null) return;

            storage = new();
            // Load existing chatbots from storage
            var storedChatbots = storage.GetAllChatbotIds();
            foreach (var id in storedChatbots)
            {
                var chatbot = storage.LoadChatbot(id);
                if (chatbot != null)
                {
                    chatbots[id] = chatbot;
                }
            }

            // If no chatbots exist, create from defaults
            if (chatbots.Count == 0)
            {
                var unityHelper = DefaultChatbots.UnityHelper;
                chatbots[unityHelper.Id] = unityHelper;
                storage.SaveChatbot(unityHelper);
            }

            currentChatbotId = chatbots.Keys.First();
        }

        public IChatbot CreateCustomChatbot(string id, string name, string description, string systemPrompt)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Chatbot ID cannot be empty");

            if (chatbots.ContainsKey(id))
                throw new ArgumentException($"Chatbot with ID '{id}' already exists");

            var chatbot = new CustomChatbot(id, name, description, systemPrompt, useStreaming: true);
            chatbots.Add(id, chatbot);
            storage.SaveChatbot(chatbot);
            OnChatbotListChanged?.Invoke();
            return chatbot;
        }

        public void UpdateCustomChatbot(string id, string name, string description, string systemPrompt)
        {
            if (!chatbots.TryGetValue(id, out var chatbot))
                throw new ArgumentException($"Chatbot with ID '{id}' does not exist");

            if (chatbot is CustomChatbot customBot)
            {
                customBot.UpdateSettings(name, description, systemPrompt);
                storage.SaveChatbot(customBot);
            }
            else
            {
                throw new ArgumentException($"Chatbot with ID '{id}' is not a custom chatbot");
            }
        }

        public void SwitchChatbot(string id)
        {
            if (!chatbots.ContainsKey(id)) return;
            if (currentChatbotId == id) return;

            currentChatbotId = id;
            OnChatbotChanged?.Invoke();
        }

        public void RemoveChatbot(string id)
        {
            if (!chatbots.ContainsKey(id))
                throw new ArgumentException($"Chatbot with ID '{id}' does not exist");

            chatbots.Remove(id);
            storage.DeleteChatbot(id);
            if (currentChatbotId == id)
            {
                currentChatbotId = chatbots.Keys.First();
                OnChatbotChanged?.Invoke();
            }
            OnChatbotListChanged?.Invoke();
        }

        public IChatbot GetCurrentChatbot()
        {
            return chatbots[currentChatbotId];
        }

        public string GetCurrentChatbotId()
        {
            return currentChatbotId;
        }

        public Dictionary<string, IChatbot> GetAllChatbots()
        {
            return chatbots;
        }
    }

    // 自定义Chatbot实现
    public class CustomChatbot : ChatbotBase
    {
        public CustomChatbot(string id, string name, string description, string systemPrompt, bool useTools = true, bool useStreaming = true, Action<ChatMessage> streamingCallback = null, bool useSessionStorage = true) : base(id, name, description, systemPrompt, useTools, useStreaming, streamingCallback, useSessionStorage)
        {
        }
    }
}
