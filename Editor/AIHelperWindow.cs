using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityLLMAPI.Models;
using UnityAIHelper.Editor.UI;
using UnityEngine.UIElements;

namespace UnityAIHelper.Editor
{
    public class AIHelperWindow : EditorWindow
    {
        // UI组件
        private ChatAreaUI chatAreaUI;
        private InputAreaUI inputAreaUI;
        private ToolbarUI toolbarUI;
        private StatusAreaUI statusAreaUI;
        private NewChatbotUI newChatbotUI;
        private ChatbotSettingsUI settingsUI;

        // UI Elements
        private VisualElement root;
        private VisualElement modalOverlay;
        private VisualElement modalContainer;
        private VisualElement chatArea;
        private VisualElement inputArea;

        // 状态
        internal bool isProcessing = false;
        internal IChatbot currentChatbot;
        internal ChatMessage currentStreamingMessage;
        internal bool isStreaming = false;
        
        
        private CancellationTokenSource cancellationTokenSource;
        private AIHelperDirtyFlag dirtyFlag;
        private List<IUIComponent> _subUIComponents=new();

        [MenuItem("Window/AI Helper")]
        public static void ShowWindow()
        {
            GetWindow<AIHelperWindow>("AI Helper");
        }

        private void CreateGUI()
        {
            root = rootVisualElement;
            root.style.flexGrow = 1;
            root.pickingMode = PickingMode.Position;

            // 加载UXML和USS
            var visualTree = PackageAssetLoader.LoadUIAsset<VisualTreeAsset>("AIHelperWindow.uxml");
            var styleSheet = PackageAssetLoader.LoadUIAsset<StyleSheet>("AIHelperWindow.uss");
            
            visualTree.CloneTree(root);
            root.styleSheets.Add(styleSheet);

            // 获取UI元素引用
            modalOverlay = root.Q<VisualElement>("modal-overlay");
            modalContainer = root.Q<VisualElement>("modal-container");
            inputArea = root.Q<VisualElement>("input-area");
            chatArea = root.Q<VisualElement>("chat-area");

            // 初始化UI组件
            chatAreaUI = new ChatAreaUI(this, chatArea);
            inputAreaUI = new InputAreaUI(this, inputArea);
            toolbarUI = new ToolbarUI(this, root.Q<VisualElement>("toolbar"));
            statusAreaUI = new StatusAreaUI(this,  root.Q<VisualElement>("status-area"));
            newChatbotUI = new NewChatbotUI(this);
            settingsUI = new ChatbotSettingsUI(this);
      
            _subUIComponents.Clear();
            _subUIComponents.Add(chatAreaUI);
            _subUIComponents.Add(inputAreaUI);
            _subUIComponents.Add(toolbarUI);
            _subUIComponents.Add(statusAreaUI);

            // 绑定事件
            inputAreaUI.OnSendMessage += SendMessage;
            toolbarUI.OnCreateNewChatbot += ShowNewChat;
            toolbarUI.OnOpenSettings += ShowSettings;
            newChatbotUI.OnCancel += HideModal;
            newChatbotUI.OnCreate += CreateNewChatbot;
            settingsUI.OnSave += HideModal;
            settingsUI.OnCancel += HideModal;
            // 绑定消息操作事件
            chatAreaUI.OnDeleteMessage += HandleDeleteMessage;




            // 注册全局事件处理
            root.RegisterCallback<MouseDownEvent>(evt => 
            {
                if (evt.target is VisualElement element)
                {
                    element.Focus();
                }
            });

   

            // 点击遮罩层关闭modal
            modalOverlay.RegisterCallback<MouseDownEvent>(evt => 
            {
                if (evt.target == modalOverlay)
                {
                    HideModal();
                    evt.StopPropagation();
                }
            });

            dirtyFlag = AIHelperDirtyFlag.All;
            Update();
        }

        private void OnEnable()
        {
            // 检查UnityLLMAPI配置
            var config = UnityLLMAPI.Config.OpenAIConfig.Instance;
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<UnityLLMAPI.Config.OpenAIConfig>();
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateAsset(config, "Assets/Resources/OpenAIConfig.asset");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("已创建默认OpenAI配置文件，请在Project窗口中设置相关参数");
            }
            
