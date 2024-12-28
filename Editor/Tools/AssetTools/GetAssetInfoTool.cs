using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor.Tools.AssetTools
{
    public class GetAssetInfoTool : UnityToolBase
    {
        public override string Name => "GetAssetInfo";
        public override string Description => "Gets information about an asset";
        public override ToolType Type => ToolType.Custom;
        public override PermissionType RequiredPermissions => PermissionType.Read;

        protected override void InitializeParameters()
        {
            AddParameter("path", typeof(string), "Path of the asset", true);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            string path = GetParameterValue<string>(parameters, "path");

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