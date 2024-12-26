using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace UnityAIHelper.Editor.UI
{
    public class ResizableTextArea : VisualElement
    {
        public enum HandlePosition
        {
            Top,
            Bottom
        }

        private TextField textField;
        private VisualElement resizeHandle;
        private bool isDragging;
        private Vector2 dragStartPos;
        private float heightAtDragStart;
        private const float MIN_HEIGHT = 60f;
        private float currentHeight = MIN_HEIGHT;
        private HandlePosition handlePosition = HandlePosition.Bottom;

        public HandlePosition ResizeHandlePosition
        {
            get => handlePosition;
            set
            {
                if (handlePosition != value)
                {
                    handlePosition = value;
                    UpdateHandlePosition();
                }
            }
        }

        public event EventCallback<ChangeEvent<string>> valueChanged;

        public string text
        {
            get => textField.value;
            set
            {
                if (textField.value != value)
                {
                    textField.value = value;
                    valueChanged?.Invoke(ChangeEvent<string>.GetPooled(textField.value, value));
                }
            }
        }

        public new class UxmlFactory : UxmlFactory<ResizableTextArea, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new UxmlStringAttributeDescription { name = "label", defaultValue = "" };
            UxmlStringAttributeDescription m_Value = new UxmlStringAttributeDescription { name = "value", defaultValue = "" };
            UxmlEnumAttributeDescription<HandlePosition> m_HandlePosition = new UxmlEnumAttributeDescription<HandlePosition> 
            { 
                name = "handle-position", 
                defaultValue = HandlePosition.Bottom
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var area = ve as ResizableTextArea;

                // 确保属性按正确的顺序初始化
                area.Label = m_Label.GetValueFromBag(bag, cc);
                area.text = m_Value.GetValueFromBag(bag, cc);

                // 显式检查handle-position属性是否存在
                if (bag.TryGetAttributeValue("handle-position", out string positionStr)
                    &&Enum.TryParse<HandlePosition>( positionStr, true,out HandlePosition handlePosition))
                {
                    area.ResizeHandlePosition = handlePosition;
                }
                else
                {
                    area.ResizeHandlePosition = m_HandlePosition.defaultValue;
                }
            }
        }

        public string Label
        {
            get => textField.label;
            set => textField.label = value;
        }

        public ResizableTextArea()
        {
            // 加载样式
            styleSheets.Add(PackageAssetLoader.LoadUIAsset<StyleSheet>("ResizableTextArea.uss"));
            AddToClassList("resizable-text-area");

            // 创建文本输入框
            textField = new TextField();
            textField.multiline = true;
            textField.name = "text-field";
            textField.AddToClassList("text-field");
            Add(textField);

            // 创建调整大小的handle
            resizeHandle = new VisualElement();
            resizeHandle.name = "resize-handle";
            resizeHandle.AddToClassList("resize-handle");
            Add(resizeHandle);

            // 设置初始高度和handle位置
            style.height = currentHeight;
            handlePosition = HandlePosition.Bottom; // 设置默认值
            UpdateHandlePosition();

            // 设置大小调整功能
            SetupResizeHandle();

            // 设置文本变化事件
            textField.RegisterValueChangedCallback(evt =>
            {
                valueChanged?.Invoke(evt);
            });

            // 注册布局完成事件，确保handle位置正确
            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                UpdateHandlePosition();
            });
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (!isDragging) return;

            float delta = handlePosition == HandlePosition.Bottom ? 
                         (evt.mousePosition.y - dragStartPos.y) : 
                         (dragStartPos.y - evt.mousePosition.y);
            
            float newHeight = Mathf.Max(MIN_HEIGHT, heightAtDragStart + delta);
            if (style.height != newHeight)
            {
                style.height = newHeight;
                currentHeight = newHeight;
            }

            evt.StopPropagation();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (!isDragging) return;
            isDragging = false;
            resizeHandle.ReleaseMouse();
            evt.StopPropagation();
        }

        private void SetupResizeHandle()
        {
            resizeHandle.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return; // 只处理左键点击
                isDragging = true;
                dragStartPos = evt.mousePosition;
                heightAtDragStart = resolvedStyle.height;
                resizeHandle.CaptureMouse();
                evt.StopPropagation();
            });
            
            resizeHandle.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            resizeHandle.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private void UpdateHandlePosition()
        {
            if (handlePosition == HandlePosition.Top)
            {
                resizeHandle.style.top = 0;
                resizeHandle.style.bottom = StyleKeyword.Null;
                textField.style.paddingTop = 12;
                textField.style.paddingBottom = 4;
            }
            else
            {
                resizeHandle.style.bottom = 0;
                resizeHandle.style.top = StyleKeyword.Null;
                textField.style.paddingTop = 4;
                textField.style.paddingBottom = 12;
            }

            // 强制刷新布局
            style.display = DisplayStyle.Flex;
        }

        public void RegisterValueChangedCallback(EventCallback<ChangeEvent<string>> evtCallback)
        {
            valueChanged += evtCallback;
        }

        public void UnregisterValueChangedCallback(EventCallback<ChangeEvent<string>> evtCallback)
        {
            valueChanged -= evtCallback;
        }

        public void Clear()
        {
            text = "";
        }

        public void Focus()
        {
            textField.Focus();
        }
    }
}