            // 订阅ChatbotManager的事件
            ChatbotManager.Instance.OnChatbotChanged += OnChatbotChanged;
            ChatbotManager.Instance.OnChatbotListChanged += OnChatbotListChanged;
            // 检查是否有未完成的对话
            currentChatbot = ChatbotManager.Instance.GetCurrentChatbot();
            if (currentChatbot.HasPendingMessage)
            {
                isProcessing = true;
            }

            RegisterChatbotEvents();
        }

        private void OnDisable()
        {
            UnRegisterChatbotEvents();
            currentChatbot = null;

            // 取消订阅ChatbotManager事件
            ChatbotManager.Instance.OnChatbotChanged -= OnChatbotChanged;
            ChatbotManager.Instance.OnChatbotListChanged -= OnChatbotListChanged;

            // 窗口关闭时确保取消任何进行中的请求
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();

        }

        private void HandleDeleteMessage(ChatMessageInfo message)
        {
            if(isProcessing)return;
            if (EditorUtility.DisplayDialog("确认删除", "确定要删除这条消息吗？", "确定", "取消"))
            {
                var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
                currentBot.DeleteMessage(message);
                MarkDirty(AIHelperDirtyFlag.MessageList);
            }
        }

        private void OnStreamingMessageReceived(ChatMessage message)
        {
            currentStreamingMessage = message;
            isStreaming = true;
            MarkDirty(AIHelperDirtyFlag.StreamingMessage);
            
        }


        private void Update()
        {
            if(dirtyFlag== AIHelperDirtyFlag.None)return;
            UpdateUI();
            dirtyFlag = AIHelperDirtyFlag.None;
        }

        private void UpdateUI()
        {
            foreach (var subUI in _subUIComponents)
            {
                subUI.OnUpdateUI();
            }
            Repaint();
        }

