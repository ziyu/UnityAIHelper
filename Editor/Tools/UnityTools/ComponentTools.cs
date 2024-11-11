using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityAIHelper.Editor.Tools.UnityTools
{
    /// <summary>
    /// 添加组件工具
    /// </summary>
    public class AddComponentTool : GameObjectToolBase
    {
        public override string Name => "AddComponent";
        public override string Description => "向GameObject添加组件";

        protected override void InitializeParameters()
        {
            AddParameter("gameObject", typeof(string), "GameObject的名称");
            AddParameter("componentType", typeof(string), "组件类型名称");
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var gameObject = GetGameObject(parameters);
            var componentTypeName = GetParameterValue<string>(parameters, "componentType");

            var componentType = GetTypeByName(componentTypeName);
            if (componentType == null)
            {
                throw new ArgumentException($"Component type '{componentTypeName}' not found");
            }

            var component = Undo.AddComponent(gameObject, componentType);
            return component;
        }

        private Type GetTypeByName(string typeName)
        {
            // 处理完整的类型名称
            var type = System.Type.GetType(typeName);
            if (type != null) return type;

            // 处理简化的组件名称
            var fullTypeName = $"UnityEngine.{typeName}, UnityEngine.CoreModule";
            type =  System.Type.GetType(fullTypeName);
            if (type != null) return type;

            // 尝试在所有程序集中查找
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetTypes().FirstOrDefault(t => 
                    t.Name == typeName || 
                    t.FullName == typeName ||
                    t.FullName == $"UnityEngine.{typeName}");
                
                if (type != null) return type;
            }

            return null;
        }
    }

    /// <summary>
    /// 删除组件工具
    /// </summary>
    public class RemoveComponentTool : GameObjectToolBase
    {
        public override string Name => "RemoveComponent";
        public override string Description => "从GameObject移除组件";

        protected override void InitializeParameters()
        {
            AddParameter("gameObject", typeof(string), "GameObject的名称");
            AddParameter("componentType", typeof(string), "组件类型名称");
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var gameObject = GetGameObject(parameters);
            var componentTypeName = GetParameterValue<string>(parameters, "componentType");

            var component = gameObject.GetComponents<Component>()
                .FirstOrDefault(c => c.GetType().Name == componentTypeName || c.GetType().FullName == componentTypeName);

            if (component != null)
            {
                Undo.DestroyObjectImmediate(component);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 获取组件属性工具
    /// </summary>
    public class GetComponentPropertyTool : GameObjectToolBase
    {
        public override string Name => "GetComponentProperty";
        public override string Description => "获取组件的属性值";

        protected override void InitializeParameters()
        {
            AddParameter("gameObject", typeof(string), "GameObject的名称");
            AddParameter("componentType", typeof(string), "组件类型名称");
            AddParameter("propertyName", typeof(string), "属性名称");
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var gameObject = GetGameObject(parameters);
            var componentTypeName = GetParameterValue<string>(parameters, "componentType");
            var propertyName = GetParameterValue<string>(parameters, "propertyName");

            var component = gameObject.GetComponents<Component>()
                .FirstOrDefault(c => c.GetType().Name == componentTypeName || c.GetType().FullName == componentTypeName);

            if (component == null)
            {
                throw new ArgumentException($"Component '{componentTypeName}' not found on GameObject '{gameObject.name}'");
            }

            var property = component.GetType().GetProperty(propertyName, 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (property == null)
            {
                var field = component.GetType().GetField(propertyName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (field == null)
                {
                    throw new ArgumentException($"Property or field '{propertyName}' not found on component '{componentTypeName}'");
                }

                return field.GetValue(component);
            }

            return property.GetValue(component);
        }
    }

    /// <summary>
    /// 设置组件属性工具
    /// </summary>
    public class SetComponentPropertyTool : GameObjectToolBase
    {
        public override string Name => "SetComponentProperty";
        public override string Description => "设置组件的属性值";

        protected override void InitializeParameters()
        {
            AddParameter("gameObject", typeof(string), "GameObject的名称");
            AddParameter("componentType", typeof(string), "组件类型名称");
            AddParameter("propertyName", typeof(string), "属性名称");
            AddParameter("value", typeof(string), "属性值");
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var gameObject = GetGameObject(parameters);
            var componentTypeName = GetParameterValue<string>(parameters, "componentType");
            var propertyName = GetParameterValue<string>(parameters, "propertyName");
            var value = GetParameterValue<string>(parameters, "value");

            var component = gameObject.GetComponents<Component>()
                .FirstOrDefault(c => c.GetType().Name == componentTypeName || c.GetType().FullName == componentTypeName);

            if (component == null)
            {
                throw new ArgumentException($"Component '{componentTypeName}' not found on GameObject '{gameObject.name}'");
            }

            Undo.RecordObject(component, $"Set {propertyName}");

            var property = component.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (property != null)
            {
                var convertedValue = ConvertValue(value, property.PropertyType);
                property.SetValue(component, convertedValue);
                return true;
            }

            var field = component.GetType().GetField(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null)
            {
                var convertedValue = ConvertValue(value, field.FieldType);
                field.SetValue(component, convertedValue);
                return true;
            }

            throw new ArgumentException($"Property or field '{propertyName}' not found on component '{componentTypeName}'");
        }

        private object ConvertValue(string value, Type targetType)
        {
            if (targetType == typeof(Vector2) && value.Contains(","))
            {
                var parts = value.Split(',');
                return new Vector2(
                    float.Parse(parts[0]),
                    float.Parse(parts[1])
                );
            }

            if (targetType == typeof(Vector3) && value.Contains(","))
            {
                var parts = value.Split(',');
                return new Vector3(
                    float.Parse(parts[0]),
                    float.Parse(parts[1]),
                    float.Parse(parts[2])
                );
            }

            if (targetType == typeof(Color) && value.Contains(","))
            {
                var parts = value.Split(',');
                return new Color(
                    float.Parse(parts[0]),
                    float.Parse(parts[1]),
                    float.Parse(parts[2]),
                    parts.Length > 3 ? float.Parse(parts[3]) : 1f
                );
            }

            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value);
            }

            return Convert.ChangeType(value, targetType);
        }
    }

    /// <summary>
    /// 复制组件工具
    /// </summary>
    public class CopyComponentTool : GameObjectToolBase
    {
        public override string Name => "CopyComponent";
        public override string Description => "复制组件到另一个GameObject";

        protected override void InitializeParameters()
        {
            AddParameter("sourceGameObject", typeof(string), "源GameObject的名称");
            AddParameter("targetGameObject", typeof(string), "目标GameObject的名称");
            AddParameter("componentType", typeof(string), "组件类型名称");
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var sourceGameObject = GameObject.Find(GetParameterValue<string>(parameters, "sourceGameObject"));
            var targetGameObject = GameObject.Find(GetParameterValue<string>(parameters, "targetGameObject"));
            var componentTypeName = GetParameterValue<string>(parameters, "componentType");

            if (sourceGameObject == null || targetGameObject == null)
            {
                throw new ArgumentException("Source or target GameObject not found");
            }

            var sourceComponent = sourceGameObject.GetComponents<Component>()
                .FirstOrDefault(c => c.GetType().Name == componentTypeName || c.GetType().FullName == componentTypeName);

            if (sourceComponent == null)
            {
                throw new ArgumentException($"Component '{componentTypeName}' not found on source GameObject");
            }

            var targetComponent = Undo.AddComponent(targetGameObject, sourceComponent.GetType());
            EditorUtility.CopySerialized(sourceComponent, targetComponent);

            return targetComponent;
        }
    }
}
