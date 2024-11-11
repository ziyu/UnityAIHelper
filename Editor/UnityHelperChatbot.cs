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
        private const string SystemPromptTemplate = @"你是一个Unity开发助手，可以帮助用户完成Unity相关的开发任务。你可以：

1. 创建、修改、删除GameObject和Component
2. 管理Unity资源和AssetBundle
3. 处理文件操作和资源导入导出
4. 动态创建工具和执行临时代码
5. 分析项目结构和依赖关系

你有以下工具可以使用：

{0}

高级功能说明：

1. 动态工具创建（CreateTool）：
用于创建新的可重用工具，工具创建后会被注册到系统中供后续使用。
参数说明：
- name: 工具名称
- scriptContent: C#脚本内容（必须实现IUnityTool接口）
- description: 工具描述（可选）

示例：创建一个新工具
{{
    ""name"": ""MyTool"",
    ""scriptContent"": ""
        public class MyTool : UnityToolBase
        {{
            public override string Name => \""MyTool\"";
            public override string Description => \""My custom tool\"";
            public override ToolType Type => ToolType.System;

            protected override void InitializeParameters()
            {{
                AddParameter(\""param1\"", typeof(string), \""Parameter 1\"");
            }}

            public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
            {{
                var value = GetParameterValue<string>(parameters, \""param1\"");
                return $\""Executed with value: {{value}}\"";
            }}
        }}
    ""
}}

2. 临时代码执行（ExecuteCode）：
用于直接执行一段C#代码，无需创建工具，适合一次性的操作。
代码会放到一个方法中执行，所以必须是一段可以直接执行的代码.
最终返回一个字符串作为执行结果.
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

使用说明：
1. 每个工具都有特定的参数要求，使用前请仔细检查参数
2. 工具可以组合使用，但要注意执行顺序
3. 对于重复使用的功能，建议使用CreateTool创建专门的工具
4. 对于一次性的操作，可以使用ExecuteCode直接执行代码
6. 在修改场景或资源时要使用Undo系统
7. 注意资源的正确导入和刷新

请根据用户的需求，选择合适的工具来完成任务。如果现有工具无法满足需求，可以使用CreateTool创建新的工具或使用ExecuteCode执行临时代码。如果遇到问题，及时报告错误并提供解决方案。";

        public override string Id => "UnityHelper";
        public override string Name => "Unity助手";

        public UnityHelperChatbot(bool useStreaming = false, Action<ChatMessage, bool> streamingCallback = null)
            : base(GenerateSystemPrompt(), useStreaming, streamingCallback)
        {
        }

        private static string GenerateSystemPrompt()
        {
            var toolsDescription = GenerateToolsDescription();
            return string.Format(SystemPromptTemplate, toolsDescription);
        }

        private static string GenerateToolsDescription()
        {
            var sb = new StringBuilder();
            var registry = ToolRegistry.Instance;
            var allTools = registry.GetAllTools().ToList();

            // 按类型分组工具
            var unityTools = allTools.Where(t => t.Type == ToolType.Unity).ToList();
            var systemTools = allTools.Where(t => t.Type == ToolType.System)
                                    .Where(t => t.Name != "CreateTool" && t.Name != "ExecuteCode")
                                    .ToList();

            // Unity工具
            if (unityTools.Any())
            {
                sb.AppendLine("Unity工具：");
                foreach (var tool in unityTools)
                {
                    sb.AppendLine($"- {tool.Name}: {tool.Description}");
                }
                sb.AppendLine();
            }

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

        protected override void RegisterTools()
        {
            // 工具已在ToolRegistry.RegisterBuiltInTools中注册，这里不需要重复注册
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
        /// 创建临时工具
        /// </summary>
        public async Task<IUnityTool> CreateToolAsync(string name, string scriptContent)
        {
            try
            {
                return await CreateTemporaryToolAsync(name, scriptContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create tool: {ex}");
                throw;
            }
        }

        /// <summary>
        /// 获取工具列表
        /// </summary>
        public IUnityTool[] GetAvailableTools()
        {
            return GetToolsByType(ToolType.Unity)
                .Concat(GetToolsByType(ToolType.System))
                .ToArray();
        }
    }
}
