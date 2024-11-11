using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityAIHelper.Editor.Tools.UnityTools
{
    /// <summary>
    /// 创建GameObject工具
    /// </summary>
    public class CreateGameObjectTool : GameObjectToolBase
    {
        public override string Name => "CreateGameObject";
        public override string Description => "创建一个新的GameObject";

        protected override void InitializeParameters()
        {
            AddParameter("name", typeof(string), "GameObject的名称");
            AddParameter("type", typeof(string), "GameObject的类型(empty/cube/sphere/cylinder/plane/capsule)", true, "empty");
            AddParameter("position", typeof(Vector3), "位置", false, Vector3.zero);
            AddParameter("rotation", typeof(Vector3), "旋转", false, Vector3.zero);
            AddParameter("scale", typeof(Vector3), "缩放", false, Vector3.one);
            AddParameter("parent", typeof(string), "父物体名称", false);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var name = GetParameterValue<string>(parameters, "name");
            var type = GetParameterValue<string>(parameters, "type").ToLower();
            var position = GetParameterValueOrDefault(parameters, "position", Vector3.zero);
            var rotation = GetParameterValueOrDefault(parameters, "rotation", Vector3.zero);
            var scale = GetParameterValueOrDefault(parameters, "scale", Vector3.one);
            var parentName = GetParameterValueOrDefault<string>(parameters, "parent", null);

            GameObject go = null;
            switch (type)
            {
                case "cube":
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
                case "sphere":
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;
                case "cylinder":
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    break;
                case "plane":
                    go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    break;
                case "capsule":
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    break;
                default:
                    go = new GameObject();
                    break;
            }

            go.name = name;
            go.transform.position = position;
            go.transform.eulerAngles = rotation;
            go.transform.localScale = scale;

            if (!string.IsNullOrEmpty(parentName))
            {
                var parent = GameObject.Find(parentName);
                if (parent != null)
                {
                    go.transform.SetParent(parent.transform, true);
                }
            }

            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            return go;
        }
    }

    /// <summary>
    /// 查找GameObject工具
    /// </summary>
    public class FindGameObjectTool : GameObjectToolBase
    {
        public override string Name => "FindGameObject";
        public override string Description => "查找场景中的GameObject";

        protected override void InitializeParameters()
        {
            AddParameter("name", typeof(string), "GameObject的名称");
            AddParameter("searchInactive", typeof(bool), "是否搜索未激活的物体", false, false);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var name = GetParameterValue<string>(parameters, "name");
            var searchInactive = GetParameterValueOrDefault(parameters, "searchInactive", false);

            GameObject go = null;
            if (searchInactive)
            {
                go = GameObject.Find(name);
                if (go == null)
                {
                    var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                    go = Array.Find(allObjects, obj => obj.name == name);
                }
            }
            else
            {
                go = GameObject.Find(name);
            }

            if (go != null)
            {
                Selection.activeGameObject = go;
            }

            return go;
        }
    }

    /// <summary>
    /// 删除GameObject工具
    /// </summary>
    public class DeleteGameObjectTool : GameObjectToolBase
    {
        public override string Name => "DeleteGameObject";
        public override string Description => "删除场景中的GameObject";

        protected override void InitializeParameters()
        {
            AddParameter("name", typeof(string), "GameObject的名称");
            AddParameter("includeChildren", typeof(bool), "是否包含子物体", false, true);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var name = GetParameterValue<string>(parameters, "name");
            var includeChildren = GetParameterValueOrDefault(parameters, "includeChildren", true);

            var go = GameObject.Find(name);
            if (go != null)
            {
                if (!includeChildren)
                {
                    // 将子物体移到父物体下
                    var parent = go.transform.parent;
                    foreach (Transform child in go.transform)
                    {
                        Undo.SetTransformParent(child, parent, "Reparent " + child.name);
                    }
                }

                Undo.DestroyObjectImmediate(go);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 复制GameObject工具
    /// </summary>
    public class DuplicateGameObjectTool : GameObjectToolBase
    {
        public override string Name => "DuplicateGameObject";
        public override string Description => "复制场景中的GameObject";

        protected override void InitializeParameters()
        {
            AddParameter("name", typeof(string), "源GameObject的名称");
            AddParameter("newName", typeof(string), "新GameObject的名称", false);
            AddParameter("position", typeof(Vector3), "新位置", false);
        }

        public override async Task<object> ExecuteAsync(IDictionary<string, object> parameters)
        {
            var name = GetParameterValue<string>(parameters, "name");
            var newName = GetParameterValueOrDefault<string>(parameters, "newName", null);
            var position = GetParameterValueOrDefault<Vector3?>(parameters, "position", null);

            var sourceGo = GameObject.Find(name);
            if (sourceGo != null)
            {
                var newGo = UnityEngine.Object.Instantiate(sourceGo);
                if (!string.IsNullOrEmpty(newName))
                {
                    newGo.name = newName;
                }
                else
                {
                    newGo.name = sourceGo.name + " Copy";
                }

                if (position.HasValue)
                {
                    newGo.transform.position = position.Value;
                }

                Undo.RegisterCreatedObjectUndo(newGo, "Duplicate " + sourceGo.name);
                return newGo;
            }

            return null;
        }
    }
}
