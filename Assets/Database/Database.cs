using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Databases
{
    public static class Database
    {
        #region Instance

        private static Dictionary<Type, BaseDataContainer> _instances = new();
        private static BaseDataContainer GetInstance(Type type)
        {
            if (!IsTypeDatabase(type)) return null;

            if (!_instances.TryGetValue(type, out var instance)
                || instance == null)
            {
                if (DatabaseSettings.TryGetDatabase(type, out instance))
                {
                    return instance;
                }

                var list = Resources.LoadAll("Databases", type);

                if (list != null && list.Length > 0)
                {
                    instance = list[0] as BaseDataContainer;
                    _instances[type] = instance;
                }
#if UNITY_EDITOR
                else if (!type.IsAbstract) // CREATION
                {
                    instance = Database.CreateAssetOfType(type, "Assets/Resources/Databases/" + type.Name + ".asset") as BaseDataContainer;
                    AssetDatabase.SaveAssets();
                }
#endif
            }

            return instance;
        }

        // --- ACCESSORS ---
        public static T Get<T>() where T : BaseDataContainer
        {
            return GetInstance(typeof(T)) as T;
        }
        public static BaseDataContainer Get(Type type)
        {
            return GetInstance(type);
        }

        // --- UTILITY ---

        /// <summary>
        /// Clears the instances dictionary and will force the database manager to search every database in Ressources again
        /// </summary>
        public static void ClearInstances()
        {
            _instances.Clear();
        }

        #endregion

        #region Database Types

        public static bool IsTypeDatabase(Type type)
        {
            return IsTypeDatabase(type, out _);
        }
        private static bool IsTypeDatabase(Type type, out DatabaseAttribute att)
        {
            att = null;
            return type.IsSubclassOf(typeof(BaseDataContainer))
                && !type.IsAbstract
#if UNITY_EDITOR
                && BaseDataContainer.TryGetDatabaseAttribute(type, out att)
#endif
                ;
        }

        #endregion


        #region Static Editor Properties

#if UNITY_EDITOR

        public static bool DebugModeEnabled { get; set; } = false;

#endif

        #endregion

        #region Static Database Accessors

        private static bool TryGetDatabaseInstance<T>(out T database) where T : BaseDataContainer
        {
            var db = Get<T>();
            if (db != null)
            {
                database = db;
                return true;
            }
            else
            {
                Debug.LogError("This DataContainer type is not a Database");
                database = null;
                return false;
            }
        }

        // --- DATA AT INDEX ---
        public static UnityEngine.Object GetDataAtIndex<T>(int index) where T : BaseDataContainer
        {
            if (TryGetDatabaseInstance<T>(out var db))
            {
                return db.GetDataAtIndex(index);
            }
            return null;
        }
        public static U GetDataAtIndex<T, U>(int index) where T : BaseDataContainer where U : UnityEngine.Object, IDataContainerElement
        {
            if (TryGetDatabaseInstance<T>(out var db))
            {
                return db.GetDataAtIndex<U>(index);
            }
            return null;
        }

        // --- DATA BY UID ---
        public static bool GetDataByUID<T>(int uid, out UnityEngine.Object obj) where T : BaseDataContainer
        {
            if (TryGetDatabaseInstance<T>(out var db))
            {
                return db.TryGetDataByUID(uid, out obj);
            }
            obj = null;
            return false;
        }
        public static bool GetDataByUID<T, U>(int uid, out U data) where T : BaseDataContainer where U : UnityEngine.Object, IDataContainerElement
        {
            if (TryGetDatabaseInstance<T>(out var db))
            {
                return db.TryGetDataByUID<U>(uid, out data);
            }
            data = null;
            return false;
        }

        // --- ENUMERATOR ---
        public static IEnumerator Enumerate<T>() where T : BaseDataContainer
        {
            if (TryGetDatabaseInstance<T>(out var db))
            {
                foreach (var item in db)
                {
                    yield return item;
                }
            }
        }
        public static IEnumerable<U> Enumerate<T, U>() where T : BaseDataContainer where U : UnityEngine.Object, IDataContainerElement
        {
            if (TryGetDatabaseInstance<T>(out var db))
            {
                foreach (var item in db.GetDataEnumerator<U>())
                {
                    yield return item;
                }
            }
        }

        #endregion

        #region Static Editor Functions

#if UNITY_EDITOR

        #region Database Instances

        internal static BaseDataContainer[] GetAllDatabaseInstances() => GetAllDatabaseInstances(GetDatabaseTypes());
        internal static BaseDataContainer[] GetAllDatabaseInstances(Func<Type, bool> predicate) => GetAllDatabaseInstances(GetDatabaseTypes(t => predicate.Invoke(t)));
        internal static BaseDataContainer[] GetAllDatabaseInstances(Func<DatabaseAttribute, bool> predicate) => GetAllDatabaseInstances(GetDatabaseTypes(da => predicate.Invoke(da)));
        private static BaseDataContainer[] GetAllDatabaseInstances(Type[] types)
        {
            BaseDataContainer[] databases = new BaseDataContainer[types.Length];

            for (int i = 0; i < databases.Length; i++)
            {
                databases[i] = GetInstance(types[i]);
            }

            return databases;
        }

        #endregion

        #region Database Types

        private static Type[] GetDatabaseTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => IsTypeDatabase(t, out _))
                .ToArray();
        }
        private static Type[] GetDatabaseTypes(Func<Type, bool> predicate)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => IsTypeDatabase(t, out _) && predicate.Invoke(t))
                .ToArray();
        }
        private static Type[] GetDatabaseTypes(Func<DatabaseAttribute, bool> predicate)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => IsTypeDatabase(t, out var att) && predicate.Invoke(att))
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
            obj.name = "New";
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
            UnityEngine.Object.DestroyImmediate(template);
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

        #region Duplication

        public static UnityEngine.Object Duplicate(UnityEngine.Object obj)
        {
            if (AssetDatabase.IsMainAsset(obj)) return DuplicateMainAsset(obj);
            else if (AssetDatabase.IsSubAsset(obj)) return DuplicateSubAsset(obj);
            return null;
        }

        private static UnityEngine.Object DuplicateMainAsset(UnityEngine.Object obj)
        {
            UnityEngine.Object duplicate = null;
            string path = AssetDatabase.GetAssetPath(obj);
            string newPath = path.Replace(obj.name, obj.name + "Copy");

            if (AssetDatabase.CopyAsset(path, newPath))
            {
                duplicate = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newPath);
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }

            return duplicate;
        }
        private static UnityEngine.Object DuplicateSubAsset(UnityEngine.Object obj)
        {
            UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(obj));

            UnityEngine.Object duplicate = null;

            if (obj is ScriptableObject so)
            {
                duplicate = ScriptableObject.Instantiate(so);
                duplicate.name = so.name + "Copy";
            }

            if (duplicate != null)
            {
                AssetDatabase.AddObjectToAsset(duplicate, mainAsset);
            }
            return duplicate;
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
        public static bool DeleteAsset(UnityEngine.Object obj, bool needValidation)
        {
            if (obj == null) return false;

            if (!AssetDatabase.IsMainAsset(obj))
            {
                return DeleteNestedAsset(obj, needValidation);
            }

            if (!needValidation
                    || EditorUtility.DisplayDialog(
                        "Delete asset permanently ?",
                        "Are you sure you want to delete " + obj.name + " permanently ?",
                        "Yes", "Cancel"))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(obj));
                return true;
            }
            return false;
        }
        /// <summary>
        /// Deletes a nested asset, records undo operation
        /// </summary>
        /// <param name="obj">Object to delete</param>
        /// <param name="asset">Asset in which the object is nested</param>
        /// <param name="needValidation"></param>
        public static bool DeleteNestedAsset(UnityEngine.Object obj, bool needValidation)
        {
            if (obj == null || AssetDatabase.IsMainAsset(obj)) return false;

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

                return true;
            }
            return false;
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
            var duplicate = UnityEngine.Object.Instantiate(objToAdd);
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
}