        private void CreateNewChatbot(string name, string description, string prompt)
        {
            try
            {
                string id = "custom_" + Guid.NewGuid().ToString("N");
                ChatbotManager.Instance.CreateCustomChatbot(id, name, description, prompt);
                ChatbotManager.Instance.SwitchChatbot(id);
                OnChatbotChanged();
                HideModal();
                MarkDirty(AIHelperDirtyFlag.ChatbotList);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"创建助手失败: {ex.Message}", "确定");
            }
        }

        internal void CancelCurrentRequest()
        {
            if (currentChatbot.HasPendingMessage)
            {
                currentChatbot.ClearPendingState();
                isProcessing = false;
                isStreaming = false;
                currentStreamingMessage = null;
                MarkDirty(AIHelperDirtyFlag.SendingMessage|AIHelperDirtyFlag.MessageList|AIHelperDirtyFlag.StreamingMessage);
            }
            else if (isProcessing)
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = null;
                isProcessing = false;
                isStreaming = false;
                currentStreamingMessage = null;
                MarkDirty(AIHelperDirtyFlag.SendingMessage|AIHelperDirtyFlag.MessageList|AIHelperDirtyFlag.StreamingMessage);
            }
        }

        private async void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            isProcessing = true;
            isStreaming = false;
            currentStreamingMessage = null;
            var msgCount = currentChatbot.GetChatHistory().Count;
            var isNewDialog = msgCount is 0 or 1;

            // 创建新的CancellationTokenSource
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();
            MarkDirty(AIHelperDirtyFlag.SendingMessage|AIHelperDirtyFlag.MessageList|AIHelperDirtyFlag.StreamingMessage);
            try
            {
                var result=await currentChatbot.SendMessageAsync(message, cancellationTokenSource.Token);
                if (!string.IsNullOrEmpty(result.content)&&isNewDialog)
                {
                    RenameSessionByAI();
                }
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
                MarkDirty(AIHelperDirtyFlag.SendingMessage|AIHelperDirtyFlag.MessageList|AIHelperDirtyFlag.StreamingMessage);
            }
        }

        internal async void ContinueMessage()
        {
            if (!currentChatbot.HasPendingMessage) return;
            MarkDirty(AIHelperDirtyFlag.SendingMessage|AIHelperDirtyFlag.StreamingMessage);
            try
            {
                // 创建新的CancellationTokenSource
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();

                await currentChatbot.ContinueMessageAsync(cancellationTokenSource.Token);
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
                MarkDirty(AIHelperDirtyFlag.SendingMessage|AIHelperDirtyFlag.MessageList|AIHelperDirtyFlag.StreamingMessage);
            }
        }

        private void OnChatbotChanged()
        {
            UnRegisterChatbotEvents();
            currentChatbot = ChatbotManager.Instance.GetCurrentChatbot();
            RegisterChatbotEvents();
            MarkDirty(AIHelperDirtyFlag.Chatbot);
        }
        
        void RegisterChatbotEvents()
        {
            if (currentChatbot != null)
            {
                currentChatbot.OnStreamingMessage -= OnStreamingMessageReceived;
                currentChatbot.OnShouldExecuteToolEvent -= OnShouldExecuteTool;
                currentChatbot.OnChatStateChangedEvent -= OnChatStageChange;
                currentChatbot.OnStreamingMessage += OnStreamingMessageReceived;
                currentChatbot.OnChatStateChangedEvent += OnChatStageChange;
                currentChatbot.OnShouldExecuteToolEvent += OnShouldExecuteTool;
            }
        }

        void UnRegisterChatbotEvents()
        {
            if (currentChatbot != null)
            {
                currentChatbot.OnStreamingMessage -= OnStreamingMessageReceived;
                currentChatbot.OnShouldExecuteToolEvent -= OnShouldExecuteTool;
                currentChatbot.OnChatStateChangedEvent -= OnChatStageChange;
            }
        }

        private void OnChatbotListChanged()
        {
            MarkDirty(AIHelperDirtyFlag.ChatbotList);
        }

        private void ShowSettings()
        {
            modalContainer.Clear();
            modalContainer.Add(settingsUI.GetRoot());
            settingsUI.Show(currentChatbot);
            ShowModal();
        }

        private void ShowNewChat()
        {
            modalContainer.Clear();
            modalContainer.Add(newChatbotUI.GetRoot());
            ShowModal();
        }

        private void OnClearHistory()
        {
            ChatbotManager.Instance.GetCurrentChatbot().ClearHistory();
            MarkDirty(AIHelperDirtyFlag.MessageList);
        }

        private void ShowModal()
        {
            modalOverlay.style.display = DisplayStyle.Flex;
            modalContainer.style.display = DisplayStyle.Flex;
        }

        private void HideModal()
        {
            modalOverlay.style.display = DisplayStyle.None;
            modalContainer.style.display = DisplayStyle.None;
        }

        public void MarkDirty(AIHelperDirtyFlag flag)
        {
            dirtyFlag |= flag;
        }

        public bool IsDirty( AIHelperDirtyFlag flag= AIHelperDirtyFlag.None)
        {
            if (flag == AIHelperDirtyFlag.None) return dirtyFlag != AIHelperDirtyFlag.None;
            
            return  (dirtyFlag & flag) == flag;
        }

        internal void ReloadSession()
        {
            if (currentChatbot != null)
            {
                currentChatbot.ReloadSession();
                MarkDirty(AIHelperDirtyFlag.MessageList);
            }
        }

        async void RenameSessionByAI()
        {
            var result = await UtilsAI.GenerateDialogName(currentChatbot.GetChatHistory());
            if (!string.IsNullOrEmpty(result))
            {
                // 更新会话名称
                currentChatbot.RenameSession(currentChatbot.Session.sessionId, result);
                MarkDirty(AIHelperDirtyFlag.SessionList);
            }
        }

        async Task<bool> OnShouldExecuteTool(ChatMessageInfo messageInfo, ToolCall toolCall)
        {
            if (!isProcessing) return false;
            return await chatAreaUI.OnShouldExecuteTool(messageInfo,toolCall);
        }

        void OnChatStageChange(ChatStateChangedEventArgs e)
        {
            if (e.NewMessageState == ChatMessageState.Succeeded || e.NewMessageState == ChatMessageState.ProcessingTool)
            {
                isStreaming = false;
                currentStreamingMessage = null;
                MarkDirty(AIHelperDirtyFlag.StreamingMessage|AIHelperDirtyFlag.MessageList);
            }
        }
    }
}
