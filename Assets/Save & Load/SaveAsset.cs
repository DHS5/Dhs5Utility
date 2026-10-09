using UnityEngine;
using Dhs5.Utility.Databases;
using System.Collections.Generic;
using System;
using System.IO;

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
        #region STRUCT CategoryRename

        [Serializable]
        private struct CategoryRename
        {
            public CategoryRename(string previousName, string currentName)
            {
                this.previousName = previousName;
                this.currentName = currentName;
            }

            public string previousName;
            public string currentName;
        }

        #endregion

        #region Members

        [SerializeField] private SaveProcessModifier m_modifier;
        [SerializeField] private List<string> m_saveCategories;
        [SerializeField] private List<ESaveCategory> m_loadOrder;

        [Tooltip("Version written in every save file\n" +
            "Increase it when the save data changes in a way loadables need to handle (see SaveManager.GetSaveVersion)")]
        [SerializeField, Min(1)] private int m_saveVersion = 1;
        [Tooltip("Former names of the save categories, used to load the sub objects of saves made before a category was renamed")]
        [SerializeField] private List<CategoryRename> m_categoryRenames = new();

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

        internal static bool HasModifier(out SaveProcessModifier modifier)
        {
            modifier = Instance != null ? Instance.m_modifier : null;
            return modifier != null;
        }

        /// <summary>
        /// Version written in new save files
        /// </summary>
        internal static int SaveVersion => Instance != null ? Instance.m_saveVersion : 0;

        /// <summary>
        /// Finds the current category of data saved under <paramref name="savedName"/>,
        /// following the renames of the category since the save was made
        /// </summary>
        /// <returns>FALSE if the category doesn't exist anymore</returns>
        internal static bool TryGetCategoryFromSavedName(string savedName, out ESaveCategory category)
        {
            if (TryParseCategory(savedName, out category))
            {
                return true;
            }

            if (Instance != null && Instance.m_categoryRenames != null)
            {
                foreach (var rename in Instance.m_categoryRenames)
                {
                    if (rename.previousName == savedName)
                    {
                        return TryParseCategory(rename.currentName, out category);
                    }
                }
            }

            category = default;
            return false;
        }
        private static bool TryParseCategory(string name, out ESaveCategory category)
        {
            return Enum.TryParse(name, out category) && Enum.IsDefined(typeof(ESaveCategory), category);
        }

        internal static IEnumerable<KeyValuePair<ESaveCategory, uint>> GetCategoriesInLoadOrder()
        {
            if (Instance != null)
            {
                Dictionary<ESaveCategory, uint> categoryIterationDico = new();

                foreach (var category in Instance.m_loadOrder)
                {
                    if (categoryIterationDico.ContainsKey(category))
                    {
                        categoryIterationDico[category]++;
                    }
                    else
                    {
                        categoryIterationDico.Add(category, 1);
                    }

                    yield return new KeyValuePair<ESaveCategory, uint>(category, categoryIterationDico[category]);
                }
            }
        }

        #endregion

        #region Static Process Methods

        /// <returns>Whether the save file was successfully written. If not, the previous save file is kept intact</returns>
        internal static bool SaveContentToDisk(SaveObject saveObject, ISaveParameter parameter)
        {
            // PATH
            var path = CreateSavePath(saveObject, parameter);

            // ENCRYPTION
            var content = GetEncryptedContent(saveObject.GetSaveContent());

            // PATH VALIDITY
            UtilityMethods.EnsureAssetParentDirectoryExistence(path);

            // WRITE
            return WriteToDisk(path, content, parameter);
        }

        /// <summary>
        /// Without a SaveProcessModifier, reads the default save file (see <see cref="GetDefaultSavePath"/>)
        /// </summary>
        internal static string ReadContentFromSelectedSaveFile(ISaveParameter parameter)
        {
            // GET SAVE CONTENT
            if (HasModifier(out var modifier))
            {
                return modifier.GetDecryptedContent(modifier.ReadSelectedSaveFileFromDisk());
            }

            return File.ReadAllText(GetDefaultSavePath(parameter), GetEncoding(parameter));
        }
        internal static string ReadContentAtPath(string path, System.Text.Encoding encoding)
        {
            if (HasModifier(out var modifier))
            {
                return modifier.GetDecryptedContent(File.ReadAllText(path, encoding));
            }
            else
            {
                return File.ReadAllText(path, encoding);
            }
        }

        #region Path

        /// <summary>
        /// Exceptions thrown by the modifier are not caught : falling back to the default path would create a save file the modifier never reads,
        /// so the save process fails instead (see <see cref="SaveManager.CompleteSaveProcess"/>) and the previous save file is kept
        /// </summary>
        private static string CreateSavePath(SaveObject saveObject, ISaveParameter parameter)
        {
            if (HasModifier(out var modifier))
            {
                return modifier.CreateSavePath(saveObject, parameter);
            }

            return GetDefaultSavePath(parameter);
        }
        /// <summary>
        /// Path of the save file when there is no SaveProcessModifier : a single save file in the persistent data folder
        /// </summary>
        internal static string GetDefaultSavePath(ISaveParameter parameter)
        {
            var extension = parameter != null ? parameter.GetExtension() : ".txt";
            return Application.persistentDataPath + "/Save/SAVE" + extension;
        }

        private static System.Text.Encoding GetEncoding(ISaveParameter parameter)
        {
            return parameter != null ? parameter.GetEncoding() : System.Text.Encoding.Default;
        }

        #endregion

        #region Encryption

        /// <summary>
        /// Exceptions thrown by the modifier are not caught : writing unencrypted content would create a save file that can't be loaded,
        /// so the save process fails instead (see <see cref="SaveManager.CompleteSaveProcess"/>) and the previous save file is kept
        /// </summary>
        private static string GetEncryptedContent(string content)
        {
            if (HasModifier(out var modifier))
            {
                return modifier.GetEncryptedContent(content);
            }
            return content;
        }

        #endregion

        #region Write

        private const string TEMP_FILE_EXTENSION = ".tmp";

        /// <summary>
        /// Writes <paramref name="content"/> to a temporary file first, then replaces the save file with it :
        /// if the write is interrupted (crash, power loss...) or fails, the previous save file stays intact
        /// </summary>
        private static bool WriteToDisk(string path, string content, ISaveParameter parameter)
        {
            var tempPath = path + TEMP_FILE_EXTENSION;
            try
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);

                if (HasModifier(out var modifier))
                {
                    modifier.WriteToDisk(tempPath, content, parameter);
                }
                else
                {
                    File.WriteAllText(tempPath, content, GetEncoding(parameter));
                }

                // The modifier didn't write at the given path (e.g. custom storage) : nothing to replace
                if (!File.Exists(tempPath)) return true;

                ReplaceFile(tempPath, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("SAVE ERROR : Could not write the save file at " + path + ", the previous save file is kept");
                try
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
                catch (Exception deleteException)
                {
                    Debug.LogException(deleteException);
                }
                return false;
            }
        }

        private static void ReplaceFile(string sourcePath, string destinationPath)
        {
            if (!File.Exists(destinationPath))
            {
                File.Move(sourcePath, destinationPath);
                return;
            }

            try
            {
                File.Replace(sourcePath, destinationPath, null);
            }
            catch (Exception e) when (e is PlatformNotSupportedException or IOException)
            {
                // Some platforms or file systems don't support File.Replace
                File.Copy(sourcePath, destinationPath, true);
                File.Delete(sourcePath);
            }
        }

        #endregion

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

        private SerializedProperty p_modifier;
        private SerializedProperty p_saveCategories;
        private SerializedProperty p_loadOrder;

        private SerializedProperty p_saveVersion;
        private SerializedProperty p_categoryRenames;

        private SerializedProperty p_saveCategoriesTextAsset;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            m_saveAsset = target as SaveAsset;

            p_modifier = serializedObject.FindProperty("m_modifier");
            p_saveCategories = serializedObject.FindProperty("m_saveCategories");
            p_loadOrder = serializedObject.FindProperty("m_loadOrder");

            p_saveVersion = serializedObject.FindProperty("m_saveVersion");
            p_categoryRenames = serializedObject.FindProperty("m_categoryRenames");

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
            var newName = EnumWriter.EnsureCorrectEnumName(EditorGUI.DelayedTextField(r_nameTextField, p_category.stringValue));
            if (newName != p_category.stringValue
                && CanUseCategoryName(newName))
            {
                RegisterCategoryRename(p_category.stringValue, newName);
                p_category.stringValue = newName;
            }

            var r_deleteButton = new Rect(marginedRect.x + marginedRect.width - 30f, marginedRect.y, 32f, 20f);
            using (new GUIHelper.GUIBackgroundColorScope(Color.red))
            {
                if (GUI.Button(r_deleteButton, EditorGUIHelper.DeleteIcon)
                    && EditorUtility.DisplayDialog("Delete category ?",
                        "Are you sure you want to delete " + p_category.stringValue + " ?\n\n" +
                        "The load order will be kept without this category.\n" +
                        "Once the category script is updated, every category after it shifts down by one, " +
                        "so other serialized ESaveCategory values pointing to them will change.",
                        "Delete", "Cancel"))
                {
                    RemoveCategoryFromLoadOrder(index);
                    RemoveCategoryRenames(p_category.stringValue);
                    p_saveCategories.DeleteArrayElementAtIndex(index);
                }
            }
        }

        #region Category Renames

        /// <summary>
        /// Former names of a category are kept so that old saves still load into it :
        /// reusing a former name for a category would make those old saves load into it instead
        /// </summary>
        private bool CanUseCategoryName(string name)
        {
            for (int i = 0; i < p_categoryRenames.arraySize; i++)
            {
                var p_rename = p_categoryRenames.GetArrayElementAtIndex(i);
                if (p_rename.FindPropertyRelative("previousName").stringValue == name)
                {
                    var currentName = p_rename.FindPropertyRelative("currentName").stringValue;
                    if (EditorUtility.DisplayDialog("Use former category name ?",
                        name + " is a former name of " + currentName + ".\n\n" +
                        "Saves made before the rename store " + currentName + "'s data under " + name + " : " +
                        "if you use this name, they will load into this category instead of " + currentName + ".",
                        "Use it anyway", "Cancel"))
                    {
                        p_categoryRenames.DeleteArrayElementAtIndex(i);
                        return true;
                    }
                    return false;
                }
            }
            return true;
        }

        private void RegisterCategoryRename(string oldName, string newName)
        {
            // Former names of the renamed category now point to its new name
            for (int i = p_categoryRenames.arraySize - 1; i >= 0; i--)
            {
                var p_rename = p_categoryRenames.GetArrayElementAtIndex(i);
                var p_currentName = p_rename.FindPropertyRelative("currentName");
                if (p_currentName.stringValue == oldName)
                {
                    p_currentName.stringValue = newName;
                }
                // Renamed back to a former name
                if (p_rename.FindPropertyRelative("previousName").stringValue == p_currentName.stringValue)
                {
                    p_categoryRenames.DeleteArrayElementAtIndex(i);
                }
            }

            // Only names written in the category script can be in save files
            if (oldName != newName
                && Enum.TryParse(oldName, out ESaveCategory category) && Enum.IsDefined(typeof(ESaveCategory), category))
            {
                p_categoryRenames.InsertArrayElementAtIndex(p_categoryRenames.arraySize);
                var p_rename = p_categoryRenames.GetArrayElementAtIndex(p_categoryRenames.arraySize - 1);
                p_rename.FindPropertyRelative("previousName").stringValue = oldName;
                p_rename.FindPropertyRelative("currentName").stringValue = newName;
            }
        }

        /// <summary>
        /// The data of a deleted category is ignored when loading old saves
        /// </summary>
        private void RemoveCategoryRenames(string deletedName)
        {
            for (int i = p_categoryRenames.arraySize - 1; i >= 0; i--)
            {
                if (p_categoryRenames.GetArrayElementAtIndex(i).FindPropertyRelative("currentName").stringValue == deletedName)
                {
                    p_categoryRenames.DeleteArrayElementAtIndex(i);
                }
            }
        }

        #endregion

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
            using (new GUIHelper.GUIBackgroundColorScope(DoesSaveCategoryScriptNeedUpdate() ? Color.cyan : Color.grey))
            {
                if (GUILayout.Button("UPDATE CATEGORY SCRIPT", GUILayout.Height(25f)))
                {
                    if (p_saveCategoriesTextAsset.objectReferenceValue is TextAsset textAsset)
                    {
                        Database.CreateOrOverwriteTextAsset(textAsset, GetSaveCategoriesScriptContent());
                    }
                    else
                    {
                        Debug.LogError("There is no ESaveCategory.cs to overwrite");
                    }
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
                            if (p_loadOrder.GetArrayElementAtIndex(i).intValue == property.intValue)
                            {
                                cycle++;
                            }
                        }
                        var isRepetition = cycle > 1;

                        EditorGUI.BeginDisabledGroup(true);
                        var r_element = new Rect(rect.x, rect.y + 2f, rect.width - (isRepetition ? 20f : 0f), EditorGUIUtility.singleLineHeight);
                        EditorGUI.TextField(r_element, "Load Step " + index, GetSaveCategoryName(property.intValue));
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

                        for (int i = 0; i < p_saveCategories.arraySize; i++)
                        {
                            menu.AddItem(new GUIContent(GetSaveCategoryName(i)), false, AddCallback, i);
                        }

                        menu.DropDown(rect);
                    },

                    onCanRemoveCallback = (list) =>
                    {
                        var property = p_loadOrder.GetArrayElementAtIndex(list.index);

                        var cycle = 0;
                        for (int i = 0; i < list.index; i++)
                        {
                            if (p_loadOrder.GetArrayElementAtIndex(i).intValue == property.intValue)
                            {
                                cycle++;
                            }
                        }
                        return cycle > 0;
                    }
                };
            }

            // Ensure at least one occurence of every categories
            // (based on the categories list rather than the compiled enum, which can be outdated)
            HashSet<int> currentElementsInLoadOrder = new();
            for (int  i = p_loadOrder.arraySize - 1; i >= 0; i--)
            {
                var categoryIndex = p_loadOrder.GetArrayElementAtIndex(i).intValue;
                if (categoryIndex >= p_saveCategories.arraySize || categoryIndex < 0)
                {
                    p_loadOrder.DeleteArrayElementAtIndex(i);
                    continue;
                }

                currentElementsInLoadOrder.Add(categoryIndex);
            }
            for (int categoryIndex = 0; categoryIndex < p_saveCategories.arraySize; categoryIndex++)
            {
                if (!currentElementsInLoadOrder.Contains(categoryIndex))
                {
                    p_loadOrder.InsertArrayElementAtIndex(p_loadOrder.arraySize);
                    p_loadOrder.GetArrayElementAtIndex(p_loadOrder.arraySize - 1).intValue = categoryIndex;
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
                property.GetArrayElementAtIndex(property.arraySize - 1).intValue = (int)obj;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private string GetSaveCategoryName(int categoryIndex)
        {
            if (categoryIndex >= 0 && categoryIndex < p_saveCategories.arraySize)
            {
                return p_saveCategories.GetArrayElementAtIndex(categoryIndex).stringValue;
            }
            return "INVALID (" + categoryIndex + ")";
        }

        /// <summary>
        /// Removes every occurence of the category at <paramref name="categoryIndex"/> from the load order
        /// and shifts the categories after it down by one, so the load order stays the same
        /// </summary>
        private void RemoveCategoryFromLoadOrder(int categoryIndex)
        {
            for (int i = p_loadOrder.arraySize - 1; i >= 0; i--)
            {
                var p_step = p_loadOrder.GetArrayElementAtIndex(i);
                if (p_step.intValue == categoryIndex)
                {
                    p_loadOrder.DeleteArrayElementAtIndex(i);
                }
                else if (p_step.intValue > categoryIndex)
                {
                    p_step.intValue--;
                }
            }
        }

        #endregion

        #region SETTINGS GUI

        public void DrawSettingsGUI()
        {
            // MODIFIER
            EditorGUILayout.LabelField("Modifier", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(p_modifier);
            if (p_modifier.objectReferenceValue == null)
            {
                if (GUILayout.Button(new GUIContent("CREATE", EditorGUIHelper.DownIcon.image), GUILayout.Height(18f), GUILayout.Width(100f)))
                {
                    EditorGUIHelper.DrawChildTypeSelector(typeof(SaveProcessModifier), Callback);

                    void Callback(Type type)
                    {
                        var newModifier = Database.CreateScriptableAsset(type, GetModifierAssetPath(ObjectNames.NicifyVariableName(type.Name)));
                        if (serializedObject != null)
                        {
                            serializedObject.FindProperty("m_modifier").objectReferenceValue = newModifier;
                            serializedObject.ApplyModifiedProperties();
                        }
                        AssetDatabase.SaveAssetIfDirty(newModifier);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // VERSION
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Version", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p_saveVersion);

            // CATEGORY RENAMES
            if (p_categoryRenames.arraySize > 0)
            {
                EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField(new GUIContent("Category Renames", p_categoryRenames.tooltip), EditorStyles.boldLabel);
                for (int i = 0; i < p_categoryRenames.arraySize; i++)
                {
                    var p_rename = p_categoryRenames.GetArrayElementAtIndex(i);
                    EditorGUILayout.LabelField(p_rename.FindPropertyRelative("previousName").stringValue + "  →  " + p_rename.FindPropertyRelative("currentName").stringValue);
                }
            }

            // SCRIPTS
            EditorGUILayout.Space(5f);
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
                    EnsureAssetSanity();
                }
            }
        }

        private void EnsureAssetSanity()
        {
            AssetDatabase.Refresh();

            // Extract the modifier from the asset (legacy sub-asset)
            var modifier = p_modifier.objectReferenceValue;
            if (modifier != null && AssetDatabase.IsSubAsset(modifier))
            {
                var modifierName = string.IsNullOrWhiteSpace(modifier.name) ? ObjectNames.NicifyVariableName(modifier.GetType().Name) : modifier.name;
                var newModifierPath = AssetDatabase.GenerateUniqueAssetPath(GetModifierAssetPath(modifierName));
                AssetDatabase.RemoveObjectFromAsset(modifier);
                AssetDatabase.CreateAsset(modifier, newModifierPath);
            }

            // Destroy intrusive objects
            EditorDataUtility.EnsureAssetValidity(m_saveAsset, (obj) => false);

            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        private string GetModifierAssetPath(string modifierName)
        {
            var saveAssetPath = AssetDatabase.GetAssetPath(m_saveAsset);
            return saveAssetPath.Substring(0, saveAssetPath.LastIndexOf('/')) + "/" + modifierName + ".asset";
        }

        #endregion


        #region Script Generation

        private string GetSaveCategoriesScriptContent()
        {
            string[] saveCategories = new string[p_saveCategories.arraySize];
            for (int i = 0; i < saveCategories.Length; i++)
            {
                saveCategories[i] = p_saveCategories.GetArrayElementAtIndex(i).stringValue;
            }

            var writer = new EnumWriter(
                enumNamespace: "Dhs5.Utility.SaveLoad",
                enumProtection: ScriptWriter.EProtection.PUBLIC,
                enumName: "ESaveCategory",
                enumContent: saveCategories,
                enumType: EnumWriter.EEnumType.USHORT);

            return writer.ToString();
        }

        #endregion

        #region Script Check

        private bool DoesSaveCategoryScriptNeedUpdate()
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
