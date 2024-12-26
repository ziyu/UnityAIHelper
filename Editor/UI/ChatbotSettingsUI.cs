using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using System.Linq;

namespace UnityAIHelper.Editor.UI
{
    public class ChatbotSettingsUI
    {
        private readonly AIHelperWindow window;
        private VisualElement root;
        private TextField nameField;
        private ResizableTextArea descriptionField;
        private ResizableTextArea systemPromptField;
        private Button saveButton;
        private Button cancelButton;

        public event Action OnSave;
        public event Action OnCancel;

        private IChatbot currentBot;

        public ChatbotSettingsUI(AIHelperWindow window)
        {
            this.window = window;
            Initialize();
        }

        private void Initialize()
        {
            // 加载UXML和样式
            var visualTree = PackageAssetLoader.LoadUIAsset<VisualTreeAsset>("ChatbotSettingsUI.uxml");
            var styleSheet = PackageAssetLoader.LoadUIAsset<StyleSheet>("ChatbotSettingsUI.uss");

            root = new VisualElement();
            visualTree.CloneTree(root);
            root.styleSheets.Add(styleSheet);

            // 获取引用
            nameField = root.Q<TextField>("name-field");
            descriptionField = root.Q<ResizableTextArea>("description-field");
            systemPromptField = root.Q<ResizableTextArea>("system-prompt-field");
            saveButton = root.Q<Button>("save-button");
            cancelButton = root.Q<Button>("cancel-button");

           // 绑定事件
           saveButton.clicked += () =>
           {
               SaveSettings();
               OnSave?.Invoke();
           };
           cancelButton.clicked += () =>
           {
               OnCancel?.Invoke();
           };
        }

        public void Show(IChatbot chatbot)
        {
            currentBot = chatbot;
            LoadSettings();
            root.style.display = DisplayStyle.Flex;
            window.Repaint();
        }

        public void Hide()
        {
            root.style.display = DisplayStyle.None;
            window.Repaint();
        }

        private void LoadSettings()
        {
            if (currentBot == null) return;

            nameField.value = currentBot.Name;
            descriptionField.text = currentBot.Description;
            systemPromptField.text = currentBot.SystemPrompt;
        }

        private void SaveSettings()
        {
            if (currentBot == null) return;

            if (currentBot is ChatbotBase chatbot)
            {
                chatbot.UpdateSettings(
                    nameField.value,
                    descriptionField.text,
                    systemPromptField.text
                );
            }
        }

        public VisualElement GetRoot()
        {
            return root;
        }
    }
}
