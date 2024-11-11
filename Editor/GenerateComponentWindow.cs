using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using UnityLLMAPI.Models;
using System.IO;

namespace UnityAIHelper.Editor
{
    [InitializeOnLoad]
    public class ScriptGenerationManager
    {
        static ScriptGenerationManager()
        {
            EditorApplication.delayCall += HandlePendingCodeGeneration;
        }

        private static void HandlePendingCodeGeneration()
        {
            GenerateComponentWindow.HandlePendingCodeGeneration();
        }
    }

    public class GenerateComponentWindow : EditorWindow
    {
        private string scriptPrompt = "";
        private bool isGenerating = false;
        private static bool isCodePending = false;
        private GameObject targetGameObject;
        private Vector2 inputScrollPosition;
        private Vector2 outputScrollPosition;
        private ComponentGeneratorChatbot componentGenerator;
        private string generatingContent = "";
        private string currentPhase = ""; // 用于显示当前生成阶段
        
        private const string PENDING_SCRIPT_KEY = "UnityAIHelper_PendingScript";
        private const string TARGET_GAMEOBJECT_ID_KEY = "UnityAIHelper_TargetGameObjectID";
        private const string WINDOW_INSTANCE_ID_KEY = "UnityAIHelper_WindowInstanceID";
        private const string SCRIPT_PATH_KEY = "UnityAIHelper_ScriptPath";

        public void SetTargetGameObject(GameObject gameObject)
        {
            targetGameObject = gameObject;
            // 为每个窗口实例创建独立的chatbot，并设置streaming回调
            componentGenerator = new ComponentGeneratorChatbot(OnStreamingResponse);
        }

        private void OnStreamingResponse(ChatMessage genMessage,bool isDone)
        {
            // 由于这是从异步回调中调用，需要确保在主线程中更新UI
            EditorApplication.delayCall += () =>
            {
                generatingContent = genMessage.content;
                Repaint(); // 请求重绘UI
            };
        }

        private void OnDisable()
        {
            // 清理EditorPrefs中的数据，但只有在没有待处理的生成任务时才清理
            if (!isCodePending)
            {
                CleanupEditorPrefs();
            }
        }

