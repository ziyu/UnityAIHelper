using UnityEngine;
using UnityEditor;

namespace UnityAIHelper.Editor.UI
{
    /// <summary>
    /// 可伸缩的文本框控件
    /// </summary>
    public class ResizableTextArea
    {
        private readonly EditorWindow window;
        private bool isDragging;
        private GUIStyle textStyle;
        private const float MIN_HEIGHT = 60f;
        private Vector2 dragStartPos;
        private float heightAtDragStart;
        private float currentHeight;
        private int controlID;
        private bool isHovering;
        private Vector2 scrollPosition;
        private float cachedContentHeight;
        private bool useScroller;

        public enum DragPosition
        {
            Top,
            Bottom
        }

        private readonly DragPosition dragPosition;

        public ResizableTextArea(EditorWindow window, float initialHeight = 100f, DragPosition dragPosition = DragPosition.Bottom,bool useScroller=true)
        {
            this.window = window;
            this.currentHeight = initialHeight;
            this.dragPosition = dragPosition;
            this.useScroller = useScroller;
        }

        private void InitStyles()
        {
            if (textStyle != null) return;
            textStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                richText = true,
                padding = new RectOffset(4, 4, 4, 4),
                stretchHeight = true,
                fixedHeight = 0,
                clipping = TextClipping.Clip
            };
        }

        public string Draw(Rect rect, string text)
        {
            InitStyles();

            var e = Event.current;
            controlID = GUIUtility.GetControlID(FocusType.Passive);

            float scrollbarWidth = 20f;

            // 拖拽区域
            var dragRect = dragPosition == DragPosition.Top ?
                new Rect(rect.x, rect.y, rect.width, 5) :
                new Rect(rect.x, rect.y + currentHeight - 5, rect.width, 5);
            
            // 文本区域
            var textAreaRect = dragPosition == DragPosition.Top ?
                new Rect(rect.x, rect.y + 5, rect.width, currentHeight - 5) :
                new Rect(rect.x, rect.y, rect.width, currentHeight - 5);

            // 处理拖拽事件
            EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.ResizeVertical);

            // 获取相对于控件的鼠标位置
            Vector2 mousePos = e.mousePosition;
            
            // 处理事件
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (dragRect.Contains(mousePos))
                    {
                        isDragging = true;
                        dragStartPos = mousePos;
                        heightAtDragStart = currentHeight;
                        GUIUtility.hotControl = controlID;
                        e.Use();
                    }
                    break;
                    
                case EventType.MouseDrag:
                    if (isDragging && GUIUtility.hotControl == controlID)
                    {
                        float delta = mousePos.y - dragStartPos.y;
                        if (dragPosition == DragPosition.Top)
                        {
                            currentHeight = Mathf.Max(MIN_HEIGHT, heightAtDragStart - delta);
                        }
                        else
                        {
                            currentHeight = Mathf.Max(MIN_HEIGHT, heightAtDragStart + delta);
                        }
                        window.Repaint();
                        e.Use();
                    }
                    break;
                    
                case EventType.MouseUp:
                    if (isDragging && GUIUtility.hotControl == controlID)
                    {
                        isDragging = false;
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;

                case EventType.MouseMove:
                    isHovering = dragRect.Contains(mousePos);
                    if (isHovering)
                    {
                        window.Repaint();
                    }
                    break;

                case EventType.MouseLeaveWindow:
                    if (isDragging && GUIUtility.hotControl == controlID)
                    {
                        // 继续处理拖拽，但确保鼠标位置在合理范围内
                        Vector2 clampedMousePos = e.mousePosition;
                        clampedMousePos.y = Mathf.Clamp(clampedMousePos.y, rect.y, rect.y + rect.height);
                        float delta = clampedMousePos.y - dragStartPos.y;
                        
                        if (dragPosition == DragPosition.Top)
                        {
                            currentHeight = Mathf.Max(MIN_HEIGHT, heightAtDragStart - delta);
                        }
                        else
                        {
                            currentHeight = Mathf.Max(MIN_HEIGHT, heightAtDragStart + delta);
                        }
                        window.Repaint();
                    }
                    break;

                case EventType.KeyDown:
                    if (isDragging && e.keyCode == KeyCode.Escape)
                    {
                        // 取消拖拽，恢复原始高度
                        isDragging = false;
                        currentHeight = heightAtDragStart;
                        GUIUtility.hotControl = 0;
                        window.Repaint();
                        e.Use();
                    }
                    break;
            }

            
            string newText;
            if (useScroller)
            {
                // 计算内容高度
                var contentSize = textStyle.CalcSize(new GUIContent(text ?? ""));
                var wrappedHeight = textStyle.CalcHeight(new GUIContent(text ?? ""), textAreaRect.width - scrollbarWidth);
                cachedContentHeight = Mathf.Max(wrappedHeight, contentSize.y);

                // 绘制文本区域
                var viewportRect = new Rect(textAreaRect.x, textAreaRect.y, textAreaRect.width, textAreaRect.height);
                var contentRect = new Rect(0, 0, textAreaRect.width - scrollbarWidth, cachedContentHeight);
                scrollPosition = GUI.BeginScrollView(viewportRect, scrollPosition, contentRect, false, true);
            
                newText = EditorGUI.TextArea(contentRect, text ?? "", textStyle);
            
                GUI.EndScrollView();
            }
            else
            {
                newText = EditorGUI.TextArea(textAreaRect, text ?? "", textStyle);
            }

  
                
            if (newText != text)
            {
                text = newText;
                GUI.changed = true;
            }

            // 绘制拖拽手柄的视觉提示
            if (isHovering || isDragging)
            {
                EditorGUI.DrawRect(dragRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            }
            
            return text;
        }

        public float GetHeight()
        {
            return currentHeight;
        }

        public void SetHeight(float newHeight)
        {
            currentHeight = Mathf.Max(MIN_HEIGHT, newHeight);
        }
    }
}
