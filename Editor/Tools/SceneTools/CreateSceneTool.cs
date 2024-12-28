using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor.Tools.SceneTools
{
    public class CreateSceneTool : UnityToolBase
    {
        public override string Name => "CreateScene";
        public override string Description => "Creates a new Unity scene";
        public override ToolType Type => ToolType.Custom;

        protected override void InitializeParameters()
        {
            AddParameter("sceneName", typeof(string), "Name of the scene", true);
            AddParameter("scenePath", typeof(string), "Path to save scene", false);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            string sceneName = GetParameterValue<string>(parameters, "sceneName");
            string scenePath = GetParameterValue<string>(parameters, "scenePath");

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            if (!string.IsNullOrEmpty(scenePath))
            {
                if (!scenePath.EndsWith(".unity"))
                    scenePath += ".unity";
                
                EditorSceneManager.SaveScene(newScene, scenePath);
            }

            return new Dictionary<string, object>
            {
                { "success", true },
                { "scenePath", scenePath },
                { "sceneName", sceneName }
            };
        }
    }
}