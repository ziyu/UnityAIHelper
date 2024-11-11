using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIHelper.Editor.Tools
{
    /// <summary>
    /// 工具执行器
    /// </summary>
    public class ToolExecutor
    {
        private readonly ToolRegistry _toolRegistry;
        private readonly Dictionary<string, object> _globalContext;
        private readonly Stack<string> _executionStack;

        public ToolExecutor(ToolRegistry toolRegistry)
        {
            _toolRegistry = toolRegistry;
            _globalContext = new Dictionary<string, object>();
            _executionStack = new Stack<string>();
        }

        /// <summary>
        /// 执行工具
        /// </summary>
        public async Task<ToolExecutionResult> ExecuteToolAsync(string toolName, IDictionary<string, object> parameters)
        {
            try
            {
                // 检查循环依赖
                if (_executionStack.Contains(toolName))
                {
                    throw new Exception($"Circular dependency detected: {string.Join(" -> ", _executionStack)} -> {toolName}");
                }

                _executionStack.Push(toolName);

                // 获取工具实例
                var tool = _toolRegistry.GetTool(toolName);
                
                // 验证和处理参数
                var processedParams = await ProcessParametersAsync(tool, parameters);
                
                // 处理依赖
                await HandleDependenciesAsync(tool);
                
                // 执行工具
                var result = await ExecuteWithContextAsync(tool, processedParams);
                
                // 更新全局上下文
                UpdateGlobalContext(toolName, result);

                _executionStack.Pop();
                
                return new ToolExecutionResult
                {
                    Success = true,
                    Result = result,
                    OutputParameters = processedParams
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing tool '{toolName}': {ex}");
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// 处理工具参数
        /// </summary>
        private async Task<IDictionary<string, object>> ProcessParametersAsync(IUnityTool tool, IDictionary<string, object> inputParams)
        {
            var processedParams = new Dictionary<string, object>();

            foreach (var param in tool.Parameters)
            {
                if (inputParams.TryGetValue(param.Name, out var value))
                {
                    // 转换参数值到正确的类型
                    processedParams[param.Name] = ConvertParameterValue(value, param.Type);
                }
                else if (param.IsRequired)
                {
                    throw new ArgumentException($"Required parameter '{param.Name}' is missing");
                }
                else
                {
                    processedParams[param.Name] = param.DefaultValue;
                }
            }

            return processedParams;
        }

        /// <summary>
        /// 处理工具依赖
        /// </summary>
        private async Task HandleDependenciesAsync(IUnityTool tool)
        {
            foreach (var dependency in tool.Dependencies)
            {
                if (!_globalContext.ContainsKey(dependency))
                {
                    // 递归执行依赖工具
                    var dependencyTool = _toolRegistry.GetTool(dependency);
                    var result = await ExecuteToolAsync(dependency, new Dictionary<string, object>());
                    if (!result.Success)
                    {
                        throw new Exception($"Failed to execute dependency '{dependency}': {result.Error}");
                    }
                }
            }
        }

        /// <summary>
        /// 在上下文中执行工具
        /// </summary>
        private async Task<object> ExecuteWithContextAsync(IUnityTool tool, IDictionary<string, object> parameters)
        {
            // 添加全局上下文到参数
            foreach (var dependency in tool.Dependencies)
            {
                if (_globalContext.TryGetValue(dependency, out var contextValue))
                {
                    parameters[$"_context_{dependency}"] = contextValue;
                }
            }

            return await tool.ExecuteAsync(parameters);
        }

        /// <summary>
        /// 更新全局上下文
        /// </summary>
        private void UpdateGlobalContext(string toolName, object result)
        {
            _globalContext[toolName] = result;
        }

        /// <summary>
        /// 转换参数值到指定类型
        /// </summary>
        private object ConvertParameterValue(object value, Type targetType)
        {
            try
            {
                if (value == null)
                {
                    return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                }

                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, value.ToString());
                }

                if (targetType == typeof(Vector2) && value is string vectorStr)
                {
                    var parts = vectorStr.Split(',');
                    return new Vector2(
                        float.Parse(parts[0]),
                        float.Parse(parts[1])
                    );
                }

                if (targetType == typeof(Vector3) && value is string vectorStr3)
                {
                    var parts = vectorStr3.Split(',');
                    return new Vector3(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2])
                    );
                }

                // 处理数组和列表类型
                if (targetType.IsArray || (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    var elementType = targetType.IsArray ? targetType.GetElementType() : targetType.GetGenericArguments()[0];
                    if (value is string str)
                    {
                        var elements = str.Split(',').Select(s => Convert.ChangeType(s.Trim(), elementType));
                        if (targetType.IsArray)
                        {
                            var array = Array.CreateInstance(elementType, elements.Count());
                            var i = 0;
                            foreach (var element in elements)
                            {
                                array.SetValue(element, i++);
                            }
                            return array;
                        }
                        else
                        {
                            var list = (System.Collections.IList)Activator.CreateInstance(targetType);
                            foreach (var element in elements)
                            {
                                list.Add(element);
                            }
                            return list;
                        }
                    }
                }

                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to convert value '{value}' to type {targetType.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理执行上下文
        /// </summary>
        public void ClearContext()
        {
            _globalContext.Clear();
            _executionStack.Clear();
        }
    }
}
