using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityAIHelper.Editor.UI
{
    public static class EditorInputDialog
    {
        public static void Show(string title, string message, string defaultText, System.Action<string> callback, Func<string, Task<string>> aiGenerateCallback = null)
        {
            var window = EditorWindow.CreateInstance<InputDialog>();
            window.titleContent = new GUIContent(title);
            window.Initialize(message, defaultText, callback, aiGenerateCallback);
            window.ShowModal();
        }

        private class InputDialog : EditorWindow
        {
            private System.Action<string> callback;
            private Func<string, Task<string>> aiGenerateCallback;

            public void Initialize(string message, string defaultText, System.Action<string> callback, Func<string, Task<string>> aiGenerateCallback)
            {
                this.callback = callback;
                this.aiGenerateCallback = aiGenerateCallback;

                // Load UXML
                var visualTree = PackageAssetLoader.LoadUIAsset<VisualTreeAsset>("EditorInputDialog.uxml");
                if (visualTree == null)
                {
                    Debug.LogError("Failed to load EditorInputDialog.uxml");
                    return;
                }

                var root = visualTree.CloneTree();
                rootVisualElement.Add(root);

                // Load USS
                var styleSheet = PackageAssetLoader.LoadUIAsset<StyleSheet>("EditorInputDialog.uss");
                if (styleSheet != null)
                {
                    rootVisualElement.styleSheets.Add(styleSheet);
                }
                else
                {
                    Debug.LogWarning("Failed to load EditorInputDialog.uss");
                }

                // Set UI elements
                var titleLabel = root.Q<Label>(className: "dialog-title");
                var messageLabel = root.Q<Label>(className: "dialog-message");
                var inputField = root.Q<TextField>(className: "dialog-input");

                if (titleLabel == null || messageLabel == null || inputField == null)
                {
                    Debug.LogError("Failed to find required UI elements in EditorInputDialog.uxml");
                    return;
                }

                titleLabel.text = titleContent.text;
                messageLabel.text = message;
                inputField.value = defaultText;

                // Focus input field
                inputField.Focus();

                // Handle confirm button
                root.Q<Button>("confirm-button").clicked += () =>
                {
                    callback?.Invoke(inputField.value);
                    Close();
                };

                // Handle cancel button
                root.Q<Button>("cancel-button").clicked += () =>
                {
                    callback?.Invoke(null);
                    Close();
                };

                // Handle Enter key
                inputField.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        callback?.Invoke(inputField.value);
                        Close();
                    }
                });

                // Handle AI generate button
                var aiGenerateButton = root.Q<Button>("ai-generate-button");
                if (aiGenerateButton != null)
                {
                    aiGenerateButton.style.display = aiGenerateCallback != null ? DisplayStyle.Flex : DisplayStyle.None;
                    if (aiGenerateCallback != null)
                    {
                        aiGenerateButton.clicked += async () =>
                        {
                            try
                            {
                                aiGenerateButton.text = "生成中...";
                                aiGenerateButton.SetEnabled(false);
                                
                                var generatedName = await aiGenerateCallback(inputField.value);
                                if (!string.IsNullOrEmpty(generatedName))
                                {
                                    inputField.value = generatedName;
                                    inputField.Focus();
                                }
                            }
                            finally
                            {
                                aiGenerateButton.text = "AI生成";
                                aiGenerateButton.SetEnabled(true);
                            }
                        };
                    }
                }

                // Handle Escape key
                rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Escape)
                    {
                        callback?.Invoke(null);
                        Close();
                    }
                });

                // Set window size
                minSize = new Vector2(300, 150);
                maxSize = new Vector2(400, 200);
                position = new Rect(
                    (Screen.currentResolution.width - minSize.x) / 2,
                    (Screen.currentResolution.height - minSize.y) / 2,
                    minSize.x,
                    minSize.y
                );
            }
        }
    }
}
