using UnityEngine;
using UnityEditor;
using System;

namespace UnityAIHelper.Editor.UI
{
    /// <summary>
    /// 输入区域UI组件
    /// </summary>
    public class InputAreaUI
    {
        private readonly AIHelperWindow window;
        private GUIStyle messageStyle;
        
        // 输入框高度相关
        private float inputAreaHeight = 60f;
        private const float MIN_INPUT_HEIGHT = 60f;
        private const float MAX_INPUT_HEIGHT = 200f;
        private bool isResizingInput = false;
        private Rect resizeHandleRect;

        // 输入内容
        private string userInput = "";

        public event Action<string> OnSendMessage;

        public InputAreaUI(AIHelperWindow window)
        {
            this.window = window;
 
        }

        void InitStyles()
        {
            if(messageStyle!=null)return;
            messageStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                richText = true
            };
        }

        public void Draw(float height, bool isProcessing)
        {
            InitStyles();
            // 使用GUILayoutUtility.GetRect确保正确的布局位置
            Rect inputAreaRect = GUILayoutUtility.GetRect(window.position.width, height);
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
                         EditorWindow.focusedWindow == window)
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
                string message = userInput;
                userInput = "";
                OnSendMessage?.Invoke(message);
            }

            GUI.enabled = true;

            // 绘制拖拽手柄
            resizeHandleRect = new Rect(inputAreaRect.x, inputAreaRect.y - 5, inputAreaRect.width + 65, 5);
            EditorGUIUtility.AddCursorRect(resizeHandleRect, MouseCursor.ResizeVertical);

            HandleInputAreaResize();
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
                        window.Repaint();
                    }
                    break;

                case EventType.MouseUp:
                    isResizingInput = false;
                    break;
            }
        }

        public float GetHeight()
        {
            return inputAreaHeight;
        }

        public void Clear()
        {
            userInput = "";
        }
    }
}
