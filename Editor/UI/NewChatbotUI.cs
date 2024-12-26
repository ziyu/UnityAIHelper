using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;

namespace UnityAIHelper.Editor.UI
{
    public class NewChatbotUI
    {
        private readonly AIHelperWindow window;
        private VisualElement root;
        private TextField nameField;
        private ResizableTextArea descriptionField;
        private ResizableTextArea systemPromptField;
        private Button createButton;
        private Button cancelButton;

        public event Action<string, string, string> OnCreate;
        public event Action OnCancel;

        public NewChatbotUI(AIHelperWindow window)
        {
            this.window = window;
            Initialize();
        }

        private void Initialize()
        {
            // 加载UXML和样式
            var visualTree = PackageAssetLoader.LoadUIAsset<VisualTreeAsset>("NewChatbotUI.uxml");
            var styleSheet = PackageAssetLoader.LoadUIAsset<StyleSheet>("NewChatbotUI.uss");
            
            root = new VisualElement();
            visualTree.CloneTree(root);
            root.styleSheets.Add(styleSheet);

            // 获取引用
            nameField = root.Q<TextField>("name-field");
            descriptionField = root.Q<ResizableTextArea>("description-field");
            systemPromptField = root.Q<ResizableTextArea>("system-prompt-field");
            createButton = root.Q<Button>("create-button");
            cancelButton = root.Q<Button>("cancel-button");

            // 绑定事件
            createButton.clicked += () =>
            {
                if (ValidateInput())
                {
                    OnCreate?.Invoke(
                        nameField.value,
                        descriptionField.text,
                        systemPromptField.text
                    );
                    ClearFields();
                }
            };

            cancelButton.clicked += () =>
            {
                ClearFields();
                OnCancel?.Invoke();
            };
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(nameField.value))
            {
                EditorUtility.DisplayDialog("错误", "请输入名称", "确定");
                return false;
            }

            return true;
        }

        private void ClearFields()
        {
            nameField.value = "";
            descriptionField.text = "";
            systemPromptField.text = "";
        }

        public VisualElement GetRoot()
        {
            return root;
        }
    }
}
