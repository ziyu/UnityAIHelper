using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityAIHelper.Editor.UI
{
    public class SettingsUI
    {
        private readonly AIHelperWindow window;
        private VisualElement root;
        private TextField apiKeyField;
        private Button toggleKeyVisibilityButton;
        private TextField apiBaseUrlField;
        private TextField defaultModelField;
        private FloatField temperatureField;
        private IntegerField maxTokensField;
        private Toggle alwaysApproveReadToggle;
        private Toggle alwaysApproveWriteToggle;
        private Toggle alwaysApproveDeleteToggle;
        private ColorField primaryColorField;
        private ColorField secondaryColorField;
        private Button saveButton;
        private Button cancelButton;

        public event Action OnSave;
        public event Action OnCancel;

        public SettingsUI(AIHelperWindow window)
        {
            this.window = window;
            Initialize();
        }

        private void Initialize()
        {
            // Load UXML and styles
            var visualTree = PackageAssetLoader.LoadUIAsset<VisualTreeAsset>("SettingsUI.uxml");
            var styleSheet = PackageAssetLoader.LoadUIAsset<StyleSheet>("SettingsUI.uss");

            root = new VisualElement();
            visualTree.CloneTree(root);
            root.styleSheets.Add(styleSheet);

            // Get references
            apiKeyField = root.Q<TextField>("api-key-field");
            toggleKeyVisibilityButton = root.Q<Button>("toggle-key-visibility-button");
            apiBaseUrlField = root.Q<TextField>("api-base-url-field");
            defaultModelField = root.Q<TextField>("default-model-field");
            temperatureField = root.Q<FloatField>("temperature-field");
            maxTokensField = root.Q<IntegerField>("max-tokens-field");

            // Setup API key visibility toggle
            toggleKeyVisibilityButton.clicked += () =>
            {
                apiKeyField.isPasswordField = !apiKeyField.isPasswordField;
                toggleKeyVisibilityButton.text = apiKeyField.isPasswordField ? "⏿" : "🔒";
            };
            alwaysApproveReadToggle = root.Q<Toggle>("always-approve-read-toggle");
            alwaysApproveWriteToggle = root.Q<Toggle>("always-approve-write-toggle");
            alwaysApproveDeleteToggle = root.Q<Toggle>("always-approve-delete-toggle");
            primaryColorField = root.Q<ColorField>("primary-color-field");
            secondaryColorField = root.Q<ColorField>("secondary-color-field");
            saveButton = root.Q<Button>("save-button");
            cancelButton = root.Q<Button>("cancel-button");

            // Bind events
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

        public void Show()
        {
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
            var settings = AIHelperSettings.Instance;
            apiKeyField.value = settings.ModelProviderSetting.apiKey;
            apiBaseUrlField.value = settings.ModelProviderSetting.apiBaseUrl;
            defaultModelField.value = settings.ModelProviderSetting.defaultModel;
            temperatureField.value = settings.ModelProviderSetting.temperature;
            maxTokensField.value = settings.ModelProviderSetting.maxTokens;
            alwaysApproveReadToggle.value = settings.AlwaysApproveReadOnlyOperations;
            alwaysApproveWriteToggle.value = settings.AlwaysApproveWriteOperations;
            alwaysApproveDeleteToggle.value = settings.AlwaysApproveDeleteOperations;
            primaryColorField.value = settings.PrimaryColor;
            secondaryColorField.value = settings.SecondaryColor;
        }

        private void SaveSettings()
        {
            var settings = AIHelperSettings.Instance;
            settings.ModelProviderSetting.apiKey = apiKeyField.value;
            settings.ModelProviderSetting.apiBaseUrl = apiBaseUrlField.value;
            settings.ModelProviderSetting.defaultModel = defaultModelField.value;
            settings.ModelProviderSetting.temperature = temperatureField.value;
            settings.ModelProviderSetting.maxTokens = maxTokensField.value;
            settings.AlwaysApproveReadOnlyOperations = alwaysApproveReadToggle.value;
            settings.AlwaysApproveWriteOperations = alwaysApproveWriteToggle.value;
            settings.AlwaysApproveDeleteOperations = alwaysApproveDeleteToggle.value;
            settings.PrimaryColor = primaryColorField.value;
            settings.SecondaryColor = secondaryColorField.value;
            AIHelperSettings.Save();
        }

        public VisualElement GetRoot()
        {
            return root;
        }
    }
}