using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor.Tools.SceneTools
{
    public class SaveSceneTool : UnityToolBase
    {
        public override string Name => "SaveScene";
        public override string Description => "Saves the current Unity scene";
        public override ToolType Type => ToolType.Custom;

        protected override void InitializeParameters()
        {
            AddParameter("scenePath", typeof(string), "Path to save the scene", true);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            string scenePath = GetParameterValue<string>(parameters, "scenePath");

            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(activeScene, scenePath);
            return new Dictionary<string, object>
            {
                { "success", true },
                { "scenePath", scenePath },
                { "sceneName", activeScene.name }
            };
        }
    }
}