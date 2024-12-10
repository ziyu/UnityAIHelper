using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Text;

namespace UnityAIHelper.Editor.Tools.SystemTools
{
    /// <summary>
    /// 执行临时代码的工具
    /// </summary>
    public class ExecuteCodeTool : UnityToolBase
    {
        public override string Name => "ExecuteCode";
        public override string Description => "临时代码执行（ExecuteCode）：\n用于直接执行一段C#代码，无需创建工具，适合一次性的操作。\n代码会放到一个方法中执行，所以必须是一段可以直接执行的代码。\n不能定义类！不能定义类！不能定义类！\n注意代码依赖关系，需要创建的脚本要先创建后再调用其函数。\n最终返回一个字符串作为执行结果。\n参数说明：\n- code: 要执行的C#代码\n\n示例：执行临时代码\n{{\n    \"code\": \"\n        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);\n        cube.transform.position = new Vector3(0, 2, 0);\n        return \"cube created\";\n    \"\n}}";
        public override ToolType Type => ToolType.System;

        private const string TempClassName = "TempClass";
        private const string TempFuncName = "Invoke";

        protected override void InitializeParameters()
        {
            AddParameter("namespaces", typeof(string), "要引用的命名空间(用,分割)");
            AddParameter("code", typeof(string), "要执行的C#代码");
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var namespacesStr = GetParameterValue<string>(parameters, "namespaces");
            var code = GetParameterValue<string>(parameters, "code");

            var namespaces = namespacesStr.Split(',');
            var namespacesCode = new StringBuilder();
            foreach (var ns in namespaces)
            {
                if(string.IsNullOrEmpty(ns.Trim()))continue;
                namespacesCode.Append($"using {ns};\n");
            }
            Assembly tempAssembly = null;
            // 生成完整的类代码
            var fullCode = GenerateFullCode(code, TempClassName, TempFuncName,namespacesCode.ToString());

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

        private string GenerateFullCode(string userCode, string className, string methodName,string usingNameSpaces="")
        {
            return $@"
using UnityEngine;
using UnityEditor;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
{usingNameSpaces}

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
                Debug.LogError($"Compilation errors:\n{string.Join('\n',result.Diagnostics)}");
                int maxUseLine = 10;
                var sb = new StringBuilder();
                foreach (var error in result.Diagnostics)
                {
                    sb.AppendLine(error.ToString());
                    maxUseLine--;
                    if(maxUseLine==0)break;
                }
                throw new Exception($"Compilation errors:\n{sb}");
            }

            // 加载程序集
            ms.Seek(0, SeekOrigin.Begin);
            return Assembly.Load(ms.ToArray());
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
                    Debug.LogWarning($"Failed to add reference to {assembly.FullName}: {ex}");
                }
            }

            return references;
        }

        private async Task<object> ExecuteCompiledCode(Assembly assembly, string className, string methodName)
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
    }
}
