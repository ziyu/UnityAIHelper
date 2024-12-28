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
    /// 权限类型枚举 (Flags)
    /// </summary>
    [Flags]
    public enum PermissionType
    {
        None = 0,       // 无权限
        Read = 1 << 0,  // 读取权限
        Write = 1 << 1, // 写入权限
        Delete = 1 << 2 // 删除权限
    }

    /// <summary>
    /// 工具接口
    /// </summary>
    public interface IUnityTool
    {
        string Name { get; }
        string Description { get; }
        ToolType Type { get; }
        
        // 获取工具所需权限
        PermissionType RequiredPermissions { get; }
        
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
    [Serializable]
    public class ToolExecutionResult
    {
        public bool Success;
        public object Result;
        public string Error;
        public IDictionary<string, object> OutputParameters;
    }
}
