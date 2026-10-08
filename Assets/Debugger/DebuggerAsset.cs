using UnityEngine;
using System.Collections.Generic;
using Dhs5.Utility.Databases;
using Dhs5.Utility.GUIs;
using System;
using UnityEngine.InputSystem;
using System.Text;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Debugger
{
    public class DebuggerAsset : ScriptableObject
    {
        #region Consts

        public const int MAX_DEBUGGER_LEVEL = 2;
        public const float DEFAULT_SCREEN_LOG_DURATION = 5.0f;

        #endregion

        #region Members

        [SerializeField] private List<DebugCategoryObject> m_debugCategories;

        [SerializeField] private bool m_enableOnScreenConsole;
        [SerializeField] private InputActionReference m_openOnScreenConsoleInputRef;
        [SerializeField] private InputActionReference m_closeOnScreenConsoleInputRef;

        #endregion


        // --- STATIC ---

        #region Instance

        private static DebuggerAsset _instance;
        internal static DebuggerAsset Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindDebuggerAssetInProject();
                }

                return _instance;
            }
        }
        private static DebuggerAsset FindDebuggerAssetInProject()
        {
            var array = Resources.LoadAll<DebuggerAsset>("Debugger");

            if (array != null && array.Length > 0)
            {
                return array[0];
            }

            if (Application.isPlaying)
            {
                Debug.LogError("No Debugger Asset found in project");
            }
            return null;
        }

        #endregion

        #region Static Accessors

        public static DebugCategoryObject GetDebugCategoryObject(EDebugCategory category)
        {
            if (Instance.m_debugCategories.IsIndexValid((int)category, out var obj))
            {
                return obj;
            }
            Debug.LogWarning("No DebugCategoryObject found for category " + category);
            return null;
        }

        private readonly static Dictionary<EDebugCategory, Color> _categoryColors = new();
        public static Color GetCategoryColor(EDebugCategory category)
        {
            if (_categoryColors.TryGetValue(category, out var color)) return color;

            _categoryColors[category] = GetDebugCategoryObject(category).Color;
            return _categoryColors[category];
        }

        #endregion

        #region Settings Static Accessors

        internal static bool EnableOnScreenConsole => Instance != null && Instance.m_enableOnScreenConsole;
        internal static bool TryGetOpenOnScreenConsoleInputRef(out InputActionReference inputRef)
        {
            if (Instance != null && Instance.m_openOnScreenConsoleInputRef != null)
            {
                inputRef = Instance.m_openOnScreenConsoleInputRef;
                return true;
            }

            inputRef = null;
            return false;
        }
        internal static bool TryGetCloseOnScreenConsoleInputRef(out InputActionReference inputRef)
        {
            if (Instance != null && Instance.m_closeOnScreenConsoleInputRef != null)
            {
                inputRef = Instance.m_closeOnScreenConsoleInputRef;
                return true;
            }

            inputRef = null;
            return false;
        }

        #endregion


        // --- EDITOR ---

#if UNITY_EDITOR

        #region Editor Members

        [Tooltip("EDITOR ONLY\nText Asset used to write the Debug Categories enum")]
        [SerializeField] private TextAsset m_debugCategoriesTextAsset;

        #endregion

