using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor.Tools.AssetTools
{
    public class RenameAssetTool : UnityToolBase
    {
        public override string Name => "RenameAsset";
        public override string Description => "Renames an asset";
        public override ToolType Type => ToolType.Custom;
        public override PermissionType RequiredPermissions => PermissionType.Write;

        protected override void InitializeParameters()
        {
            AddParameter("sourcePath", typeof(string), "Current path of the asset", true);
            AddParameter("newName", typeof(string), "New name for the asset", true);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            string sourcePath = GetParameterValue<string>(parameters, "sourcePath");
            string newName = GetParameterValue<string>(parameters, "newName");

            var result = AssetDatabase.RenameAsset(sourcePath, newName);
            return new Dictionary<string, object>
            {
                { "success", string.IsNullOrEmpty(result) },
                { "originalPath", sourcePath },
                { "newPath", result }
            };
        }
    }
}