        internal static void HandlePendingCodeGeneration()
        {
            isCodePending = false;
             // 检查是否有待处理的脚本
            string scriptPath = EditorPrefs.GetString(SCRIPT_PATH_KEY, "");
            string targetObjectIdString = EditorPrefs.GetString(TARGET_GAMEOBJECT_ID_KEY, "");
            int windowInstanceId = EditorPrefs.GetInt(WINDOW_INSTANCE_ID_KEY, -1);

            if (!string.IsNullOrEmpty(scriptPath) && !string.IsNullOrEmpty(targetObjectIdString))
            {
                // 恢复目标GameObject引用
                GlobalObjectId targetObjectId;
                if (GlobalObjectId.TryParse(targetObjectIdString, out targetObjectId))
                {
                    var targetObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(targetObjectId);
                    if (targetObject is GameObject targetGameObject)
                    {
                        // 通过脚本路径获取类型
                        var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                        if (monoScript != null)
                        {
                            var scriptType = monoScript.GetClass();
                            if (scriptType != null)
                            {
                                targetGameObject.AddComponent(scriptType);
                                Debug.Log($"Successfully added {scriptType.Name} to {targetGameObject.name}");

                                // 关闭生成窗口
                                if (windowInstanceId != -1)
                                {
                                    var window = EditorUtility.InstanceIDToObject(windowInstanceId) as EditorWindow;
                                    if (window != null)
                                    {
                                        window.Close();
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError($"Failed to get type from script: {scriptPath}");
                            }
                        }
                        else
                        {
                            Debug.LogError($"Failed to load script at path: {scriptPath}");
                        }
                    }
                }

                // 清理EditorPrefs
                CleanupEditorPrefs();
            }
        }

        private void SavePendingData(string scriptName, string scriptPath)
        {
            // 保存窗口实例ID，用于识别哪个窗口在等待编译完成
            EditorPrefs.SetInt(WINDOW_INSTANCE_ID_KEY, GetInstanceID());
            
            // 保存脚本名称
            EditorPrefs.SetString(PENDING_SCRIPT_KEY, scriptName);
            
            // 保存脚本路径
            EditorPrefs.SetString(SCRIPT_PATH_KEY, scriptPath);
            
            // 保存目标GameObject的GlobalObjectId
            if (targetGameObject != null)
            {
                var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(targetGameObject);
                EditorPrefs.SetString(TARGET_GAMEOBJECT_ID_KEY, globalObjectId.ToString());
            }
        }

        private static void CleanupEditorPrefs()
        {
            EditorPrefs.DeleteKey(PENDING_SCRIPT_KEY);
            EditorPrefs.DeleteKey(TARGET_GAMEOBJECT_ID_KEY);
            EditorPrefs.DeleteKey(WINDOW_INSTANCE_ID_KEY);
            EditorPrefs.DeleteKey(SCRIPT_PATH_KEY);
        }

        private void OnGUI()
        {
            if (targetGameObject == null)
            {
                EditorGUILayout.HelpBox("No GameObject selected", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(10);
            
            // 输入区域
            EditorGUILayout.LabelField($"Generate component for '{targetGameObject.name}':", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox("Describe the functionality you want for this component.", MessageType.Info);
            
            // 输入滚动区域
            inputScrollPosition = EditorGUILayout.BeginScrollView(inputScrollPosition, GUILayout.Height(100));
            using (new EditorGUI.DisabledScope(isGenerating))
            {
                scriptPrompt = EditorGUILayout.TextArea(scriptPrompt, GUILayout.ExpandHeight(true));
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // 生成按钮
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(scriptPrompt) || isGenerating))
            {
                if (GUILayout.Button("Generate And Add", GUILayout.Height(30)))
                {
                    GenerateAndAddComponent();
                }
            }

            EditorGUILayout.Space(10);

            // 输出区域
            if (isGenerating || !string.IsNullOrEmpty(generatingContent))
            {
                EditorGUILayout.LabelField("Generation Progress:", EditorStyles.boldLabel);
                
                if (!string.IsNullOrEmpty(currentPhase))
                {
                    EditorGUILayout.HelpBox(currentPhase, MessageType.Info);
                }

                // 输出滚动区域
                outputScrollPosition = EditorGUILayout.BeginScrollView(outputScrollPosition, GUILayout.Height(200));
                EditorGUILayout.TextArea(generatingContent, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        private async void GenerateAndAddComponent()
        {
            if(isGenerating || componentGenerator == null) return;
            
            try
            {
                isGenerating = true;
                generatingContent = ""; // 清空之前的内容
                
                // 1. 生成脚本名称
                currentPhase = "Generating script name...";
                Repaint();
                
                var namePrompt = $"为GameObject '{targetGameObject.name}'生成一个合适的组件名称。功能描述：{scriptPrompt}\n" +
                               "只返回类名，不需要任何其他文字。";
                var nameResponse = await componentGenerator.SendMessageAsync(namePrompt);
                string generatedScriptName = nameResponse.content.Trim();
                
                // 2. 生成脚本内容
                currentPhase = "Generating script content...";
                generatingContent = ""; // 清空名称生成的内容
                Repaint();
                
                var codePrompt = $"为GameObject '{targetGameObject.name}'创建一个组件脚本。要求：\n" +
                                $"1. 脚本名称为 {generatedScriptName}\n" +
                                $"2. 功能描述：{scriptPrompt}\n" +
                                $"3. 生成完整的MonoBehaviour脚本，包含必要的using语句\n" +
                                $"4. 添加适当的注释说明\n" +
                                "请只返回脚本的完整代码，不需要其他解释。";
                
                var codeResponse = await componentGenerator.SendMessageAsync(codePrompt);
                Debug.Log($"Generated script name: {generatedScriptName}");
                Debug.Log($"Generated script: {codeResponse.content}");
                
                // 提取生成的代码内容
                string generatedScriptContent = ExtractCodeFromResponse(codeResponse.content);
                
                // 创建并添加脚本
                if (!string.IsNullOrEmpty(generatedScriptContent))
                {
                    currentPhase = "Creating script file...";
                    Repaint();

                    // 获取脚本将要创建的路径
                    string scriptPath = Path.Combine("Assets/Scripts", $"{generatedScriptName}.cs");

                    // 保存必要的信息到EditorPrefs
                    SavePendingData(generatedScriptName, scriptPath);
                    isCodePending = true;
                    // 直接调用CreateScript方法
                    UnityCommandExecutor.CreateScript(new string[] { generatedScriptName, generatedScriptContent });
                    
                    // 刷新资产数据库以触发编译
                    AssetDatabase.Refresh();

                    currentPhase = "Waiting for script compilation...";
                    Repaint();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error generating component: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate component: {ex.Message}", "OK");
                currentPhase = "Error: Generation failed";
                isCodePending = false;
                Repaint();
                CleanupEditorPrefs();
            }
            finally
            {
                isGenerating = false;
                Repaint();
            }
        }

        private string ExtractCodeFromResponse(string response)
        {
            // 如果响应中包含代码块标记，提取其中的代码
            if (response.Contains("```"))
            {
                var startIndex = response.IndexOf("```") + 3;
                var endIndex = response.LastIndexOf("```");
                
                // 跳过语言标识符行
                if (response.Contains("```csharp") || response.Contains("```cs"))
                {
                    startIndex = response.IndexOf('\n', startIndex) + 1;
                }
                
                if (startIndex < endIndex && startIndex > 3)
                {
                    return response.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            
            // 如果没有代码块标记，直接返回整个响应
            return response.Trim();
        }
    }
}
