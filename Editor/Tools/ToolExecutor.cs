using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityAIHelper.Editor.Tools
{

    public class ToolExecutor
    {
        private readonly ToolRegistry _toolRegistry;
        private readonly Dictionary<string, object> _globalContext;
        private readonly ToolExecutionQueue _executionQueue;

        public bool IsExecuting => _executionQueue.IsExecuting;

        public ToolExecutor(ToolRegistry registry)
        {
            _toolRegistry = registry;
            _globalContext = new Dictionary<string, object>();
            _executionQueue = new ToolExecutionQueue(registry);

            // 监听工具执行状态
            _executionQueue.OnToolExecutionCompleted += OnToolExecutionCompleted;
            _executionQueue.OnToolExecutionFailed += OnToolExecutionFailed;
        }

        /// <summary>
        /// 执行工具
        /// </summary>
        public async Task<ToolExecutionResult> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters)
        {
            try
            {
                var tool = _toolRegistry.GetTool(toolName);
                
                // 处理参数
                var processedParams = await ProcessParametersAsync(tool, parameters);
                
                // 处理依赖
                await HandleDependenciesAsync(tool);

                // 创建TaskCompletionSource来等待工具执行完成
                var tcs = new TaskCompletionSource<object>();
                
                // 创建完成回调
                void OnComplete(ToolExecutionItem item)
                {
                    if (item.ToolName == toolName)
                    {
                        if (item.Result != null)
                        {
                            tcs.TrySetResult(item.Result);
                        }
                        else if (item.Error != null)
                        {
                            tcs.TrySetException(item.Error);
                        }
              
                        
                        // 移除事件监听
                        _executionQueue.OnToolExecutionCompleted -= OnComplete;
                        _executionQueue.OnToolExecutionFailed -= OnComplete;
                    }
                }

                // 添加事件监听
                _executionQueue.OnToolExecutionCompleted += OnComplete;
                _executionQueue.OnToolExecutionFailed += OnComplete;

                // 将工具添加到执行队列
                _executionQueue.EnqueueTool(toolName, processedParams);

                // 等待执行完成
                var result = await tcs.Task;
                
                // 更新全局上下文
                UpdateGlobalContext(toolName, result);

                if (result != null)
                {
                    return new ToolExecutionResult()
                    {
                        Success = true,
                        Result = result
                    };
                }
                return new ToolExecutionResult()
                {
                    Success = false,
                    Error = tcs.Task.Exception?.Message
                };

            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to execute tool '{toolName}': {ex}");
                return new ToolExecutionResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private void OnToolExecutionCompleted(ToolExecutionItem item)
        {
            Debug.Log($"Tool '{item.ToolName}' completed successfully,result:{item.Result}");
        }

        private void OnToolExecutionFailed(ToolExecutionItem item)
        {
            Debug.LogError($"Tool '{item.ToolName}' failed: {item.Error}");
        }

        /// <summary>
        /// 处理工具参数
        /// </summary>
        private async Task<Dictionary<string, object>> ProcessParametersAsync(IUnityTool tool, Dictionary<string, object> inputParams)
        {
            var processedParams = new Dictionary<string, object>();

            foreach (var param in tool.Parameters)
            {
                if (inputParams.TryGetValue(param.Name, out var value))
                {
                    processedParams[param.Name] = ConvertParameterValue(value, param.Type);
                }
                else if (param.IsRequired)
                {
                    throw new ArgumentException($"Required parameter '{param.Name}' not provided for tool '{tool.Name}'");
                }
                else if (param.DefaultValue != null)
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
            if (tool.Dependencies == null || tool.Dependencies.Count == 0)
                return;

            foreach (var dependency in tool.Dependencies)
            {
                if (!_toolRegistry.HasTool(dependency))
                {
                    throw new InvalidOperationException($"Dependency tool '{dependency}' not found for tool '{tool.Name}'");
                }
            }
        }

        /// <summary>
        /// 更新全局上下文
        /// </summary>
        private void UpdateGlobalContext(string toolName, object result)
        {
            if (result != null)
            {
                _globalContext[toolName] = result;
            }
        }

        /// <summary>
        /// 转换参数值类型
        /// </summary>
        private object ConvertParameterValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                if (targetType.IsEnum)
                    return Enum.Parse(targetType, value.ToString());

                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to convert parameter value '{value}' to type '{targetType}'", ex);
            }
        }

        /// <summary>
        /// 清理全局上下文
        /// </summary>
        public void ClearContext()
        {
            _globalContext.Clear();
            _executionQueue.Clear();
        }
    }
}
