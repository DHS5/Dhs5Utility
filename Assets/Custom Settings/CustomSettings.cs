using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Dhs5.Utility.Databases;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using System.Reflection;
using System.IO;
#endif

namespace Dhs5.Utility.Settings
{
    public abstract class BaseSettings : ScriptableObject 
    {
        #region Instance

        private static Dictionary<Type, BaseSettings> _instances = new();
        internal static BaseSettings GetInstance(Type type)
        {
            if (!type.IsSubclassOf(typeof(BaseSettings))) return null;

            if (!_instances.TryGetValue(type, out var instance) 
                || instance == null)
            {
                var list = Resources.LoadAll("Settings", type);

                if (list != null && list.Length > 0)
                {
                    instance = list[0] as BaseSettings;
                    _instances[type] = instance;
                }
#if UNITY_EDITOR
                else
                {
                    instance = BaseDatabase.CreateAssetOfType(type, "Assets/Resources/Settings/" + type.Name + ".asset") as BaseSettings;
                    AssetDatabase.SaveAssets();
                }
#endif
            }

            return instance;
        }

        internal static BaseSettings[] GetAllInstances() => GetAllInstances(GetAllChildTypes());
        internal static BaseSettings[] GetAllInstances(Func<Type, bool> predicate) => GetAllInstances(GetAllChildTypes(t => predicate.Invoke(t)));
        private static BaseSettings[] GetAllInstances(Type[] childTypes)
        {
            BaseSettings[] settings = new BaseSettings[childTypes.Length];

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

#endif

        #endregion

        #region Editor Functions

#if UNITY_EDITOR

        private static Dictionary<Type, SettingsAttribute> _attributes = new();
        protected static bool TryGetAttribute(Type type, out SettingsAttribute attribute)
        {
            if (_attributes.TryGetValue(type, out attribute))
            {
                return true;
            }

            attribute = type.GetCustomAttribute<SettingsAttribute>(inherit: true);

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
        internal static SettingsScope GetScope(Type type)
        {
            if (TryGetAttribute(type, out var attribute))
            {
                return (SettingsScope)attribute.scope;
            }
            return SettingsScope.Project;
        }

        private static Type[] GetAllChildTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(BaseSettings)) && !t.IsAbstract && TryGetAttribute(t, out _))
                .ToArray();
        }
        private static Type[] GetAllChildTypes(Func<Type, bool> predicate)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(BaseSettings)) && !t.IsAbstract && TryGetAttribute(t, out _) && predicate.Invoke(t))
                .ToArray();
        }
        [SettingsProviderGroup]
        public static SettingsProvider[] GetCustomSettingsProviders()
        {
            return GetAllChildTypes().Select(t => new CustomSettingsProvider(t)).ToArray();
        }

#endif

        #endregion
    }
    public abstract class CustomSettings<T> : BaseSettings where T : CustomSettings<T>
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

    #region Settings Provider

#if UNITY_EDITOR
    public class CustomSettingsProvider : SettingsProvider
    {
        #region Members

        private Type m_type;
        private BaseSettings m_settings;
        private Editor m_editor;

        #endregion

        #region Constructor

        public CustomSettingsProvider(Type type) : base(BaseSettings.GetPath(type), BaseSettings.GetScope(type))
        {
            m_type = type;
        }

        #endregion

        #region Activation Behaviour

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_settings = BaseSettings.GetInstance(m_type);
            if (m_settings != null)
            {
                m_editor = Editor.CreateEditor(m_settings);
            }
        }
        public override void OnDeactivate()
        {
            base.OnDeactivate();

            if (m_editor != null)
            {
                GameObject.DestroyImmediate(m_editor);
            }
        }

        #endregion

        #region GUI

        public override void OnGUI(string searchContext)
        {
            if (m_editor != null)
            {
                //EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 2f), Color.white);
                //EditorGUILayout.Space(10f);

                m_editor.OnInspectorGUI();
            }
        }

        public override void OnTitleBarGUI()
        {
            if (!EditorIsBaseSettingsEditor(out var editor)
                || !editor.OnTitleBarGUI())
            {
                base.OnTitleBarGUI(); // No override
            }
        }
        public override void OnFooterBarGUI()
        {
            if (!EditorIsBaseSettingsEditor(out var editor)
                || !editor.OnFooterBarGUI())
            {
                base.OnFooterBarGUI(); // No override
            }
        }

        #endregion


        #region Utility

        private bool EditorIsBaseSettingsEditor(out BaseSettingsEditor editor)
        {
            if (m_editor is BaseSettingsEditor e)
            {
                editor = e;
                return true;
            }
            editor = null;
            return false;
        }

        #endregion
    }
#endif

    #endregion

    #region Base Settings Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(BaseSettings), editorForChildClasses:true)]
    public class BaseSettingsEditor : Editor
    {
        #region Members

        protected BaseSettings m_settings;

        protected SerializedProperty p_script;

        protected List<string> m_excludedProperties;

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_settings = (BaseSettings)target;

            p_script = serializedObject.FindProperty("m_Script");

            m_excludedProperties = new()
            {
                p_script.propertyPath,
            };
        }

        #endregion

        #region GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());

            serializedObject.ApplyModifiedProperties();
        }

        /// <returns>True if you want to override the title bar GUI</returns>
        public virtual bool OnTitleBarGUI()
        {
            return false;
        }
        
        /// <returns>True if you want to override the footer bar GUI</returns>
        public virtual bool OnFooterBarGUI()
        {
            return false;
        }

        #endregion
    }

#endif

    #endregion
}
