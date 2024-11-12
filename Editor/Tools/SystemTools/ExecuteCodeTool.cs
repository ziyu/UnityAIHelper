using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;

namespace UnityAIHelper.Editor.Tools.SystemTools
{
    /// <summary>
    /// 执行临时代码的工具
    /// </summary>
    public class ExecuteCodeTool : UnityToolBase
    {
        public override string Name => "ExecuteCode";
        public override string Description => "动态编译并执行C#代码";
        public override ToolType Type => ToolType.System;

        private const string TempClassName = "TempClass";
        private const string TempFuncName = "Invoke";

        protected override void InitializeParameters()
        {
            AddParameter("code", typeof(string), "要执行的C#代码");
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var code = GetParameterValue<string>(parameters, "code");
            Assembly tempAssembly = null;
            // 生成完整的类代码
            var fullCode = GenerateFullCode(code, TempClassName, TempFuncName);

            // 编译代码
            tempAssembly = await CompileCodeAsync(fullCode);
            if (tempAssembly == null)
            {
                throw new Exception("Failed to compile code");
            }

            // 执行代码
            var result = await ExecuteCompiledCode(tempAssembly, TempClassName, TempFuncName);
            return result;
        }

        private string GenerateFullCode(string userCode, string className, string methodName)
        {
            return $@"
using UnityEngine;
using UnityEditor;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace UnityAIHelper.Editor.TempCode
{{
    public class {className}
    {{
        public static async Task<object> {methodName}()
        {{
            try
            {{
                {userCode}
            }}
            catch (Exception ex)
            {{
                Debug.LogError($""Code execution error: {{ex}}"");
                throw;
            }}
        }}
    }}
}}";
        }

        private async Task<Assembly> CompileCodeAsync(string code)
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
                    code,
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
                    var errors = string.Join("\n", result.Diagnostics);
                    Debug.LogError($"Compilation errors:\n{errors}");
                    return null;
                }

                // 加载程序集
                ms.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(ms.ToArray());
            }
            catch (Exception ex)
            {
                Debug.LogError($"Compilation error: {ex}");
                return null;
            }
        }

        private List<MetadataReference> GetAssemblyReferences()
        {
            var references = new List<MetadataReference>();

            // 添加基础程序集引用
            var assemblies = new[]
            {
                typeof(object).Assembly,
                typeof(Debug).Assembly,
                typeof(Task).Assembly,
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

        private async Task<object> ExecuteCompiledCode(Assembly assembly, string className, string methodName)
        {
            try
            {
                // 获取类型
                var type = assembly.GetType($"UnityAIHelper.Editor.TempCode.{className}");
                if (type == null)
                {
                    throw new Exception($"Class '{className}' not found");
                }

                // 获取方法
                var method = type.GetMethod(methodName);
                if (method == null)
                {
                    throw new Exception($"Method '{methodName}' not found");
                }

                // 执行方法
                var task = (Task<object>)method.Invoke(null, null);
                return await task;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Execution error: {ex}");
                throw;
            }
        }
    }
}
