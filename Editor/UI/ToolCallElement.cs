using UnityEngine;
using UnityEngine.UIElements;
using UnityLLMAPI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityAIHelper.Editor.UI
{
    /// <summary>
    /// 工具调用UI元素
    /// </summary>
    public class ToolCallElement : VisualElement
    {
        private readonly ToolCall toolCall;
        private readonly ChatMessage result;
        private bool initialized;
        private bool collapsed = true;  // Default collapsed state
        private VisualElement contentContainer;  // Container for collapsible content

        public ToolCallElement(ToolCall toolCall, ChatMessage result = null)
        {
            this.toolCall = toolCall;
            this.result = result;
            
            Initialize();
            CreateUI();
        }

        private void Initialize()
        {
            if (initialized) return;

            // 加载样式表
            var styleSheet = PackageAssetLoader.LoadUIAsset<StyleSheet>("ToolCallUI.uss");
            styleSheets.Add(styleSheet);

            initialized = true;
        }

        private void CreateUI()
        {
            AddToClassList("tool-call-container");

            // 工具名称和时间
            var header = new VisualElement();
            header.AddToClassList("tool-call-header");
            header.RegisterCallback<ClickEvent>(evt => ToggleCollapse());

            var nameLabel = new Label("工具调用 "+toolCall.function.name);
            nameLabel.AddToClassList("tool-name");
            header.Add(nameLabel);

            Add(header);

            // Content container for collapsible content
            contentContainer = new VisualElement();
            contentContainer.AddToClassList("tool-content");
            Add(contentContainer);

            // 参数
            try
            {
                var argsJson = JToken.Parse(toolCall.function.arguments);
                var argsElement = new JsonObjectUIElement(argsJson);
                argsElement.AddToClassList("tool-args");
                contentContainer.Add(argsElement);
            }
            catch
            {
                var argsLabel = new Label(toolCall.function.arguments);
                argsLabel.AddToClassList("tool-args");
                contentContainer.Add(argsLabel);
            }

            // 结果
            if (result != null)
            {
                try
                {
                    var resultJson = JToken.Parse(result.content);
                    var resultElement = new JsonObjectUIElement(resultJson);
                    resultElement.AddToClassList("tool-result");
                    contentContainer.Add(resultElement);
                }
                catch
                {
                    var resultLabel = new Label(result.content);
                    resultLabel.AddToClassList("tool-result");
                    contentContainer.Add(resultLabel);
                }
            }

            // Set initial state
            UpdateCollapseState();
        }

        private void ToggleCollapse()
        {
            collapsed = !collapsed;
            UpdateCollapseState();
        }

        private void UpdateCollapseState()
        {
            if (collapsed)
            {
                contentContainer.style.display = DisplayStyle.None;
                RemoveFromClassList("expanded");
                AddToClassList("collapsed");
            }
            else
            {
                contentContainer.style.display = DisplayStyle.Flex;
                RemoveFromClassList("collapsed");
                AddToClassList("expanded");
            }
        }
    }
}
