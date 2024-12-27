using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityAIHelper.Editor.UI
{
    public class ToolbarUI : UIComponentBase
    {
        private readonly AIHelperWindow window;
        private VisualElement root;
        private DropdownField chatbotDropdown;
        private DropdownField sessionDropdown;
        private Button newSessionButton;
        private Button clearHistoryButton;
        private Button settingsButton;
        private string defaultSessionName = "新会话";

        // 事件
        public event System.Action OnCreateNewChatbot;
        public event System.Action OnClearHistory;
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
            sessionDropdown = root.Q<DropdownField>("session-dropdown");
            newSessionButton = root.Q<Button>("new-session-button");
            clearHistoryButton = root.Q<Button>("clear-history-button");
            settingsButton = root.Q<Button>("settings-button");

            // 初始化下拉菜单
            UpdateChatbotDropdown();
            UpdateSessionDropdown();

            // 绑定事件
            chatbotDropdown.RegisterValueChangedCallback(OnChatbotDropdownValueChanged);
            sessionDropdown.RegisterValueChangedCallback(OnSessionDropdownValueChanged);
            newSessionButton.clicked += CreateNewSession;
            clearHistoryButton.clicked += () => 
            {
                if (EditorUtility.DisplayDialog("确认", "是否清空所有对话记录？", "确定", "取消"))
                {
                    OnClearHistory?.Invoke();
                }
            };
            settingsButton.clicked += () => OnOpenSettings?.Invoke();
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
                var choices = new List<string>();
                
                foreach (var session in sessions)
                {
                    var title = string.IsNullOrEmpty(session.Value) ? defaultSessionName : session.Value;
                    choices.Add(title);
                }

                sessionDropdown.choices = choices;
                
                // 设置当前会话
                var currentSession = window.currentChatbot.Session;
                var currentTitle = string.IsNullOrEmpty(currentSession.title) ? 
                    defaultSessionName : currentSession.title;
                sessionDropdown.value = currentTitle;
                
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

        private void OnSessionDropdownValueChanged(ChangeEvent<string> evt)
        {
            var sessions = window.currentChatbot?.GetSessionList();
            if (sessions != null)
            {
                var session = sessions.FirstOrDefault(s => 
                    evt.newValue == (string.IsNullOrEmpty(s.Value) ? defaultSessionName : s.Value));
                
                if (!string.IsNullOrEmpty(session.Key))
                {
                    window.currentChatbot.SwitchSession(session.Key);
                    window.MarkDirty(AIHelperDirtyFlag.MessageList | AIHelperDirtyFlag.Session);
                }
            }
        }

        private void CreateNewSession()
        {
            if (window.currentChatbot != null)
            {
                window.currentChatbot.CreateSession("");
                window.MarkDirty(AIHelperDirtyFlag.MessageList | AIHelperDirtyFlag.SessionList);
            }
        }

        public override void OnUpdateUI()
        {
            if (window.IsDirty(AIHelperDirtyFlag.Chatbot))
            {
                UpdateChatbotDropdown();
                UpdateSessionDropdown();
            }
            else if (window.IsDirty(AIHelperDirtyFlag.ChatbotList))
            {
                UpdateChatbotDropdown();
                UpdateSessionDropdown();
            }
            else if (window.IsDirty(AIHelperDirtyFlag.Session | AIHelperDirtyFlag.SessionList))
            {
                UpdateSessionDropdown();
            }
        }
    }
}
