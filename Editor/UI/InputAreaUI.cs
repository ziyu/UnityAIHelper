using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;

namespace UnityAIHelper.Editor.UI
{
    public class InputAreaUI:UIComponentBase
    {
        private readonly AIHelperWindow window;
        private VisualElement root;
        private ResizableTextArea inputTextField;
        private Button sendButton;
        private string userInput = "";

        private bool _lastIsProcessing = false;

        public event Action<string> OnSendMessage;

        public InputAreaUI(AIHelperWindow window, VisualElement root)
        {
            this.window = window;
            this.root = root;
            Initialize();
        }

        void Initialize()
        {
            var styleSheet = PackageAssetLoader.LoadUIAsset<StyleSheet>("InputAreaUI.uss");
            root.styleSheets.Add(styleSheet);

            // Get references
            inputTextField = root.Q<ResizableTextArea>("input-text-field");
            sendButton = root.Q<Button>("send-button");

            // Setup input handling
            SetupInputHandling();

            SetProcessing(window.isProcessing);
            _lastIsProcessing = window.isProcessing;
        }

        private void SetupInputHandling()
        {
            // 文本变化事件
            inputTextField.RegisterValueChangedCallback(evt =>
            {
                userInput = evt.newValue;
                window.Repaint();
            });

            // Send button click
            sendButton.clicked += SendMessage;

            // Enter key handling
            inputTextField.RegisterCallback<KeyDownEvent>(evt => 
            {
                if (evt.keyCode == KeyCode.Return && !evt.shiftKey && 
                    !string.IsNullOrEmpty(GetText().Trim()) &&
                    EditorWindow.focusedWindow == window)
                {
                    SendMessage();
                    evt.StopPropagation();
                    evt.PreventDefault();
                }
            });
        }

        private void SendMessage()
        {
            string trimmedInput = userInput.Trim();
            if (!string.IsNullOrEmpty(trimmedInput))
            {
                string message = trimmedInput;
                userInput = "";
                inputTextField.text = "";
                OnSendMessage?.Invoke(message);
            }
        }

        void SetProcessing(bool isProcessing)
        {
            if (inputTextField != null)
            {
                inputTextField.SetEnabled(!isProcessing);
            }
            if (sendButton != null)
            {
                sendButton.SetEnabled(!isProcessing);
            }
        }

        public string GetText()
        {
            return userInput;
        }

        public void SetText(string text)
        {
            userInput = text ?? "";
            if (inputTextField != null)
            {
                inputTextField.text = userInput;
            }
            window.Repaint();
        }

        public void Clear()
        {
            SetText("");
        }
        

        public override void OnUpdateUI()
        {
            if(_lastIsProcessing==window.isProcessing)return;
            SetProcessing(window.isProcessing);
            _lastIsProcessing = window.isProcessing;
        }
    }
}
