using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor.Tools.SceneTools
{
    public class SceneStructureTool : UnityToolBase
    {
        public override string Name => "SceneStructure";
        public override string Description => "Reads the current scene structure with depth support";
        public override ToolType Type => ToolType.Custom;

        protected override void InitializeParameters()
        {
            AddParameter("maxDepth", typeof(int), "Maximum depth to traverse (0 for unlimited)", false);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            int maxDepth = GetParameterValueOrDefault(parameters, "maxDepth", 0);
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            
            var sceneStructure = new List<object>();
            foreach (var root in rootObjects)
            {
                sceneStructure.Add(ProcessGameObject(root, 1, maxDepth));
            }

            return new Dictionary<string, object>
            {
                { "success", true },
                { "structure", sceneStructure }
            };
        }

        private Dictionary<string, object> ProcessGameObject(GameObject go, int currentDepth, int maxDepth)
        {
            var result = new Dictionary<string, object>
            {
                { "name", go.name },
                { "position", go.transform.position },
                { "children", new List<object>() }
            };

            if (maxDepth > 0 && currentDepth >= maxDepth)
                return result;

            foreach (Transform child in go.transform)
            {
                ((List<object>)result["children"]).Add(
                    ProcessGameObject(child.gameObject, currentDepth + 1, maxDepth)
                );
            }

            return result;
        }
    }
}