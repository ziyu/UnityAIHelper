namespace UnityAIHelper.Editor
{
    /// <summary>
    /// 编辑器资源路径配置
    /// </summary>
    public static class EditorPathConfig
    {
        // 包和项目根路径
        public const string PACKAGE_NAME = "com.ziyu.unity-ai-helper";
        public const string PACKAGE_ROOT = "Packages/" + PACKAGE_NAME;
        public const string ASSETS_ROOT = "Assets/UnityAIHelper";

        // 编辑器相关路径
        public const string EDITOR_ROOT = "Editor";
        
        // UI资源路径
        public const string UI_RESOURCES = EDITOR_ROOT + "/UI/Resources";

        /// <summary>
        /// 获取UI资源在Package中的路径
        /// </summary>
        public static string GetPackageUIPath(string relativePath) => $"{PACKAGE_ROOT}/{UI_RESOURCES}/{relativePath}";

        /// <summary>
        /// 获取UI资源在Assets中的路径
        /// </summary>
        public static string GetAssetsUIPath(string relativePath) => $"{ASSETS_ROOT}/{UI_RESOURCES}/{relativePath}";

        /// <summary>
        /// 获取编辑器资源在Package中的路径
        /// </summary>
        public static string GetPackageEditorPath(string relativePath) => $"{PACKAGE_ROOT}/{EDITOR_ROOT}/{relativePath}";

        /// <summary>
        /// 获取编辑器资源在Assets中的路径
        /// </summary>
        public static string GetAssetsEditorPath(string relativePath) => $"{ASSETS_ROOT}/{EDITOR_ROOT}/{relativePath}";
    }
}
