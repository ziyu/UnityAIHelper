using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor.Tools.AssetTools
{
    public class MoveAssetTool : UnityToolBase
    {
        public override string Name => "MoveAsset";
        public override string Description => "Moves an asset to a new location";
        public override ToolType Type => ToolType.Custom;
        public override PermissionType RequiredPermissions => PermissionType.Read | PermissionType.Write;

        protected override void InitializeParameters()
        {
            AddParameter("sourcePath", typeof(string), "Current path of the asset", true);
            AddParameter("destinationPath", typeof(string), "New path for the asset", true);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            string sourcePath = GetParameterValue<string>(parameters, "sourcePath");
            string destinationPath = GetParameterValue<string>(parameters, "destinationPath");

            var result = AssetDatabase.MoveAsset(sourcePath, destinationPath);
            return new Dictionary<string, object>
            {
                { "success", string.IsNullOrEmpty(result) },
                { "sourcePath", sourcePath },
                { "destinationPath", destinationPath }
            };
        }
    }
}