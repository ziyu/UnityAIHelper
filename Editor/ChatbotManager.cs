using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityLLMAPI.Config;
using UnityLLMAPI.Models;
using UnityLLMAPI.Services;

namespace UnityAIHelper.Editor
{
    [InitializeOnLoad]
    public class ChatbotManager
    {
        private static ChatbotManager instance;
        public static ChatbotManager Instance
        {
            get
            {
                instance ??= new ChatbotManager();
                return instance;
            }
        }

        private readonly Dictionary<string, IChatbot> chatbots = new();
        private string currentChatbotId;

        public string CurrentChatbotId => currentChatbotId;
        public IReadOnlyDictionary<string, IChatbot> Chatbots => chatbots;

        private ChatbotManager()
        {
            // 创建默认的Unity助手chatbot，启用streaming
            var unityHelper = new UnityHelperChatbot(useStreaming: true);
            chatbots.Add(unityHelper.Id, unityHelper);
            currentChatbotId = unityHelper.Id;
        }

        public IChatbot CreateCustomChatbot(string id, string name, string description, string systemPrompt)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Chatbot ID cannot be empty");

            if (chatbots.ContainsKey(id))
                throw new ArgumentException($"Chatbot with ID '{id}' already exists");

            var chatbot = new CustomChatbot(id, name, description, systemPrompt, useStreaming: true);
            chatbots.Add(id, chatbot);
            return chatbot;
        }

        public void UpdateCustomChatbot(string id, string name, string description, string systemPrompt)
        {
            if (!chatbots.ContainsKey(id))
                throw new ArgumentException($"Chatbot with ID '{id}' does not exist");

            if (chatbots[id] is CustomChatbot customBot)
            {
                customBot.UpdateSettings(name, description, systemPrompt);
            }
            else
            {
                throw new ArgumentException($"Chatbot with ID '{id}' is not a custom chatbot");
            }
        }

        public void SwitchChatbot(string id)
        {
            if (!chatbots.ContainsKey(id))
                throw new ArgumentException($"Chatbot with ID '{id}' does not exist");

            currentChatbotId = id;
        }

        public void RemoveChatbot(string id)
        {
            if (id == "unity_helper")
                throw new ArgumentException("Cannot remove the default Unity Helper chatbot");

            if (!chatbots.ContainsKey(id))
                throw new ArgumentException($"Chatbot with ID '{id}' does not exist");

            chatbots.Remove(id);
            if (currentChatbotId == id)
            {
                currentChatbotId = "unity_helper";
            }
        }

        public IChatbot GetCurrentChatbot()
        {
            return chatbots[currentChatbotId];
        }
    }

    // 自定义Chatbot实现
    public class CustomChatbot : ChatbotBase
    {
        private readonly string id;
        private string name;
        private string description;
        private string systemPrompt;

        public override string Id => id;
        public override string Name => name;
        public override string Description => description;
        public string SystemPrompt => systemPrompt;

        public CustomChatbot(string id, string name, string description, string systemPrompt, bool useStreaming = false) 
            : base(systemPrompt, useStreaming: useStreaming, useSessionStorage: true)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.systemPrompt = systemPrompt;
        }

        public void UpdateSettings(string name, string description, string systemPrompt)
        {
            this.name = name;
            this.description = description;
            this.systemPrompt = systemPrompt;
            
            // 清空历史记录并重新初始化会话
            ClearHistory();
        }
    }
}
