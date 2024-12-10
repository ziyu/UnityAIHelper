using UnityEngine;
using UnityEditor;
using System;

namespace UnityAIHelper.Editor.UI
{
    public class InputAreaUI
    {
        private readonly AIHelperWindow window;
        private string userInput = "";
        private GUIStyle buttonStyle;
        private ResizableTextArea textArea;

        public event Action<string> OnSendMessage;

        public InputAreaUI(AIHelperWindow window)
        {
            this.window = window;
            textArea = new ResizableTextArea(window, 60f, ResizableTextArea.DragPosition.Top, false);
        }

        private void InitStyles()
        {
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fixedWidth = 60,
                    fixedHeight = 25
                };
            }
        }

        public void Draw(float height, bool isProcessing, string buttonText = "发送", Action customCallback = null)
        {
            InitStyles();

            // 主容器
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(height));
            {
                // 水平布局包含输入区域和按钮
                EditorGUILayout.BeginHorizontal();
                {
                    // 输入区域容器
                    EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
                    {
                        // 获取文本区域的矩形
                        Rect textAreaRect = EditorGUILayout.GetControlRect(false, textArea.GetHeight());
                        
                        // 使用ResizableTextArea
                        GUI.enabled = !isProcessing;
                        string newInput = textArea.Draw(textAreaRect, userInput);
                        if (newInput != userInput)
                        {
                            userInput = newInput;
                            GUI.changed = true;
                        }
                        GUI.enabled = true;
                    }
                    EditorGUILayout.EndVertical();

                    // 发送按钮
                    EditorGUILayout.BeginVertical(GUILayout.Width(70));
                    {
                        if (GUILayout.Button(buttonText, buttonStyle))
                        {
                            if (customCallback != null)
                            {
                                customCallback();
                            }
                            else
                            {
                                SendMessage();
                            }
                        }

                        // 处理快捷键
                        var e = Event.current;
                        if (e.type == EventType.KeyDown && 
                            e.keyCode == KeyCode.Return && 
                            !e.shift && 
                            !isProcessing && 
                            !string.IsNullOrEmpty(userInput.Trim()) &&
                            EditorWindow.focusedWindow == window)
                        {
                            if (customCallback != null)
                            {
                                customCallback();
                            }
                            else
                            {
                                SendMessage();
                            }
                            e.Use();
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            // 如果GUI发生改变，重绘窗口
            if (GUI.changed)
            {
                window.Repaint();
            }
        }

        private void SendMessage()
        {
            string trimmedInput = userInput.Trim();
            if (!string.IsNullOrEmpty(trimmedInput))
            {
                string message = trimmedInput;
                userInput = "";
                OnSendMessage?.Invoke(message);
                GUI.FocusControl(null);
                window.Repaint();
            }
        }

        public float GetHeight()
        {
            return textArea.GetHeight() + 5; // 添加边距
        }

        public void Clear()
        {
            userInput = "";
            GUI.FocusControl(null);
            window.Repaint();
        }

        public void Focus()
        {
            window.Repaint();
        }

        /// <summary>
        /// 设置输入文本
        /// </summary>
        public void SetText(string text)
        {
            userInput = text ?? "";
            window.Repaint();
        }

        /// <summary>
        /// 获取当前输入文本
        /// </summary>
        public string GetText()
        {
            return userInput;
        }
    }
}
