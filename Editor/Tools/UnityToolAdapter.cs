using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityLLMAPI.Models;
using UnityLLMAPI.Utils.Json;

namespace UnityAIHelper.Editor.Tools
{
    /// <summary>
    /// 将IUnityTool适配为UnityLLMAPI的Tool格式
    /// </summary>
    public static class UnityToolAdapter
    {
        /// <summary>
        /// 将IUnityTool转换为UnityLLMAPI的Tool
        /// </summary>
        public static Tool ToLLMTool(IUnityTool unityTool)
        {
            var tool = new Tool
            {
                type = "function",
                function = new ToolFunction
                {
                    name = unityTool.Name,
                    description = unityTool.Description,
                    parameters = CreateToolParameters(unityTool.Parameters)
                }
            };

            return tool;
        }

        /// <summary>
        /// 创建工具参数定义
        /// </summary>
        private static ToolParameters CreateToolParameters(IReadOnlyList<ToolParameter> parameters)
        {
            var toolParams = new ToolParameters
            {
                type = "object",
                properties = new Dictionary<string, ToolParameterProperty>(),
                required = parameters.Where(p => p.IsRequired)
                                   .Select(p => p.Name)
                                   .ToArray()
            };

            foreach (var param in parameters)
            {
                toolParams.properties[param.Name] = new ToolParameterProperty
                {
                    type = GetJsonSchemaType(param.Type),
                    description = param.Description
                };
            }

            return toolParams;
        }

        /// <summary>
        /// 获取类型对应的JSON Schema类型
        /// </summary>
        private static string GetJsonSchemaType(Type type)
        {
            if (type == typeof(string))
                return "string";
            if (type == typeof(int) || type == typeof(float) || type == typeof(double))
                return "number";
            if (type == typeof(bool))
                return "boolean";
            if (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
                return "array";
            return "object";
        }

        /// <summary>
        /// 创建工具调用处理函数
        /// </summary>
        public static Func<ToolCall, Task<string>> CreateToolHandler(IUnityTool unityTool, ToolExecutor executor)
        {
            return async (ToolCall toolCall) =>
            {
                try
                {
                    // 解析参数
                    var parameters = ParseToolCallArguments(toolCall, unityTool.Parameters);
                    
                    // 执行工具
                    var result = await executor.ExecuteToolAsync(unityTool.Name, parameters);

                    // 转换结果为字符串
                    var resultJson = JsonConverter.SerializeObject(result);
                    Debug.Log(resultJson);
                    return resultJson;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error executing tool {unityTool.Name}: {ex}");
                    throw;
                }
            };
        }

        /// <summary>
        /// 解析工具调用参数
        /// </summary>
        private static Dictionary<string, object> ParseToolCallArguments(ToolCall toolCall, IReadOnlyList<ToolParameter> parameterDefs)
        {
            var parameters = new Dictionary<string, object>();
            
            if (string.IsNullOrEmpty(toolCall.function?.arguments))
                return parameters;

            try
            {
                var args = JsonConverter.DeserializeObject<Dictionary<string, object>>(toolCall.function.arguments);
                foreach (var paramDef in parameterDefs)
                {
                    if (args.TryGetValue(paramDef.Name, out var value))
                    {
                        parameters[paramDef.Name] = ConvertValue(value, paramDef.Type);
                    }
                    else if (paramDef.IsRequired)
                    {
                        throw new ArgumentException($"Required parameter '{paramDef.Name}' is missing");
                    }
                    else if (paramDef.DefaultValue != null)
                    {
                        parameters[paramDef.Name] = paramDef.DefaultValue;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to parse tool arguments: {ex.Message}");
            }

            return parameters;
        }

        /// <summary>
        /// 转换值到指定类型
        /// </summary>
        private static object ConvertValue(object value, Type targetType)
        {
            try
            {
                if (value == null)
                {
                    return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                }

                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, value.ToString());
                }

                if (targetType == typeof(Vector2) && value is string vectorStr)
                {
                    var parts = vectorStr.Split(',');
                    return new Vector2(
                        float.Parse(parts[0]),
                        float.Parse(parts[1])
                    );
                }

                if (targetType == typeof(Vector3) && value is string vectorStr3)
                {
                    var parts = vectorStr3.Split(',');
                    return new Vector3(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2])
                    );
                }

                // 处理数组和列表类型
                if (targetType.IsArray)
                {
                    var elementType = targetType.GetElementType();
                    var sourceArray = value as System.Collections.IEnumerable;
                    if (sourceArray == null) throw new ArgumentException($"Cannot convert {value.GetType()} to array");

                    var elements = sourceArray.Cast<object>()
                        .Select(item => ConvertValue(item, elementType))
                        .ToArray();

                    var array = Array.CreateInstance(elementType, elements.Length);
                    Array.Copy(elements, array, elements.Length);
                    return array;
                }

                // 处理泛型列表
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementType = targetType.GetGenericArguments()[0];
                    var sourceList = value as System.Collections.IEnumerable;
                    if (sourceList == null) throw new ArgumentException($"Cannot convert {value.GetType()} to list");

                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var list = (System.Collections.IList)Activator.CreateInstance(listType);

                    foreach (var item in sourceList)
                    {
                        list.Add(ConvertValue(item, elementType));
                    }

                    return list;
                }

                // 基本类型转换
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to convert value '{value}' to type {targetType.Name}: {ex.Message}");
            }
        }
    }
}
