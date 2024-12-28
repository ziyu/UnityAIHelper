using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityLLMAPI.Models;
using UnityLLMAPI.Utils.Json;
using System.Threading.Tasks;

namespace UnityAIHelper.Editor.UI
{
    public class ToolbarUI : UIComponentBase
    {
        private readonly AIHelperWindow window;
        private VisualElement root;
        private DropdownField chatbotDropdown;
        private IndexedDropdownField sessionDropdown;
        private Button newSessionButton;
        private Button deleteSessionButton;
        private Button settingsButton;
        private Button renameSessionButton;
        private string defaultSessionName = "新对话";
        private List<string> currentSessionIds = new List<string>();

        // 事件
        public event System.Action OnCreateNewChatbot;
        public event System.Action OnDeleteSession;
        public event System.Action OnOpenSettings;

        public ToolbarUI(AIHelperWindow window, VisualElement root)
        {
            this.window = window;
            this.root = root;
            Initialize();
        }

        private void Initialize()
        {
            // 加载USS
            var styleSheet = PackageAssetLoader.LoadUIAsset<StyleSheet>("ToolbarUI.uss");
            root.styleSheets.Add(styleSheet);

            // 创建工具栏
            var toolbar = root.Q<UnityEditor.UIElements.Toolbar>();

            // 获取UI元素引用
            chatbotDropdown = root.Q<DropdownField>("chatbot-dropdown");
            sessionDropdown = root.Q<IndexedDropdownField>("session-dropdown");
            newSessionButton = root.Q<Button>("new-session-button");
            deleteSessionButton = root.Q<Button>("clear-history-button");
            settingsButton = root.Q<Button>("settings-button");
            renameSessionButton = root.Q<Button>("rename-session-button");

            // 初始化下拉菜单
            UpdateChatbotDropdown();
            UpdateSessionDropdown();

            // 绑定事件
            chatbotDropdown.RegisterValueChangedCallback(OnChatbotDropdownValueChanged);
            sessionDropdown.OnSelectedIndexChanged -= OnSessionIndexChanged;
            sessionDropdown.OnSelectedIndexChanged += OnSessionIndexChanged;
            deleteSessionButton.clicked += () =>
            {
                if (window.currentChatbot == null || sessionDropdown.Index < 0)
                    return;

                var sessionId = currentSessionIds[sessionDropdown.Index];
                if (EditorUtility.DisplayDialog("确认", "是否删除当前对话？", "确定", "取消"))
                {
                    window.currentChatbot.DeleteSession(sessionId);
                    OnDeleteSession?.Invoke();
                    UpdateSessionDropdown();
                    window.MarkDirty(AIHelperDirtyFlag.SessionList | AIHelperDirtyFlag.MessageList);
                }
            };
            settingsButton.clicked += () => OnOpenSettings?.Invoke();
            newSessionButton.clicked += CreateNewSession;
            renameSessionButton.clicked += RenameCurrentSession;
        }

        private void UpdateChatbotDropdown()
        {
            var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
            var choices = new List<string>();
            var chatbots = ChatbotManager.Instance.Chatbots.Values.ToList();
            
            // 添加现有chatbot
            foreach (var bot in chatbots)
            {
                choices.Add(bot.Name);
            }
            
            // 添加分隔符和创建新助手选项
            choices.Add("---");
            choices.Add("创建新助手...");
            
            chatbotDropdown.choices = choices;
            chatbotDropdown.value = currentBot.Name;
        }

        private void UpdateSessionDropdown()
        {
            var sessions = window.currentChatbot?.GetSessionList();
            if (sessions != null)
            {
                currentSessionIds = sessions.Select(s => s.SessionId).ToList();
                var choices = sessions
                    .Select(s => string.IsNullOrEmpty(s.Title) ? defaultSessionName : s.Title)
                    .ToList();

                sessionDropdown.Choices = choices;
                
                // 设置当前会话
                var currentSession = window.currentChatbot.Session;
                var currentIndex = currentSessionIds.IndexOf(currentSession.sessionId);
                sessionDropdown.SetIndexWithoutNotify(currentIndex);
                
                sessionDropdown.style.display = DisplayStyle.Flex;
                newSessionButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                sessionDropdown.style.display = DisplayStyle.None;
                newSessionButton.style.display = DisplayStyle.None;
            }
        }

