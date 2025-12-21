using UnityEngine;
using Dhs5.Utility.Databases;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
using Dhs5.Utility.GUIs;
using UnityEditorInternal;
#endif

namespace Dhs5.Utility.SaveLoad
{
    public class SaveAsset : ScriptableObject
    {
        #region Members

        [SerializeField] private List<string> m_saveCategories;
        [SerializeField] private List<ESaveCategory> m_loadOrder;

        #endregion


        // --- STATIC ---

        #region Static Instance

        private static SaveAsset _instance;
        internal static SaveAsset Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindSaveLoadAssetInProject();
                }

                return _instance;
            }
        }
        private static SaveAsset FindSaveLoadAssetInProject()
        {
            var array = Resources.LoadAll<SaveAsset>("Save");

            if (array != null && array.Length > 0)
            {
                return array[0];
            }

            if (Application.isPlaying)
            {
                Debug.LogError("No SaveAsset found in project");
            }
            return null;
        }

        #endregion

        #region Static Accessors

        internal static IEnumerable<KeyValuePair<ESaveCategory, uint>> GetCategoriesInLoadOrder()
        {
            Dictionary<ESaveCategory, uint> categoryCountDico = new();

            foreach (var category in Instance.m_loadOrder)
            {
                if (categoryCountDico.ContainsKey(category))
                {
                    categoryCountDico[category]++;
                }
                else
                {
                    categoryCountDico.Add(category, 1);
                }

                yield return new KeyValuePair<ESaveCategory, uint>(category, categoryCountDico[category]);
            }
        }

        #endregion


        // --- EDITOR ---

#if UNITY_EDITOR

        #region Editor Members

        [Tooltip("EDITOR ONLY\nText Asset used to write the Save Categories enum")]
        [SerializeField] private TextAsset m_saveCategoriesTextAsset;

        #endregion

