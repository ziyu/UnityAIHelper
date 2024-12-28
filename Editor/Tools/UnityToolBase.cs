using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAIHelper.Editor.Tools
{
    /// <summary>
    /// Unity工具基类
    /// </summary>
    public abstract class UnityToolBase : IUnityTool
    {
        private readonly List<ToolParameter> parameters;
        private readonly List<string> dependencies;

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract ToolType Type { get; }
        public abstract PermissionType RequiredPermissions { get; }

        public IReadOnlyList<ToolParameter> Parameters => parameters;
        public IReadOnlyList<string> Dependencies => dependencies;

        protected UnityToolBase()
        {
            parameters = new List<ToolParameter>();
            dependencies = new List<string>();
            InitializeParameters();
            InitializeDependencies();
        }

        protected abstract void InitializeParameters();
        protected virtual void InitializeDependencies() { }

        public abstract Task<object> ExecuteAsync(IDictionary<string, object> parameters);

        protected void AddParameter(string name, Type type, string description, bool isRequired = true, object defaultValue = null)
        {
            parameters.Add(new ToolParameter
            {
                Name = name,
                Type = type,
                Description = description,
                IsRequired = isRequired,
                DefaultValue = defaultValue
            });
        }

        protected void AddDependency(string toolName)
        {
            if (!dependencies.Contains(toolName))
            {
                dependencies.Add(toolName);
            }
        }

        protected T GetParameterValue<T>(IDictionary<string, object> parameters, string name)
        {
            if (!parameters.TryGetValue(name, out var value))
            {
                throw new ArgumentException($"Parameter '{name}' not found");
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to convert parameter '{name}' to type {typeof(T).Name}: {ex.Message}");
            }
        }

        protected T GetParameterValueOrDefault<T>(IDictionary<string, object> parameters, string name, T defaultValue = default)
        {
            if (parameters.TryGetValue(name, out var value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }
    }
}
