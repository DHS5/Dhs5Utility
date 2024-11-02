using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace Dhs5.Utility.Settings
{
    public abstract class BaseSettings : ScriptableObject 
    {
        #region Instance

        private static Dictionary<Type, BaseSettings> _instances = new();
        public static BaseSettings GetInstance(Type type)
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
                else
                {
                    instance = CreateInstance(type) as BaseSettings;
                    AssetDatabase.CreateAsset(instance, "Assets/Resources/Settings/" + type.Name + ".asset");
                    AssetDatabase.SaveAssets();
                }
            }

            return instance;
        }

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
        public static string GetPath(Type type)
        {
            if (TryGetAttribute(type, out var attribute))
            {
                return attribute.path;
            }
            return "Null path";
        }
        public static SettingsScope GetScope(Type type)
        {
            if (TryGetAttribute(type, out var attribute))
            {
                return (SettingsScope)attribute.scope;
            }
            return SettingsScope.Project;
        }

        //public static SettingsProvider GetCustomSettingsProvider()
        //{
        //    return new CustomSettingsProvider<T>();
        //}

        private static Type[] GetAllChildTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(BaseSettings)) && !t.IsAbstract)
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
        public static T Instance
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

#if UNITY_EDITOR
    public class CustomSettingsProvider : SettingsProvider //where T : CustomSettings<T>
    {
        private Type m_type;
        private BaseSettings m_settings;
        private Editor m_editor;

        public CustomSettingsProvider(Type type) : base(BaseSettings.GetPath(type), BaseSettings.GetScope(type))
        {
            m_type = type;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_settings = BaseSettings.GetInstance(m_type);
            if (m_settings != null)
            {
                m_editor = Editor.CreateEditor(m_settings);
            }
        }

        public override void OnGUI(string searchContext)
        {
            if (m_editor != null)
            {
                m_editor.OnInspectorGUI();
            }
        }
    }
#endif
}