        private void OnChatbotDropdownValueChanged(ChangeEvent<string> evt)
        {
            if (evt.newValue == "---")
            {
                // 如果选择了分隔符，恢复到之前的选择
                chatbotDropdown.value = evt.previousValue;
                return;
            }

            if (evt.newValue == "创建新助手...")
            {
                // 恢复到之前的选择并触发创建新助手事件
                chatbotDropdown.value = evt.previousValue;
                OnCreateNewChatbot?.Invoke();
                return;
            }

            // 切换到选中的chatbot
            var selectedBot = ChatbotManager.Instance.Chatbots.Values.FirstOrDefault(b => b.Name == evt.newValue);
            if (selectedBot != null)
            {
                ChatbotManager.Instance.SwitchChatbot(selectedBot.Id);
                UpdateSessionDropdown();
                window.Repaint();

                // 显示删除选项（仅对非默认chatbot）
                if (selectedBot.Id != "unity_helper")
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("删除当前助手"), false, () =>
                    {
                        if (EditorUtility.DisplayDialog("确认删除",
                            $"是否确定删除助手 '{selectedBot.Name}'？\n此操作无法撤销。",
                            "删除", "取消"))
                        {
                            ChatbotManager.Instance.RemoveChatbot(selectedBot.Id);
                            UpdateChatbotDropdown();
                            window.Repaint();
                        }
                    });
                    menu.ShowAsContext();
                }
            }
        }

        private void OnSessionIndexChanged(int index)
        {
            if (index >= 0 && index < currentSessionIds.Count)
            {
                var sessionId = currentSessionIds[index];
                window.currentChatbot.SwitchSession(sessionId);
                window.MarkDirty(AIHelperDirtyFlag.MessageList | AIHelperDirtyFlag.Session);
            }
        }

        private void CreateNewSession()
        {
            if (window.currentChatbot == null)return;
            
            window.currentChatbot.CreateSession("");
            window.MarkDirty(AIHelperDirtyFlag.MessageList | AIHelperDirtyFlag.SessionList);
        }

        private void RenameCurrentSession()
        {
            if (window.currentChatbot == null || sessionDropdown.Index < 0)
                return;

            var sessionId = currentSessionIds[sessionDropdown.Index];
            var currentTitle = sessionDropdown.Choices[sessionDropdown.Index];

            EditorInputDialog.Show("重命名会话", "请输入新的会话名称：", currentTitle, newTitle =>
            {
                if (newTitle != null)
                {
                    window.currentChatbot.RenameSession(sessionId, newTitle);
                    UpdateSessionDropdown();
                    window.MarkDirty(AIHelperDirtyFlag.SessionList);
                }
            },
            async currentText => 
            {
                try
                {
                    var chatHistory = window.currentChatbot.GetChatHistory();
                    if (chatHistory != null)
                    {
                        var generatedName = await UtilsAI.GenerateDialogName(chatHistory);
                        if (!string.IsNullOrEmpty(generatedName))
                        {
                            return generatedName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to generate AI session name: {ex.Message}");
                }
                return null;
            });
        }

        public override void OnUpdateUI()
        {
            if (window.IsDirty(AIHelperDirtyFlag.Chatbot)||window.IsDirty(AIHelperDirtyFlag.ChatbotList))
            {
                UpdateChatbotDropdown();
                UpdateSessionDropdown();
            }
            else if (window.IsDirty(AIHelperDirtyFlag.Session )||window.IsDirty(AIHelperDirtyFlag.SessionList ))
            {
                UpdateSessionDropdown();
            }
        }
    }
}
