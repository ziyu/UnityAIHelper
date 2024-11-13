using UnityEngine;
using UnityEditor;
using System;

namespace UnityAIHelper.Editor.UI
{
    /// <summary>
    /// 新建Chatbot面板UI组件
    /// </summary>
    public class NewChatbotUI
    {
        private readonly AIHelperWindow window;

        private string newChatbotName = "";
        private string newChatbotDescription = "";
        private string newChatbotPrompt = "";

        public event Action OnCancel;
        public event Action<string, string, string> OnCreate; // name, description, prompt

        public NewChatbotUI(AIHelperWindow window)
        {
            this.window = window;
        }

        public void Draw()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("创建新的AI助手", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("名称:");
            newChatbotName = EditorGUILayout.TextField(newChatbotName);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("描述:");
            newChatbotDescription = EditorGUILayout.TextField(newChatbotDescription);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("系统提示:");
            newChatbotPrompt = EditorGUILayout.TextArea(newChatbotPrompt, GUILayout.Height(100));
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("取消"))
            {
                Clear();
                OnCancel?.Invoke();
            }
            
            GUI.enabled = !string.IsNullOrEmpty(newChatbotName) && !string.IsNullOrEmpty(newChatbotPrompt);
            
            if (GUILayout.Button("创建"))
            {
                try
                {
                    OnCreate?.Invoke(newChatbotName, newChatbotDescription, newChatbotPrompt);
                    Clear();
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("错误", $"创建助手失败: {ex.Message}", "确定");
                }
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void Clear()
        {
            newChatbotName = "";
            newChatbotDescription = "";
            newChatbotPrompt = "";
        }
    }
}
