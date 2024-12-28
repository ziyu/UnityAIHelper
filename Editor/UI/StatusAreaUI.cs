using UnityEngine;
using UnityEngine.UIElements;

namespace UnityAIHelper.Editor.UI
{
    public class StatusAreaUI:UIComponentBase
    {
        private readonly AIHelperWindow window;
        private VisualElement root;
        private VisualElement statusArea;

        private VisualElement buttonContainer;
        private Button continueButton;
        private Button cancelButton;
        
        public StatusAreaUI(AIHelperWindow window, VisualElement root)
        {
            this.window = window;
            this.root = root;
            Initialize();
        }

        void Initialize()
        {
            statusArea = root;
        }

        public override void OnUpdateUI()
        {
            if (window.isProcessing)
            {
                statusArea.style.display = DisplayStyle.Flex;
                statusArea.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                statusArea.style.paddingLeft = 8;
                statusArea.style.paddingRight = 8;
                statusArea.style.paddingTop = 4;
                statusArea.style.paddingBottom = 4;
                statusArea.style.flexDirection = FlexDirection.Row;
                statusArea.style.justifyContent = Justify.SpaceBetween;
                statusArea.style.alignItems = Align.Center;

                statusArea.Clear();

                // 状态文本
                var currentBot = ChatbotManager.Instance.GetCurrentChatbot();
                var statusText = currentBot.HasPendingMessage ? "有未完成的对话..." : (window.isStreaming ? "AI正在回答..." : "AI思考中...");
                var label = new Label(statusText);
                label.style.color = Color.white;
                statusArea.Add(label);

                // 按钮容器
                if (buttonContainer == null)
                {
                    buttonContainer = new VisualElement();
                    buttonContainer.style.flexDirection = FlexDirection.Row;
                    buttonContainer.style.justifyContent = Justify.FlexEnd;
                }
                if(!statusArea.Contains(buttonContainer))
                    statusArea.Add(buttonContainer);

                // 继续按钮
                if (currentBot.HasPendingMessage)
                {
                    if (continueButton == null)
                    {
                        continueButton = new Button(window.ContinueMessage) { text = "继续之前对话" };
                        continueButton.style.marginRight = 8;
                    }
                    if(!buttonContainer.Contains(continueButton))
                        buttonContainer.Add(continueButton);
                }
                else
                {
                    if(continueButton!=null&&buttonContainer.Contains(continueButton))
                        buttonContainer.Remove(continueButton);
                }

                // 取消按钮
                cancelButton ??= new Button(window.CancelCurrentRequest) { text = "取消" };
                if(!buttonContainer.Contains(cancelButton))
                    buttonContainer.Add(cancelButton);
            }
            else
            {
                statusArea.style.display = DisplayStyle.None;
                statusArea.Clear();
            }
        }
    }
}