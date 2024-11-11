using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityAIHelper.Editor.Tools.UnityTools
{
    /// <summary>
    /// Asset工具基类
    /// </summary>
    public abstract class AssetToolBase : UnityToolBase
    {
        public override ToolType Type => ToolType.Unity;

        protected string GetAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            
            // 确保路径以Assets开头
            if (!path.StartsWith("Assets/"))
            {
                path = Path.Combine("Assets", path);
            }
            
            return path.Replace('\\', '/');
        }
    }

    /// <summary>
    /// 导入Asset工具
    /// </summary>
    public class ImportAssetTool : AssetToolBase
    {
        public override string Name => "ImportAsset";
        public override string Description => "导入资源文件";

        protected override void InitializeParameters()
        {
            AddParameter("sourcePath", typeof(string), "源文件路径");
            AddParameter("targetPath", typeof(string), "目标Asset路径");
            AddParameter("importSettings", typeof(string), "导入设置(JSON格式)", false);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var sourcePath = GetParameterValue<string>(parameters, "sourcePath");
            var targetPath = GetAssetPath(GetParameterValue<string>(parameters, "targetPath"));
            var importSettings = GetParameterValueOrDefault<string>(parameters, "importSettings", null);

            if (!File.Exists(sourcePath))
            {
                throw new Exception($"Source file not found: {sourcePath}");
            }

            // 确保目标目录存在
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // 复制文件到Assets目录
            File.Copy(sourcePath, targetPath, true);
            AssetDatabase.Refresh();

            // 应用导入设置
            if (!string.IsNullOrEmpty(importSettings))
            {
                var importer = AssetImporter.GetAtPath(targetPath);
                if (importer != null)
                {
                    JsonUtility.FromJsonOverwrite(importSettings, importer);
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.AssetPathToGUID(targetPath);
        }
    }

    /// <summary>
    /// 导出Asset工具
    /// </summary>
    public class ExportAssetTool : AssetToolBase
    {
        public override string Name => "ExportAsset";
        public override string Description => "导出资源文件";

        protected override void InitializeParameters()
        {
            AddParameter("assetPath", typeof(string), "Asset路径");
            AddParameter("targetPath", typeof(string), "导出目标路径");
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var assetPath = GetAssetPath(GetParameterValue<string>(parameters, "assetPath"));
            var targetPath = GetParameterValue<string>(parameters, "targetPath");

            if (!File.Exists(assetPath))
            {
                throw new Exception($"Asset not found: {assetPath}");
            }

            var targetDir = Path.GetDirectoryName(targetPath);
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.Copy(assetPath, targetPath, true);
            return targetPath;
        }
    }

    
    /// <summary>
    /// 资源依赖分析工具
    /// </summary>
    public class AnalyzeAssetDependenciesTool : AssetToolBase
    {
        public override string Name => "AnalyzeAssetDependencies";
        public override string Description => "分析资源依赖关系";

        protected override void InitializeParameters()
        {
            AddParameter("assetPath", typeof(string), "Asset路径");
            AddParameter("recursive", typeof(bool), "是否递归分析", false, true);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var assetPath = GetAssetPath(GetParameterValue<string>(parameters, "assetPath"));
            var recursive = GetParameterValueOrDefault(parameters, "recursive", true);

            if (!File.Exists(assetPath))
            {
                throw new Exception($"Asset not found: {assetPath}");
            }

            var dependencies = new HashSet<string>();
            AnalyzeDependencies(assetPath, dependencies, recursive);

            return dependencies.ToList();
        }

        private void AnalyzeDependencies(string assetPath, HashSet<string> dependencies, bool recursive)
        {
            var directDependencies = AssetDatabase.GetDependencies(assetPath, false);
            foreach (var dependency in directDependencies)
            {
                if (dependencies.Add(dependency) && recursive)
                {
                    AnalyzeDependencies(dependency, dependencies, true);
                }
            }
        }
    }

    /// <summary>
    /// 资源引用查找工具
    /// </summary>
    public class FindAssetReferencesTool : AssetToolBase
    {
        public override string Name => "FindAssetReferences";
        public override string Description => "查找资源被引用的位置";

        protected override void InitializeParameters()
        {
            AddParameter("assetPath", typeof(string), "Asset路径");
            AddParameter("searchInScenes", typeof(bool), "是否在场景中搜索", false, true);
            AddParameter("searchInPrefabs", typeof(bool), "是否在预制体中搜索", false, true);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var assetPath = GetAssetPath(GetParameterValue<string>(parameters, "assetPath"));
            var searchInScenes = GetParameterValueOrDefault(parameters, "searchInScenes", true);
            var searchInPrefabs = GetParameterValueOrDefault(parameters, "searchInPrefabs", true);

            if (!File.Exists(assetPath))
            {
                throw new Exception($"Asset not found: {assetPath}");
            }

            var references = new HashSet<string>();
            var targetObject = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            var targetGuid = AssetDatabase.AssetPathToGUID(assetPath);

            // 搜索所有资源文件
            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            foreach (var path in allAssetPaths)
            {
                // 跳过不需要搜索的资源类型
                if (!searchInScenes && path.EndsWith(".unity")) continue;
                if (!searchInPrefabs && path.EndsWith(".prefab")) continue;

                // 检查依赖关系
                var dependencies = AssetDatabase.GetDependencies(path, false);
                if (dependencies.Contains(assetPath))
                {
                    references.Add(path);
                }
            }

            return references.ToList();
        }
    }
}
