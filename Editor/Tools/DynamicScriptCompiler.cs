using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace UnityAIHelper.Editor.Tools
{
    /// <summary>
    /// 动态脚本编译器
    /// </summary>
    public static class DynamicScriptCompiler
    {
        private static readonly string TempScriptFolder = "Assets/UnityAIHelper/Editor/TempScripts";
        
        /// <summary>
        /// 编译并创建工具实例
        /// </summary>
        public static async Task<IUnityTool> CompileAndCreateToolAsync(string name, string scriptContent)
        {
            try
            {
                // 确保临时脚本文件夹存在
                if (!Directory.Exists(TempScriptFolder))
                {
                    Directory.CreateDirectory(TempScriptFolder);
                }

                // 生成完整的脚本内容
                var fullScriptContent = GenerateFullScriptContent(name, scriptContent);
                
                // 保存临时脚本文件
                var scriptPath = Path.Combine(TempScriptFolder, $"{name}.cs");
                File.WriteAllText(scriptPath, fullScriptContent);

                // 刷新资源数据库以识别新文件
                AssetDatabase.Refresh();

                // 编译脚本
                var assembly = await CompileScriptAsync(fullScriptContent);
                if (assembly == null)
                {
                    throw new Exception("Failed to compile script");
                }

                // 创建工具实例
                var toolType = assembly.GetType($"UnityAIHelper.Editor.Tools.{name}");
                if (toolType == null)
                {
                    throw new Exception($"Tool type '{name}' not found in compiled assembly");
                }

                var tool = (IUnityTool)Activator.CreateInstance(toolType);
                return tool;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating tool '{name}': {ex}");
                throw;
            }
        }

        /// <summary>
        /// 生成完整的脚本内容
        /// </summary>
        private static string GenerateFullScriptContent(string name, string scriptContent)
        {
            var sb = new StringBuilder();
            
            // 添加必要的using语句
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEditor;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            
            // 添加命名空间
            sb.AppendLine("namespace UnityAIHelper.Editor.Tools");
            sb.AppendLine("{");
            
            // 如果scriptContent不包含完整的类定义，则添加类定义
            if (!scriptContent.Contains("class"))
            {
                sb.AppendLine($"    public class {name} : IUnityTool");
                sb.AppendLine("    {");
                sb.AppendLine($"        public string Name => \"{name}\";");
                sb.AppendLine("        public string Description { get; }");
                sb.AppendLine("        public ToolType Type => ToolType.TempScript;");
                sb.AppendLine("        public IReadOnlyList<ToolParameter> Parameters { get; }");
                sb.AppendLine("        public IReadOnlyList<string> Dependencies => Array.Empty<string>();");
                sb.AppendLine();
                sb.AppendLine("        " + scriptContent);
                sb.AppendLine("    }");
            }
            else
            {
                sb.AppendLine(scriptContent);
            }
            
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        /// <summary>
        /// 编译脚本
        /// </summary>
        private static async Task<Assembly> CompileScriptAsync(string scriptContent)
        {
            try
            {
                // 创建编译选项
                var options = new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug,
                    allowUnsafe: true
                );

                // 创建语法树
                var syntaxTree = CSharpSyntaxTree.ParseText(
                    SourceText.From(scriptContent),
                    new CSharpParseOptions(LanguageVersion.Latest)
                );

                // 获取程序集引用
                var references = GetAssemblyReferences();

                // 创建编译
                var compilation = CSharpCompilation.Create(
                    Path.GetRandomFileName(),
                    new[] { syntaxTree },
                    references,
                    options
                );

                // 编译到内存流
                using var ms = new MemoryStream();
                var result = compilation.Emit(ms);

                if (!result.Success)
                {
                    // 输出编译错误
                    var errors = result.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.GetMessage());
                    
                    throw new Exception($"Compilation failed: {string.Join("\n", errors)}");
                }

                // 加载程序集
                ms.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(ms.ToArray());
            }
            catch (Exception ex)
            {
                Debug.LogError($"Compilation error: {ex}");
                throw;
            }
        }

        /// <summary>
        /// 获取程序集引用
        /// </summary>
        private static List<MetadataReference> GetAssemblyReferences()
        {
            var references = new List<MetadataReference>();

            // 添加基础程序集引用
            var assemblies = new[]
            {
                typeof(object).Assembly,
                typeof(Task).Assembly,
                typeof(IEnumerable<>).Assembly,
                typeof(UnityEngine.Object).Assembly,
                typeof(UnityEditor.Editor).Assembly,
                Assembly.Load("UnityEngine"),
                Assembly.Load("UnityEditor"),
                Assembly.Load("UnityEngine.CoreModule"),
                Assembly.GetExecutingAssembly()
            };

            foreach (var assembly in assemblies)
            {
                if (assembly != null)
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }

            // 添加当前项目的程序集引用
            var projectAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location));

            foreach (var assembly in projectAssemblies)
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to add reference to {assembly.FullName}: {ex.Message}");
                }
            }

            return references;
        }

        /// <summary>
        /// 清理临时脚本
        /// </summary>
        public static void CleanupTempScripts()
        {
            if (Directory.Exists(TempScriptFolder))
            {
                Directory.Delete(TempScriptFolder, true);
                AssetDatabase.Refresh();
            }
        }
    }
}
