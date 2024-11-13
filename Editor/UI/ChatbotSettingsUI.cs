using UnityEngine;
using UnityEditor;
using System;

namespace UnityAIHelper.Editor.UI
{
    /// <summary>
    /// Chatbot设置界面UI组件
    /// </summary>
    public class ChatbotSettingsUI
    {
        private readonly AIHelperWindow window;
        private Vector2 scrollPosition;
        private ResizableTextArea promptArea;
        
        // 编辑字段
        private string name;
        private string description;
        private string systemPrompt;
        private GUIStyle labelStyle;

        public event Action OnClose;

        public ChatbotSettingsUI(AIHelperWindow window)
        {
            this.window = window;
            promptArea = new ResizableTextArea(window, 100f);
            LoadCurrentSettings();
        }

        private void InitStyles()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    margin = new RectOffset(0, 0, 10, 5)
                };
            }
        }

        private void LoadCurrentSettings()
        {
            var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
            name = currentBot.Name;
            description = currentBot.Description;
            systemPrompt = currentBot.SystemPrompt;
        }

        public void Draw()
        {
            InitStyles();

            EditorGUILayout.BeginVertical();
            {
                // 标题
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Chatbot 设置", labelStyle);
                EditorGUILayout.Space(10);

                // 滚动视图
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                {
                    // 基本信息
                    name = EditorGUILayout.TextField("名称", name);
                    description = EditorGUILayout.TextField("描述", description);

                    EditorGUILayout.Space(10);

                    // 系统提示词
                    EditorGUILayout.LabelField("系统提示词", labelStyle);
                    
                    // 使用EditorGUILayout.GetControlRect获取区域
                    var promptRect = EditorGUILayout.GetControlRect(
                        false, 
                        promptArea.GetHeight(), 
                        GUILayout.ExpandWidth(true)
                    );

                    string newPrompt = promptArea.Draw(promptRect, systemPrompt);
                    if (newPrompt != systemPrompt)
                    {
                        systemPrompt = newPrompt;
                        GUI.changed = true;
                    }
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(10);

                // 底部按钮
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("保存", GUILayout.Height(30)))
                    {
                        SaveSettings();
                        OnClose?.Invoke();
                    }
                    if (GUILayout.Button("取消", GUILayout.Height(30)))
                    {
                        LoadCurrentSettings();
                        OnClose?.Invoke();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10);
            }
            EditorGUILayout.EndVertical();

            // 如果GUI发生改变，重绘窗口
            if (GUI.changed)
            {
                window.Repaint();
            }
        }

        private void SaveSettings()
        {
            var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
            if (currentBot is ChatbotBase chatbot)
            {
                chatbot.UpdateSettings(name, description, systemPrompt);
            }
        }
    }
}
