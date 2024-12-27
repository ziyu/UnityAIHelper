using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityLLMAPI.Models;
using UnityLLMAPI.Services;
using UnityLLMAPI.Utils.Json;

namespace UnityAIHelper.Editor
{
    public static class UtilsAI
    {
        private static OpenAIService _openAIService = new();

        public static async Task<string> GenerateDialogName(IReadOnlyList<ChatMessageInfo> chatHistory)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            foreach (var messageInfo in chatHistory)
            {
                if(messageInfo.message.role=="system")continue;
                sb.Append(JsonConverter.SerializeObject(messageInfo.message));
                sb.Append(",");
            }
            sb.Append('}');
            return await GenerateDialogName(sb.ToString());
        }

        public static async Task<string> GenerateDialogName(string chatContent)
        {
            // 使用底层API生成会话名称
            var prompt = $"根据以下对话内容生成一个简短的会话名称（不超过10个字),只要名称，不要返回其他任何内容:\"\"\"{chatContent}\"\"\"";
            var genMsg = OpenAIService.CreateUserMessage(prompt);
            List<ChatMessage> messages = new() { genMsg };
            string genName = null;
            try
            {
                var message = await _openAIService.ChatCompletion(messages);
                genName = message.content?.Trim('\'', '\"', ' ');
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            return genName;
        }
    }
}