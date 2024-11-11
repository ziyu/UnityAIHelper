using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor
{
    public class UnityHelperChatbot : ChatbotBase
    {
        private const string SYSTEM_PROMPT = @"你是一个Unity开发助手，可以帮助用户：
1. 解答Unity相关的开发问题
2. 提供代码示例和最佳实践
3. 执行Unity Editor相关的操作
4. 提供性能优化建议
5. 根据用户需求创建脚本文件

你支持处理复合命令，例如：
- '创建10个带有刚体的方块' 可以使用 repeat 命令
- '创建多个不同类型的游戏对象' 可以使用 batch 命令
- '创建对象并添加多个组件' 可以在create命令中指定多个组件

命令示例：
- repeat 10 'create Cube Rigidbody'
- batch 'create Sphere;create Cube;create Cylinder'
- create Player Rigidbody BoxCollider AudioSource
- createscript PlayerController '使用MonoBehaviour的完整脚本内容'

当用户要求创建脚本时：
1. 理解用户的需求，生成完整的脚本内容
2. 使用createscript命令创建脚本文件
3. 确保生成的脚本遵循Unity最佳实践和编码规范

请用简洁专业的方式回答问题。当需要执行Unity操作时，请使用execute_unity_command工具。对于复杂的操作，优先考虑使用batch或repeat命令来组合基本命令。";

        public override string Id => "unity_helper";
        public override string Name => "Unity助手";

        public UnityHelperChatbot() : base(SYSTEM_PROMPT)
        {
            RegisterUnityCommandTool();
        }

        private void RegisterUnityCommandTool()
        {
            var unityCommandTool = new Tool
            {
                type = "function",
                function = new ToolFunction
                {
                    name = "execute_unity_command",
                    description = @"执行Unity编辑器命令。支持以下命令类型：
- create [名称] [组件1] [组件2]... : 创建游戏对象并添加组件
- select [名称] : 选择游戏对象
- delete [名称] : 删除游戏对象
- addcomponent [对象名] [组件名] : 添加组件
- setposition [对象名] [x] [y] [z] : 设置位置
- setrotation [对象名] [x] [y] [z] : 设置旋转
- setscale [对象名] [x] [y] [z] : 设置缩放
- batch '命令1;命令2;...' : 批量执行多个命令
- repeat [次数] [命令] : 重复执行指定命令
- save : 保存场景
- build : 构建项目
- createscript [脚本名称] [脚本内容] : 创建新的C#脚本文件。脚本内容应该是完整的类定义，包括所有必要的using语句和命名空间。",
                    parameters = new ToolParameters
                    {
                        type = "object",
                        properties = new Dictionary<string, ToolParameterProperty>
                        {
                            {
                                "command",
                                new ToolParameterProperty
                                {
                                    type = "string",
                                    description = "要执行的Unity命令，支持单个命令或使用batch/repeat进行复合命令"
                                }
                            }
                        },
                        required = new[] { "command" }
                    }
                }
            };

            RegisterTool(unityCommandTool, HandleUnityCommand);
        }

        private async Task<string> HandleUnityCommand(ToolCall toolCall)
        {
            try
            {
                // 从JSON参数中提取命令
                var args = JsonUtility.FromJson<CommandArgs>(toolCall.function.arguments);
                
                // 执行命令
                UnityCommandExecutor.ExecuteCommand(args.command);
                
                return $"命令 '{args.command}' 执行成功";
            }
            catch (Exception ex)
            {
                Debug.LogError($"执行Unity命令时出错: {ex.Message}");
                return $"执行命令失败: {ex.Message}";
            }
        }

        // 用于解析命令参数的辅助类
        [Serializable]
        private class CommandArgs
        {
            public string command;
        }
    }
}
