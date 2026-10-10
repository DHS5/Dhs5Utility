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
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Settings
{
    public abstract class BaseSettings : ScriptableObject 
    {
        #region Instance

        private static Dictionary<Type, BaseSettings> _instances = new();
        /// <summary>
        /// Gets the settings of <paramref name="type"/> : the asset of its most derived non-abstract type
        /// (a settings class with a non-abstract subclass is replaced by it)
        /// </summary>
        internal static BaseSettings GetInstance(Type type)
        {
            if (!type.IsSubclassOf(typeof(BaseSettings))) return null;

            if (_instances.TryGetValue(type, out var instance) && instance != null)
            {
                return instance;
            }

            // LoadAll does load every object of type and child types
            var assets = Resources.LoadAll("Settings", type);

#if UNITY_EDITOR
            // In the editor, the most derived type is known : its asset is loaded, or created if missing
            var mostDerivedType = GetMostDerivedType(type);
            if (mostDerivedType == null) return null; // Abstract without non-abstract subclass

            // User scope (Preferences) : per user, stored in the UserSettings folder, editor only
            if (GetScope(mostDerivedType) == SettingsScope.User)
            {
                instance = LoadOrCreateUserSettings(mostDerivedType, assets);
                _instances[type] = instance;
                return instance;
            }

            instance = null;
            foreach (var asset in assets)
            {
                if (asset != null && asset.GetType() == mostDerivedType)
                {
                    instance = (BaseSettings)asset;
                    break;
                }
            }
            if (instance == null)
            {
                instance = Database.CreateAssetOfType(mostDerivedType, "Assets/Resources/Settings/" + mostDerivedType.Name + ".asset") as BaseSettings;
                AssetDatabase.SaveAssets();
                // Can happen from game code in play mode, or when opening the Settings window : made visible
                Debug.Log("Settings asset created for " + mostDerivedType.Name + " : " + AssetDatabase.GetAssetPath(instance), instance);
            }
#else
            // In builds, the asset of the most derived type among the loaded ones
            instance = GetMostDerivedAsset(assets);
#endif

            if (instance != null)
            {
                _instances[type] = instance;
            }
            return instance;
        }

#if !UNITY_EDITOR
        // Build only : in the editor, the most derived type is known through the TypeCache
        private static BaseSettings GetMostDerivedAsset(UnityEngine.Object[] assets)
        {
            BaseSettings mostDerived = null;
            int maxDepth = -1;
            foreach (var asset in assets)
            {
                if (asset is BaseSettings settings)
                {
                    var depth = GetInheritanceDepth(settings.GetType());
                    if (depth > maxDepth)
                    {
                        maxDepth = depth;
                        mostDerived = settings;
                    }
                }
            }
            return mostDerived;
        }
        private static int GetInheritanceDepth(Type type)
        {
            int depth = 0;
            for (var t = type; t != null; t = t.BaseType) depth++;
            return depth;
        }
#endif

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

        /// <summary>
        /// Gives a key to the PlayerPrefMembers without one as soon as the settings are loaded or edited in the editor.<br></br>
        /// Declared in every build so that subclasses can override it without #if (Unity only calls it in the editor).
        /// If you override it, call base.OnValidate().
        /// </summary>
        protected virtual void OnValidate()
        {
#if UNITY_EDITOR
            // Delayed : the asset can't be modified safely during OnValidate
            EditorApplication.delayCall -= AssignPlayerPrefKeys;
            EditorApplication.delayCall += AssignPlayerPrefKeys;
#endif
        }

#if UNITY_EDITOR

        internal string Editor_GetPath()
        {
            return GetPath(GetType());
        }

        /// <summary>
        /// OnValidate isn't called for already loaded assets after a domain reload : every settings asset is checked once the editor is loaded
        /// </summary>
        [InitializeOnLoadMethod]
        private static void AssignAllPlayerPrefKeysOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                foreach (var type in TypeCache.GetTypesDerivedFrom<BaseSettings>())
                {
                    if (type.IsAbstract || type.IsGenericType) continue;

                    foreach (var guid in AssetDatabase.FindAssets("t:" + type.Name))
                    {
                        if (AssetDatabase.LoadAssetAtPath<BaseSettings>(AssetDatabase.GUIDToAssetPath(guid)) is BaseSettings settings
                            && settings.GetType() == type)
                        {
                            settings.AssignPlayerPrefKeys();
                        }
                    }
                }
            };
        }

        private void AssignPlayerPrefKeys()
        {
            if (this == null) return;

            using var serializedObject = new SerializedObject(this);
            var property = serializedObject.GetIterator();
            bool changed = false;
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.Generic) continue;

                // PlayerPrefMember only : checked on the actual object, not just on field names
                var p_key = property.FindPropertyRelative("m_key");
                if (p_key == null || p_key.propertyType != SerializedPropertyType.String
                    || !PlayerPrefMemberDrawer.IsPlayerPrefMember(PlayerPrefMemberDrawer.GetMemberObject(property))) continue;

                if (string.IsNullOrWhiteSpace(p_key.stringValue))
                {
                    p_key.stringValue = PlayerPrefMemberDrawer.GetDefaultKey(this, property.propertyPath);
                    changed = true;
                }
            }

            if (changed)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(this);
                // Saved right away : builds must get the keys
                AssetDatabase.SaveAssetIfDirty(this);
            }

            // User settings aren't assets : saved to their file after every change (OnValidate is called on edits and undo)
            if (Editor_IsUserSettings)
            {
                SaveUserSettings(this);
            }
        }

        #region User Settings

        /// <summary>
        /// User scope settings (Preferences) are stored per user in the UserSettings folder, not in the project's assets.
        /// They only exist in the editor.
        /// </summary>
        internal bool Editor_IsUserSettings => !AssetDatabase.Contains(this) && GetScope(GetType()) == SettingsScope.User;

        private static string GetUserSettingsPath(Type type) => "UserSettings/" + type.Name + ".asset";

        private static BaseSettings LoadOrCreateUserSettings(Type type, UnityEngine.Object[] resourcesAssets)
        {
            var path = GetUserSettingsPath(type);
            BaseSettings settings = null;

            if (System.IO.File.Exists(path))
            {
                foreach (var obj in UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(path))
                {
                    if (obj is BaseSettings loaded && loaded.GetType() == type)
                    {
                        settings = loaded;
                        break;
                    }
                }
            }

            if (settings == null)
            {
                settings = (BaseSettings)CreateInstance(type);
                settings.name = type.Name;

                // Former user settings stored as an asset in Resources/Settings : their values are copied once
                foreach (var asset in resourcesAssets)
                {
                    if (asset != null && asset.GetType() == type)
                    {
                        EditorUtility.CopySerialized(asset, settings);
                        settings.name = type.Name;
                        Debug.Log("User settings " + type.Name + " moved to " + path + " : the asset " + AssetDatabase.GetAssetPath(asset) + " isn't used anymore and can be deleted");
                        break;
                    }
                }
            }

            // Not saved with scenes nor unloaded : saved to its own file
            settings.hideFlags = HideFlags.DontSave;
            if (!System.IO.File.Exists(path))
            {
                SaveUserSettings(settings);
            }
            return settings;
        }

        private static void SaveUserSettings(BaseSettings settings)
        {
            var path = GetUserSettingsPath(settings.GetType());
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new UnityEngine.Object[] { settings }, path, allowTextSerialization: true);
        }

        #endregion

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

            // [Settings] is not inherited by the attribute system : walk up the hierarchy so that a subclass
            // without its own attribute replaces its base class at the same path
            for (var t = type; t != null && t != typeof(BaseSettings); t = t.BaseType)
            {
                attribute = t.GetCustomAttribute<SettingsAttribute>(inherit: false);
                if (attribute != null)
                {
                    _attributes.Add(type, attribute);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// A settings type is used only if it's non-abstract and no non-abstract subclass exists
        /// </summary>
        private static bool IsMostDerivedType(Type type)
        {
            if (type.IsAbstract) return false;
            foreach (var subType in TypeCache.GetTypesDerivedFrom(type))
            {
                if (!subType.IsAbstract) return false;
            }
            return true;
        }
        /// <returns>The type whose settings asset is used for <paramref name="type"/>, null if there is none (abstract type without non-abstract subclass)</returns>
        private static Type GetMostDerivedType(Type type)
        {
            if (IsMostDerivedType(type)) return type;

            Type mostDerivedType = null;
            foreach (var subType in TypeCache.GetTypesDerivedFrom(type))
            {
                if (IsMostDerivedType(subType))
                {
                    if (mostDerivedType == null)
                    {
                        mostDerivedType = subType;
                    }
                    else
                    {
                        Debug.LogWarning("Settings type " + type.Name + " has several non-abstract leaf subclasses (" + mostDerivedType.Name + ", " + subType.Name + ") : " + mostDerivedType.Name + " is used");
                        break;
                    }
                }
            }
            return mostDerivedType;
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

        /// <returns>The settings types shown and used : non-abstract, without non-abstract subclass, with a [Settings] path</returns>
        private static Type[] GetAllChildTypes()
        {
            return TypeCache.GetTypesDerivedFrom<BaseSettings>()
                .Where(t => IsMostDerivedType(t) && TryGetAttribute(t, out _))
                .ToArray();
        }
        private static Type[] GetAllChildTypes(Func<Type, bool> predicate)
        {
            return GetAllChildTypes()
                .Where(t => predicate.Invoke(t))
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
        protected Dictionary<FieldInfo, SubSettingsAttribute> m_subSettingsFields;
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
        protected virtual void OnDisable()
        {
            // The sub settings editors are owned by this editor
            ClearSubSettingsEditor();
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

        #region Sub Settings Assets

        /// <summary>
        /// Sub settings are assets in the folder of their settings asset, named with this prefix
        /// </summary>
        protected const string SUB_SETTINGS_PREFIX = "SUBS_";

        protected string GetSettingsFolderPath()
        {
            var settingsPath = AssetDatabase.GetAssetPath(m_settings);
            return settingsPath.Substring(0, settingsPath.LastIndexOf('/'));
        }

        /// <summary>
        /// SUBS_ + the name of the field without spaces (m_subSetTest -> SUBS_SubSetTest)
        /// </summary>
        protected string GetNewSubSettingsAssetPath(FieldInfo field)
        {
            var name = SUB_SETTINGS_PREFIX + ObjectNames.NicifyVariableName(field.Name).Replace(" ", "");
            return AssetDatabase.GenerateUniqueAssetPath(GetSettingsFolderPath() + "/" + name + ".asset");
        }

        /// <summary>
        /// Name shown in the sub settings foldout : without the SUBS_ prefix, nicified (SUBS_SubSetTest -> Sub Set Test)
        /// </summary>
        protected static string GetSubSettingsDisplayName(ScriptableObject subSettings)
        {
            var name = subSettings.name;
            if (name.StartsWith(SUB_SETTINGS_PREFIX)) name = name.Substring(SUB_SETTINGS_PREFIX.Length);
            return ObjectNames.NicifyVariableName(name);
        }

        protected ScriptableObject CreateSubSettings(Type type, FieldInfo field)
        {
            var newSubSettings = Database.CreateScriptableAsset(type, GetNewSubSettingsAssetPath(field));
            AssetDatabase.SaveAssetIfDirty(newSubSettings);
            return newSubSettings;
        }

        /// <summary>
        /// Whether <paramref name="subSettings"/> is its own asset, in the folder of the settings, with the SUBS_ prefix
        /// </summary>
        protected bool IsSubSettingsPlaced(ScriptableObject subSettings)
        {
            if (!AssetDatabase.IsMainAsset(subSettings)) return false;

            var path = AssetDatabase.GetAssetPath(subSettings);
            return path.Substring(0, path.LastIndexOf('/')) == GetSettingsFolderPath()
                && subSettings.name.StartsWith(SUB_SETTINGS_PREFIX);
        }

        /// <summary>
        /// Makes <paramref name="subSettings"/> its own asset in the folder of the settings (extracted if nested, moved otherwise)
        /// and ensures its name has the SUBS_ prefix (the rest of its name is kept)
        /// </summary>
        protected void PlaceSubSettings(ScriptableObject subSettings, FieldInfo field)
        {
            if (subSettings == null || IsSubSettingsPlaced(subSettings)) return;

            var currentName = string.IsNullOrWhiteSpace(subSettings.name)
                ? ObjectNames.NicifyVariableName(field.Name).Replace(" ", "")
                : subSettings.name;
            var placedName = currentName.StartsWith(SUB_SETTINGS_PREFIX) ? currentName : SUB_SETTINGS_PREFIX + currentName;
            var placedPath = GetSettingsFolderPath() + "/" + placedName + ".asset";

            // Nested in an asset (former sub settings were added to their settings asset) or not saved at all : becomes its own asset
            if (!AssetDatabase.IsMainAsset(subSettings))
            {
                placedPath = AssetDatabase.GenerateUniqueAssetPath(placedPath);
                if (AssetDatabase.IsSubAsset(subSettings))
                {
                    AssetDatabase.RemoveObjectFromAsset(subSettings);
                }
                subSettings.name = System.IO.Path.GetFileNameWithoutExtension(placedPath);
                AssetDatabase.CreateAsset(subSettings, placedPath);
                return;
            }

            // Own asset in another folder or without the prefix : moved / renamed
            var currentPath = AssetDatabase.GetAssetPath(subSettings);
            if (currentPath != placedPath)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(placedPath) != null)
                {
                    placedPath = AssetDatabase.GenerateUniqueAssetPath(placedPath);
                }
                var error = AssetDatabase.MoveAsset(currentPath, placedPath);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError("Could not place sub settings " + subSettings.name + " at " + placedPath + " : " + error);
                }
            }
        }

        /// <summary>
        /// Places every sub settings in the folder of the settings, then removes the objects nested in the settings asset
        /// </summary>
        protected void EnsureSubSettingsValidity()
        {
            if (m_subSettingsFields.IsValid())
            {
                foreach (var (field, _) in m_subSettingsFields)
                {
                    var p_subSettings = serializedObject.FindProperty(field.Name);
                    if (p_subSettings != null && p_subSettings.objectReferenceValue is ScriptableObject subSettings)
                    {
                        PlaceSubSettings(subSettings, field);
                    }
                }
            }

            // Sub settings that couldn't be extracted are kept
            EditorDataUtility.EnsureAssetValidity(m_settings, (subAsset) =>
            {
                if (m_subSettingsFields.IsValid())
                {
                    foreach (var (field, _) in m_subSettingsFields)
                    {
                        var p_subSettings = serializedObject.FindProperty(field.Name);
                        if (p_subSettings != null && p_subSettings.objectReferenceValue == subAsset)
                        {
                            return true;
                        }
                    }
                }
                return false;
            });

            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region Utility Methods

        protected virtual void FetchSubSettingsField()
        {
            m_subSettingsFields = new();
            try
            {
                // DeclaredOnly on each type of the hierarchy : GetFields on the type itself doesn't return the private fields of its base classes
                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                for (var type = target.GetType(); type != null && type != typeof(BaseSettings); type = type.BaseType)
                {
                    foreach (var field in type.GetFields(bindingFlags))
                    {
                        var attribute = field.GetCustomAttribute<SubSettingsAttribute>();
                        if (attribute != null
                            && typeof(ScriptableObject).IsAssignableFrom(field.FieldType)
                            && !m_subSettingsFields.ContainsKey(field))
                        {
                            m_subSettingsFields.Add(field, attribute);
                            m_excludedProperties.Add(field.Name);
                        }
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
            if (m_subSettingsEditors == null) return;

            foreach (var (_, editor) in m_subSettingsEditors)
            {
                if (editor != null) DestroyImmediate(editor);
            }
            m_subSettingsEditors.Clear();
        }
        protected void DestroySubSettingsEditor(ScriptableObject so)
        {
            if (m_subSettingsEditors != null && m_subSettingsEditors.TryGetValue(so, out var editor))
            {
                if (editor != null) DestroyImmediate(editor);
                m_subSettingsEditors.Remove(so);
            }
        }

        #endregion

        #region GUI

        protected virtual void OnSubSettingsReferenceGUI()
        {
            // Sub settings are assets next to their settings asset : user settings are stored in a file, not as an asset
            if (m_settings.Editor_IsUserSettings)
            {
                if (m_subSettingsFields.IsValid())
                {
                    EditorGUILayout.HelpBox("Sub settings aren't supported for user settings (Preferences), which are stored in the UserSettings folder", MessageType.Info);
                }
                return;
            }

            if (m_subSettingsFields.IsValid())
            {
                foreach (var (field, attribute) in m_subSettingsFields)
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
                        if (p_subSettings.objectReferenceValue is ScriptableObject subSettings)
                        {
                            if (!IsSubSettingsPlaced(subSettings))
                            {
                                using (new GUIHelper.GUIBackgroundColorScope(Color.yellow))
                                {
                                    if (GUI.Button(buttonRect, "PLACE IN FOLDER"))
                                    {
                                        PlaceSubSettings(subSettings, field);
                                        AssetDatabase.SaveAssets();
                                    }
                                }
                            }
                            else
                            {
                                using (new GUIHelper.GUIBackgroundColorScope(Color.red))
                                {
                                    if (GUI.Button(buttonRect, "DELETE SUB SETTINGS")
                                        && EditorUtility.DisplayDialog("Delete sub settings ?",
                                            "Delete " + subSettings.name + " ?\n\n" +
                                            "The asset is moved to the system's recycle bin : restore it from there, then undo (Ctrl+Z) to get the reference back.",
                                            "Delete", "Cancel"))
                                    {
                                        DestroySubSettingsEditor(subSettings);
                                        // Unity's undo can't restore a deleted asset file : moved to the trash instead of deleted.
                                        // The reference change goes through the serialized object, so it can be undone
                                        if (AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(subSettings)))
                                        {
                                            p_subSettings.objectReferenceValue = null;
                                        }
                                        else
                                        {
                                            Debug.LogError("Could not move " + subSettings.name + " to the trash");
                                        }
                                    }
                                }
                            }
                        }
                        else if (field.FieldType != typeof(ScriptableObject))
                        {
                            using (new GUIHelper.GUIBackgroundColorScope(Color.green))
                            {
                                if (!field.FieldType.IsAbstract)
                                {
                                    if (GUI.Button(buttonRect, "CREATE " + field.FieldType.ToString().ToUpper()))
                                    {
                                        p_subSettings.objectReferenceValue = CreateSubSettings(field.FieldType, field);
                                    }
                                }
                                else
                                {
                                    if (GUI.Button(buttonRect, new GUIContent("CREATE", EditorGUIHelper.DownIcon.image)))
                                    {
                                        EditorGUIHelper.DrawChildTypeSelector(field.FieldType, Callback);

                                        void Callback(Type type)
                                        {
                                            var newSubSettings = CreateSubSettings(type, field);
                                            if (serializedObject != null)
                                            {
                                                serializedObject.FindProperty(field.Name).objectReferenceValue = newSubSettings;
                                                serializedObject.ApplyModifiedProperties();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            EditorGUI.LabelField(buttonRect, "Can't create asset of type " + field.FieldType.Name);
                        }

                        EditorGUILayout.Space(5f);
                    }
                }
            }

            using (new GUIHelper.GUIBackgroundColorScope(Color.cyan))
            {
                if (GUILayout.Button("ENSURE ASSET VALIDITY"))
                {
                    EnsureSubSettingsValidity();
                }
            }
        }

        protected virtual void OnSubSettingsGUI()
        {
            if (m_subSettingsFields.IsValid())
            {
                bool hasValidSubSettings = false;
                foreach (var (field, attribute) in m_subSettingsFields)
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
            // TODO Menu options
            var rect = EditorGUILayout.GetControlRect(false, 22f);
            var decoRect = new Rect(0f, rect.y, EditorGUIUtility.currentViewWidth, rect.height);
            EditorGUI.DrawRect(decoRect, GUIHelper.grey015);
            decoRect.height = 1f;
            EditorGUI.DrawRect(decoRect, Color.black);

            // Foldout
            var foldoutRect = new Rect(rect.x, rect.y, rect.width - 30f, rect.height);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GetSubSettingsDisplayName(so), true);
            // Options Button
            var optionsButtonRect = new Rect(rect.x + rect.width - 20f, rect.y + 2f, 30f, rect.height);
            if (GUI.Button(optionsButtonRect, EditorGUIHelper.MenuIcon, EditorStyles.iconButton))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Ping Asset"), false, PingAssetCallback);
                menu.AddItem(new GUIContent("Edit Script"), false, EditScriptCallback);
                menu.ShowAsContext();

                void PingAssetCallback() { EditorUtils.FullPingObject(so); }
                void EditScriptCallback()
                {
                    if (so != null)
                    {
                        var serializedObject = new SerializedObject(so);
                        AssetDatabase.OpenAsset(serializedObject.FindProperty("m_Script").objectReferenceValue);
                        serializedObject.Dispose();
                    }
                }
            }

            if (property.isExpanded)
            {
                EditorGUILayout.Space(2f);
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
