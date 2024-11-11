using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

namespace UnityAIHelper.Editor
{
    public static class UnityCommandExecutor
    {
        private static Dictionary<string, Action<string[]>> commandHandlers;

        static UnityCommandExecutor()
        {
            InitializeCommandHandlers();
        }

        private static void InitializeCommandHandlers()
        {
            commandHandlers = new Dictionary<string, Action<string[]>>
            {
                { "create", CreateGameObject },
                { "select", SelectGameObject },
                { "delete", DeleteGameObject },
                { "addcomponent", AddComponent },
                { "setposition", SetPosition },
                { "setrotation", SetRotation },
                { "setscale", SetScale },
                { "save", SaveScene },
                { "build", BuildProject },
                { "batch", ExecuteBatchCommands },
                { "repeat", RepeatCommand },
                { "createscript", CreateScript }
            };
        }

        public static void ExecuteCommand(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine)) return;

            // 移除命令两端的引号（如果有）
            commandLine = commandLine.Trim('"', '\'');
            
            // 使用正则表达式分割命令，保持引号内的内容完整
            var parts = Regex.Matches(commandLine, @"[\""].+?[\""]|[^ ]+")
                             .Cast<Match>()
                             .Select(m => m.Value.Trim('"', '\''))
                             .ToArray();

            if (parts.Length == 0) return;

            var command = parts[0].ToLower();
            var parameters = new string[parts.Length - 1];
            Array.Copy(parts, 1, parameters, 0, parts.Length - 1);

            if (commandHandlers.TryGetValue(command, out var handler))
            {
                try
                {
                    handler(parameters);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error executing command {command}: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"Unknown command: {command}");
            }
        }

        public static void CreateScript(string[] parameters)
        {
            if (parameters.Length < 2) return;

            string scriptName = parameters[0];
            string scriptContent = parameters[1];

            // 确保脚本名称首字母大写
            scriptName = char.ToUpper(scriptName[0]) + scriptName.Substring(1);
            
            // 如果没有.cs后缀，添加它
            if (!scriptName.EndsWith(".cs"))
            {
                scriptName += ".cs";
            }

            // 获取Scripts文件夹路径，如果不存在则创建
            string scriptsFolder = "Assets/Scripts";
            if (!Directory.Exists(scriptsFolder))
            {
                Directory.CreateDirectory(scriptsFolder);
            }

            // 完整的脚本文件路径
            string scriptPath = Path.Combine(scriptsFolder, scriptName);

            try
            {
                // 写入脚本文件
                File.WriteAllText(scriptPath, scriptContent);
                
                // 刷新资源数据库以显示新创建的脚本
                AssetDatabase.Refresh();
                
                Debug.Log($"Successfully created script: {scriptPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating script {scriptName}: {ex.Message}");
            }
        }

        private static void ExecuteBatchCommands(string[] parameters)
        {
            if (parameters.Length == 0) return;
            
            // 移除参数两端的引号（如果有）
            var batchCommands = parameters[0].Trim('"', '\'');
            
            var commands = batchCommands.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var command in commands)
            {
                ExecuteCommand(command.Trim());
            }
        }

        private static void RepeatCommand(string[] parameters)
        {
            if (parameters.Length < 2) return;
            
            if (int.TryParse(parameters[0], out int count))
            {
                // 将剩余参数组合成完整命令
                var command = string.Join(" ", parameters.Skip(1));
                for (int i = 0; i < count; i++)
                {
                    ExecuteCommand(command);
                }
            }
        }

        private static void CreateGameObject(string[] parameters)
        {
            if (parameters.Length == 0) return;

            string name = parameters[0];
            GameObject go;

            // 检查是否是Unity原生基础类型
            switch (name.ToLower())
            {
                case "cube":
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
                case "sphere":
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;
                case "cylinder":
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    break;
                case "plane":
                    go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    break;
                case "capsule":
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    break;
                default:
                    go = new GameObject(name);
                    break;
            }

            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);

            // 处理额外的组件参数
            for (int i = 1; i < parameters.Length; i++)
            {
                var componentName = parameters[i];
                var componentType = GetTypeByName(componentName);
                if (componentType != null)
                {
                    Undo.AddComponent(go, componentType);
                }
            }
        }

        private static void SelectGameObject(string[] parameters)
        {
            if (parameters.Length == 0) return;
            var go = GameObject.Find(parameters[0]);
            if (go != null)
            {
                Selection.activeGameObject = go;
            }
        }

        private static void DeleteGameObject(string[] parameters)
        {
            if (parameters.Length == 0) return;
            var go = GameObject.Find(parameters[0]);
            if (go != null)
            {
                Undo.DestroyObjectImmediate(go);
            }
        }

        private static void AddComponent(string[] parameters)
        {
            if (parameters.Length < 2) return;
            var go = GameObject.Find(parameters[0]);
            if (go == null) return;

            var componentName = parameters[1];
            var componentType = GetTypeByName(componentName);
            if (componentType != null)
            {
                Undo.AddComponent(go, componentType);
            }
        }

        private static void SetPosition(string[] parameters)
        {
            if (parameters.Length < 4) return;
            var go = GameObject.Find(parameters[0]);
            if (go == null) return;

            if (float.TryParse(parameters[1], out float x) &&
                float.TryParse(parameters[2], out float y) &&
                float.TryParse(parameters[3], out float z))
            {
                Undo.RecordObject(go.transform, "Set Position");
                go.transform.position = new Vector3(x, y, z);
            }
        }

        private static void SetRotation(string[] parameters)
        {
            if (parameters.Length < 4) return;
            var go = GameObject.Find(parameters[0]);
            if (go == null) return;

            if (float.TryParse(parameters[1], out float x) &&
                float.TryParse(parameters[2], out float y) &&
                float.TryParse(parameters[3], out float z))
            {
                Undo.RecordObject(go.transform, "Set Rotation");
                go.transform.eulerAngles = new Vector3(x, y, z);
            }
        }

        private static void SetScale(string[] parameters)
        {
            if (parameters.Length < 4) return;
            var go = GameObject.Find(parameters[0]);
            if (go == null) return;

            if (float.TryParse(parameters[1], out float x) &&
                float.TryParse(parameters[2], out float y) &&
                float.TryParse(parameters[3], out float z))
            {
                Undo.RecordObject(go.transform, "Set Scale");
                go.transform.localScale = new Vector3(x, y, z);
            }
        }

        private static void SaveScene(string[] parameters)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        private static void BuildProject(string[] parameters)
        {
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes,
                                    "Builds/Build",
                                    EditorUserBuildSettings.activeBuildTarget,
                                    BuildOptions.None);
        }

        private static Type GetTypeByName(string typeName)
        {
            // 处理完整的类型名称
            var type = Type.GetType(typeName);
            if (type != null) return type;

            // 处理简化的组件名称
            var fullTypeName = $"UnityEngine.{typeName}, UnityEngine.CoreModule";
            type = Type.GetType(fullTypeName);
            if (type != null) return type;

            // 尝试在所有程序集中查找
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName) ?? assembly.GetType($"UnityEngine.{typeName}");
                if (type != null) return type;
            }

            return null;
        }
    }
}
