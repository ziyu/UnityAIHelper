using UnityEngine.UIElements;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace UnityAIHelper.Editor.UI
{
    public class JsonObjectUIElement : VisualElement
    {
        private const int STRING_LENGTH_THRESHOLD = 100; // 字符串长度超过此值默认收起
        private const int NODE_COUNT_THRESHOLD = 10;  // 子节点数量超过此值默认收起
        private bool collapsed;
        private readonly VisualElement contentContainer;
        private readonly Label toggleLabel;
        private readonly JToken jsonToken;
        private bool initialized;
        private Label previewLabel;
        private Button copyButton;
        private bool isCopying;
        private string headerName;

        private static StyleSheet JsonStyleSheet;
        
        public JsonObjectUIElement(JToken token, int depth = 0,string headerName="")
        {
            jsonToken = token;
            contentContainer = new VisualElement();
            this.headerName = headerName;
            toggleLabel = new Label("▶");
            
            // 根据内容决定是否默认收起
            collapsed = ShouldInitiallyCollapse(token);
            
            Initialize();
            CreateUI(depth);
        }

        private bool ShouldInitiallyCollapse(JToken token)
        {
            if (token is JProperty prop)
            {
                token = prop.Value;
            }
            
            switch (token.Type)
            {
                case JTokenType.Object:
                    var obj = token as JObject;
                    return obj.Count > NODE_COUNT_THRESHOLD;
                    
                case JTokenType.Array:
                    var arr = token as JArray;
                    return arr.Count > NODE_COUNT_THRESHOLD;
                    
                case JTokenType.String:
                    var str = token.Value<string>();
                    return str != null && str.Length > STRING_LENGTH_THRESHOLD;
                    
                default:
                    return false;
            }
        }

        private void Initialize()
        {
            if (initialized) return;
            
            // 加载样式表
            JsonStyleSheet ??= PackageAssetLoader.LoadUIAsset<StyleSheet>("JsonUI.uss");
            styleSheets.Add(JsonStyleSheet);
            
            initialized = true;
        }
        
        private void CreateUI(int depth)
        {
            AddToClassList("json-object");
            
            var header = new VisualElement();
            header.AddToClassList("json-header");
            header.style.marginLeft = depth * 16;
            
            var shouldCreateChildren = ShouldCreateChildren(jsonToken);
            // 只有需要展开/收起的节点才显示箭头
            if (shouldCreateChildren)
            {
                toggleLabel.AddToClassList("json-toggle");
                header.Add(toggleLabel);
            }
            
            var preview = CreatePreviewElement(jsonToken);
            header.Add(preview);

            // 添加复制按钮
            copyButton = new Button(() => CopyToClipboard()) { text = "Copy" };
            copyButton.AddToClassList("json-copy-button");
            copyButton.style.display = DisplayStyle.None;
            header.Add(copyButton);
            
            // 添加hover事件
            header.RegisterCallback<MouseEnterEvent>(evt => 
            {
                if (!isCopying)
                {
                    copyButton.text = "Copy";
                    copyButton.style.display = DisplayStyle.Flex;
                }
            });
            header.RegisterCallback<MouseLeaveEvent>(evt => 
            {
                if (!isCopying)
                {
                    copyButton.style.display = DisplayStyle.None;
                }
            });
            
            if (shouldCreateChildren)
            {
                header.RegisterCallback<ClickEvent>(evt => 
                {
                    if (evt.target != copyButton)
                    {
                        ToggleCollapse();
                    }
                });
            }
            Add(header);
            
            contentContainer.AddToClassList("json-content");
            Add(contentContainer);
            
            CreateContent(jsonToken, depth + 1);
            
            UpdateCollapseState();
        }

        private async void CopyToClipboard()
        {
            if (isCopying) return;
            
            isCopying = true;
            string textToCopy;
            if (jsonToken is JProperty prop)
            {
                if (prop.Value.Type == JTokenType.String)
                {
                    textToCopy = prop.Value.Value<string>();
                }
                else
                {
                    textToCopy = prop.Value.ToString(Formatting.Indented);
                }
            }
            else
            {
                textToCopy = jsonToken.ToString(Formatting.Indented);
            }
            
            GUIUtility.systemCopyBuffer = textToCopy;
            
            // 显示复制成功状态
            copyButton.text = "✓";
            copyButton.AddToClassList("json-copy-button-success");
            
            // 1秒后恢复原状
            await Task.Delay(1000);
            
            if (copyButton != null)
            {
                copyButton.text = "Copy";
                copyButton.RemoveFromClassList("json-copy-button-success");
                copyButton.style.display = DisplayStyle.None;
            }
            isCopying = false;
        }

        private VisualElement CreatePreviewElement(JToken token)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.AddToClassList("json-preview");

            switch (token.Type)
            {
                case JTokenType.Object:
                    previewLabel = new Label("{...}");
                    container.Add(previewLabel);
                    break;
                case JTokenType.Array:
                    previewLabel = new Label("[...]");
                    container.Add(previewLabel);
                    break;
                case JTokenType.Property:
                    var prop = token as JProperty;
                    var nameLabel = new Label($"\"{prop.Name}\":");
                    nameLabel.AddToClassList("json-property-name");
                    container.Add(nameLabel);
                    
                    if (!ShouldCreateChildren(prop.Value))
                    {
                        container.Add(CreateValueLabel(prop.Value));
                    }
                    else if (prop.Value.Type == JTokenType.String)
                    {
                        previewLabel = new Label("...");
                        container.Add(previewLabel);
                    }
                    break;
                default:
                    container.Add(CreateValueLabel(token));
                    break;
            }
            
            return container;
        }

        private Label CreateValueLabel(JToken token)
        {
            var label = new Label();
            switch (token.Type)
            {
                case JTokenType.String:
                    var str = token.Value<string>();
                    if (str != null && str.Length > STRING_LENGTH_THRESHOLD)
                    {
                        label.text = $"\"{str.Substring(0, 50)}...\"";
                    }
                    else
                    {
                        label.text = $"\"{str}\"";
                    }
                    label.AddToClassList("json-string");
                    break;
                case JTokenType.Integer:
                case JTokenType.Float:
                    label.text = token.ToString();
                    label.AddToClassList("json-number");
                    break;
                case JTokenType.Boolean:
                    label.text = token.ToString().ToLower();
                    label.AddToClassList("json-boolean");
                    break;
                case JTokenType.Null:
                    label.text = "null";
                    label.AddToClassList("json-null");
                    break;
                default:
                    label.text = token.ToString();
                    break;
            }
            return label;
        }
        
        private bool ShouldCreateChildren(JToken token)
        {
            if (token is JProperty prop)
            {
                token = prop.Value;
            }
            
            switch (token.Type)
            {
                case JTokenType.Object:
                case JTokenType.Array:
                    return true;
                case JTokenType.String:
                    var str = token.Value<string>();
                    return str != null && str.Length > STRING_LENGTH_THRESHOLD;
                default:
                    return false;
            }
        }
        
        private void ToggleCollapse()
        {
            collapsed = !collapsed;
            UpdateCollapseState();
        }
        
        private void UpdateCollapseState()
        {
            string headerText;
            if (collapsed)
            {
                contentContainer.style.display = DisplayStyle.None;
                headerText = $"▶ <color=#AAAAAA>{headerName}</color>";
                if (previewLabel != null)
                {
                    previewLabel.style.display = DisplayStyle.Flex;
                }
            }
            else
            {
                contentContainer.style.display = DisplayStyle.Flex;
                headerText = $"▼ <color=#AAAAAA>{headerName}</color>";
                if (previewLabel != null)
                {
                    previewLabel.style.display = DisplayStyle.None;
                }
            }
            toggleLabel.enableRichText = true;
            toggleLabel.text = headerText;
        }

        private void CreateContent(JToken token, int depth)
        {
            if (token is JProperty prop)
            {
                token = prop.Value;
            }

            switch (token.Type)
            {
                case JTokenType.Object:
                    var obj = token as JObject;
                    foreach (var property in obj.Properties())
                    {
                        contentContainer.Add(new JsonObjectUIElement(property, depth));
                    }
                    break;

                case JTokenType.Array:
                    var arr = token as JArray;
                    for (int i = 0; i < arr.Count; i++)
                    {
                        var arrayContainer = new VisualElement();
                        arrayContainer.style.flexDirection = FlexDirection.Row;
                        
                        var indexLabel = new Label($"[{i}]");
                        indexLabel.AddToClassList("json-array-index");
                        arrayContainer.Add(indexLabel);
                        
                        var arrayItem = new JsonObjectUIElement(arr[i], depth);
                        arrayContainer.Add(arrayItem);
                        
                        contentContainer.Add(arrayContainer);
                    }
                    break;

                case JTokenType.String:
                    var str = token.Value<string>();
                    if (str != null && str.Length > STRING_LENGTH_THRESHOLD)
                    {
                        var fullTextLabel = new Label(str);
                        fullTextLabel.AddToClassList("json-string");
                        fullTextLabel.AddToClassList("json-string-full");
                        contentContainer.Add(fullTextLabel);
                    }
                    break;
            }
        }
    }
}
