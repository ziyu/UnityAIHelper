namespace UnityAIHelper.Editor.UI
{
    public interface IUIComponent
    {
        public void OnUpdateUI();
    }

    public abstract class UIComponentBase:IUIComponent
    {
        public abstract void OnUpdateUI();
    }

}