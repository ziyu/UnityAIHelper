using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIHelper.Editor.Tools.SystemTools
{
    /// <summary>
    /// 创建临时工具的工具
    /// </summary>
    public class CreateToolTool : UnityToolBase
    {
        public override string Name => "CreateTool";
        public override string Description => "创建并注册一个新的临时工具";
        public override ToolType Type => ToolType.System;

        protected override void InitializeParameters()
        {
            AddParameter("name", typeof(string), "工具名称");
            AddParameter("scriptContent", typeof(string), "工具的C#脚本内容");
            AddParameter("description", typeof(string), "工具描述", false, "");
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var name = GetParameterValue<string>(parameters, "name");
            var scriptContent = GetParameterValue<string>(parameters, "scriptContent");
            var description = GetParameterValueOrDefault<string>(parameters, "description", "");

            try
            {
                // 检查工具名称是否已存在
                if (ToolRegistry.Instance.HasTool(name))
                {
                    throw new InvalidOperationException($"Tool '{name}' already exists");
                }

                // 如果提供了描述，添加到脚本内容中
                if (!string.IsNullOrEmpty(description))
                {
                    scriptContent = scriptContent.Replace("public string Description { get; }", 
                        $"public string Description => \"{description}\";");
                }

                // 创建临时工具
                var tool = await ToolRegistry.Instance.CreateTemporaryToolAsync(name, scriptContent);

                return new
                {
                    Success = true,
                    Message = $"Tool '{name}' created successfully",
                    Tool = new
                    {
                        Name = tool.Name,
                        Description = tool.Description,
                        Type = tool.Type.ToString(),
                        Parameters = tool.Parameters
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create tool '{name}': {ex}");
                return new
                {
                    Success = false,
                    Message = $"Failed to create tool: {ex.Message}"
                };
            }
        }
    }
}
