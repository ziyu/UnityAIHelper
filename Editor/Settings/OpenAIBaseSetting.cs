namespace UnityAIHelper.Editor
{
    public class OpenAIBaseSetting:IModelProviderSetting
    {
        public string apiKey;
        public string apiBaseUrl = "https://api.openai.com/v1";
        public string defaultModel = "gpt-4o-mini";

        public float temperature = 0.7f;
        public int maxTokens = 2000;
    }
}