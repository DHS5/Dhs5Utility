using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Dhs5.Utility.Editors;

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

        #region Instance Editor Methods

#if UNITY_EDITOR

        #region Instance Attribute

        internal string Editor_GetPath()
        {
            return GetPath(GetType());
        }

        protected virtual bool Editor_IsElementValid(UnityEngine.Object element)
        {
            if (element == null || element == this) return false;

            if (HasDataType(GetType(), out var type))
            {
                Type elementType = element.GetType();

                // Scriptable
                if (type == typeof(ScriptableObject) || type.IsSubclassOf(typeof(ScriptableObject)))
                {
                    return elementType == type || elementType.IsSubclassOf(type);
                }
                // Game Object
                if (type == typeof(GameObject))
                {
                    return elementType == type;
                }
                // Component
                if (type == typeof(Component) || type.IsSubclassOf(typeof(Component)))
                {
                    return element is GameObject go && go.TryGetComponent(type, out _);
                }

                return false;
            }
            return true;
        }

        #endregion

        #region Instance Content Management

        internal virtual IEnumerable<UnityEngine.Object> Editor_GetDatabaseContent()
        {
            yield return null;
        }

        internal bool Editor_DeleteElementAtIndex(int index)
        {
            if (Editor_OnDeleteElementAtIndex(index))
            {
                Editor_DatabaseContentChanged?.Invoke();
                return true;
            }
            return false;
        }
        protected virtual bool Editor_OnDeleteElementAtIndex(int index)
        {
            throw new NotImplementedException();
        }

        internal event Action Editor_DatabaseContentChanged;

        internal virtual void Editor_ShouldRecomputeDatabaseContent()
        {
            Editor_DatabaseContentChanged?.Invoke();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        /// <summary>
        /// Callback triggered when a new element has been created for the database<br></br>
        /// At this moment, the database content has already been recomputed so the element is already in it
        /// </summary>
        /// <param name="element"></param>
        internal virtual void Editor_OnNewElementCreated(UnityEngine.Object element) { }

        #endregion

#endif

        #endregion

        #region Static Editor Functions

#if UNITY_EDITOR

        #region Attributes & Child Types

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
        internal static bool HasDataType(Type type, out Type dataType)
        {
            if (TryGetAttribute(type, out var attribute))
            {
                dataType = attribute.dataType;
                return !attribute.anyType;
            }
            dataType = null;
            return false;
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

        #region Data Creation & Deletion Utility

        #region Creation

        public static UnityEngine.Object CreateAssetOfType(Type type, string path)
        {
            if (type.IsSubclassOf(typeof(ScriptableObject)))
            {
                return CreateScriptableAsset(type, path);
            }
            else if (type.IsSubclassOf(typeof(Component)))
            {
                return CreatePrefabWithComponent(type, path);
            }
            else if (type == typeof(GameObject))
            {
                return CreateEmptyPrefab(path);
            }
            return null;
        }

        // --- Scriptable ---
        public static ScriptableObject CreateScriptableAsset(Type type, string path)
        {
            if (!path.EndsWith(".asset")) path += ".asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var obj = ScriptableObject.CreateInstance(type);
            if (obj != null)
            {
                AssetDatabase.CreateAsset(obj, path);
                AssetDatabase.SaveAssetIfDirty(obj);
                EditorUtils.TriggerAssetRename(obj);
            }
            return obj;
        }
        public static T CreateScriptableAsset<T>(string path) where T : ScriptableObject
        {
            if (!path.EndsWith(".asset")) path += ".asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var obj = ScriptableObject.CreateInstance<T>();
            if (obj != null)
            {
                AssetDatabase.CreateAsset(obj, path);
                AssetDatabase.SaveAssetIfDirty(obj);
                EditorUtils.TriggerAssetRename(obj);
            }
            return obj;
        }
        public static ScriptableObject CreateScriptableAndAddToAsset(Type type, UnityEngine.Object asset)
        {
            var obj = ScriptableObject.CreateInstance(type);
            if (obj != null)
            {
                AssetDatabase.AddObjectToAsset(obj, AssetDatabase.GetAssetPath(asset));
                AssetDatabase.SaveAssets();
            }
            return obj;
        }
        public static T CreateScriptableAndAddToAsset<T>(UnityEngine.Object asset) where T : ScriptableObject
        {
            var obj = ScriptableObject.CreateInstance<T>();
            if (obj != null)
            {
                AssetDatabase.AddObjectToAsset(obj, AssetDatabase.GetAssetPath(asset));
                AssetDatabase.SaveAssets();
            }
            return obj;
        }

        // --- Prefab ---
        public static GameObject CreateEmptyPrefab(string path)
        {
            if (!path.EndsWith(".prefab")) path += ".prefab";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var template = new GameObject();
            var obj = PrefabUtility.SaveAsPrefabAsset(template, path, out var success);
            DestroyImmediate(template);
            if (success)
            {
                EditorUtils.TriggerAssetRename(obj);
                return obj;
            }
            return null;
        }
        public static Component CreatePrefabWithComponent(Type behaviourType, string path)
        {
            var obj = CreateEmptyPrefab(path);
            if (obj != null && !behaviourType.IsAbstract)
            {
                var component = obj.AddComponent(behaviourType);
                PrefabUtility.SavePrefabAsset(obj);
                return component;
            }
            return null;
        }

        #endregion

        #region Deletion

        public static bool IsAssetDeletableFromCode(UnityEngine.Object obj)
        {
            return obj != null;
        }

        /// <summary>
        /// Deletes an asset<br></br>
        /// If it's a main asset, deletes permanently and CAN'T UNDO<br></br>
        /// If it's not, delete the nested asset and CAN UNDO
        /// </summary>
        public static void DeleteAsset(UnityEngine.Object obj, bool needValidation)
        {
            if (obj == null) return;

            if (!AssetDatabase.IsMainAsset(obj))
            {
                DeleteNestedAsset(obj, needValidation);
                return;
            }

            if (!needValidation
                    || EditorUtility.DisplayDialog(
                        "Delete asset permanently ?",
                        "Are you sure you want to delete " + obj.name + " permanently ?",
                        "Yes", "Cancel"))
            {
                if (obj is GameObject)
                {
                    EditorUtils.TriggerAssetHardDeletion(obj);
                }
                else
                {
                    DestroyImmediate(obj, true);
                }
            }
        }
        /// <summary>
        /// Deletes a nested asset, records undo operation
        /// </summary>
        /// <param name="obj">Object to delete</param>
        /// <param name="asset">Asset in which the object is nested</param>
        /// <param name="needValidation"></param>
        public static void DeleteNestedAsset(UnityEngine.Object obj, bool needValidation)
        {
            if (obj == null || AssetDatabase.IsMainAsset(obj)) return;

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(obj));

            if (!needValidation
                    || EditorUtility.DisplayDialog(
                        "Delete nested asset ?",
                        "Are you sure you want to delete " + obj.name + " ?",
                        "Yes", "Cancel"))
            {
                Undo.SetCurrentGroupName("Delete asset " + obj.name);
                int undoGroup = Undo.GetCurrentGroup();

                Undo.RecordObject(asset, "Remove nested asset from " + asset.name);
                AssetDatabase.RemoveObjectFromAsset(obj);

                Undo.DestroyObjectImmediate(obj);

                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        #endregion

        #region Renaming

        public static void RenameAsset(UnityEngine.Object obj, string newName)
        {
            if (AssetDatabase.IsMainAsset(obj))
            {
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(obj), newName);
                AssetDatabase.SaveAssetIfDirty(obj);
            }
            else
            {
                obj.name = newName;
                AssetDatabase.SaveAssets();
            }
        }

        #endregion

        #endregion

        #region Sort Functions

        public static int Sort_ByName(UnityEngine.Object obj1, UnityEngine.Object obj2)
        {
            return obj1.name.CompareTo(obj2.name);
        }
        public static int Sort_ByType(UnityEngine.Object obj1, UnityEngine.Object obj2)
        {
            return obj1.GetType().FullName.CompareTo(obj2.GetType().FullName);
        }

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

        protected const string RenamingControlName = "RenameControl";

        #endregion

        #region Properties

        // Database Informations
        protected bool DatabaseInformationsFoldoutOpen { get; set; }

        // Database Content List Window
        protected int DatabaseContentListSelectionIndex { get; set; } = -1;
        protected Vector2 DatabaseContentListWindowScrollPos { get; set; }
        protected float DatabaseContentListElementHeight { get; set; } = 20f;
        protected float DatabaseContentListElementPingButtonWidth { get; set; } = 30f;
        protected float DatabaseContentListElementDeleteButtonWidth { get; set; } = 30f;
        protected float DatabaseContentListButtonsHeight { get; set; } = 20f;

        // Renaming
        protected bool IsRenamingElement { get; set; }
        protected string RenamingString { get; set; }
        protected UnityEngine.Object RenamingElement { get; set; }
        private bool m_justStartedRename;

        // Database Element Display
        private Dictionary<UnityEngine.Object, Editor> m_editors = new();
        protected Vector2 DatabaseElementDisplayScrollPos { get; set; }

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_database = (BaseDatabase)target;
            m_database.Editor_DatabaseContentChanged += OnDatabaseContentChanged;
            
            p_script = serializedObject.FindProperty("m_Script");
            
            m_excludedProperties = new()
            {
                p_script.propertyPath,
            };
        }
        protected virtual void OnDisable()
        {
            ClearEditors();

            m_database.Editor_DatabaseContentChanged -= OnDatabaseContentChanged;
        }

        #endregion

        #region Event Callbacks

        protected virtual void OnDatabaseContentChanged() { }

        #endregion

        #region Base GUI

        public sealed override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnGUI();

            OnEventReceived(Event.current);

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

        #region Events

        protected virtual void OnEventReceived(Event e)
        {
            if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.Delete:
                        OnEventReceived_Delete(e); break;
                    case KeyCode.F2:
                        OnEventReceived_Rename(e); break;
                    case KeyCode.UpArrow:
                        OnEventReceived_Up(e); break;
                    case KeyCode.DownArrow:
                        OnEventReceived_Down(e); break;
                }
            }
            if (e.type == EventType.MouseDown)
            {
                CompleteRenaming();
                e.Use();
            }
        }

        protected virtual void OnEventReceived_Delete(Event e)
        {
            if (TryDeleteCurrentSelection())
            {
                e.Use();
            }
        }

        protected virtual void OnEventReceived_Rename(Event e)
        {
            if (!IsRenamingElement)
            {
                if (DatabaseContentListSelectionIndex >= 0)
                {
                    BeginRenaming(GetDatabaseCurrentSelection());
                    e.Use();
                }
            }
            else if (CompleteRenaming())
            {
                e.Use();
            }
        }
        

        protected virtual void OnEventReceived_Up(Event e)
        {
            if (CanSelectUp())
            {
                DatabaseContentListSelectionIndex--;
                e.Use();
            }
        }
        protected virtual void OnEventReceived_Down(Event e)
        {
            if (CanSelectDown())
            {
                DatabaseContentListSelectionIndex++;
                e.Use();
            }
        }

        #endregion

        #region Helper GUI

        #region Database Informations

        protected virtual void OnDatabaseInformationsGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DatabaseInformationsFoldoutOpen = EditorGUILayout.Foldout(DatabaseInformationsFoldoutOpen, "Database Informations", true, EditorGUIHelper.foldoutStyle);
            if (DatabaseInformationsFoldoutOpen)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5f);
                OnDatabaseInformationsContentGUI();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
        protected virtual void OnDatabaseInformationsContentGUI() { }

        #endregion

        #region Database Content

        protected virtual int DatabaseContentListCount => throw new NotImplementedException();

        protected bool CanSelectUp() => DatabaseContentListSelectionIndex > 0;
        protected bool CanSelectDown() => DatabaseContentListSelectionIndex < DatabaseContentListCount - 1;

        protected UnityEngine.Object GetDatabaseCurrentSelection()
        {
            return GetDatabaseContentElementAtIndex(DatabaseContentListSelectionIndex);
        }
        protected virtual UnityEngine.Object GetDatabaseContentElementAtIndex(int index)
        {
            throw new NotImplementedException();
        }

        protected void ForceDatabaseContentRefresh() { m_database.Editor_ShouldRecomputeDatabaseContent(); }

        #endregion

        #region Database Content List

        protected virtual bool IsDatabaseContentListInteractable()
        {
            return !IsRenamingElement;
        }

        protected virtual void OnDatabaseContentListWindowGUI(Rect rect, bool refreshButton = false, bool addButton = false, bool deleteButtons = false)
        {
            bool hasAtLeastOneButton = (refreshButton || addButton);
            Rect listRect = new Rect(rect.x, rect.y, rect.width, rect.height - (hasAtLeastOneButton ? DatabaseContentListButtonsHeight : 0f));
            EditorGUI.DrawRect(listRect, EditorGUIHelper.transparentBlack01);

            bool needScrollRect = DatabaseContentListCount * DatabaseContentListElementHeight > listRect.height;
            if (needScrollRect)
            {
                Rect viewRect = new Rect(0, 0, listRect.width - 15f, DatabaseContentListCount * DatabaseContentListElementHeight);
                DatabaseContentListWindowScrollPos = GUI.BeginScrollView(listRect, DatabaseContentListWindowScrollPos, viewRect);

                Rect dataRect = new Rect(0, 0, viewRect.width, DatabaseContentListElementHeight);
                for (int i = 0; i < DatabaseContentListCount; i++)
                {
                    OnDatabaseContentListElementGUI(dataRect, i, GetDatabaseContentElementAtIndex(i), deleteButtons);
                    dataRect.y += DatabaseContentListElementHeight;
                }

                GUI.EndScrollView();
            }
            else
            {
                Rect dataRect = new Rect(listRect.x, listRect.y, listRect.width, DatabaseContentListElementHeight);
                for (int i = 0; i < DatabaseContentListCount; i++)
                {
                    OnDatabaseContentListElementGUI(dataRect, i, GetDatabaseContentElementAtIndex(i), deleteButtons);
                    dataRect.y += DatabaseContentListElementHeight;
                }
            }

            if (hasAtLeastOneButton)
            {
                EditorGUI.BeginDisabledGroup(!IsDatabaseContentListInteractable());
                if (refreshButton)
                {
                    // Refresh Button
                    Rect refreshButtonRect = new Rect(rect.x, rect.y + rect.height - DatabaseContentListButtonsHeight, rect.width * (addButton ? 0.5f : 1f), DatabaseContentListButtonsHeight);
                    if (GUI.Button(refreshButtonRect, new GUIContent(EditorGUIHelper.RefreshIcon) { text = "Refresh" }))
                    {
                        ForceDatabaseContentRefresh();
                    }
                }
                if (addButton)
                {
                    // Add Button
                    Rect addButtonRect = new Rect(rect.x + (refreshButton ? rect.width * 0.5f : 0f), rect.y + rect.height - DatabaseContentListButtonsHeight, rect.width * (refreshButton ? 0.5f : 1f), DatabaseContentListButtonsHeight);
                    if (GUI.Button(addButtonRect, new GUIContent(EditorGUIHelper.AddIcon) { text = "Add" }))
                    {
                        CreateNewData();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
        }
        protected virtual void OnDatabaseContentListElementGUI(Rect rect, int index, UnityEngine.Object element, bool deleteButton)
        {
            bool selected = DatabaseContentListSelectionIndex == index;
            deleteButton = deleteButton && BaseDatabase.IsAssetDeletableFromCode(element); 

            Rect elementRect = new Rect(rect.x + DatabaseContentListElementPingButtonWidth, rect.y, rect.width - DatabaseContentListElementPingButtonWidth - (deleteButton ? DatabaseContentListElementDeleteButtonWidth : 0f), rect.height);
            if (IsDatabaseContentListInteractable() && GUI.Button(elementRect, GUIContent.none, new GUIStyle()))
            {
                if (selected)
                {
                    DatabaseContentListSelectionIndex = -1;
                    OnDeselectDatabaseContentListElement(element);
                    selected = false;
                }
                else
                {
                    DatabaseContentListSelectionIndex = index;
                    OnSelectDatabaseContentListElement(element);
                    selected = true;
                }
            }
            
            OnDatabaseContentListElementBackgroundGUI(rect, index, selected, element);

            EditorGUI.BeginDisabledGroup(!IsDatabaseContentListInteractable());
            Rect pingButtonRect = new Rect(rect.x, rect.y, DatabaseContentListElementPingButtonWidth, rect.height);
            if (GUI.Button(pingButtonRect, EditorGUIHelper.CanSeeIcon))
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(element);
            }
            EditorGUI.EndDisabledGroup();

            if (element == null)
            {
                OnDatabaseContentListNullElementGUI(elementRect, index, selected);
                return;
            }
            switch (element)
            {
                case GameObject go:
                    OnDatabaseContentListGameObjectElementGUI(elementRect, index, selected, go); break;
                case ScriptableObject so:
                    OnDatabaseContentListScriptableObjectElementGUI(elementRect, index, selected, so); break;
                default:
                    OnDatabaseContentListOtherObjectElementGUI(elementRect, index, selected, element); break;
            }

            if (deleteButton)
            {
                EditorGUI.BeginDisabledGroup(!IsDatabaseContentListInteractable());
                Rect deleteButtonRect = new Rect(rect.x + rect.width - DatabaseContentListElementDeleteButtonWidth, rect.y, DatabaseContentListElementDeleteButtonWidth, rect.height);
                OnDatabaseContentListElementDeleteButtonGUI(deleteButtonRect, index, selected, element);
                EditorGUI.EndDisabledGroup();
            }
        }
        protected virtual void OnDatabaseContentListElementBackgroundGUI(Rect rect, int index, bool selected, UnityEngine.Object element)
        {
            Rect backgroundRect = new Rect(rect.x + 2f, rect.y, rect.width - 4f, rect.height);
            EditorGUI.DrawRect(backgroundRect, selected ? EditorGUIHelper.transparentWhite01 : (index % 2 == 0 ? EditorGUIHelper.transparentBlack02 : EditorGUIHelper.transparentBlack04));
        }

        protected virtual void OnDatabaseContentListNullElementGUI(Rect rect, int index, bool selected)
        {
            ForceDatabaseContentRefresh();
        }
        protected virtual void OnDatabaseContentListGameObjectElementGUI(Rect rect, int index, bool selected, GameObject element)
        {
            string elementName = element.name;
            Texture2D elementTexture = AssetPreview.GetAssetPreview(element);
            if (elementTexture == null) elementTexture = AssetPreview.GetMiniThumbnail(element);

            if (element.TryGetComponent(out IDatabaseElement elem))
            {
                if (elem.HasDatabaseElementName(out var newName)) elementName = newName;
                if (elem.HasDatabaseElementTexture(out var newTexture)) elementTexture = newTexture;
            }

            OnDatabaseContentListElementWithNameAndTextureGUI(rect, index, selected, element, elementName, elementTexture);
        }
        protected virtual void OnDatabaseContentListScriptableObjectElementGUI(Rect rect, int index, bool selected, ScriptableObject element)
        {
            string elementName = element.name;
            Texture2D elementTexture = AssetPreview.GetAssetPreview(element);
            if (elementTexture == null) elementTexture = AssetPreview.GetMiniThumbnail(element);

            if (element is IDatabaseElement elem)
            {
                if (elem.HasDatabaseElementName(out var newName)) elementName = newName;
                if (elem.HasDatabaseElementTexture(out var newTexture)) elementTexture = newTexture;
            }

            OnDatabaseContentListElementWithNameAndTextureGUI(rect, index, selected, element, elementName, elementTexture);
        }
        protected virtual void OnDatabaseContentListOtherObjectElementGUI(Rect rect, int index, bool selected, UnityEngine.Object obj)
        {
            OnDatabaseContentListElementWithNameAndTextureGUI(rect, index, selected, obj, obj.name, AssetPreview.GetMiniThumbnail(obj));
        }

        protected virtual void OnDatabaseContentListElementWithNameAndTextureGUI(Rect rect, int index, bool selected, UnityEngine.Object obj, string name, Texture2D texture)
        {
            bool hasTexture = texture != null;
            if (hasTexture)
            {
                Rect textureRect = new Rect(rect.x + 5f, rect.y + 1f, rect.height - 2f, rect.height - 2f);
                GUI.DrawTexture(textureRect, texture, ScaleMode.ScaleToFit);
            }

            Rect labelRect = new Rect(rect.x + 5f + (hasTexture ? 5f + rect.height : 0f), rect.y, rect.width - 5f - (hasTexture ? 5f + rect.height : 0f), rect.height);
            if (IsRenamingElement && RenamingElement == obj)
            {
                GUI.SetNextControlName(RenamingControlName);

                EditorGUI.BeginChangeCheck();
                RenamingString = EditorGUI.DelayedTextField(labelRect, RenamingString);
                if (EditorGUI.EndChangeCheck()
                    || (!m_justStartedRename && !EditorGUIUtility.editingTextField)) // Stop editing
                {
                    CompleteRenaming();
                }
                // Get focus
                if (m_justStartedRename)
                {
                    EditorGUI.FocusTextInControl(RenamingControlName);
                    m_justStartedRename = false;
                }
            }
            else
            {
                EditorGUI.LabelField(labelRect, name);
            }
        }

        protected virtual void OnDatabaseContentListElementDeleteButtonGUI(Rect rect, int index, bool selected, UnityEngine.Object element)
        {
            if (element != null)
            {
                Color guiBackgroundColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUI.Button(rect, EditorGUIHelper.DeleteIcon))
                {
                    OnTryDeleteDatabaseElementAtIndex(index);
                }
                GUI.backgroundColor = guiBackgroundColor;
            }
        }


        protected virtual void OnSelectDatabaseContentListElement(UnityEngine.Object element) { }
        protected virtual void OnDeselectDatabaseContentListElement(UnityEngine.Object element) { }

        #endregion

        #region Editor Handling

        protected void ClearEditors()
        {
            if (m_editors != null)
            {
                foreach (var editor in m_editors.Values)
                {
                    if (editor != null)
                        DestroyImmediate(editor);
                }
                m_editors.Clear();
            }
        }
        protected Editor GetOrCreateEditorFor(UnityEngine.Object element)
        {
            if (m_editors.TryGetValue(element, out Editor editor)
                && editor != null)
            {
                return editor;
            }

            editor = CreateEditorFor(element);
            if (editor != null)
            {
                m_editors[element] = editor;
            }
            return editor;
        }
        protected virtual Editor CreateEditorFor(UnityEngine.Object element)
        {
            if (element is GameObject go)
            {
                if (BaseDatabase.HasDataType(m_database.GetType(), out Type dataType)
                    && dataType.IsSubclassOf(typeof(Component))
                    && go.TryGetComponent(dataType, out Component component))
                {
                    return CreateEditor(component);
                }
                return CreateEditor(go.transform);
            }
            return CreateEditor(element);
        }

        protected bool ShowElementEditorIfPossible(UnityEngine.Object element)
        {
            if (element == null) return false;

            var editor = GetOrCreateEditorFor(element);
            if (editor != null)
            {
                editor.OnInspectorGUI();
                return true;
            }
            return false;
        }

        #endregion

        #region Database Element Display

        protected bool DisplayCurrentDatabaseContentListSelection()
        {
            if (DatabaseContentListSelectionIndex != -1)
            {
                DisplayDatabaseElement(GetDatabaseCurrentSelection());
                return true;
            }
            return false;
        }
        protected virtual void DisplayDatabaseElement(UnityEngine.Object element)
        {
            DatabaseElementDisplayScrollPos = EditorGUILayout.BeginScrollView(DatabaseElementDisplayScrollPos);

            switch (element)
            {
                case null:
                    OnDisplayDatabaseNullElement(); break;
                case GameObject go:
                    OnDisplayDatabaseGameObjectElement(go); break;
                case ScriptableObject so:
                    OnDisplayDatabaseScriptableObjectElement(so); break;
                default:
                    OnDisplayDatabaseOtherObjectElement(element); break;
            }

            EditorGUILayout.EndScrollView();
        }

        protected virtual void OnDisplayDatabaseNullElement()
        {

        }
        protected virtual void OnDisplayDatabaseGameObjectElement(GameObject element)
        {
            ShowElementEditorIfPossible(element);
        }
        protected virtual void OnDisplayDatabaseScriptableObjectElement(ScriptableObject element)
        {
            ShowElementEditorIfPossible(element);
        }
        protected virtual void OnDisplayDatabaseOtherObjectElement(UnityEngine.Object element)
        {
            ShowElementEditorIfPossible(element);
        }

        #endregion

        #region Data Creation

        protected bool CreateNewData()
        {
            if (OnCreateNewData(out var obj))
            {
                OnAddNewDataToDatabase(obj);
                m_database.Editor_OnNewElementCreated(obj);
                return true;
            }
            return false;
        }
        protected virtual bool OnCreateNewData(out UnityEngine.Object obj) { throw new NotImplementedException(); }
        protected virtual bool CreateNewData(string path, out UnityEngine.Object obj)
        {
            if (BaseDatabase.HasDataType(m_database.GetType(), out Type dataType))
            {
                path = AssetDatabase.GenerateUniqueAssetPath(path);

                if (dataType.IsSubclassOf(typeof(ScriptableObject)))
                {
                    obj = OnCreateNewScriptableObject(path, dataType);
                    return obj != null;
                }
                else if (dataType.IsSubclassOf(typeof(Component)))
                {
                    obj = OnCreateNewPrefabWithComponent(path, dataType);
                    return obj != null;
                }
                else if (dataType == typeof(GameObject))
                {
                    obj = OnCreateNewEmptyPrefab(path);
                    return obj != null;
                }
            }
            obj = null;
            return false;
        }

        protected virtual GameObject OnCreateNewEmptyPrefab(string path)
        {
            return BaseDatabase.CreateEmptyPrefab(path);
        }
        protected virtual Component OnCreateNewPrefabWithComponent(string path, Type componentType)
        {
            return BaseDatabase.CreatePrefabWithComponent(componentType, path);
        }
        protected virtual ScriptableObject OnCreateNewScriptableObject(string path, Type scriptableType)
        {
            return BaseDatabase.CreateScriptableAsset(scriptableType, path);
        }

        protected virtual void OnAddNewDataToDatabase(UnityEngine.Object obj)
        {
            m_database.Editor_ShouldRecomputeDatabaseContent();
        }

        #endregion

        #region Data Deletion

        protected bool TryDeleteCurrentSelection()
        {
            if (DatabaseContentListSelectionIndex != -1)
            {
                OnTryDeleteDatabaseElementAtIndex(DatabaseContentListSelectionIndex);
                return true;
            }
            return false;
        }
        protected virtual void OnTryDeleteDatabaseElementAtIndex(int index)
        {
            m_database.Editor_DeleteElementAtIndex(index);
        }

        #endregion

        #region Data Renaming

        private void BeginRenaming(UnityEngine.Object obj)
        {
            RenamingElement = obj;
            RenamingString = obj.name;
            IsRenamingElement = true;
            m_justStartedRename = true;
        }
        private bool CompleteRenaming()
        {
            if (IsRenamingElement)
            {
                OnCompleteRenaming(RenamingElement);
                IsRenamingElement = false;
                RenamingElement = null;
                return true;
            }
            return false;
        }
        protected virtual void OnCompleteRenaming(UnityEngine.Object obj)
        {
            BaseDatabase.RenameAsset(obj, RenamingString);
        }

        #endregion

        #region Decoratives

        protected void Separator(float height, Color color)
        {
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, height), color);
        }

        #endregion

        #endregion
    }

#endif

    #endregion
}
