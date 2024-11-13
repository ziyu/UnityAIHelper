using UnityEngine;
using UnityEditor;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityLLMAPI.Models;
using UnityAIHelper.Editor.UI;

namespace UnityAIHelper.Editor
{
    public class AIHelperWindow : EditorWindow
    {
        // UI组件
        private ChatAreaUI chatAreaUI;
        private InputAreaUI inputAreaUI;
        private ToolbarUI toolbarUI;
        private NewChatbotUI newChatbotUI;

        // 状态
        private bool isProcessing = false;
        private bool isCreatingNew = false;
        private CancellationTokenSource cancellationTokenSource;
        private const float STATUS_HEIGHT = 30f;
        private GUIStyle statusStyle;

        // Streaming相关
        private ChatMessage currentStreamingMessage;
        private bool isStreaming = false;

        [MenuItem("Window/AI Helper")]
        public static void ShowWindow()
        {
            GetWindow<AIHelperWindow>("AI Helper");
        }

        private void OnEnable()
        {
            // 初始化UI组件
            chatAreaUI = new ChatAreaUI(this);
            inputAreaUI = new InputAreaUI(this);
            toolbarUI = new ToolbarUI(this);
            newChatbotUI = new NewChatbotUI(this);

            // 绑定事件
            inputAreaUI.OnSendMessage += SendMessage;
            toolbarUI.OnCreateNewChatbot += () => { isCreatingNew = true; Repaint(); };
            toolbarUI.OnClearHistory += () => { ChatbotManager.Instance.GetCurrentChatbot().ClearHistory(); Repaint(); };
            newChatbotUI.OnCancel += () => { isCreatingNew = false; Repaint(); };
            newChatbotUI.OnCreate += CreateNewChatbot;

            // 初始化样式
            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 10, 5, 5),
                    fontSize = 12
                };
            }

            // 检查是否有未完成的对话
            var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
            if (currentBot.HasPendingMessage)
            {
                isProcessing = true;
                Repaint();
            }

            // 订阅streaming事件
            currentBot.OnStreamingMessage += OnStreamingMessageReceived;
        }

        private void OnDisable()
        {
            // 取消订阅streaming事件
            var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
            currentBot.OnStreamingMessage -= OnStreamingMessageReceived;

            // 窗口关闭时确保取消任何进行中的请求
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }

        private void OnGUI()
        {
            // 更新聊天区域视图宽度
            chatAreaUI.SetViewWidth(position.width);

            float totalHeight = position.height;
            float toolbarHeight = EditorStyles.toolbar.fixedHeight;
            float statusAreaHeight = isProcessing ? STATUS_HEIGHT : 0;
            float inputAreaTotalHeight = inputAreaUI.GetHeight() + 20; // 20为边距
            float chatAreaHeight = totalHeight - toolbarHeight - inputAreaTotalHeight - statusAreaHeight;

            toolbarUI.Draw();
            
            if (isCreatingNew)
            {
                newChatbotUI.Draw();
                return;
            }

            EditorGUILayout.BeginVertical();
            
            GUILayout.Space(toolbarHeight);
            
            var chatHistory = ChatbotManager.Instance.GetCurrentChatbot().GetChatHistory();
            chatAreaUI.Draw(chatAreaHeight, chatHistory, currentStreamingMessage, isStreaming);
            
            if (isProcessing)
            {
                DrawStatusArea();
            }
            
            inputAreaUI.Draw(inputAreaTotalHeight, isProcessing);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawStatusArea()
        {
            Rect statusRect = GUILayoutUtility.GetRect(position.width, STATUS_HEIGHT);
            GUI.Box(statusRect, "", statusStyle);

            // 状态文本区域
            Rect textRect = new Rect(statusRect.x + 10, statusRect.y, statusRect.width - 200, STATUS_HEIGHT);
            
            var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
            if (currentBot.HasPendingMessage)
            {
                GUI.Label(textRect, "有未完成的对话...", statusStyle);
                // 继续按钮
                Rect continueRect = new Rect(statusRect.xMax - 220, statusRect.y + 5, 150, 20);
                if (GUI.Button(continueRect, "继续之前对话"))
                {
                    ContinueMessage();
                }
            }
            else
            {
                GUI.Label(textRect, isStreaming ? "AI正在回答..." : "AI思考中...", statusStyle);
            }

            // 取消按钮
            Rect cancelRect = new Rect(statusRect.xMax - 70, statusRect.y + 5, 60, 20);
            if (GUI.Button(cancelRect, "取消"))
            {
                CancelCurrentRequest();
            }
        }

        private void OnStreamingMessageReceived(ChatMessage message)
        {
            currentStreamingMessage = message;
            isStreaming = true;
            chatAreaUI.ScrollToBottom();
            Repaint();
        }

        private void CreateNewChatbot(string name, string description, string prompt)
        {
            try
            {
                string id = "custom_" + Guid.NewGuid().ToString("N");
                ChatbotManager.Instance.CreateCustomChatbot(id, name, description, prompt);
                ChatbotManager.Instance.SwitchChatbot(id);
                isCreatingNew = false;
                Repaint();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"创建助手失败: {ex.Message}", "确定");
            }
        }

        private void CancelCurrentRequest()
        {
            var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
            if (currentBot.HasPendingMessage)
            {
                currentBot.ClearPendingState();
                isProcessing = false;
                isStreaming = false;
                currentStreamingMessage = null;
                Repaint();
            }
            else if (isProcessing)
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = null;
                isProcessing = false;
                isStreaming = false;
                currentStreamingMessage = null;
                Repaint();
            }
        }

        private async void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            isProcessing = true;
            chatAreaUI.ScrollToBottom();
            isStreaming = false;
            currentStreamingMessage = null;

            // 创建新的CancellationTokenSource
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            Repaint();

            try
            {
                await ChatbotManager.Instance.GetCurrentChatbot().SendMessageAsync(message, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("AI响应已取消");
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取AI响应时出错: {ex.Message}");
                EditorUtility.DisplayDialog("错误", "无法获取AI响应，请查看控制台了解详细信息。", "确定");
            }
            finally
            {
                cancellationTokenSource = null;
                isProcessing = false;
                isStreaming = false;
                currentStreamingMessage = null;
                Repaint();
            }
        }

        private async void ContinueMessage()
        {
            if (!isProcessing) return;

            try
            {
                // 创建新的CancellationTokenSource
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();

                await ChatbotManager.Instance.GetCurrentChatbot().ContinueMessageAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("AI响应已取消");
            }
            catch (Exception ex)
            {
                Debug.LogError($"继续对话时出错: {ex}");
                EditorUtility.DisplayDialog("错误", "无法继续对话，请查看控制台了解详细信息。", "确定");
            }
            finally
            {
                cancellationTokenSource = null;
                isProcessing = false;
                isStreaming = false;
                currentStreamingMessage = null;
                Repaint();
            }
        }
    }
}
