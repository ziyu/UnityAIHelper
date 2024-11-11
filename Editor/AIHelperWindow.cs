using UnityEngine;
using UnityEditor;
using System;
using System.Threading.Tasks;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor
{
    public class AIHelperWindow : EditorWindow
    {
        private string userInput = "";
        private Vector2 scrollPosition;
        private bool isProcessing = false;
        
        // 新建chatbot相关
        private bool isCreatingNew = false;
        private string newChatbotName = "";
        private string newChatbotPrompt = "";

        [MenuItem("Window/AI Helper")]
        public static void ShowWindow()
        {
            GetWindow<AIHelperWindow>("AI Helper");
        }

        private void OnGUI()
        {
            DrawToolbar();
            
            if (isCreatingNew)
            {
                DrawNewChatbotPanel();
                return;
            }

            DrawChatArea();
            DrawInputArea();
        }

        private void DrawToolbar()
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
                        Repaint();
                    });
                }
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("创建新助手..."), false, () => 
                {
                    isCreatingNew = true;
                    newChatbotName = "";
                    newChatbotPrompt = "";
                    Repaint();
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
                            Repaint();
                        }
                    });
                }

                menu.ShowAsContext();
            }

            // 清空对话按钮
            if (GUILayout.Button("清空对话", EditorStyles.toolbarButton))
            {
                if (EditorUtility.DisplayDialog("确认", "是否清空所有对话记录？", "确定", "取消"))
                {
                    ChatbotManager.Instance.GetCurrentChatbot().ClearHistory();
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawNewChatbotPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("创建新的AI助手", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("名称:");
            newChatbotName = EditorGUILayout.TextField(newChatbotName);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("系统提示:");
            newChatbotPrompt = EditorGUILayout.TextArea(newChatbotPrompt, GUILayout.Height(100));
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("取消"))
            {
                isCreatingNew = false;
                Repaint();
            }
            
            GUI.enabled = !string.IsNullOrEmpty(newChatbotName) && !string.IsNullOrEmpty(newChatbotPrompt);
            
            if (GUILayout.Button("创建"))
            {
                try
                {
                    string id = "custom_" + Guid.NewGuid().ToString("N");
                    ChatbotManager.Instance.CreateCustomChatbot(id, newChatbotName, newChatbotPrompt);
                    ChatbotManager.Instance.SwitchChatbot(id);
                    isCreatingNew = false;
                    Repaint();
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

        private void DrawChatArea()
        {
            // 聊天历史记录区域
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(position.height - 100));
            
            var chatHistory = ChatbotManager.Instance.GetCurrentChatbot().GetChatHistory();
            foreach (var message in chatHistory)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // 显示发送者和内容
                var style = new GUIStyle(EditorStyles.boldLabel);
                style.normal.textColor = GetMessageColor(message.role);
                EditorGUILayout.LabelField(GetMessagePrefix(message.role), style);
                
                EditorGUILayout.TextArea(message.content ?? "", GUILayout.ExpandWidth(true));

                // 如果有工具调用，显示工具调用信息
                if (message.tool_calls != null)
                {
                    foreach (var toolCall in message.tool_calls)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("执行命令:", EditorStyles.boldLabel);
                        EditorGUILayout.TextArea($"{toolCall.function.name}: {toolCall.function.arguments}", 
                            GUILayout.ExpandWidth(true));
                    }
                }
                
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }

            if (Event.current.type == EventType.Layout && chatHistory.Count > 0)
            {
                scrollPosition.y = float.MaxValue;
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawInputArea()
        {
            // 输入区域
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUI.enabled = !isProcessing;
            
            // 处理回车键
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown && 
                currentEvent.keyCode == KeyCode.Return && 
                !string.IsNullOrEmpty(userInput) && 
                EditorWindow.focusedWindow == this)
            {
                SendMessage(userInput);
                currentEvent.Use();
            }
            
            userInput = EditorGUILayout.TextField(userInput, GUILayout.ExpandWidth(true));
            
            if (GUILayout.Button("发送", GUILayout.Width(60)) && !string.IsNullOrEmpty(userInput))
            {
                SendMessage(userInput);
            }
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // 处理中提示
            if (isProcessing)
            {
                EditorGUILayout.HelpBox("AI思考中...", MessageType.Info);
            }
        }

        private string GetMessagePrefix(string role)
        {
            return role switch
            {
                "system" => "系统:",
                "user" => "你:",
                "assistant" => "AI:",
                "tool" => "工具执行结果:",
                _ => role + ":"
            };
        }

        private Color GetMessageColor(string role)
        {
            return role switch
            {
                "system" => Color.yellow,
                "user" => Color.blue,
                "assistant" => Color.green,
                "tool" => Color.cyan,
                _ => Color.white
            };
        }

        private async void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            string currentInput = userInput;
            userInput = "";
            isProcessing = true;
            Repaint();

            try
            {
                await ChatbotManager.Instance.GetCurrentChatbot().SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取AI响应时出错: {ex.Message}");
                EditorUtility.DisplayDialog("错误", "无法获取AI响应，请查看控制台了解详细信息。", "确定");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }
    }
}
