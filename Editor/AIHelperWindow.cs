using UnityEngine;
using UnityEditor;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor
{
    public class AIHelperWindow : EditorWindow
    {
        private string userInput = "";
        private Vector2 scrollPosition;
        private bool isProcessing = false;
        private CancellationTokenSource cancellationTokenSource;
        
        // 新建chatbot相关
        private bool isCreatingNew = false;
        private string newChatbotName = "";
        private string newChatbotDescription = "";
        private string newChatbotPrompt = "";

        // 用于缓存消息内容高度
        private GUIStyle messageStyle;
        private GUIContent tempContent = new GUIContent();

        // 输入框高度相关
        private float inputAreaHeight = 60f;
        private const float MIN_INPUT_HEIGHT = 60f;
        private const float MAX_INPUT_HEIGHT = 200f;
        private bool isResizingInput = false;
        private Rect resizeHandleRect;

        // 自动滚动相关
        private bool shouldScrollToBottom = false;
        private float lastContentHeight = 0f;

        // 缓存的视图宽度
        private float cachedViewWidth = 0f;

        // 状态显示相关
        private const float STATUS_HEIGHT = 30f;
        private GUIStyle statusStyle;

        // Streaming相关
        private ChatMessage currentStreamingMessage;
        private bool isStreaming = false;

        [MenuItem("Window/AI Helper")]
        public static void ShowWindow()
        {
            GetWindow<AIHelperWindow>("AI Helper");
        }

        private void InitializeStyles()
        {
            if (messageStyle == null && EditorStyles.textArea != null)
            {
                messageStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    richText = true
                };
            }

            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 10, 5, 5),
                    fontSize = 12
                };
            }
        }

        private void OnEnable()
        {
            // 检查是否有未完成的对话
            var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
            if (currentBot.HasPendingMessage)
            {
                isProcessing = true;
                Repaint();
            }

            // 订阅streaming事件
            currentBot.OnStreamingMessage += OnStreamingMessageReceived;
        }

        private void OnDisable()
        {
            // 取消订阅streaming事件
            var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
            currentBot.OnStreamingMessage -= OnStreamingMessageReceived;

            // 窗口关闭时确保取消任何进行中的请求
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }

        private void OnStreamingMessageReceived(ChatMessage message)
        {
            currentStreamingMessage = message;
            isStreaming = true;
            shouldScrollToBottom = true;
            Repaint();
        }

        private void OnGUI()
        {
            // 初始化样式
            InitializeStyles();
            
            // 如果样式仍未初始化，本帧跳过绘制
            if (messageStyle == null || statusStyle == null) 
            {
                return;
            }

            // 更新缓存的视图宽度
            if (Math.Abs(cachedViewWidth - position.width) > 1)
            {
                cachedViewWidth = position.width;
                Repaint(); // 强制重绘以更新布局
            }

            float totalHeight = position.height;
            float toolbarHeight = EditorStyles.toolbar.fixedHeight;
            float statusAreaHeight = isProcessing ? STATUS_HEIGHT : 0;
            float inputAreaTotalHeight = inputAreaHeight + 20; // 20为边距
            float chatAreaHeight = totalHeight - toolbarHeight - inputAreaTotalHeight - statusAreaHeight;

            DrawToolbar();
            
            if (isCreatingNew)
            {
                DrawNewChatbotPanel();
                return;
            }

            // 使用BeginVertical来确保正确的布局顺序
            EditorGUILayout.BeginVertical();
            
            // 聊天区域
            GUILayout.Space(toolbarHeight); // 为工具栏留出空间
            DrawChatArea(chatAreaHeight);
            
            // 状态显示区域
            if (isProcessing)
            {
                DrawStatusArea();
            }
            
            // 输入区域（将自动定位在底部）
            DrawInputArea(inputAreaTotalHeight);
            
            EditorGUILayout.EndVertical();

            HandleInputAreaResize();
        }

        private void DrawStatusArea()
        {
            // 创建状态区域
            Rect statusRect = GUILayoutUtility.GetRect(position.width, STATUS_HEIGHT);
            GUI.Box(statusRect, "", statusStyle);

            // 状态文本区域
            Rect textRect = new Rect(statusRect.x + 10, statusRect.y, statusRect.width - 200, STATUS_HEIGHT);
            GUI.Label(textRect, isStreaming ? "AI正在回答..." : "AI思考中...", statusStyle);

            var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
            if (currentBot.HasPendingMessage)
            {
                // 继续按钮
                Rect continueRect = new Rect(statusRect.xMax - 220, statusRect.y + 5, 150, 20);
                if (GUI.Button(continueRect, "继续之前对话"))
                {
                    ContinueMessage();
                }
            }

            // 取消按钮
            Rect cancelRect = new Rect(statusRect.xMax - 70, statusRect.y + 5, 60, 20);
            if (GUI.Button(cancelRect, "取消"))
            {
                CancelCurrentRequest();
            }
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
                if (currentBot.Id != "UnityHelper")
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
                isCreatingNew = false;
                Repaint();
            }
            
            GUI.enabled = !string.IsNullOrEmpty(newChatbotName) && !string.IsNullOrEmpty(newChatbotPrompt);
            
            if (GUILayout.Button("创建"))
            {
                try
                {
                    string id = "custom_" + Guid.NewGuid().ToString("N");
                    ChatbotManager.Instance.CreateCustomChatbot(id, newChatbotName,newChatbotDescription, newChatbotPrompt);
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

        private void DrawChatArea(float height)
        {
            // 使用固定的宽度
            float viewWidth = cachedViewWidth;
            float contentWidth = viewWidth - 20; // 预留滚动条宽度

            // 创建固定大小的滚动视图区域
            Rect scrollViewRect = GUILayoutUtility.GetRect(viewWidth, height);
            Rect contentRect = new Rect(0, 0, contentWidth - 20, 0);

            var chatHistory = ChatbotManager.Instance.GetCurrentChatbot().GetChatHistory();
            float totalContentHeight = 0;

            // 计算内容总高度
            foreach (var message in chatHistory)
            {
                totalContentHeight += CalculateMessageHeight(message, contentWidth - 40);
                totalContentHeight += 10; // 消息间距
            }

            // 如果有streaming消息，添加其高度
            if (isStreaming && currentStreamingMessage != null)
            {
                totalContentHeight += CalculateMessageHeight(currentStreamingMessage, contentWidth - 40);
                totalContentHeight += 10;
            }

            contentRect.height = totalContentHeight;

            // 检查是否需要滚动到底部
            if (shouldScrollToBottom || Mathf.Abs(totalContentHeight - lastContentHeight) > 1)
            {
                scrollPosition.y = totalContentHeight - height;
                shouldScrollToBottom = false;
                lastContentHeight = totalContentHeight;
            }

            // 绘制滚动视图
            scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, contentRect);

            float currentY = 0;
            foreach (var message in chatHistory)
            {
                float messageHeight = DrawMessage(message, currentY, contentWidth - 40);
                currentY += messageHeight + 5;
            }

            // 绘制streaming消息
            if (isStreaming && currentStreamingMessage != null)
            {
                DrawMessage(currentStreamingMessage, currentY, contentWidth - 40);
            }

            GUI.EndScrollView();
        }

        private float CalculateMessageHeight(ChatMessage message, float width)
        {
            float height = 20; // 发送者标签高度

            // 计算消息内容高度
            tempContent.text = message.content ?? "";
            height += messageStyle.CalcHeight(tempContent, width);

            // 如果有工具调用，计算工具调用信息高度
            if (message.tool_calls != null)
            {
                foreach (var toolCall in message.tool_calls)
                {
                    height += 20; // 工具调用标签高度
                    tempContent.text = $"{toolCall.function.name}: {toolCall.function.arguments}";
                    height += messageStyle.CalcHeight(tempContent, width);
                }
            }

            return height + 20; // 添加边距
        }

        private float DrawMessage(ChatMessage message, float y, float width)
        {
            float startY = y;
            float currentY = y + 5;
            
            // 绘制发送者
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = GetMessageColor(message.role);
            GUI.Label(new Rect(10, currentY, width, 20), GetMessagePrefix(message.role), style);
            currentY += 20;

            // 绘制消息内容
            tempContent.text = message.content ?? "";
            float contentHeight = messageStyle.CalcHeight(tempContent, width);
            GUI.TextArea(new Rect(10, currentY, width, contentHeight), message.content ?? "", messageStyle);
            currentY += contentHeight;

            // 如果有工具调用，绘制工具调用信息
            if (message.tool_calls != null)
            {
                foreach (var toolCall in message.tool_calls)
                {
                    currentY += 5;
                    GUI.Label(new Rect(10, currentY, width, 20), "执行命令:", EditorStyles.boldLabel);
                    currentY += 20;

                    tempContent.text = $"{toolCall.function.name}: {toolCall.function.arguments}";
                    float toolCallHeight = messageStyle.CalcHeight(tempContent, width);
                    GUI.TextArea(new Rect(10, currentY, width, toolCallHeight), 
                        $"{toolCall.function.name}: {toolCall.function.arguments}", messageStyle);
                    currentY += toolCallHeight;
                }
            }

            return currentY - startY + 5;
        }

        private void DrawInputArea(float height)
        {
            // 使用GUILayoutUtility.GetRect确保正确的布局位置
            Rect inputAreaRect = GUILayoutUtility.GetRect(position.width, height);
            GUI.Box(inputAreaRect, "", EditorStyles.helpBox);

            // 调整内部元素的位置
            inputAreaRect.x += 5;
            inputAreaRect.y += 5;
            inputAreaRect.width -= 70; // 为发送按钮留出空间
            inputAreaRect.height -= 10;

            GUI.enabled = !isProcessing;

            // 处理回车键
            var currentEvent = Event.current;
            bool shouldSend = false;
            
            if (currentEvent.type == EventType.KeyDown && 
                currentEvent.keyCode == KeyCode.Return)
            {
                if (currentEvent.shift)
                {
                    // Shift+Enter插入换行
                    userInput += "\n";
                }
                else if (!string.IsNullOrEmpty(userInput) && 
                         EditorWindow.focusedWindow == this)
                {
                    // 普通Enter发送消息
                    shouldSend = true;
                }
                currentEvent.Use();
            }

            // 绘制输入框
            userInput = GUI.TextArea(inputAreaRect, userInput, messageStyle);

            // 绘制发送按钮
            Rect sendButtonRect = new Rect(inputAreaRect.xMax + 5, inputAreaRect.y, 60, 30);
            if ((GUI.Button(sendButtonRect, "发送") || shouldSend) && !string.IsNullOrEmpty(userInput))
            {
                SendMessage(userInput);
            }

            GUI.enabled = true;

            // 绘制拖拽手柄
            resizeHandleRect = new Rect(inputAreaRect.x, inputAreaRect.y - 5, inputAreaRect.width + 65, 5);
            EditorGUIUtility.AddCursorRect(resizeHandleRect, MouseCursor.ResizeVertical);
        }

        private void HandleInputAreaResize()
        {
            var currentEvent = Event.current;
            
            switch (currentEvent.type)
            {
                case EventType.MouseDown:
                    if (resizeHandleRect.Contains(currentEvent.mousePosition))
                    {
                        isResizingInput = true;
                        currentEvent.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (isResizingInput)
                    {
                        inputAreaHeight = Mathf.Clamp(
                            inputAreaHeight - currentEvent.delta.y,
                            MIN_INPUT_HEIGHT,
                            MAX_INPUT_HEIGHT
                        );
                        currentEvent.Use();
                        Repaint();
                    }
                    break;

                case EventType.MouseUp:
                    isResizingInput = false;
                    break;
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

        private void CancelCurrentRequest()
        {
            var currentBot=ChatbotManager.Instance.GetCurrentChatbot();
            if (currentBot.HasPendingMessage)
            {
                currentBot.ClearPendingState();
                isProcessing = false;
                isStreaming = false;
                currentStreamingMessage = null;
                Repaint();
            }
            else if (isProcessing)
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource = null;
                isProcessing = false;
                isStreaming = false;
                currentStreamingMessage = null;
                Repaint();
            }
        }

        private async void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            string currentInput = userInput;
            userInput = "";
            isProcessing = true;
            shouldScrollToBottom = true;
            isStreaming = false;
            currentStreamingMessage = null;

            // 创建新的CancellationTokenSource
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            Repaint();

            try
            {
                await ChatbotManager.Instance.GetCurrentChatbot().SendMessageAsync(message, cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("AI响应已取消");
            }
            catch (Exception ex)
            {
                Debug.LogError($"获取AI响应时出错: {ex.Message}");
                EditorUtility.DisplayDialog("错误", "无法获取AI响应，请查看控制台了解详细信息。", "确定");
            }
            finally
            {
                cancellationTokenSource = null;
                isProcessing = false;
                isStreaming = false;
                currentStreamingMessage = null;
                Repaint();
            }
        }

        private async void ContinueMessage()
        {
            if (!isProcessing) return;

            try
            {
                // 创建新的CancellationTokenSource
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();

                await ChatbotManager.Instance.GetCurrentChatbot().ContinueMessageAsync(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("AI响应已取消");
            }
            catch (Exception ex)
            {
                Debug.LogError($"继续对话时出错: {ex}");
                EditorUtility.DisplayDialog("错误", "无法继续对话，请查看控制台了解详细信息。", "确定");
            }
            finally
            {
                cancellationTokenSource = null;
                isProcessing = false;
                isStreaming = false;
                currentStreamingMessage = null;
                Repaint();
            }
        }
    }
}
