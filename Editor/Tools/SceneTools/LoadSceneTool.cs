using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor.Tools.SceneTools
{
    public class LoadSceneTool : UnityToolBase
    {
        public override string Name => "LoadScene";
        public override string Description => "Loads an existing Unity scene";
        public override ToolType Type => ToolType.Custom;

        protected override void InitializeParameters()
        {
            AddParameter("scenePath", typeof(string), "Path to the scene file", true);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            string scenePath = GetParameterValue<string>(parameters, "scenePath");

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            return new Dictionary<string, object>
            {
                { "success", true },
                { "scenePath", scenePath },
                { "sceneName", scene.name }
            };
        }
    }
}