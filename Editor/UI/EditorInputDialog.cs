using UnityEngine;
using UnityEditor;

namespace UnityAIHelper.Editor.UI
{
    public static class EditorInputDialog
    {
        public static string Show(string title, string message, string defaultText = "")
        {
            // 创建一个临时的ScriptableObject来存储用户输入
            var inputContainer = ScriptableObject.CreateInstance<InputContainer>();
            inputContainer.userInput = defaultText;
            
            // 显示输入对话框
            bool confirmed = false;
            EditorGUIUtility.ShowObjectPicker<InputContainer>(inputContainer, false, "", 0);
            
            var position = EditorGUIUtility.GetObjectPickerControlID();
            var window = EditorWindow.GetWindow<InputDialog>(true, title);
            window.Initialize(message, defaultText, result =>
            {
                confirmed = result.confirmed;
                if (confirmed)
                {
                    inputContainer.userInput = result.input;
                }
                EditorGUIUtility.ExitGUI();
            });

            // 等待用户操作完成
            while (window != null && !window.IsClosed)
            {
                EditorGUIUtility.GetObjectPickerObject();
            }
            
            var result = confirmed ? inputContainer.userInput : null;
            Object.DestroyImmediate(inputContainer);
            return result;
        }

        private class InputContainer : ScriptableObject
        {
            public string userInput;
        }

        private class InputDialog : EditorWindow
        {
            private string message;
            private string input;
            private System.Action<(bool confirmed, string input)> callback;
            public bool IsClosed { get; private set; }

            public void Initialize(string message, string defaultInput, System.Action<(bool confirmed, string input)> callback)
            {
                this.message = message;
                this.input = defaultInput;
                this.callback = callback;
                IsClosed = false;
                
                // 设置窗口大小
                minSize = maxSize = new Vector2(300, 100);
                position = new Rect(
                    (Screen.currentResolution.width - minSize.x) / 2,
                    (Screen.currentResolution.height - minSize.y) / 2,
                    minSize.x,
                    minSize.y
                );
                
                ShowUtility();
            }

            private void OnGUI()
            {
                EditorGUILayout.LabelField(message);
                GUI.SetNextControlName("InputField");
                input = EditorGUILayout.TextField(input);
                
                // 自动聚焦到输入框
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.FocusControl("InputField");
                }

                // 处理回车键
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
                {
                    Submit(true);
                    return;
                }
                
                // 处理ESC键
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    Submit(false);
                    return;
                }

                EditorGUILayout.Space();
                
                // 按钮区域
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("确定", GUILayout.Width(60)))
                    {
                        Submit(true);
                    }
                    
                    if (GUILayout.Button("取消", GUILayout.Width(60)))
                    {
                        Submit(false);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            private void Submit(bool confirmed)
            {
                IsClosed = true;
                callback?.Invoke((confirmed, input));
                Close();
            }

            private void OnDestroy()
            {
                if (!IsClosed)
                {
                    Submit(false);
                }
            }
        }
    }
}
