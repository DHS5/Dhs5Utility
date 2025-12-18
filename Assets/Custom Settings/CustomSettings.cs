using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Dhs5.Utility.Databases;
using Dhs5.Utility.GUIs;

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
                else if (!type.IsAbstract)
                {
                    instance = Database.CreateAssetOfType(type, "Assets/Resources/Settings/" + type.Name + ".asset") as BaseSettings;
                    AssetDatabase.SaveAssets();
                }
#endif
            }

            return instance;
        }

#if UNITY_EDITOR

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

#endif

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
        protected Dictionary<FieldInfo, SubSettingsAttribute> m_subSettingsField;
        protected Dictionary<ScriptableObject, Editor> m_subSettingsEditors;

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

            FetchSubSettingsField();
        }

        #endregion

        #region GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (SettingsWindow.ShowSubSettingsReferences)
            {
                OnSubSettingsReferenceGUI();
            }
            else
            {
                OnSubSettingsGUI();
                DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());
            }

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

        #region Sub Settings

        #region Utility Methods

        protected virtual void FetchSubSettingsField()
        {
            m_subSettingsField = new();
            try
            {
                var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    var attribute = fields[i].GetCustomAttribute<SubSettingsAttribute>();
                    if (attribute != null
                        && typeof(ScriptableObject).IsAssignableFrom(fields[i].FieldType))
                    {
                        m_subSettingsField.Add(fields[i], attribute);
                        m_excludedProperties.Add(fields[i].Name);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        protected Editor GetOrCreateSubSettingsEditor(ScriptableObject so, SubSettingsAttribute attribute)
        {
            if (so != null)
            {
                if (m_subSettingsEditors == null) m_subSettingsEditors = new();

                if (m_subSettingsEditors.TryGetValue(so, out var editor) && editor != null)
                {
                    return editor;
                }
                else
                {
                    m_subSettingsEditors[so] = attribute.editorType != null ? Editor.CreateEditor(so, attribute.editorType) : Editor.CreateEditor(so);
                    return m_subSettingsEditors[so];
                }
            }
            return null;
        }

        protected void ClearSubSettingsEditor()
        {
            foreach (var (_, editor) in m_subSettingsEditors)
            {
                DestroyImmediate(editor);
            }
            m_subSettingsEditors.Clear();
        }

        #endregion

        #region GUI

        protected virtual void OnSubSettingsReferenceGUI()
        {
            if (m_subSettingsField.IsValid())
            {
                foreach (var (field, attribute) in m_subSettingsField)
                {
                    var p_subSettings = serializedObject.FindProperty(field.Name);
                    if (p_subSettings != null)
                    {
                        var rect = EditorGUILayout.GetControlRect(false, 20f);

                        // Sub Settings
                        var subSettingsRect = new Rect(rect.x, rect.y, rect.width * 0.7f - 2f, rect.height);
                        EditorGUI.PropertyField(subSettingsRect, p_subSettings);

                        // Button
                        var buttonRect = new Rect(rect.x + rect.width * 0.7f, rect.y, rect.width * 0.3f, rect.height);
                        if (p_subSettings.objectReferenceValue != null)
                        {
                            if (AssetDatabase.GetAssetPath(p_subSettings.objectReferenceValue) != AssetDatabase.GetAssetPath(m_settings))
                            {
                                if (GUI.Button(buttonRect, "ADD TO ASSET"))
                                {
                                    if (AssetDatabase.IsSubAsset(p_subSettings.objectReferenceValue))
                                    {
                                        AssetDatabase.RemoveObjectFromAsset(p_subSettings.objectReferenceValue);
                                    }
                                    AssetDatabase.AddObjectToAsset(p_subSettings.objectReferenceValue, AssetDatabase.GetAssetPath(m_settings));
                                }
                            }
                            else
                            {
                                using (new GUIHelper.GUIBackgroundColorScope(Color.red))
                                {
                                    if (GUI.Button(buttonRect, "DESTROY SUB SETTINGS"))
                                    {
                                        if (AssetDatabase.IsSubAsset(p_subSettings.objectReferenceValue))
                                        {
                                            AssetDatabase.RemoveObjectFromAsset(p_subSettings.objectReferenceValue);
                                        }
                                        DestroyImmediate(p_subSettings.objectReferenceValue);
                                        p_subSettings.objectReferenceValue = null;
                                        AssetDatabase.SaveAssetIfDirty(m_settings);
                                    }
                                }
                            }
                        }
                        else if (!field.FieldType.IsAbstract && field.FieldType != typeof(ScriptableObject))
                        {
                            using (new GUIHelper.GUIBackgroundColorScope(Color.green))
                            {
                                if (GUI.Button(buttonRect, "CREATE " + field.FieldType.ToString().ToUpper()))
                                {
                                    var newSubSettings = Database.CreateScriptableAndAddToAsset(field.FieldType, m_settings);
                                    newSubSettings.name = ObjectNames.NicifyVariableName(field.Name);
                                    p_subSettings.objectReferenceValue = newSubSettings;
                                    AssetDatabase.SaveAssetIfDirty(newSubSettings);
                                }
                            }
                        }
                        else
                        {
                            // TODO popup to create from child type
                            EditorGUI.LabelField(buttonRect, "Can't create asset of type " + field.FieldType.Name);
                        }

                        EditorGUILayout.Space(5f);
                    }
                }
            }
        }

        protected virtual void OnSubSettingsGUI()
        {
            if (m_subSettingsField.IsValid())
            {
                bool hasValidSubSettings = false;
                foreach (var (field, attribute) in m_subSettingsField)
                {
                    var p_subSettings = serializedObject.FindProperty(field.Name);
                    if (p_subSettings != null && p_subSettings.objectReferenceValue is ScriptableObject so)
                    {
                        var editor = GetOrCreateSubSettingsEditor(so, attribute);
                        if (editor != null)
                        {
                            hasValidSubSettings = true;
                            DrawSubSettingsElementGUI(p_subSettings, so, editor);
                        }
                    }
                }

                if (hasValidSubSettings)
                {
                    var rect = EditorGUILayout.GetControlRect(false, 2f);
                    rect.x = 0f; rect.width = EditorGUIUtility.currentViewWidth;
                    EditorGUI.DrawRect(rect, Color.white);
                    EditorGUILayout.Space(5f);
                }
            }
        }
        protected virtual void DrawSubSettingsElementGUI(SerializedProperty property, ScriptableObject so, Editor editor)
        {
            var rect = EditorGUILayout.GetControlRect(false, 22f);
            var decoRect = new Rect(0f, rect.y, EditorGUIUtility.currentViewWidth, rect.height);
            EditorGUI.DrawRect(decoRect, GUIHelper.grey015);
            decoRect.height = 1f;
            EditorGUI.DrawRect(decoRect, Color.black);

            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, so.name, true);
            if (property.isExpanded)
            {
                if (editor is ISubSettingsEditor subSettingsEditor)
                {
                    subSettingsEditor.DrawSubSettingsGUI();
                }
                else
                {
                    DrawPropertiesExcluding(editor.serializedObject, "m_Script");
                }
                EditorGUILayout.Space(2f);
                rect = EditorGUILayout.GetControlRect(false, 1f);
                rect.x = 0f; rect.width = EditorGUIUtility.currentViewWidth;
                EditorGUI.DrawRect(rect, Color.black);
            }
            else
            {
                decoRect.y += 21f;
                EditorGUI.DrawRect(decoRect, Color.black);
            }
        }

        #endregion

        #endregion
    }

#endif

    #endregion
}
