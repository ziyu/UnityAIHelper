using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor.Tools
{
    /// <summary>
    /// 工具注册表，管理所有可用的工具
    /// </summary>
    public class ToolRegistry
    {
        private static ToolRegistry instance;
        public static ToolRegistry Instance => instance ??= new ToolRegistry();

        private readonly Dictionary<string, IUnityTool> registeredTools = new();
        private readonly Dictionary<string, Type> toolTypes = new();
        private readonly Dictionary<string, object> toolInstances = new();
        private readonly ToolSet llmToolSet;
        private readonly ToolExecutor toolExecutor;

        private ToolRegistry()
        {
            llmToolSet = new ToolSet();
            toolExecutor = new ToolExecutor(this);
            RegisterBuiltInTools();
        }

        /// <summary>
        /// 获取LLM工具集
        /// </summary>
        public ToolSet LLMToolSet => llmToolSet;

        /// <summary>
        /// 注册工具
        /// </summary>
        public void RegisterTool(IUnityTool tool)
        {
            if (string.IsNullOrEmpty(tool.Name))
                throw new ArgumentException("Tool name cannot be empty");

            // 检查工具是否已经注册
            if (registeredTools.ContainsKey(tool.Name))
            {
                Debug.LogWarning($"Tool '{tool.Name}' is already registered. Skipping registration.");
                return;
            }

            // 检查LLM工具集中是否已存在
            if (llmToolSet.HasTool(tool.Name))
            {
                Debug.LogWarning($"Tool '{tool.Name}' is already registered in LLM tool set. Skipping registration.");
                return;
            }

            registeredTools[tool.Name] = tool;

            // 转换为LLM工具并注册
            var llmTool = UnityToolAdapter.ToLLMTool(tool);
            var handler = UnityToolAdapter.CreateToolHandler(tool, toolExecutor);
            llmToolSet.RegisterTool(llmTool, handler);
        }

        /// <summary>
        /// 注册工具类型
        /// </summary>
        public void RegisterToolType<T>() where T : IUnityTool
        {
            var type = typeof(T);
            if (toolTypes.ContainsKey(type.Name))
            {
                Debug.LogWarning($"Tool type '{type.Name}' is already registered. Skipping registration.");
                return;
            }
            toolTypes[type.Name] = type;
        }

        /// <summary>
        /// 获取工具实例
        /// </summary>
        public IUnityTool GetTool(string name)
        {
            if (registeredTools.TryGetValue(name, out var tool))
                return tool;

            if (toolTypes.TryGetValue(name, out var type))
            {
                if (!toolInstances.TryGetValue(name, out var instance))
                {
                    instance = Activator.CreateInstance(type);
                    toolInstances[name] = instance;
                }
                return (IUnityTool)instance;
            }

            throw new KeyNotFoundException($"Tool '{name}' not found");
        }

        /// <summary>
        /// 动态创建临时工具
        /// </summary>
        public async Task<IUnityTool> CreateTemporaryToolAsync(string name, string scriptContent)
        {
            try
            {
                // 检查是否已存在同名工具
                if (HasTool(name))
                {
                    throw new InvalidOperationException($"A tool named '{name}' already exists");
                }

                // 编译临时脚本
                var tool = await DynamicScriptCompiler.CompileAndCreateToolAsync(name, scriptContent);
                
                // 注册临时工具
                RegisterTool(tool);
                
                return tool;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create temporary tool: {ex}");
                throw;
            }
        }

        /// <summary>
        /// 获取所有注册的工具
        /// </summary>
        public IEnumerable<IUnityTool> GetAllTools()
        {
            return registeredTools.Values;
        }

        /// <summary>
        /// 获取指定类型的工具
        /// </summary>
        public IEnumerable<IUnityTool> GetToolsByType(ToolType type)
        {
            return registeredTools.Values.Where(t => t.Type == type);
        }

        /// <summary>
        /// 检查工具是否存在
        /// </summary>
        public bool HasTool(string name)
        {
            return registeredTools.ContainsKey(name) || toolTypes.ContainsKey(name) || llmToolSet.HasTool(name);
        }

        /// <summary>
        /// 移除工具
        /// </summary>
        public void RemoveTool(string name)
        {
            registeredTools.Remove(name);
            toolTypes.Remove(name);
            toolInstances.Remove(name);
            llmToolSet.UnregisterTool(name);
        }

        /// <summary>
        /// 清理所有工具
        /// </summary>
        public void Clear()
        {
            registeredTools.Clear();
            toolTypes.Clear();
            toolInstances.Clear();
            llmToolSet.Clear();
        }

        /// <summary>
        /// 注册内置工具
        /// </summary>
        private void RegisterBuiltInTools()
        {
            // Unity工具
            RegisterTool(new UnityTools.CreateGameObjectTool());
            RegisterTool(new UnityTools.FindGameObjectTool());
            RegisterTool(new UnityTools.DeleteGameObjectTool());
            RegisterTool(new UnityTools.DuplicateGameObjectTool());

            RegisterTool(new UnityTools.AddComponentTool());
            RegisterTool(new UnityTools.RemoveComponentTool());
            RegisterTool(new UnityTools.GetComponentPropertyTool());
            RegisterTool(new UnityTools.SetComponentPropertyTool());
            RegisterTool(new UnityTools.CopyComponentTool());

            RegisterTool(new UnityTools.ImportAssetTool());
            RegisterTool(new UnityTools.ExportAssetTool());
            RegisterTool(new UnityTools.AnalyzeAssetDependenciesTool());
            RegisterTool(new UnityTools.FindAssetReferencesTool());

            // 系统工具
            RegisterTool(new SystemTools.CreateFileTool());
            RegisterTool(new SystemTools.ReadFileTool());
            RegisterTool(new SystemTools.DeleteFileTool());
            RegisterTool(new SystemTools.CopyFileTool());
            RegisterTool(new SystemTools.SearchFilesTool());
            RegisterTool(new SystemTools.WatchFileTool());
            RegisterTool(new SystemTools.CreateToolTool());
            RegisterTool(new SystemTools.ExecuteCodeTool());
        }
    }
}
