using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityLLMAPI.Config;
using UnityLLMAPI.Models;
using UnityLLMAPI.Services;

namespace UnityAIHelper.Editor
{
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
            // 创建默认的Unity助手chatbot
            var unityHelper = new UnityHelperChatbot();
            chatbots.Add(unityHelper.Id, unityHelper);
            currentChatbotId = unityHelper.Id;
        }

        public IChatbot CreateCustomChatbot(string id, string name, string systemPrompt)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Chatbot ID cannot be empty");

            if (chatbots.ContainsKey(id))
                throw new ArgumentException($"Chatbot with ID '{id}' already exists");

            var chatbot = new CustomChatbot(id, name, systemPrompt);
            chatbots.Add(id, chatbot);
            return chatbot;
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
    public class CustomChatbot : IChatbot
    {
        private readonly ChatbotService chatbotService;
        private readonly string id;
        private readonly string name;

        public string Id => id;
        public string Name => name;

        public CustomChatbot(string id, string name, string systemPrompt)
        {
            this.id = id;
            this.name = name;

            // 1. 获取OpenAI配置
            var openAIConfig = OpenAIConfig.Instance;
            if (openAIConfig == null)
            {
                throw new Exception("请在Resources文件夹中创建OpenAIConfig配置文件");
            }

            // 2. 创建OpenAI服务
            var openAIService = new OpenAIService(openAIConfig);

            // 3. 配置ChatBot
            var chatbotConfig = new ChatbotConfig
            {
                systemPrompt = systemPrompt,
                useStreaming = false,
                defaultModel = openAIConfig.defaultModel
            };

            // 4. 创建ChatBot服务
            chatbotService = new ChatbotService(openAIService, chatbotConfig);
        }

        public async Task<ChatMessage> SendMessageAsync(string message)
        {
            try
            {
                return await chatbotService.SendMessage(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in CustomChatbot: {ex.Message}");
                throw;
            }
        }

        public IReadOnlyList<ChatMessage> GetChatHistory()
        {
            return chatbotService.Messages;
        }

        public void ClearHistory()
        {
            chatbotService.ClearHistory();
        }
    }
}
