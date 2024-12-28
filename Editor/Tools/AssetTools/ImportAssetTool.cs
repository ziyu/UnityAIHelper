using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor.Tools.AssetTools
{
    public class ImportAssetTool : UnityToolBase
    {
        public override string Name => "ImportAsset";
        public override string Description => "Imports an existing asset into Unity";
        public override ToolType Type => ToolType.Custom;

        protected override void InitializeParameters()
        {
            AddParameter("path", typeof(string), "Path of the asset to import", true);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            string path = GetParameterValue<string>(parameters, "path");

            AssetDatabase.ImportAsset(path);
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            
            return new Dictionary<string, object>
            {
                { "success", asset != null },
                { "path", path },
                { "type", asset?.GetType().Name },
                { "name", asset?.name }
            };
        }
    }
}