#endif
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(SaveAsset))]
    public class SaveAssetEditor : Editor
    {
        #region Members

        private SaveAsset m_saveAsset;

        private Vector2 m_categoriesScrollPos;

        private ReorderableList m_loadList;

        #endregion

        #region Serialized Properties

        private SerializedProperty p_saveCategories;
        private SerializedProperty p_loadOrder;

        private SerializedProperty p_saveCategoriesTextAsset;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            m_saveAsset = target as SaveAsset;

            p_saveCategories = serializedObject.FindProperty("m_saveCategories");
            p_loadOrder = serializedObject.FindProperty("m_loadOrder");

            p_saveCategoriesTextAsset = serializedObject.FindProperty("m_saveCategoriesTextAsset");
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

            for (int i = 0; i < p_saveCategories.arraySize; i++)
            {
                var p_saveCategory = p_saveCategories.GetArrayElementAtIndex(i);
                DrawCategoryListElement(p_saveCategory, i);
                if (i < p_saveCategories.arraySize - 1)
                {
                    EditorGUILayout.Space(2f);
                }
            }

            EditorGUILayout.EndScrollView();
        }
        private void DrawCategoryListElement(SerializedProperty p_category, int index)
        {
            var rect = EditorGUILayout.GetControlRect(false, 28f);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            var marginedRect = new Rect(rect.x + 5f, rect.y + 4f, rect.width - 10f, rect.height - 8f);

            // Name
            var buttonsTotalWidth = 35f;
            var r_indexLabel = new Rect(marginedRect.x, marginedRect.y, 20f, 20f);
            var r_nameTextField = new Rect(marginedRect.x + 20f, marginedRect.y, marginedRect.width - buttonsTotalWidth - 20f, 20f);
            EditorGUI.LabelField(r_indexLabel, index.ToString(), EditorStyles.boldLabel);
            var newName = EditorDataUtility.EnumWriter.EnsureCorrectEnumName(EditorGUI.DelayedTextField(r_nameTextField, p_category.stringValue));
            if (newName != p_category.stringValue)
            {
                p_category.stringValue = newName;
            }

            var r_deleteButton = new Rect(marginedRect.x + marginedRect.width - 30f, marginedRect.y, 32f, 20f);
            using (new GUIHelper.GUIBackgroundColorScope(Color.red))
            {
                if (GUI.Button(r_deleteButton, EditorGUIHelper.DeleteIcon))
                {
                    p_saveCategories.DeleteArrayElementAtIndex(index);
                }
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
                    p_saveCategories.InsertArrayElementAtIndex(p_saveCategories.arraySize);
                    p_saveCategories.GetArrayElementAtIndex(p_saveCategories.arraySize - 1).stringValue = "NEW_CATEGORY";
                }
            }
            using (new GUIHelper.GUIBackgroundColorScope(DoesUpdateChannelScriptNeedUpdate() ? Color.cyan : Color.grey))
            {
                if (GUILayout.Button("UPDATE CHANNEL SCRIPT", GUILayout.Height(25f)))
                {

                }
            }
            EditorGUILayout.Space(3f);

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region LOAD GUI

        public void DrawLoadGUI()
        {
            EnsureListValidity();

            var lastRect = GUILayoutUtility.GetLastRect();
            var listRect = EditorGUILayout.BeginVertical();
            listRect.x += 5f; listRect.width -= 10f;
            m_loadList.DoList(listRect);
            EditorGUILayout.EndVertical();
        }

        private void EnsureListValidity()
        {
            if (m_loadList == null)
            {
                m_loadList = new ReorderableList(serializedObject, p_loadOrder, true, true, true, true)
                {
                    drawHeaderCallback = (rect) =>
                    {
                        EditorGUI.LabelField(rect, "LOAD ORDER", EditorStyles.boldLabel);
                    },

                    drawElementCallback = (rect, index, active, focused) =>
                    {
                        var property = p_loadOrder.GetArrayElementAtIndex(index);

                        var cycle = 1;
                        for (int i = 0; i < index; i++)
                        {
                            if (p_loadOrder.GetArrayElementAtIndex(i).enumValueIndex == property.enumValueIndex)
                            {
                                cycle++;
                            }
                        }
                        var isRepetition = cycle > 1;

                        EditorGUI.BeginDisabledGroup(true);
                        var r_element = new Rect(rect.x, rect.y + 2f, rect.width - (isRepetition ? 20f : 0f), rect.height);
                        EditorGUI.PropertyField(r_element, property, new GUIContent("Load Step " + index));
                        if (isRepetition)
                        {
                            var r_cycle = new Rect(rect.x + rect.width - 15f, rect.y, 15f, rect.height);
                            EditorGUI.LabelField(r_cycle, cycle.ToString(), EditorStyles.boldLabel);
                        }
                        EditorGUI.EndDisabledGroup();
                    },

                    onAddDropdownCallback = (rect, list) =>
                    {
                        GenericMenu menu = new();

                        foreach (var value in Enum.GetValues(typeof(ESaveCategory)))
                        {
                            ESaveCategory category = (ESaveCategory)value;
                            menu.AddItem(new GUIContent(category.ToString()), false, AddCallback, (int)category);
                        }

                        menu.DropDown(rect);
                    },

                    onCanRemoveCallback = (list) =>
                    {
                        var property = p_loadOrder.GetArrayElementAtIndex(list.index);

                        var cycle = 0;
                        for (int i = 0; i < list.index; i++)
                        {
                            if (p_loadOrder.GetArrayElementAtIndex(i).enumValueIndex == property.enumValueIndex)
                            {
                                cycle++;
                            }
                        }
                        return cycle > 0;
                    }
                };
            }

            // Ensure at least one occurence of every categories
            HashSet<ESaveCategory> currentElementsInLoadOrder = new();
            for (int  i = 0; i < p_loadOrder.arraySize; i++)
            {
                currentElementsInLoadOrder.Add((ESaveCategory)p_loadOrder.GetArrayElementAtIndex(i).enumValueIndex);
            }
            foreach (var value in Enum.GetValues(typeof(ESaveCategory)))
            {
                ESaveCategory category = (ESaveCategory)value;
                if (!currentElementsInLoadOrder.Contains(category))
                {
                    p_loadOrder.InsertArrayElementAtIndex(p_loadOrder.arraySize);
                    p_loadOrder.GetArrayElementAtIndex(p_loadOrder.arraySize - 1).enumValueIndex = (int)category;
                }
            }
        }

        void AddCallback(object obj)
        {
            if (serializedObject != null)
            {
                serializedObject.Update();
                var property = serializedObject.FindProperty("m_loadOrder");
                property.InsertArrayElementAtIndex(property.arraySize);
                property.GetArrayElementAtIndex(property.arraySize - 1).enumValueIndex = (int)obj;
                serializedObject.ApplyModifiedProperties();
            }
        }

        #endregion

        #region SETTINGS GUI

        public void DrawSettingsGUI()
        {
            // SCRIPTS
            EditorGUILayout.LabelField("Scripts", EditorStyles.boldLabel);

            if (p_saveCategoriesTextAsset.objectReferenceValue == null)
            {
                var scriptObj = serializedObject.FindProperty("m_Script").objectReferenceValue;
                if (scriptObj != null)
                {
                    string path;
                    if (p_saveCategoriesTextAsset.objectReferenceValue == null)
                    {
                        path = ProjectWindowUtil.GetContainingFolder(AssetDatabase.GetAssetPath(scriptObj)) + "/ESaveCategory.cs";
                        p_saveCategoriesTextAsset.objectReferenceValue = Database.CreateOrLoadTextAsset(path);
                    }
                }
            }
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.ObjectField(p_saveCategoriesTextAsset);
            }

            // ASSET SANITY
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Asset Sanity", EditorStyles.boldLabel);

            using (new GUIHelper.GUIBackgroundColorScope(Color.green))
            {
                if (GUILayout.Button("ENSURE ASSET SANITY"))
                {
                  
                }
            }
        }

        #endregion


        #region Script Check

        private bool DoesUpdateChannelScriptNeedUpdate()
        {
            if (p_saveCategoriesTextAsset.objectReferenceValue != null)
            {
                int i = 0;
                var values = Enum.GetValues(typeof(ESaveCategory));
                if (values.Length != p_saveCategories.arraySize) return true;

                foreach (var obj in values)
                {
                    var value = (ESaveCategory)obj;
                    if (p_saveCategories.arraySize <= i
                        || value.ToString() != p_saveCategories.GetArrayElementAtIndex(i).stringValue)
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

#endif

    #endregion
}
