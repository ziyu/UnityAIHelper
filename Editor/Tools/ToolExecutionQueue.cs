using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
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
        Failed,
        Cancelled,
    }

    /// <summary>
    /// 工具执行项
    /// </summary>
    [Serializable]
    public class ToolExecutionItem
    {
        public string ToolName;
        public Dictionary<string, object> Parameters;
        public ToolExecutionStatus Status ;
        public object Result ;
        public Exception Error;
    }

    /// <summary>
    /// 工具执行队列管理器
    /// </summary>
    public class ToolExecutionQueue
    {
        private readonly Queue<ToolExecutionItem> _executionQueue = new Queue<ToolExecutionItem>();
        private readonly ToolRegistry _toolRegistry;
        private bool _isExecuting;
        private ToolExecutionItem _currentItem;
        private CancellationTokenSource _executeCancellationTokenSource;
        public event Action<ToolExecutionItem> OnToolExecutionStarted;
        public event Action<ToolExecutionItem> OnToolExecutionCompleted;
        public event Action<ToolExecutionItem> OnToolExecutionFailed;

        /// <summary>
        /// 获取待执行的工具列表
        /// </summary>
        public Queue<ToolExecutionItem> PendingItems => _executionQueue;

        /// <summary>
        /// 获取当前正在执行的工具
        /// </summary>
        public ToolExecutionItem CurrentItem => _currentItem;

        public ToolExecutionQueue(ToolRegistry toolRegistry)
        {
            _toolRegistry = toolRegistry;
            _isExecuting = false;
        }

        /// <summary>
        /// 添加工具到执行队列
        /// </summary>
        public void EnqueueTool(string toolName, Dictionary<string, object> parameters)
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
                _executeCancellationTokenSource = new();
                _ = ExecuteQueueAsync(_executeCancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// 执行队列中的工具
        /// </summary>
        private async Task ExecuteQueueAsync(CancellationToken cancellationToken)
        {
            if (_isExecuting) return;
            
            _isExecuting = true;
            
            while (_executionQueue.Count > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Clear();
                    break;
                }

                _currentItem = _executionQueue.Peek();
                
                try
                {
                    if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        Clear();
                        break;
                    }

                    _currentItem.Status = ToolExecutionStatus.Running;
                    OnToolExecutionStarted?.Invoke(_currentItem);

                    var tool = _toolRegistry.GetTool(_currentItem.ToolName);
                    _currentItem.Result = await tool.ExecuteAsync(_currentItem.Parameters);
                    cancellationToken.ThrowIfCancellationRequested();

                    _currentItem.Status = ToolExecutionStatus.Completed;
                    
                    OnToolExecutionCompleted?.Invoke(_currentItem);
                }
                catch (Exception ex)
                {
                    if (ex is TaskCanceledException or OperationCanceledException)
                    {
                        _currentItem.Status = ToolExecutionStatus.Cancelled;
                        _currentItem.Error = ex;
                        OnToolExecutionFailed?.Invoke(_currentItem);
                    
                        Debug.Log($"Tool '{_currentItem.ToolName}' execution cancelled.");
                    }
                    else
                    {
                        _currentItem.Status = ToolExecutionStatus.Failed;
                        _currentItem.Error = ex;
                        OnToolExecutionFailed?.Invoke(_currentItem);
                        Debug.LogError($"Tool '{_currentItem.ToolName}' execution failed: {ex}");
                    }
                    break;
                }
                finally
                {
                    // 无论成功失败都从队列移除
                    _executionQueue.Dequeue();
                    _currentItem = null;
                }
            }
            _isExecuting = false;
        }

        /// <summary>
        /// 清空执行队列
        /// </summary>
        public void Clear()
        {
            _executeCancellationTokenSource?.Cancel();
            _executeCancellationTokenSource = null;
            _executionQueue.Clear();
            _currentItem = null;
            _isExecuting = false;
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
