using System.IO;
using UnityEngine;
using UnityLLMAPI.Config;
using UnityLLMAPI.Utils.Json;

namespace UnityAIHelper.Editor
{

    public class AIHelperSettings
    {
        private static AIHelperSettings _instance;
        private const string SettingsPath = "Library/AIHelper/Settings.json";

        public int MaxChatHistoryEntries = 100;
        public float ToolExecutionTimeout = 30f;

        public bool AlwaysApproveReadOnlyOperations = false;
        public bool AlwaysApproveWriteOperations = false;
        public bool AlwaysApproveDeleteOperations = false;

        public Color PrimaryColor = new Color(0.2f, 0.6f, 1f);
        public Color SecondaryColor = new Color(0.8f, 0.8f, 0.8f);

        public OpenAIBaseSetting ModelProviderSetting = new();
        
        public bool enableLogging = true;
        public LogType minimumLogLevel = LogType.Error;

        public static AIHelperSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    LoadOrCreate();
                }
                return _instance;
            }
        }

        public static event System.Action OnSettingsUpdateEvent;
        private static OpenAIConfig _openAIConfig { get; set; }

        private static void LoadOrCreate()
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                _instance = JsonConverter.DeserializeObject<AIHelperSettings>(json);
            }
            else
            {
                _instance = new AIHelperSettings();
                Save();
            }
        }

        public static void Save()
        {
            if (_instance == null) return;
            var json = JsonConverter.SerializeObject(_instance);
            File.WriteAllText(SettingsPath, json);
            OnSettingsUpdateEvent?.Invoke();
            GetOpenAIConfig();
        }

        public static void Delete()
        {
            if (File.Exists(SettingsPath))
            {
                File.Delete(SettingsPath);
                _instance = null;
            }
        }
        
        
        public static OpenAIConfig GetOpenAIConfig()
        {
            _openAIConfig ??= ScriptableObject.CreateInstance<OpenAIConfig>();
            ToOpenAIConfig(Instance,_openAIConfig);
            return _openAIConfig;
        }

        static void ToOpenAIConfig(AIHelperSettings settings,OpenAIConfig config)
        {
            var modelProviderSetting = settings.ModelProviderSetting;
            config.apiKey = string.IsNullOrEmpty(modelProviderSetting.apiKey)?"invalid":modelProviderSetting.apiKey;
            config.defaultModel = modelProviderSetting.defaultModel;
            config.apiBaseUrl = modelProviderSetting.apiBaseUrl;
            config.maxTokens = modelProviderSetting.maxTokens;
            config.temperature = modelProviderSetting.temperature;
            config.enableLogging = settings.enableLogging;
            config.minimumLogLevel = settings.minimumLogLevel;
        }
    }
}