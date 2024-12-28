using System;
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
        private Component targetComponent;
        private Vector2 inputScrollPosition;
        private IChatbot componentGenerator;
        private string generatingContent = "";
        private string currentPhase = ""; 
        private string existingScriptContent = "";
        private string newScriptContent = "";
        private CodeDiffViewer codeDiffViewer;
        
        // 新增状态控制
        private bool isCodeGenerated = false;
        private string generatedScriptName = "";
        private string generatedScriptPath = "";

        private const string DefaultGenerateScriptPath = "Assets/Scripts";
        private const string PENDING_SCRIPT_KEY = "UnityAIHelper_PendingScript";
        private const string TARGET_GAMEOBJECT_ID_KEY = "UnityAIHelper_TargetGameObjectID";
        private const string WINDOW_INSTANCE_ID_KEY = "UnityAIHelper_WindowInstanceID";
        private const string SCRIPT_PATH_KEY = "UnityAIHelper_ScriptPath";

        private void OnEnable()
        {
            if (codeDiffViewer == null)
            {
                codeDiffViewer = new CodeDiffViewer(this);
                if (!string.IsNullOrEmpty(existingScriptContent) || !string.IsNullOrEmpty(newScriptContent))
                {
                    codeDiffViewer.SetCodes(existingScriptContent, newScriptContent);
                }
            }
        }

        public void SetTargetGameObject(GameObject gameObject)
        {
            targetGameObject = gameObject;
            componentGenerator = DefaultChatbots.ComponentGenerator;
            componentGenerator.OnStreamingMessage -= OnStreamingResponse;
            componentGenerator.OnStreamingMessage += OnStreamingResponse;
            codeDiffViewer = new CodeDiffViewer(this);
            existingScriptContent = "";
            newScriptContent = "";
            codeDiffViewer.SetCodes(existingScriptContent, newScriptContent);
            ResetGenerationState();
        }

        public void SetTargetComponent(Component component)
        {
            targetComponent = component;
            if (component != null)
            {
                var monoScript = MonoScript.FromMonoBehaviour(component as MonoBehaviour);
                if (monoScript != null)
                {
                    existingScriptContent = monoScript.text;
                    
                    newScriptContent = "";
                    if (codeDiffViewer != null)
                    {
                        codeDiffViewer.SetCodes(existingScriptContent, newScriptContent);
                    }
                }
            }
            ResetGenerationState();
        }

        private void ResetGenerationState()
        {
            isCodeGenerated = false;
            generatedScriptName = "";
            generatedScriptPath = "";
            currentPhase = "";
        }

        private void OnStreamingResponse(ChatMessage genMessage)
        {
            EditorApplication.delayCall += () =>
            {
                generatingContent = genMessage.content;
                string extractedCode = ExtractCodeFromResponse(generatingContent);
                if (!string.IsNullOrEmpty(extractedCode))
                {
                    newScriptContent = extractedCode;
                    if (codeDiffViewer != null)
                    {
                        codeDiffViewer.SetCodes(existingScriptContent, newScriptContent);
                    }
                }
                Repaint();
            };
        }

        private void OnDisable()
        {
            if (!isCodePending)
            {
                CleanupEditorPrefs();
            }
        }

        internal static void HandlePendingCodeGeneration()
        {
            isCodePending = false;
            string scriptPath = EditorPrefs.GetString(SCRIPT_PATH_KEY, "");
            string targetObjectIdString = EditorPrefs.GetString(TARGET_GAMEOBJECT_ID_KEY, "");
            int windowInstanceId = EditorPrefs.GetInt(WINDOW_INSTANCE_ID_KEY, -1);

            if (!string.IsNullOrEmpty(scriptPath) && !string.IsNullOrEmpty(targetObjectIdString))
            {
                GlobalObjectId targetObjectId;
                if (GlobalObjectId.TryParse(targetObjectIdString, out targetObjectId))
                {
                    var targetObject = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(targetObjectId);
                    if (targetObject is GameObject targetGameObject)
                    {
                        var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                        if (monoScript != null)
                        {
                            var scriptType = monoScript.GetClass();
                            if (scriptType != null)
                            {
                                if (targetGameObject.GetComponent(scriptType) == null)
                                {
                                    targetGameObject.AddComponent(scriptType);
                                    Debug.Log($"Successfully added {scriptType.Name} to {targetGameObject.name}");
                                }

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

                CleanupEditorPrefs();
            }
        }

        private void SavePendingData(string scriptName, string scriptPath)
        {
            EditorPrefs.SetInt(WINDOW_INSTANCE_ID_KEY, GetInstanceID());
            EditorPrefs.SetString(PENDING_SCRIPT_KEY, scriptName);
            EditorPrefs.SetString(SCRIPT_PATH_KEY, scriptPath);
            
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
            EditorGUILayout.LabelField(targetComponent != null ? "Modify component:" : $"Generate component for '{targetGameObject.name}':", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox(targetComponent != null ? "Describe the modifications you want to make to this component." : "Describe the functionality you want for this component.", MessageType.Info);
            
            // 输入滚动区域
            inputScrollPosition = EditorGUILayout.BeginScrollView(inputScrollPosition, GUILayout.Height(100));
            using (new EditorGUI.DisabledScope(isGenerating || isCodeGenerated))
            {
                scriptPrompt = EditorGUILayout.TextArea(scriptPrompt, GUILayout.ExpandHeight(true));
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // 生成按钮区域
            if (!isCodeGenerated)
            {
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(scriptPrompt) || isGenerating))
                {
                    if (GUILayout.Button(targetComponent != null ? "Generate Modified Version" : "Generate Code", GUILayout.Height(30)))
                    {
                        GenerateComponent();
                    }
                }
            }
            else
            {
                // 确认区域
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Apply Code", GUILayout.Height(30)))
                {
                    ApplyGeneratedCode();
                }
                
                if (GUILayout.Button("Regenerate", GUILayout.Height(30)))
                {
                    ResetGenerationState();
                    GenerateComponent();
                }
                
                if (GUILayout.Button("Cancel", GUILayout.Height(30)))
                {
                    ResetGenerationState();
                    Repaint();
                }
                
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(10);

            // 生成状态
            if (isGenerating || !string.IsNullOrEmpty(currentPhase))
            {
                EditorGUILayout.LabelField("Generation Progress:", EditorStyles.boldLabel);
                
                if (!string.IsNullOrEmpty(currentPhase))
                {
                    EditorGUILayout.HelpBox(currentPhase, MessageType.Info);
                }
            }

            EditorGUILayout.Space(10);

            // 代码对比区域
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            if (codeDiffViewer != null)
            {
                codeDiffViewer.OnGUI();
            }
            EditorGUILayout.EndVertical();
        }

        private async void GenerateComponent()
        {
            if(isGenerating || componentGenerator == null) return;
            
            try
            {
                isGenerating = true;
                generatingContent = "";
                newScriptContent = "";
                componentGenerator.ClearHistory();
                
                if (targetComponent != null)
                {
                    // 如果是修改现有组件，使用相同的名称
                    generatedScriptName = targetComponent.GetType().Name;
                    currentPhase = "Generating modified script content...";
                }
                else
                {
                    // 生成新的脚本名称
                    currentPhase = "Generating script name...";
                    Repaint();
                    
                    var namePrompt = $"为GameObject '{targetGameObject.name}'生成一个合适的组件名称。功能描述：{scriptPrompt}\n" +
                                   "只返回类名，不需要任何其他文字。";
                    var nameResponse = await componentGenerator.SendMessageAsync(namePrompt);
                    generatedScriptName = nameResponse.content.Trim();
                    
                    currentPhase = "Generating script content...";
                    generatingContent = "";
                }
                
                Repaint();
                
                string codePrompt;
                if (targetComponent != null && !string.IsNullOrEmpty(existingScriptContent))
                {
                    codePrompt = $"修改现有的组件脚本。要求：\n" +
                                $"1. 脚本名称为 {generatedScriptName}\n" +
                                $"2. 修改需求：{scriptPrompt}\n" +
                                $"3. 现有代码：\n{existingScriptContent}\n" +
                                $"4. 保持原有功能的基础上进行改进\n" +
                                $"5. 添加适当的注释说明\n" +
                                "请只返回完整的修改后的代码，不需要其他解释。";
                    
                    var monoScript = MonoScript.FromMonoBehaviour(targetComponent as MonoBehaviour);
                    generatedScriptPath = AssetDatabase.GetAssetPath(monoScript);
                }
                else
                {
                    codePrompt = $"为GameObject '{targetGameObject.name}'创建一个组件脚本。要求：\n" +
                                $"1. 脚本名称为 {generatedScriptName}\n" +
                                $"2. 功能描述：{scriptPrompt}\n" +
                                $"3. 生成完整的MonoBehaviour脚本，包含必要的using语句\n" +
                                $"4. 添加适当的注释说明\n" +
                                "请只返回脚本的完整代码，不需要其他解释。";
                    generatedScriptPath = Path.Combine(DefaultGenerateScriptPath,$"{generatedScriptName}.cs");
                }
                
                var codeResponse = await componentGenerator.SendMessageAsync(codePrompt);
                Debug.Log($"Generated script name: {generatedScriptName}");
                Debug.Log($"Generated script: {codeResponse.content}");
                
                string generatedScriptContent = ExtractCodeFromResponse(codeResponse.content);
                
                if (!string.IsNullOrEmpty(generatedScriptContent))
                {
                    newScriptContent = generatedScriptContent;
                    if (codeDiffViewer != null)
                    {
                        codeDiffViewer.SetCodes(existingScriptContent, newScriptContent);
                    }

                    currentPhase = "Code generation completed. Please review the changes.";
                    isCodeGenerated = true;
                    Repaint();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error generating component: {ex}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate component: {ex.Message}", "OK");
                currentPhase = "Error: Generation failed";
                ResetGenerationState();
                Repaint();
            }
            finally
            {
                isGenerating = false;
                Repaint();
            }
        }

        private void ApplyGeneratedCode()
        {
            if (string.IsNullOrEmpty(newScriptContent) || string.IsNullOrEmpty(generatedScriptName))
            {
                Debug.LogError("No generated code to apply");
                return;
            }

            try
            {
                SavePendingData(generatedScriptName, generatedScriptPath);
                isCodePending = true;
                var scriptFullPath = Utils.CreateScript(generatedScriptName,generatedScriptPath,newScriptContent);
                AssetDatabase.Refresh();
                currentPhase = "Waiting for script compilation...";
                Repaint();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error applying code: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to apply code: {ex.Message}", "OK");
                currentPhase = "Error: Failed to apply code";
                isCodePending = false;
                CleanupEditorPrefs();
                Repaint();
            }
        }

        private string ExtractCodeFromResponse(string response)
        {
            if (response.Contains("```"))
            {
                var startIndex = response.IndexOf("```", StringComparison.Ordinal) + 3;
                var endIndex = response.LastIndexOf("```", StringComparison.Ordinal);
 

                if (response.Contains("```csharp") || response.Contains("```cs"))
                {
                    startIndex = response.IndexOf('\n', startIndex) + 1;
                }
                
                if (endIndex <=startIndex)
                {
                    endIndex = response.Length - 1;
                }
                
                if (startIndex < endIndex && startIndex > 3)
                {
                    return response.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            
            return response.Trim();
        }
    }
}
