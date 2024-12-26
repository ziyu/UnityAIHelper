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
        private ChatbotSettingsUI settingsUI;

        // 状态
        private bool isProcessing = false;
        private bool isCreatingNew = false;
        private bool isShowingSettings = false;
        private bool isEditing = false;
        private ChatMessageInfo editingMessage;
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
            // 检查UnityLLMAPI配置
            var config = UnityLLMAPI.Config.OpenAIConfig.Instance;
            if (config == null)
            {
                // 创建默认配置
                config = ScriptableObject.CreateInstance<UnityLLMAPI.Config.OpenAIConfig>();
                
                // 确保Resources目录存在
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                
                // 保存配置文件
                AssetDatabase.CreateAsset(config, "Assets/Resources/OpenAIConfig.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log("已创建默认OpenAI配置文件，请在Project窗口中设置相关参数");
            }

            // 初始化UI组件
            chatAreaUI = new ChatAreaUI(this);
            inputAreaUI = new InputAreaUI(this);
            toolbarUI = new ToolbarUI(this);
            newChatbotUI = new NewChatbotUI(this);
            settingsUI = new ChatbotSettingsUI(this);

            // 绑定事件
            inputAreaUI.OnSendMessage += SendMessage;
            toolbarUI.OnCreateNewChatbot += () => { isCreatingNew = true; Repaint(); };
            toolbarUI.OnClearHistory += () => { ChatbotManager.Instance.GetCurrentChatbot().ClearHistory(); Repaint(); };
            toolbarUI.OnOpenSettings += () => { isShowingSettings = true; Repaint(); };
            newChatbotUI.OnCancel += () => { isCreatingNew = false; Repaint(); };
            newChatbotUI.OnCreate += CreateNewChatbot;
            settingsUI.OnClose += () => { isShowingSettings = false; Repaint(); };

            // 绑定消息操作事件
            chatAreaUI.OnEditMessage += HandleEditMessage;
            chatAreaUI.OnDeleteMessage += HandleDeleteMessage;

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
            float inputAreaTotalHeight = inputAreaUI.GetHeight();
            float chatAreaHeight = totalHeight - toolbarHeight - inputAreaTotalHeight - statusAreaHeight;

            toolbarUI.Draw();
            
            if (isCreatingNew)
            {
                newChatbotUI.Draw();
                return;
            }

            if (isShowingSettings)
            {
                settingsUI.Draw();
                return;
            }

            // 计算各个区域的位置
            float currentY = toolbarHeight;

            // 聊天区域
            var chatHistory = ChatbotManager.Instance.GetCurrentChatbot().GetChatHistory();
            chatAreaUI.Draw(chatAreaHeight, chatHistory, currentStreamingMessage, isStreaming);
            currentY += chatAreaHeight;

            // 状态区域
            if (isProcessing)
            {
                DrawStatusArea();
                currentY += statusAreaHeight;
            }

            // 输入区域
            if (isEditing)
            {
                inputAreaUI.SetText(editingMessage.message.content);
                inputAreaUI.Draw(inputAreaTotalHeight, false, "更新", () => UpdateMessage(editingMessage, inputAreaUI.GetText()));
            }
            else
            {
                inputAreaUI.Draw(inputAreaTotalHeight, isProcessing);
            }
        }

        private void DrawStatusArea()
        {
            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 10, 5, 5),
                    fontSize = 12
                };
            }

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

        private void HandleEditMessage(ChatMessageInfo message)
        {
            if (message.message.role != "user") return; // 只允许编辑用户消息
            
            isEditing = true;
            editingMessage = message;
            inputAreaUI.SetText(message.message.content);
            Repaint();
        }

        private void HandleDeleteMessage(ChatMessageInfo message)
        {
            if (EditorUtility.DisplayDialog("确认删除", "确定要删除这条消息吗？", "确定", "取消"))
            {
                var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
                currentBot.DeleteMessage(message);
                Repaint();
            }
        }

        private void UpdateMessage(ChatMessageInfo message, string newContent)
        {
            if (string.IsNullOrEmpty(newContent))
            {
                EditorUtility.DisplayDialog("错误", "消息内容不能为空", "确定");
                return;
            }

            var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
            currentBot.UpdateMessage(message, newContent);
            
            isEditing = false;
            editingMessage = null;
            inputAreaUI.SetText("");
            Repaint();
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