#endif
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(DebuggerAsset))]
    public class DebuggerAssetEditor : Editor
    {
        #region Consts

        private const string CATEGORY_PREFIX = "DC_";
        private const string BASE_CATEGORY_NAME = "BASE";

        #endregion

        #region Members

        private DebuggerAsset m_debuggerAsset;

        private Vector2 m_categoriesScrollPos;

        #endregion

        #region Serialized Properties

        private SerializedProperty p_debugCategories;
        private SerializedProperty p_enableOnScreenConsole;
        private SerializedProperty p_openOnScreenConsoleInputRef;
        private SerializedProperty p_closeOnScreenConsoleInputRef;

        private SerializedProperty p_debugCategoriesTextAsset;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            m_debuggerAsset = (DebuggerAsset)target;

            p_debugCategories = serializedObject.FindProperty("m_debugCategories");
            p_enableOnScreenConsole = serializedObject.FindProperty("m_enableOnScreenConsole");
            p_openOnScreenConsoleInputRef = serializedObject.FindProperty("m_openOnScreenConsoleInputRef");
            p_closeOnScreenConsoleInputRef = serializedObject.FindProperty("m_closeOnScreenConsoleInputRef");

            p_debugCategoriesTextAsset = serializedObject.FindProperty("m_debugCategoriesTextAsset");

            EnsureCorrectChannelsIndexation();
        }

        #endregion


        #region CATEGORIES GUI

        public void DrawCategoriesGUI()
        {
            EditorGUILayout.BeginVertical();

            // List
            DrawCategoriesList();

            // Footer
            DrawCategoriesFooter();

            EditorGUILayout.EndVertical();
        }

        private void DrawCategoriesList()
        {
            m_categoriesScrollPos = EditorGUILayout.BeginScrollView(m_categoriesScrollPos);

            for (int i = 0; i < p_debugCategories.arraySize; i++)
            {
                if (p_debugCategories.GetArrayElementAtIndex(i).objectReferenceValue is DebugCategoryObject element)
                {
                    DrawCategoryListElement(element, i);
                    if (i < p_debugCategories.arraySize - 1)
                    {
                        EditorGUILayout.Space(2f);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
        private void DrawCategoryListElement(DebugCategoryObject element, int index)
        {
            var rect = EditorGUILayout.GetControlRect(false, 48f);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            var so = new SerializedObject(element);
            if (so != null)
            {
                EditorGUI.DrawRect(new Rect(rect.x + 2f, rect.y, rect.width - 4f, 1f), element.Color);
                var marginedRect = new Rect(rect.x + 5f, rect.y + 4f, rect.width - 10f, rect.height - 8f);

                // Up/Down/Delete Buttons
                bool ret = false;
                using (new EditorGUI.DisabledGroupScope(index == 0))
                {
                    var r_upButton = new Rect(marginedRect.x + marginedRect.width - 94f, marginedRect.y - 2f, 32f, 20f);
                    using (new EditorGUI.DisabledGroupScope(index <= 1))
                    {
                        if (GUI.Button(r_upButton, EditorGUIHelper.UpIcon))
                        {
                            p_debugCategories.MoveArrayElement(index, index - 1);
                            ret = true;
                        }
                    }
                    var r_downButton = new Rect(marginedRect.x + marginedRect.width - 62f, marginedRect.y - 2f, 32f, 20f);
                    using (new EditorGUI.DisabledGroupScope(index == p_debugCategories.arraySize - 1))
                    {
                        if (GUI.Button(r_downButton, EditorGUIHelper.DownIcon))
                        {
                            p_debugCategories.MoveArrayElement(index, index + 1);
                            ret = true;
                        }
                    }
                    var r_deleteButton = new Rect(marginedRect.x + marginedRect.width - 30f, marginedRect.y - 2f, 32f, 20f);
                    using (new GUIHelper.GUIBackgroundColorScope(Color.red))
                    {
                        if (GUI.Button(r_deleteButton, EditorGUIHelper.DeleteIcon)
                            && Database.DeleteAsset(element, true))
                        {
                            p_debugCategories.DeleteArrayElementAtIndex(index);
                            AssetDatabase.SaveAssetIfDirty(m_debuggerAsset);
                            ret = true;
                        }
                    }
                }
                if (ret)
                {
                    so.Dispose();
                    EnsureCorrectChannelsIndexation();
                    return;
                }

                // Name
                EditorGUI.BeginDisabledGroup(index == 0);
                var buttonsTotalWidth = 100f;
                var p_enumIndex = so.FindProperty("m_enumIndex");
                var r_indexLabel = new Rect(marginedRect.x, marginedRect.y, 20f, 20f);
                var r_nameTextField = new Rect(marginedRect.x + 20f, marginedRect.y, marginedRect.width - buttonsTotalWidth - 20f, 20f);
                EditorGUI.LabelField(r_indexLabel, p_enumIndex.intValue.ToString(), EditorStyles.boldLabel);
                var displayName = GetCategoryEnumName(element);
                var newName = EnumWriter.EnsureCorrectEnumName(EditorGUI.DelayedTextField(r_nameTextField, displayName));
                if (newName != displayName)
                {
                    RenameCategory(element, newName);
                }
                EditorGUI.EndDisabledGroup();

                marginedRect.y += 22f;
                marginedRect.height -= 22f;

                // Light rim
                var r_lightRim = new Rect(marginedRect.x, marginedRect.y, 22f, 22f);
                EditorGUI.LabelField(r_lightRim, element.Level > -1 ? EditorGUIHelper.GreenLightIcon : EditorGUIHelper.RedLightIcon);

                // Level
                var r_level = new Rect(marginedRect.x + 30f, marginedRect.y, marginedRect.width - 30f - 100f, 18f);
                var p_level = so.FindProperty("m_level");
                p_level.intValue = EditorGUI.IntSlider(r_level, p_level.intValue, -1, DebuggerAsset.MAX_DEBUGGER_LEVEL);

                // Color
                EditorGUI.BeginDisabledGroup(index == 0);
                var r_color = new Rect(marginedRect.x + marginedRect.width - 94f, marginedRect.y, 94f, 18f);
                var p_color = so.FindProperty("m_color");
                EditorGUI.BeginChangeCheck();
                p_color.colorValue = EditorGUI.ColorField(r_color, p_color.colorValue);
                if (EditorGUI.EndChangeCheck())
                {
                    element.RefreshColorString();
                }
                EditorGUI.EndDisabledGroup();

                so.ApplyModifiedProperties();
                so.Dispose();
            }
        }

        private void DrawCategoriesFooter()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.Space(3f);
            using (new GUIHelper.GUIBackgroundColorScope(Color.green))
            {
                if (GUILayout.Button("ADD NEW CATEGORY", GUILayout.Height(25f)))
                {
                    var first = p_debugCategories.arraySize == 0;
                    var newElementName = first ? BASE_CATEGORY_NAME : GetUniqueCategoryEnumName("NEW_CATEGORY");
                    var newElement = Database.CreateScriptableAsset<DebugCategoryObject>(GetCategoryAssetPath(newElementName));
                    if (first)
                    {
                        newElement.Editor_SetColor(Color.white);
                        EditorUtility.SetDirty(newElement);
                    }
                    p_debugCategories.InsertArrayElementAtIndex(p_debugCategories.arraySize);
                    p_debugCategories.GetArrayElementAtIndex(p_debugCategories.arraySize - 1).objectReferenceValue = newElement;
                    AssetDatabase.SaveAssetIfDirty(newElement);
                    EnsureCorrectChannelsIndexation();
                }
            }
            using (new GUIHelper.GUIBackgroundColorScope(DoesDebugCategoryScriptNeedUpdate() ? Color.cyan : Color.grey))
            {
                if (GUILayout.Button("UPDATE CATEGORY SCRIPT", GUILayout.Height(25f)))
                {
                    if (p_debugCategoriesTextAsset.objectReferenceValue is TextAsset textAsset)
                    {
                        Database.CreateOrOverwriteTextAsset(textAsset, GetDebugCategoriesScriptContent());
                    }
                    else
                    {
                        Debug.LogError("There is no EDebugCategory.cs to overwrite");
                    }
                }
            }
            EditorGUILayout.Space(3f);

            EditorGUILayout.EndVertical();
        }

        private void EnsureCorrectChannelsIndexation()
        {
            if (p_debugCategories != null)
            {
                for (int i = 0; i < p_debugCategories.arraySize; i++)
                {
                    var obj = p_debugCategories.GetArrayElementAtIndex(i).objectReferenceValue;

                    if (obj != null)
                    {
                        var so = new SerializedObject(obj);
                        if (so != null)
                        {
                            so.FindProperty("m_enumIndex").intValue = i;

                            so.ApplyModifiedProperties();
                            so.Dispose();
                        }
                    }
                }
            }
        }

        #endregion

        #region Category Assets Utility

        private string GetCategoriesFolderPath()
        {
            var debuggerAssetPath = AssetDatabase.GetAssetPath(m_debuggerAsset);
            return debuggerAssetPath.Substring(0, debuggerAssetPath.LastIndexOf('/'));
        }
        private string GetCategoryAssetPath(string enumName)
        {
            return GetCategoriesFolderPath() + "/" + CATEGORY_PREFIX + enumName + ".asset";
        }

        private static string GetCategoryEnumName(UnityEngine.Object categoryObject)
        {
            var name = categoryObject.name;
            if (name != null && name.StartsWith(CATEGORY_PREFIX)) return name.Substring(CATEGORY_PREFIX.Length);
            return name;
        }
        private string GetUniqueCategoryEnumName(string enumName)
        {
            var uniqueName = enumName;
            for (int i = 1; AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GetCategoryAssetPath(uniqueName)) != null; i++)
            {
                uniqueName = enumName + "_" + i;
            }
            return uniqueName;
        }

        private void RenameCategory(DebugCategoryObject categoryObject, string newEnumName)
        {
            if (AssetDatabase.IsMainAsset(categoryObject))
            {
                var error = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(categoryObject), CATEGORY_PREFIX + newEnumName);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError("Could not rename category " + GetCategoryEnumName(categoryObject) + " : " + error);
                }
            }
            else
            {
                categoryObject.name = CATEGORY_PREFIX + newEnumName;
                AssetDatabase.SaveAssetIfDirty(categoryObject);
            }
        }

        #endregion

        #region SETTINGS GUI

        public void DrawSettingsGUI()
        {
            // SCRIPTS
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Scripts", EditorStyles.boldLabel);

            if (p_debugCategoriesTextAsset.objectReferenceValue == null)
            {
                var scriptObj = serializedObject.FindProperty("m_Script").objectReferenceValue;
                if (scriptObj != null)
                {
                    string path;
                    if (p_debugCategoriesTextAsset.objectReferenceValue == null)
                    {
                        path = ProjectWindowUtil.GetContainingFolder(AssetDatabase.GetAssetPath(scriptObj)) + "/EDebugCategory.cs";
                        p_debugCategoriesTextAsset.objectReferenceValue = Database.CreateOrLoadTextAsset(path);
                    }
                }
            }
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.ObjectField(p_debugCategoriesTextAsset);
            }

            // ASSET SANITY
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Asset Sanity", EditorStyles.boldLabel);

            using (new GUIHelper.GUIBackgroundColorScope(Color.green))
            {
                if (GUILayout.Button("ENSURE ASSET SANITY"))
                {
                    EnsureAssetSanity();
                }
            }

            // CONSOLE
            EditorGUILayout.Space(15f);
            EditorGUILayout.LabelField("Console", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p_enableOnScreenConsole);
            EditorGUILayout.PropertyField(p_openOnScreenConsoleInputRef);
            EditorGUILayout.PropertyField(p_closeOnScreenConsoleInputRef);
        }

        private void EnsureAssetSanity()
        {
            AssetDatabase.Refresh();

            // Put all categories nested in the asset inside list (legacy sub-assets)
            foreach (var subAsset in EditorDataUtility.GetSubAssets(m_debuggerAsset))
            {
                if (subAsset is DebugCategoryObject categoryObject)
                {
                    AddToCategoriesListIfMissing(categoryObject);
                }
            }

            // Remove list null elements
            RemoveCategoriesListNullElements();

            // Put all categories in list inside the folder, with correct names
            for (int i = 0; i < p_debugCategories.arraySize; i++)
            {
                if (p_debugCategories.GetArrayElementAtIndex(i).objectReferenceValue is DebugCategoryObject categoryObject)
                {
                    var enumName = i == 0 ? BASE_CATEGORY_NAME : GetCategoryEnumName(categoryObject);
                    var currentObjectPath = AssetDatabase.GetAssetPath(categoryObject);
                    var newObjectPath = GetCategoryAssetPath(enumName);

                    if (currentObjectPath == newObjectPath) continue;

                    if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newObjectPath) != null)
                    {
                        Debug.LogError("Can't move category " + enumName + " to " + newObjectPath + " : an asset already exists at this path");
                        continue;
                    }

                    if (AssetDatabase.IsSubAsset(categoryObject))
                    {
                        AssetDatabase.RemoveObjectFromAsset(categoryObject);
                        categoryObject.name = CATEGORY_PREFIX + enumName;
                        AssetDatabase.CreateAsset(categoryObject, newObjectPath);
                    }
                    else
                    {
                        var error = AssetDatabase.MoveAsset(currentObjectPath, newObjectPath);
                        if (!string.IsNullOrEmpty(error))
                        {
                            Debug.LogError("Can't move category " + enumName + " to " + newObjectPath + " : " + error);
                        }
                    }
                }
            }

            // Put all categories in folder inside list
            var guids = AssetDatabase.FindAssets("t:DebugCategoryObject", new[] { GetCategoriesFolderPath() });
            foreach (var categoryObject in guids.Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadMainAssetAtPath)
                .OfType<DebugCategoryObject>())
            {
                AddToCategoriesListIfMissing(categoryObject);
            }

            // Destroy intrusive objects (categories that failed to move are kept)
            EditorDataUtility.EnsureAssetValidity(m_debuggerAsset, (subAsset) =>
            {
                return subAsset is DebugCategoryObject;
            });

            // Make sure first debug category is white
            if (p_debugCategories.arraySize > 0
                && p_debugCategories.GetArrayElementAtIndex(0).objectReferenceValue is DebugCategoryObject baseElement)
            {
                baseElement.Editor_SetColor(Color.white);
                baseElement.RefreshColorString();
                EditorUtility.SetDirty(baseElement);
            }

            EnsureCorrectChannelsIndexation();

            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        private void AddToCategoriesListIfMissing(DebugCategoryObject categoryObject)
        {
            for (int i = 0; i < p_debugCategories.arraySize; i++)
            {
                if (p_debugCategories.GetArrayElementAtIndex(i).objectReferenceValue == categoryObject)
                {
                    return;
                }
            }

            p_debugCategories.InsertArrayElementAtIndex(p_debugCategories.arraySize);
            p_debugCategories.GetArrayElementAtIndex(p_debugCategories.arraySize - 1).objectReferenceValue = categoryObject;
        }
        private void RemoveCategoriesListNullElements()
        {
            for (int i = p_debugCategories.arraySize - 1; i >= 0; i--)
            {
                if (p_debugCategories.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    p_debugCategories.DeleteArrayElementAtIndex(i);
                }
            }
        }

        #endregion


        #region Script Generation

        private string GetDebugCategoriesScriptContent()
        {
            string[] debugCategories = new string[p_debugCategories.arraySize];
            for (int i = 0; i < debugCategories.Length; i++)
            {
                debugCategories[i] = GetCategoryEnumName(p_debugCategories.GetArrayElementAtIndex(i).objectReferenceValue);
            }

            var sb = new StringBuilder();

            var writer = new EnumWriter(
                enumNamespace: null,
                enumProtection: ScriptWriter.EProtection.PUBLIC,
                enumName: "EDebugCategory",
                enumContent: debugCategories,
                enumType: EnumWriter.EEnumType.USHORT);
            sb.AppendLine(writer.ToString());

            var flagsWriter = new EnumWriter(
                enumNamespace: null,
                enumProtection: ScriptWriter.EProtection.PUBLIC,
                enumName: "EDebugCategoryFlags",
                enumContent: debugCategories,
                enumType: EnumWriter.EEnumType.INT,
                attributes: new ScriptWriter.IAttribute[] { new ScriptWriter.EnumFlagsAttribute() });
            sb.AppendLine(flagsWriter.ToStringWithoutUsings());

            var extensionWriter = new ScriptWriter(
                scriptNamespace: null,
                scriptProtection: ScriptWriter.EProtection.PUBLIC,
                scriptType: ScriptWriter.EScriptType.STATIC_CLASS,
                scriptName: "DebugCategoryExtensions");
            extensionWriter.AppendMethod(
                protection: ScriptWriter.EProtection.PUBLIC,
                isStatic: true,
                returnType: typeof(bool),
                methodName: "HasCategory",
                isExtension: true,
                parameters: new ScriptWriter.MethodParameter[]
                {
                    new ScriptWriter.MethodParameter(typeof(EDebugCategoryFlags), "flags"),
                    new ScriptWriter.MethodParameter(typeof(EDebugCategory), "category")
                },
                methodContent: new string[]
                {
                    "return (flags & ((EDebugCategoryFlags)(1 << ((int)category)))) != 0;"
                });
            sb.Append(extensionWriter.ToStringWithoutUsings());

            return sb.ToString();
        }

        #endregion

        #region Script Check

        private bool DoesDebugCategoryScriptNeedUpdate()
        {
            if (p_debugCategoriesTextAsset.objectReferenceValue != null)
            {
                int i = 0;
                var values = Enum.GetValues(typeof(EDebugCategory));
                if (values.Length != p_debugCategories.arraySize) return true;

                foreach (var obj in values)
                {
                    var value = (EDebugCategory)obj;
                    if (p_debugCategories.arraySize <= i
                        || value.ToString() != GetCategoryEnumName(p_debugCategories.GetArrayElementAtIndex(i).objectReferenceValue))
                    {
                        return true;
                    }
                    i++;
                }
            }
            return false;
        }

        #endregion
    }

#endif

    #endregion
}

