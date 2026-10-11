using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Databases
{
    public static class Database
    {
        #region Instance

        private static Dictionary<Type, BaseDataContainer> _instances = new();
        /// <param name="createIfMissing">Editor only : creates the asset if it doesn't exist, otherwise null is returned</param>
        private static BaseDataContainer GetInstance(Type type, bool createIfMissing = false)
        {
            if (!IsTypeDatabase(type)) return null;

            if (!_instances.TryGetValue(type, out var instance)
                || instance == null)
            {
                var list = Resources.LoadAll("Databases", type);

                if (list != null && list.Length > 0)
                {
                    instance = list[0] as BaseDataContainer;
                    _instances[type] = instance;
                }
#if UNITY_EDITOR
                else if (createIfMissing && !type.IsAbstract) // CREATION
                {
                    instance = EditorDataUtility.CreateAssetOfType(type, "Assets/Resources/Databases/" + type.Name + ".asset") as BaseDataContainer;
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

        /// <param name="types">Database type of each instance : an instance is null when its asset is missing and not created</param>
        internal static BaseDataContainer[] GetAllDatabaseInstances(bool createIfMissing, out Type[] types)
        {
            types = GetDatabaseTypes();
            return GetAllDatabaseInstances(types, createIfMissing);
        }
        /// <param name="types">Database type of each instance : an instance is null when its asset is missing and not created</param>
        internal static BaseDataContainer[] GetAllDatabaseInstances(Func<Type, bool> predicate, bool createIfMissing, out Type[] types)
        {
            types = GetDatabaseTypes(t => predicate.Invoke(t));
            return GetAllDatabaseInstances(types, createIfMissing);
        }
        /// <param name="types">Database type of each instance : an instance is null when its asset is missing and not created</param>
        internal static BaseDataContainer[] GetAllDatabaseInstances(Func<DatabaseAttribute, bool> predicate, bool createIfMissing, out Type[] types)
        {
            types = GetDatabaseTypes(da => predicate.Invoke(da));
            return GetAllDatabaseInstances(types, createIfMissing);
        }
        private static BaseDataContainer[] GetAllDatabaseInstances(Type[] types, bool createIfMissing)
        {
            BaseDataContainer[] databases = new BaseDataContainer[types.Length];

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
            return TypeCache.GetTypesDerivedFrom<BaseDataContainer>()
                .Where(t => IsTypeDatabase(t, out _))
                .ToArray();
        }
        private static Type[] GetDatabaseTypes(Func<Type, bool> predicate)
        {
            return TypeCache.GetTypesDerivedFrom<BaseDataContainer>()
                .Where(t => IsTypeDatabase(t, out _) && predicate.Invoke(t))
                .ToArray();
        }
        private static Type[] GetDatabaseTypes(Func<DatabaseAttribute, bool> predicate)
        {
            return TypeCache.GetTypesDerivedFrom<BaseDataContainer>()
                .Where(t => IsTypeDatabase(t, out var att) && predicate.Invoke(att))
                .ToArray();
        }

        #endregion

#endif

        #endregion
    }
}
