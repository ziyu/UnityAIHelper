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
        private readonly Dictionary<string, IUnityTool> registeredTools = new();
        private readonly ToolSet toolSet;

        public ToolRegistry()
        {
            this.toolSet = new ToolSet();
        }

        /// <summary>
        /// 获取LLM工具集
        /// </summary>
        public ToolSet LLMToolSet => toolSet;

        /// <summary>
        /// 注册工具
        /// </summary>
        public void RegisterTool(IUnityTool tool,ToolExecutor toolExecutor)
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
            if (toolSet.HasTool(tool.Name))
            {
                Debug.LogWarning($"Tool '{tool.Name}' is already registered in LLM tool set. Skipping registration.");
                return;
            }

            registeredTools[tool.Name] = tool;

            // 转换为LLM工具并注册
            var llmTool = UnityToolAdapter.ToLLMTool(tool);
            var handler = UnityToolAdapter.CreateToolHandler(tool, toolExecutor);
            toolSet.RegisterTool(llmTool, handler);
        }

        /// <summary>
        /// 获取工具实例
        /// </summary>
        public IUnityTool GetTool(string name)
        {
            if (registeredTools.TryGetValue(name, out var tool))
                return tool;

            throw new KeyNotFoundException($"Tool '{name}' not found");
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
            return registeredTools.ContainsKey(name) || toolSet.HasTool(name);
        }

        /// <summary>
        /// 移除工具
        /// </summary>
        public void RemoveTool(string name)
        {
            registeredTools.Remove(name);
            toolSet.UnregisterTool(name);
        }

        /// <summary>
        /// 清理所有工具和执行上下文
        /// </summary>
        public void Clear()
        {
            registeredTools.Clear();
            toolSet.Clear();
        }

        /// <summary>
        /// 注册内置工具
        /// </summary>
        public void RegisterBuiltInTools(ToolExecutor toolExecutor)
        {
            // 系统工具
            RegisterTool(new SystemTools.ReadFileTool(),toolExecutor);
            RegisterTool(new SystemTools.DeleteFileTool(),toolExecutor);
            RegisterTool(new SystemTools.CopyFileTool(),toolExecutor);
            RegisterTool(new SystemTools.SearchFilesTool(),toolExecutor);
            RegisterTool(new SystemTools.WatchFileTool(),toolExecutor);
            RegisterTool(new SystemTools.ExecuteCodeTool(),toolExecutor);
            RegisterTool(new SystemTools.CreateScriptTool(),toolExecutor);
            
            // Scene tools
            RegisterTool(new SceneTools.CreateSceneTool(), toolExecutor);
            RegisterTool(new SceneTools.LoadSceneTool(), toolExecutor);
            RegisterTool(new SceneTools.SaveSceneTool(), toolExecutor);
            RegisterTool(new SceneTools.SceneStructureTool(), toolExecutor);
            
            // Asset tools
            RegisterTool(new AssetTools.ImportAssetTool(), toolExecutor);
            RegisterTool(new AssetTools.MoveAssetTool(), toolExecutor);
            RegisterTool(new AssetTools.RenameAssetTool(), toolExecutor);
            RegisterTool(new AssetTools.DeleteAssetTool(), toolExecutor);
            RegisterTool(new AssetTools.GetAssetInfoTool(), toolExecutor);
        }
    }
}
