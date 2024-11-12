using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace UnityAIHelper.Editor.Tools
{
    /// <summary>
    /// 工具类型枚举
    /// </summary>
    public enum ToolType
    {
        System,         // 系统工具
        Custom,        // 自定义工具
    }

    /// <summary>
    /// 工具接口
    /// </summary>
    public interface IUnityTool
    {
        string Name { get; }
        string Description { get; }
        ToolType Type { get; }
        
        // 获取工具参数定义
        IReadOnlyList<ToolParameter> Parameters { get; }
        
        // 执行工具
        Task<object> ExecuteAsync(IDictionary<string, object> parameters);
        
        // 获取依赖的其他工具
        IReadOnlyList<string> Dependencies { get; }
    }

    /// <summary>
    /// 工具参数定义
    /// </summary>
    public class ToolParameter
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Type Type { get; set; }
        public bool IsRequired { get; set; }
        public object DefaultValue { get; set; }
    }

    /// <summary>
    /// 工具执行结果
    /// </summary>
    public class ToolExecutionResult
    {
        public bool Success { get; set; }
        public object Result { get; set; }
        public string Error { get; set; }
        public IDictionary<string, object> OutputParameters { get; set; }
    }
}
