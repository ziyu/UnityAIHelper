using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityLLMAPI.Models;
using UnityAIHelper.Editor.Tools;

namespace UnityAIHelper.Editor
{
    /// <summary>
    /// Unity助手Chatbot，提供Unity开发相关的智能辅助功能
    /// </summary>
    public class UnityHelperChatbot : ChatbotBase
    {
        private const string DefaultSystemPrompt = @"你是一个Unity开发助手，可以帮助用户完成Unity相关的开发任务。你可以：

1. 创建、修改、删除GameObject和Component
2. 管理Unity资源和AssetBundle
3. 处理文件操作和资源导入导出
4. 动态创建工具和执行临时代码
5. 分析项目结构和依赖关系

你有以下工具可以使用：

{0}

高级功能说明：
临时代码执行（ExecuteCode）：
用于直接执行一段C#代码，无需创建工具，适合一次性的操作。
代码会放到一个方法中执行，所以必须是一段可以直接执行的代码。
不能定义类！不能定义类！不能定义类！
不能添加using代码，必须使用完整的命名空间调用函数。
不能添加using代码！不能添加using代码！不能添加using代码！
注意代码依赖关系，需要创建的脚本要先创建后再调用其函数。
最终返回一个字符串作为执行结果。
参数说明：
- code: 要执行的C#代码

示例：执行临时代码
{{
    ""code"": ""
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = new Vector3(0, 2, 0);
        return ""cube created"";
    ""
}}

工具使用说明：
1. 每个工具都有特定的参数要求，使用前请仔细检查参数
2. 工具可以组合使用，但要注意执行顺序
4. 大多数功能需求都通过执行临时代码实现
6. 在修改场景或资源时要使用Undo系统
7. 注意资源的正确导入和刷新

请根据用户的需求，选择合适的工具来完成任务。大多数功能需求都通过执行临时代码实现。需要创建代码通过CreateScript工具实现。如果遇到问题，及时报告错误并提供解决方案。";

        public override string Id => "unity_helper";

        public UnityHelperChatbot(bool useStreaming = false, Action<ChatMessage> streamingCallback = null)
            : base(string.Empty, useStreaming, streamingCallback)
        {
            name = "Unity助手";
            description = "Unity开发助手，可以帮助完成Unity相关的开发任务";
            systemPrompt = DefaultSystemPrompt;
        }

        protected override string GetSystemPrompt(string basePrompt)
        {
            var toolsDescription = GenerateToolsDescription();
            return string.Format(systemPrompt, toolsDescription);
        }

        private string GenerateToolsDescription()
        {
            var sb = new StringBuilder();
            var allTools = toolRegistry.GetAllTools().ToList();
            
            var systemTools = allTools.Where(t => t.Type == ToolType.System)
                                    .Where(t => t.Name != "ExecuteCode")
                                    .ToList();

            // 系统工具
            if (systemTools.Any())
            {
                sb.AppendLine("系统工具：");
                foreach (var tool in systemTools)
                {
                    sb.AppendLine($"- {tool.Name}: {tool.Description}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public override async Task<ChatMessage> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            try
            {
                // 在发送消息前确保Unity处于编辑器模式
                if (!Application.isEditor)
                {
                    throw new InvalidOperationException("UnityHelperChatbot只能在Unity编辑器中使用");
                }

                return await base.SendMessageAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.LogError($"UnityHelperChatbot error: {ex}");
                throw;
            }
        }

        /// <summary>
        /// 获取工具列表
        /// </summary>
        public IUnityTool[] GetAvailableTools()
        {
            return toolRegistry.GetAllTools().ToArray();
        }

        public override void UpdateSettings(string name, string description, string systemPrompt)
        {
            base.UpdateSettings(name, description, 
                string.IsNullOrEmpty(systemPrompt) ? DefaultSystemPrompt : systemPrompt);
        }
    }
}
