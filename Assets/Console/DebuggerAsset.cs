using UnityEngine;
using System.Collections.Generic;
using Dhs5.Utility.Debuggers;
using Dhs5.Utility.Databases;
using Dhs5.Utility.GUIs;
using System;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Console
{
    public class DebuggerAsset : ScriptableObject
    {
        #region Consts

        public const int MAX_DEBUGGER_LEVEL = 2;
        public const float DEFAULT_SCREEN_LOG_DURATION = 5.0f;

        #endregion

        #region Members

        [SerializeField] private List<DebugCategoryObject> m_debugCategories;

        #endregion


        // --- STATIC ---

        #region Static Accessors

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

        public static DebugCategoryObject GetDebugCategoryObject(EDebugCategory category)
        {
            if (Instance.m_debugCategories.IsIndexValid((int)category, out var obj))
            {
                return obj;
            }
            Debug.LogWarning("No DebugCategoryObject found for category " + category);
            return null;
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
        #region Members

        private DebuggerAsset m_debuggerAsset;

        private Vector2 m_categoriesScrollPos;

        #endregion

        #region Serialized Properties

        private SerializedProperty p_debugCategories;

        private SerializedProperty p_debugCategoriesTextAsset;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            m_debuggerAsset = (DebuggerAsset)target;

            p_debugCategories = serializedObject.FindProperty("m_debugCategories");

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
                            && Database.DeleteNestedAsset(element, true))
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
                var newName = EnumWriter.EnsureCorrectEnumName(EditorGUI.DelayedTextField(r_nameTextField, element.name));
                if (newName != element.name)
                {
                    element.name = newName;
                    AssetDatabase.SaveAssetIfDirty(element);
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
                    p_debugCategories.InsertArrayElementAtIndex(p_debugCategories.arraySize);
                    p_debugCategories.GetArrayElementAtIndex(p_debugCategories.arraySize - 1).objectReferenceValue = null;
                    var newElement = Database.CreateScriptableAndAddToAsset<DebugCategoryObject>(m_debuggerAsset);
                    newElement.name = "NEW_CATEGORY";
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
                    AssetDatabase.Refresh();

                    // Put all categories in list inside the asset
                    var updaterAssetPath = AssetDatabase.GetAssetPath(m_debuggerAsset);
                    for (int i = 0; i < p_debugCategories.arraySize; i++)
                    {
                        if (p_debugCategories.GetArrayElementAtIndex(i).objectReferenceValue is DebugCategoryObject categoryObject
                            && AssetDatabase.GetAssetPath(categoryObject) != updaterAssetPath)
                        {
                            if (AssetDatabase.IsSubAsset(categoryObject))
                            {
                                AssetDatabase.RemoveObjectFromAsset(categoryObject);
                            }
                            AssetDatabase.AddObjectToAsset(categoryObject, updaterAssetPath);
                        }
                    }
                    // Put all channels in asset inside list
                    foreach (var subAsset in EditorDataUtility.GetSubAssets(m_debuggerAsset))
                    {
                        if (subAsset is DebugCategoryObject categoryObject)
                        {
                            bool isInside = false;
                            for (int j = 0; j < p_debugCategories.arraySize; j++)
                            {
                                if (p_debugCategories.GetArrayElementAtIndex(j).objectReferenceValue == categoryObject)
                                {
                                    isInside = true;
                                    break;
                                }
                            }

                            if (!isInside)
                            {
                                p_debugCategories.InsertArrayElementAtIndex(p_debugCategories.arraySize);
                                p_debugCategories.GetArrayElementAtIndex(p_debugCategories.arraySize - 1).objectReferenceValue = categoryObject;
                            }
                            continue;
                        }
                        // Same for Timelines
                    }

                    // Remove list null elements
                    for (int i = p_debugCategories.arraySize - 1; i >= 0; i--)
                    {
                        if (p_debugCategories.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        {
                            p_debugCategories.DeleteArrayElementAtIndex(i);
                        }
                    }

                    // Destroy intrusive objects
                    EditorDataUtility.EnsureAssetValidity(m_debuggerAsset, (subAsset) =>
                    {
                        return subAsset is DebugCategoryObject;
                    });
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
                debugCategories[i] = p_debugCategories.GetArrayElementAtIndex(i).objectReferenceValue.name;
            }

            var writer = new EnumWriter(
                enumNamespace: null,
                enumProtection: ScriptWriter.EProtection.PUBLIC,
                enumName: "EDebugCategory",
                enumContent: debugCategories,
                enumType: EnumWriter.EEnumType.USHORT);

            return writer.ToString();
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
                        || value.ToString() != p_debugCategories.GetArrayElementAtIndex(i).objectReferenceValue.name)
                    {
                        return true;
                    }
                    i++;
                }
            }
            return false;
        }
    }

    #endregion
}

#endif

    #endregion

