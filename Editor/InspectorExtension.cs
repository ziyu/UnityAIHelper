using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.Linq;
using System.Reflection;

namespace UnityAIHelper.Editor
{
    [InitializeOnLoad]
    public static class InspectorExtension
    {
        private static EditorWindow lastInspectorWindow;
        private static GameObject lastSelectedGameObject;
        private static Button generateButton;
        private static System.Type propertyEditorType;
        private static EditorWindow inspectorWindow;

        static InspectorExtension()
        {
            // 获取PropertyEditor类型
            var assembly = typeof(EditorWindow).Assembly;
            propertyEditorType = assembly.GetType("UnityEditor.PropertyEditor");
            
            EditorApplication.update += OnEditorUpdate;
        }

        // 为组件添加上下文菜单
        [MenuItem("CONTEXT/Component/AI Edit")]
        private static void OnAIEditComponent(MenuCommand command)
        {
            var component = command.context as Component;
            if (component != null)
            {
                var window = EditorWindow.GetWindow<GenerateComponentWindow>(true, "Generate Component");
                window.minSize = new Vector2(400, 200);
                window.SetTargetGameObject(component.gameObject);
                window.SetTargetComponent(component);
                window.Show();
            }
        }

        private static void OnEditorUpdate()
        {
            if (inspectorWindow == null)
            {
                var wins = Resources.FindObjectsOfTypeAll(propertyEditorType);

                if (wins is { Length: > 0 })
                {
                    inspectorWindow = (EditorWindow)wins[0];
                }
                else
                {
                    return;
                }
            }

            // 如果没有显示inspectorWindow，移除按钮
            if (Selection.activeGameObject==null||inspectorWindow.rootVisualElement is not { visible: true })
            {
                RemoveGenerateButton();
                lastInspectorWindow = null;
                inspectorWindow = null;
                lastSelectedGameObject = null;
                return;
            }

            // 获取当前选中的GameObject和Inspector窗口
            var selectedObject = Selection.activeGameObject;

            // 如果Inspector窗口或选中对象发生变化
            if (inspectorWindow != lastInspectorWindow || selectedObject != lastSelectedGameObject)
            {
                lastInspectorWindow = inspectorWindow;
                lastSelectedGameObject = selectedObject;

                if (selectedObject != null)
                {
                    // 延迟一帧添加按钮，确保Inspector已完全加载
                    EditorApplication.delayCall += () => AddGenerateButton(inspectorWindow, selectedObject);
                }
                else
                {
                    RemoveGenerateButton();
                }
            }
        }

        private static void RemoveGenerateButton()
        {
            if (generateButton != null)
            {
                generateButton.RemoveFromHierarchy();
                generateButton = null;
            }
        }

        private static void AddGenerateButton(EditorWindow inspectorWindow, GameObject targetObject)
        {
            if(inspectorWindow==null||targetObject==null)return;
            // 获取Inspector的根视觉元素
            var root = inspectorWindow.rootVisualElement;
            if (root == null) return;

            // 移除旧按钮（如果存在）
            RemoveGenerateButton();

            // 查找mainScrollView按钮
            var mainScrollView = root.Query<ScrollView>().Where(b => b.viewDataKey == "inspector-window-main-scroll-view").First();
            if (mainScrollView == null) return;
            var addComponentButton=mainScrollView.contentContainer.Children().Last();

            // 创建新按钮
            generateButton = new Button(() => 
            {
                var window = EditorWindow.GetWindow<GenerateComponentWindow>(true, "Generate Component");
                window.minSize = new Vector2(400, 200);
                window.SetTargetGameObject(targetObject);
                window.Show();
            })
            {
                text = "Generate Component",
                style = 
                {
                    height = 30,
                    marginLeft = 50,
                    marginRight = 50,
                    marginTop = 2,
                    marginBottom = 10
                }
            };

            // 获取Add Component按钮的父元素
            var parent = addComponentButton.parent;
            
            // 在Add Component按钮后面添加Generate Component按钮
            var index = parent.IndexOf(addComponentButton);
            parent.Insert(index + 1, generateButton);
        }
    }
}
