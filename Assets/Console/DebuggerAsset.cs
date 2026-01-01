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
        #region Members

        [SerializeField] private List<DebugCategoryElement> m_debugCategories;

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
                var p_saveCategory = p_debugCategories.GetArrayElementAtIndex(i);
                DrawCategoryListElement(p_saveCategory, i);
                if (i < p_debugCategories.arraySize - 1)
                {
                    EditorGUILayout.Space(2f);
                }
            }

            EditorGUILayout.EndScrollView();
        }
        private void DrawCategoryListElement(SerializedProperty p_category, int index)
        {
            //var rect = EditorGUILayout.GetControlRect(false, 28f);
            //GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            //
            //var marginedRect = new Rect(rect.x + 5f, rect.y + 4f, rect.width - 10f, rect.height - 8f);
            //
            //// Name
            //var buttonsTotalWidth = 35f;
            //var r_indexLabel = new Rect(marginedRect.x, marginedRect.y, 20f, 20f);
            //var r_nameTextField = new Rect(marginedRect.x + 20f, marginedRect.y, marginedRect.width - buttonsTotalWidth - 20f, 20f);
            //EditorGUI.LabelField(r_indexLabel, index.ToString(), EditorStyles.boldLabel);
            //var newName = EnumWriter.EnsureCorrectEnumName(EditorGUI.DelayedTextField(r_nameTextField, p_category.stringValue));
            //if (newName != p_category.stringValue)
            //{
            //    p_category.stringValue = newName;
            //}
            //
            //var r_deleteButton = new Rect(marginedRect.x + marginedRect.width - 30f, marginedRect.y, 32f, 20f);
            //using (new GUIHelper.GUIBackgroundColorScope(Color.red))
            //{
            //    if (GUI.Button(r_deleteButton, EditorGUIHelper.DeleteIcon))
            //    {
            //        p_debugCategories.DeleteArrayElementAtIndex(index);
            //    }
            //}
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
                    p_debugCategories.GetArrayElementAtIndex(p_debugCategories.arraySize - 1).stringValue = "NEW_CATEGORY";
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
        }

        #endregion


        #region Script Generation

        private string GetDebugCategoriesScriptContent()
        {
            string[] debugCategories = new string[p_debugCategories.arraySize];
            for (int i = 0; i < debugCategories.Length; i++)
            {
                debugCategories[i] = p_debugCategories.GetArrayElementAtIndex(i).stringValue;
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
                        || value.ToString() != p_debugCategories.GetArrayElementAtIndex(i).stringValue)
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

