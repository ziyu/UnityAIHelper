using UnityEngine;
using UnityEditor;
using System;

namespace UnityAIHelper.Editor.UI
{
    /// <summary>
    /// 工具栏UI组件
    /// </summary>
    public class ToolbarUI
    {
        private readonly AIHelperWindow window;

        public event Action OnCreateNewChatbot;
        public event Action OnClearHistory;
        public event Action OnOpenSettings;

        public ToolbarUI(AIHelperWindow window)
        {
            this.window = window;
        }

        public void Draw()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Chatbot选择下拉菜单
            var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
            if (EditorGUILayout.DropdownButton(new GUIContent(currentBot.Name), FocusType.Keyboard, EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();
                foreach (var bot in ChatbotManager.Instance.Chatbots.Values)
                {
                    menu.AddItem(new GUIContent(bot.Name), currentBot.Id == bot.Id, () => 
                    {
                        ChatbotManager.Instance.SwitchChatbot(bot.Id);
                        window.Repaint();
                    });
                }
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("创建新助手..."), false, () => 
                {
                    OnCreateNewChatbot?.Invoke();
                });

                // 只有非默认chatbot才能删除
                if (currentBot.Id != "unity_helper")
                {
                    menu.AddItem(new GUIContent("删除当前助手"), false, () =>
                    {
                        if (EditorUtility.DisplayDialog("确认删除", 
                            $"是否确定删除助手 '{currentBot.Name}'？\n此操作无法撤销。", 
                            "删除", "取消"))
                        {
                            ChatbotManager.Instance.RemoveChatbot(currentBot.Id);
                            window.Repaint();
                        }
                    });
                }

                menu.ShowAsContext();
            }

            GUILayout.FlexibleSpace();

            // 设置按钮
            if (GUILayout.Button("设置", EditorStyles.toolbarButton))
            {
                OnOpenSettings?.Invoke();
            }

            // 清空对话按钮
            if (GUILayout.Button("清空对话", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("确认", "是否清空所有对话记录？", "确定", "取消"))
                {
                    OnClearHistory?.Invoke();
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
