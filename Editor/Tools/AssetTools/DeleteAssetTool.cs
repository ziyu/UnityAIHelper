using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor.Tools.AssetTools
{
    public class DeleteAssetTool : UnityToolBase
    {
        public override string Name => "DeleteAsset";
        public override string Description => "Deletes an asset";
        public override ToolType Type => ToolType.Custom;
        public override PermissionType RequiredPermissions => PermissionType.Delete;

        protected override void InitializeParameters()
        {
            AddParameter("path", typeof(string), "Path of the asset to delete", true);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            string path = GetParameterValue<string>(parameters, "path");

            AssetDatabase.DeleteAsset(path);
            return new Dictionary<string, object>
            {
                { "success", true },
                { "path", path }
            };
        }
    }
}