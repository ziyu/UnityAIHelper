using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityAIHelper.Editor.Tools
{
    /// <summary>
    /// 工具执行状态
    /// </summary>
    public enum ToolExecutionStatus
    {
        Pending,
        Running,
        Completed,
        Failed
    }

    /// <summary>
    /// 工具执行项
    /// </summary>
    public class ToolExecutionItem
    {
        public string ToolName { get; set; }
        public IDictionary<string, object> Parameters { get; set; }
        public ToolExecutionStatus Status { get; set; }
        public object Result { get; set; }
        public Exception Error { get; set; }
    }

    /// <summary>
    /// 工具执行队列管理器
    /// </summary>
    public class ToolExecutionQueue
    {

        private readonly Queue<ToolExecutionItem> _executionQueue = new Queue<ToolExecutionItem>();
        private readonly ToolRegistry _toolRegistry;
        private bool _isExecuting;

        public event Action<ToolExecutionItem> OnToolExecutionStarted;
        public event Action<ToolExecutionItem> OnToolExecutionCompleted;
        public event Action<ToolExecutionItem> OnToolExecutionFailed;

        public ToolExecutionQueue(ToolRegistry toolRegistry)
        {
            _toolRegistry = toolRegistry;
            _isExecuting = false;
        }

        /// <summary>
        /// 添加工具到执行队列
        /// </summary>
        public void EnqueueTool(string toolName, IDictionary<string, object> parameters)
        {
            if (!_toolRegistry.HasTool(toolName))
            {
                throw new ArgumentException($"Tool '{toolName}' not found");
            }

            var executionItem = new ToolExecutionItem
            {
                ToolName = toolName,
                Parameters = parameters,
                Status = ToolExecutionStatus.Pending
            };

            _executionQueue.Enqueue(executionItem);
            
            // 如果队列没有在执行，开始执行
            if (!_isExecuting)
            {
                _ = ExecuteQueueAsync();
            }
        }

        /// <summary>
        /// 执行队列中的工具
        /// </summary>
        private async Task ExecuteQueueAsync()
        {
            if (_isExecuting) return;
            
            _isExecuting = true;

            try
            {
                while (_executionQueue.Count > 0)
                {
                    var item = _executionQueue.Peek();
                    
                    try
                    {
                        item.Status = ToolExecutionStatus.Running;
                        OnToolExecutionStarted?.Invoke(item);

                        var tool = _toolRegistry.GetTool(item.ToolName);
                        item.Result = await tool.ExecuteAsync(item.Parameters);
                        
                        item.Status = ToolExecutionStatus.Completed;
                        OnToolExecutionCompleted?.Invoke(item);
                    }
                    catch (Exception ex)
                    {
                        item.Status = ToolExecutionStatus.Failed;
                        item.Error = ex;
                        OnToolExecutionFailed?.Invoke(item);
                        
                        Debug.LogError($"Tool '{item.ToolName}' execution failed: {ex}");
                        // 发生错误时清空队列
                        _executionQueue.Clear();
                        break;
                    }
                    finally
                    {
                        // 无论成功失败都从队列移除
                        _executionQueue.Dequeue();
                    }
                }
            }
            finally
            {
                _isExecuting = false;
            }
        }

        /// <summary>
        /// 清空执行队列
        /// </summary>
        public void Clear()
        {
            _executionQueue.Clear();
        }

        /// <summary>
        /// 获取当前队列中的工具数量
        /// </summary>
        public int Count => _executionQueue.Count;

        /// <summary>
        /// 检查是否有工具正在执行
        /// </summary>
        public bool IsExecuting => _isExecuting;
    }
}
