using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityAIHelper.Editor
{
    /// <summary>
    /// 用于加载包内资源的工具类
    /// </summary>
    public static class PackageAssetLoader
    {
        /// <summary>
        /// 加载UI资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="relativePath">相对于UI/Resources的路径，例如："ToolbarUI.uxml"</param>
        /// <returns>加载的资源</returns>
        public static T LoadUIAsset<T>(string relativePath) where T : UnityEngine.Object
        {
            var packagePath = EditorPathConfig.GetPackageUIPath(relativePath);
            var assetsPath = EditorPathConfig.GetAssetsUIPath(relativePath);

            var asset = EditorGUIUtility.Load(packagePath) as T;
            if (asset == null)
            {
                asset = AssetDatabase.LoadAssetAtPath<T>(assetsPath);
            }

            if (asset == null)
            {
                Debug.LogError($"Failed to load UI asset: {relativePath}");
            }

            return asset;
        }

        /// <summary>
        /// 加载编辑器资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="relativePath">相对于Editor目录的路径</param>
        /// <returns>加载的资源</returns>
        public static T LoadEditorAsset<T>(string relativePath) where T : UnityEngine.Object
        {
            var packagePath = EditorPathConfig.GetPackageEditorPath(relativePath);
            var assetsPath = EditorPathConfig.GetAssetsEditorPath(relativePath);

            var asset = EditorGUIUtility.Load(packagePath) as T;
            if (asset == null)
            {
                asset = AssetDatabase.LoadAssetAtPath<T>(assetsPath);
            }

            if (asset == null)
            {
                Debug.LogError($"Failed to load editor asset: {relativePath}");
            }

            return asset;
        }
    }
}
