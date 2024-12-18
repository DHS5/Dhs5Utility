using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
using System.IO;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Databases
{
    public abstract class BaseDatabase : BaseDataContainer
    {
        #region Instance

        private static Dictionary<Type, BaseDatabase> _instances = new();
        internal static BaseDatabase GetInstance(Type type)
        {
            if (!type.IsSubclassOf(typeof(BaseDatabase))) return null;

            if (!_instances.TryGetValue(type, out var instance)
                || instance == null)
            {
                var list = Resources.LoadAll("Databases", type);

                if (list != null && list.Length > 0)
                {
                    instance = list[0] as BaseDatabase;
                    _instances[type] = instance;
                }
#if UNITY_EDITOR
                else
                {
                    instance = BaseDatabase.CreateAssetOfType(type, "Assets/Resources/Databases/" + type.Name + ".asset") as BaseDatabase;
                    AssetDatabase.SaveAssets();
                }
#endif
            }

            return instance;
        }

        internal static BaseDatabase[] GetAllInstances() => GetAllInstances(GetAllChildTypes());
        internal static BaseDatabase[] GetAllInstances(Func<Type, bool> predicate) => GetAllInstances(GetAllChildTypes(t => predicate.Invoke(t)));
        internal static BaseDatabase[] GetAllInstances(Func<DatabaseAttribute, bool> predicate) => GetAllInstances(GetAllChildTypes(da => predicate.Invoke(da)));
        private static BaseDatabase[] GetAllInstances(Type[] childTypes)
        {
            BaseDatabase[] databases = new BaseDatabase[childTypes.Length];

            for (int i = 0; i < databases.Length; i++)
            {
                databases[i] = GetInstance(childTypes[i]);
            }

            return databases;
        }

        #endregion

        #region Instance Editor Methods

#if UNITY_EDITOR

        #region Instance Attribute

        internal string Editor_GetPath()
        {
            return GetPath(GetType());
        }

        internal override bool Editor_ContainerHasValidDataType(out Type dataType)
        {
            return HasDataType(GetType(), out dataType) &&
                typeof(IDataContainerElement).IsAssignableFrom(dataType);
        }

        internal override bool Editor_IsElementValid(UnityEngine.Object element)
        {
            if (element == null || element == this) return false;

            if (Editor_ContainerHasValidDataType(out var type))
            {
                Type elementType = element.GetType();

                // Scriptable
                if (type.IsSubclassOf(typeof(ScriptableObject)))
                {
                    return elementType == type || elementType.IsSubclassOf(type);
                }
                // Component
                if (type.IsSubclassOf(typeof(Component)))
                {
                    return element is GameObject go && go.TryGetComponent(type, out _);
                }

                return false;
            }
            return !HasDataType(GetType(), out _); // True if anyType and false if has data type cause the type is invalid
        }

        #endregion

#endif

        #endregion

        #region Static Editor Functions

#if UNITY_EDITOR

        #region Attributes & Child Types

        private static Dictionary<Type, DatabaseAttribute> _attributes = new();
        protected static bool TryGetAttribute(Type type, out DatabaseAttribute attribute)
        {
            if (_attributes.TryGetValue(type, out attribute))
            {
                return true;
            }

            attribute = type.GetCustomAttribute<DatabaseAttribute>(inherit: true);

            if (attribute != null)
            {
                _attributes.Add(type, attribute);
                return true;
            }
            return false;
        }
        internal static string GetPath(Type type)
        {
            if (TryGetAttribute(type, out var attribute))
            {
                return attribute.path;
            }
            return "Null path";
        }
        internal static bool HasDataType(Type type, out Type dataType)
        {
            if (TryGetAttribute(type, out var attribute))
            {
                dataType = attribute.dataType;
                return !attribute.anyType;
            }
            dataType = null;
            return false;
        }

        private static Type[] GetAllChildTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(BaseDatabase)) && !t.IsAbstract && TryGetAttribute(t, out _))
                .ToArray();
        }
        private static Type[] GetAllChildTypes(Func<Type, bool> predicate)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(BaseDatabase)) && !t.IsAbstract && TryGetAttribute(t, out _) && predicate.Invoke(t))
                .ToArray();
        }
        private static Type[] GetAllChildTypes(Func<DatabaseAttribute, bool> predicate)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(BaseDatabase)) && !t.IsAbstract && TryGetAttribute(t, out var att) && predicate.Invoke(att))
                .ToArray();
        }

        #endregion

        #region Data Creation & Deletion Utility

        #region Creation

        public static UnityEngine.Object CreateAssetOfType(Type type, string path, bool triggerRename = false)
        {
            if (type.IsSubclassOf(typeof(ScriptableObject)))
            {
                return CreateScriptableAsset(type, path, triggerRename);
            }
            else if (type.IsSubclassOf(typeof(Component)))
            {
                return CreatePrefabWithComponent(type, path, triggerRename);
            }
            else if (type == typeof(GameObject))
            {
                return CreateEmptyPrefab(path, triggerRename);
            }
            return null;
        }

        // --- Scriptable ---
        public static ScriptableObject CreateScriptableAsset(Type type, string path, bool triggerRename = false)
        {
            if (!path.EndsWith(".asset")) path += ".asset";
            EditorUtils.EnsureAssetParentDirectoryExistence(path);
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var obj = ScriptableObject.CreateInstance(type);
            if (obj != null)
            {
                AssetDatabase.CreateAsset(obj, path);
                AssetDatabase.SaveAssetIfDirty(obj);
                if (triggerRename) EditorUtils.TriggerAssetRename(obj);
            }
            return obj;
        }
        public static T CreateScriptableAsset<T>(string path, bool triggerRename = false) where T : ScriptableObject
        {
            if (!path.EndsWith(".asset")) path += ".asset";
            EditorUtils.EnsureAssetParentDirectoryExistence(path);
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var obj = ScriptableObject.CreateInstance<T>();
            if (obj != null)
            {
                AssetDatabase.CreateAsset(obj, path);
                AssetDatabase.SaveAssetIfDirty(obj);
                if (triggerRename) EditorUtils.TriggerAssetRename(obj);
            }
            return obj;
        }
        public static ScriptableObject CreateScriptableAndAddToAsset(Type type, UnityEngine.Object asset)
        {
            var obj = ScriptableObject.CreateInstance(type);
            if (obj != null)
            {
                AssetDatabase.AddObjectToAsset(obj, AssetDatabase.GetAssetPath(asset));
                AssetDatabase.SaveAssets();
            }
            return obj;
        }
        public static T CreateScriptableAndAddToAsset<T>(UnityEngine.Object asset) where T : ScriptableObject
        {
            var obj = ScriptableObject.CreateInstance<T>();
            if (obj != null)
            {
                AssetDatabase.AddObjectToAsset(obj, AssetDatabase.GetAssetPath(asset));
                AssetDatabase.SaveAssets();
            }
            return obj;
        }

        // --- Prefab ---
        public static GameObject CreateEmptyPrefab(string path, bool triggerRename = false)
        {
            if (!path.EndsWith(".prefab")) path += ".prefab";
            EditorUtils.EnsureAssetParentDirectoryExistence(path);
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var template = new GameObject();
            var obj = PrefabUtility.SaveAsPrefabAsset(template, path, out var success);
            DestroyImmediate(template);
            if (success)
            {
                if (triggerRename) EditorUtils.TriggerAssetRename(obj);
                return obj;
            }
            return null;
        }
        public static Component CreatePrefabWithComponent(Type behaviourType, string path, bool triggerRename = false)
        {
            var obj = CreateEmptyPrefab(path, triggerRename);
            if (obj != null && !behaviourType.IsAbstract)
            {
                var component = obj.AddComponent(behaviourType);
                PrefabUtility.SavePrefabAsset(obj);
                return component;
            }
            return null;
        }

        // --- Scripts ---
        public static TextAsset CreateOrOverwriteScript(string path, string content)
        {
            EditorUtils.EnsureAssetParentDirectoryExistence(path);
            File.WriteAllText(path, content);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }

        #endregion

        #region Deletion

        public static bool IsAssetDeletableFromCode(UnityEngine.Object obj)
        {
            return obj != null;
        }

        /// <summary>
        /// Deletes an asset<br></br>
        /// If it's a main asset, deletes permanently and CAN'T UNDO<br></br>
        /// If it's not, delete the nested asset and CAN UNDO
        /// </summary>
        public static void DeleteAsset(UnityEngine.Object obj, bool needValidation)
        {
            if (obj == null) return;

            if (!AssetDatabase.IsMainAsset(obj))
            {
                DeleteNestedAsset(obj, needValidation);
                return;
            }

            if (!needValidation
                    || EditorUtility.DisplayDialog(
                        "Delete asset permanently ?",
                        "Are you sure you want to delete " + obj.name + " permanently ?",
                        "Yes", "Cancel"))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(obj));
            }
        }
        /// <summary>
        /// Deletes a nested asset, records undo operation
        /// </summary>
        /// <param name="obj">Object to delete</param>
        /// <param name="asset">Asset in which the object is nested</param>
        /// <param name="needValidation"></param>
        public static void DeleteNestedAsset(UnityEngine.Object obj, bool needValidation)
        {
            if (obj == null || AssetDatabase.IsMainAsset(obj)) return;

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(obj));

            if (!needValidation
                    || EditorUtility.DisplayDialog(
                        "Delete nested asset ?",
                        "Are you sure you want to delete " + obj.name + " ?",
                        "Yes", "Cancel"))
            {
                Undo.SetCurrentGroupName("Delete asset " + obj.name);
                int undoGroup = Undo.GetCurrentGroup();

                Undo.RecordObject(asset, "Remove nested asset from " + asset.name);
                AssetDatabase.RemoveObjectFromAsset(obj);

                Undo.DestroyObjectImmediate(obj);

                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        #endregion

        #region Renaming

        public static void RenameAsset(UnityEngine.Object obj, string newName)
        {
            if (AssetDatabase.IsMainAsset(obj))
            {
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj), newName);
                AssetDatabase.SaveAssetIfDirty(obj);
            }
            else
            {
                obj.name = newName;
                AssetDatabase.SaveAssets();
            }
        }

        #endregion

        #region Moving

        public static bool MoveAssetToFolder(UnityEngine.Object obj, string folder)
        {
            string oldPath = AssetDatabase.GetAssetPath(obj);
            string newPath;
            int num = oldPath.LastIndexOf("/", StringComparison.Ordinal);
            if (num == -1)
            {
                return false;
            }
            newPath = folder + oldPath.Substring(num, oldPath.Length - num);
            if (AssetDatabase.ValidateMoveAsset(oldPath, newPath) == string.Empty)
            {
                return AssetDatabase.MoveAsset(oldPath, newPath) == string.Empty;
            }
            return false;
        }

        public static void AddAssetToOtherAsset(UnityEngine.Object objToAdd, UnityEngine.Object asset)
        {
            var duplicate = Instantiate(objToAdd);
            if (AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(objToAdd))
                && duplicate != null)
            {
                AssetDatabase.AddObjectToAsset(duplicate, asset);
            }
            else
            {
                Debug.Log("of course the object is null");
            }
        }

        #endregion

        #endregion

#endif

        #endregion
    }

    public abstract class Database<T> : BaseDatabase where T : Database<T>
    {
        #region Instance

        private static T _instance;
        public static T I
        {
            get
            {
                if (_instance == null)
                {
                    if (GetInstance(typeof(T)) is T t)
                    {
                        _instance = t;
                    }
                }

                return _instance;
            }
        }

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    public abstract class BaseDatabaseEditor : BaseDataContainerEditor
    {
        #region Members

        protected BaseDatabase m_database;

        #endregion

        #region Properties

        

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            m_database = (BaseDatabase)target;
        }

        #endregion


        #region Base GUI

        protected override string ContainerInvalidDataTypeMessage()
        {
            return "The data type of this Database is not valid.\n\n" +
                    "- Add the DatabaseAttribute to the top of your script.\n" +
                    "- Make sure the dataType parameter implements at least the IDataContainerElement interface.";
        }

        #endregion
    }

#endif

    #endregion
}
