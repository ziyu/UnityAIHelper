using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using System.IO;
using System;
using UnityLLMAPI.Models;

namespace UnityAIHelper.Editor.Tools.SystemTools
{
    public class CreateScriptTool : UnityToolBase
    {
        private TaskCompletionSource<bool> _compilationComplete;
        
        public override string Name => "CreateScript";
        public override string Description => "Creates a new C# script at target path";
        public override ToolType Type => ToolType.System;
        public override PermissionType RequiredPermissions => PermissionType.Write;

        protected override void InitializeParameters()
        {
            AddParameter("scriptName", typeof(string), "Name of the script to create", true);
            AddParameter("scriptPath", typeof(string), "Path where to create the script (relative to Assets folder)", true);
            AddParameter("script", typeof(string), "Script content", true);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            string scriptName = GetParameterValue<string>(parameters, "scriptName");
            string scriptPath = GetParameterValue<string>(parameters, "scriptPath");
            string script = GetParameterValue<string>(parameters, "script");

            var (fullPath, getScript) = Utils.GetScriptPathAndContent(scriptName, scriptPath);
            if (getScript == script)
            {
                return new Dictionary<string, object>
                {
                    { "success", true },
                    { "scriptPath", fullPath }
                };
            }

            var scriptFullPath = Utils.CreateScript(scriptName,scriptPath,script);
            
            AssetDatabase.Refresh();

            // Wait for compilation to complete
            _compilationComplete = new TaskCompletionSource<bool>();
            
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;

            try 
            {
                await _compilationComplete.Task;
                
                return new Dictionary<string, object>
                {
                    { "success", true },
                    { "scriptPath", scriptFullPath }
                };
            }
            finally
            {
                CompilationPipeline.compilationStarted -= OnCompilationStarted;
                CompilationPipeline.compilationFinished -= OnCompilationFinished;
            }
        }

        private void OnCompilationStarted(object context)
        {
            // Reset completion source if compilation starts again
            if (_compilationComplete.Task.IsCompleted)
            {
                _compilationComplete = new TaskCompletionSource<bool>();
            }
        }

        private void OnCompilationFinished(object context)
        {
            _compilationComplete.TrySetResult(true);
        }
    }
}
