using Unity.Scripting.LifecycleManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Databases
{
    [NoAutoStaticsCleanup]
    public static class Database
    {
        #region Instance

        private static Dictionary<Type, Container> _instances = new();
        /// <param name="createIfMissing">Editor only : creates the asset if it doesn't exist, otherwise null is returned</param>
        private static Container GetInstance(Type type, bool createIfMissing = false)
        {
            if (!IsTypeDatabase(type)) return null;

            if (!_instances.TryGetValue(type, out var instance)
                || instance == null)
            {
                var list = Resources.LoadAll("Databases", type);

                if (list != null && list.Length > 0)
                {
                    instance = list[0] as Container;
                    _instances[type] = instance;
                }
#if UNITY_EDITOR
                else if (createIfMissing && !type.IsAbstract) // CREATION
                {
                    instance = EditorDataUtility.CreateAssetOfType(type, "Assets/Resources/Databases/" + type.Name + ".asset") as Container;
                    AssetDatabase.SaveAssets();
                }
#endif
            }

            return instance;
        }

        // --- ACCESSORS ---
        public static T Get<T>() where T : Container
        {
            return GetInstance(typeof(T)) as T;
        }
        public static Container Get(Type type)
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
            return type.IsSubclassOf(typeof(Container))
                && !type.IsAbstract
#if UNITY_EDITOR
                && Editor_TryGetDatabaseAttribute(type, out att)
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

        private static bool TryGetDatabaseInstance<T>(out T database) where T : Container
        {
            var db = Get<T>();
            if (db != null)
            {
                database = db;
                return true;
            }
            else
            {
                Debug.LogError("This Container type is not a Database");
                database = null;
                return false;
            }
        }

        // --- DATA AT INDEX ---
        public static UnityEngine.Object GetDataAtIndex<T>(int index) where T : Container
        {
            if (TryGetDatabaseInstance<T>(out var db))
            {
                return db.GetAtIndex(index);
            }
            return null;
        }
        public static U GetDataAtIndex<T, U>(int index) where T : Container where U : UnityEngine.Object, IContainerElement
        {
            if (TryGetDatabaseInstance<T>(out var db))
            {
                return db.GetAtIndex<U>(index);
            }
            return null;
        }

        // --- DATA BY UID ---
        public static bool GetDataByUID<T>(int uid, out UnityEngine.Object obj) where T : Container
        {
            if (TryGetDatabaseInstance<T>(out var db))
            {
                return db.TryGetByUID(uid, out obj);
            }
            obj = null;
            return false;
        }
        public static bool GetDataByUID<T, U>(int uid, out U data) where T : Container where U : UnityEngine.Object, IContainerElement
        {
            if (TryGetDatabaseInstance<T>(out var db))
            {
                return db.TryGetByUID(uid, out data);
            }
            data = null;
            return false;
        }

        // --- ENUMERATOR ---
        public static IEnumerator Enumerate<T>() where T : Container
        {
            if (TryGetDatabaseInstance<T>(out var db))
            {
                foreach (var item in db.GetObjects())
                {
                    yield return item;
                }
            }
        }
        public static IEnumerable<U> Enumerate<T, U>() where T : Container where U : UnityEngine.Object, IContainerElement
        {
            if (TryGetDatabaseInstance<T>(out var db))
            {
                foreach (var item in db.GetObjectsOfType<U>())
                {
                    yield return item;
                }
            }
        }

        #endregion

        #region Static Editor Functions

#if UNITY_EDITOR

        #region Attributes

        private readonly static Dictionary<Type, DatabaseAttribute> _attributes = new();
        public static bool Editor_TryGetDatabaseAttribute(Type type, out DatabaseAttribute attribute)
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
        public static string Editor_GetDatabaseWindowPath(Type type)
        {
            if (Editor_TryGetDatabaseAttribute(type, out var attribute))
            {
                return attribute.path;
            }
            return null;
        }

        #endregion

        #region Database Instances

        /// <param name="types">Database type of each instance : an instance is null when its asset is missing and not created</param>
        internal static Container[] GetAllDatabaseInstances(bool createIfMissing, out Type[] types)
        {
            types = GetDatabaseTypes();
            return GetAllDatabaseInstances(types, createIfMissing);
        }
        /// <param name="types">Database type of each instance : an instance is null when its asset is missing and not created</param>
        internal static Container[] GetAllDatabaseInstances(Func<Type, bool> predicate, bool createIfMissing, out Type[] types)
        {
            types = GetDatabaseTypes(t => predicate.Invoke(t));
            return GetAllDatabaseInstances(types, createIfMissing);
        }
        /// <param name="types">Database type of each instance : an instance is null when its asset is missing and not created</param>
        internal static Container[] GetAllDatabaseInstances(Func<DatabaseAttribute, bool> predicate, bool createIfMissing, out Type[] types)
        {
            types = GetDatabaseTypes(da => predicate.Invoke(da));
            return GetAllDatabaseInstances(types, createIfMissing);
        }
        private static Container[] GetAllDatabaseInstances(Type[] types, bool createIfMissing)
        {
            Container[] databases = new Container[types.Length];

            for (int i = 0; i < databases.Length; i++)
            {
                databases[i] = GetInstance(types[i], createIfMissing);
            }

            return databases;
        }

        #endregion

        #region Database Types

        // TypeCache : instant, no scan of every type of every assembly
        private static Type[] GetDatabaseTypes()
        {
            return TypeCache.GetTypesDerivedFrom<Container>()
                .Where(t => IsTypeDatabase(t, out _))
                .ToArray();
        }
        private static Type[] GetDatabaseTypes(Func<Type, bool> predicate)
        {
            return TypeCache.GetTypesDerivedFrom<Container>()
                .Where(t => IsTypeDatabase(t, out _) && predicate.Invoke(t))
                .ToArray();
        }
        private static Type[] GetDatabaseTypes(Func<DatabaseAttribute, bool> predicate)
        {
            return TypeCache.GetTypesDerivedFrom<Container>()
                .Where(t => IsTypeDatabase(t, out var att) && predicate.Invoke(att))
                .ToArray();
        }

        #endregion

#endif

        #endregion
    }
}
