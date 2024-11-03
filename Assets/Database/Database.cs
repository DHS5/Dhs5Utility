using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
using System.IO;
#endif

namespace Dhs5.Utility.Databases
{
    public abstract class BaseDatabase : ScriptableObject
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
                    instance = CreateInstance(type) as BaseDatabase;

                    if (!Directory.Exists(Application.dataPath + "/Resources/Databases"))
                    {
                        if (!Directory.Exists(Application.dataPath + "/Resources"))
                        {
                            Directory.CreateDirectory(Application.dataPath + "/Resources");
                        }
                        Directory.CreateDirectory(Application.dataPath + "/Resources/Databases");
                    }

                    AssetDatabase.CreateAsset(instance, "Assets/Resources/Databases/" + type.Name + ".asset");
                    AssetDatabase.SaveAssets();
                }
#endif
            }

            return instance;
        }

        internal static BaseDatabase[] GetAllInstances() => GetAllInstances(GetAllChildTypes());
        internal static BaseDatabase[] GetAllInstances(Func<Type, bool> predicate) => GetAllInstances(GetAllChildTypes(t => predicate.Invoke(t)));
        private static BaseDatabase[] GetAllInstances(Type[] childTypes)
        {
            BaseDatabase[] settings = new BaseDatabase[childTypes.Length];

            for (int i = 0; i < settings.Length; i++)
            {
                settings[i] = GetInstance(childTypes[i]);
            }

            return settings;
        }

        #endregion

        #region Editor Utility

#if UNITY_EDITOR

        internal string Editor_GetPath()
        {
            return GetPath(GetType());
        }

        internal virtual IEnumerable<UnityEngine.Object> Editor_GetDatabaseContent()
        {
            yield return null;
        }

#endif

        #endregion

        #region Editor Functions

#if UNITY_EDITOR

        #region Statics

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
        internal static Type GetDataType(Type type)
        {
            if (TryGetAttribute(type, out var attribute))
            {
                return attribute.dataType;
            }
            return typeof(UnityEngine.Object);
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

        #endregion

        #region Callbacks

        internal virtual void Editor_ShouldRecomputeDatabaseContent() { }

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

    [CustomEditor(typeof(BaseDatabase), editorForChildClasses:true)]
    public class BaseDatabaseEditor : Editor
    {
        #region Members

        protected BaseDatabase m_database;

        protected SerializedProperty p_script;

        protected List<string> m_excludedProperties;

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_database = (BaseDatabase)target;

            p_script = serializedObject.FindProperty("m_Script");

            m_excludedProperties = new()
            {
                p_script.propertyPath,
            };
        }

        #endregion

        #region GUI

        public sealed override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnGUI();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnGUI()
        {
            DrawDefault();
        }
        protected void DrawDefault()
        {
            DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());
        }

        #endregion
    }

#endif

    #endregion
}
