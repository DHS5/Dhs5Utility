using UnityEngine;
using System.Collections.Generic;
using Dhs5.Utility.Editors;
using static UnityEditor.LightingExplorerTableColumn;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.NewDatabase
{
    public class Container : ScriptableObject
    {
        #region STRUCT MappedObject

        protected readonly struct MappedObject
        {
            public MappedObject(UnityEngine.Object obj, IContainerElement elem)
            {
                this.obj = obj;
                this.elem = elem;
            }

            public readonly UnityEngine.Object obj;
            public readonly IContainerElement elem;
        }

        #endregion


        #region Members

        [SerializeField] protected List<UnityEngine.Object> m_objects = new();

        protected Dictionary<int, MappedObject> m_mappedObjects;

        #endregion

        #region Editor Members

#if UNITY_EDITOR

        [SerializeField] protected string m_lastCreateFolderPath;

#endif

        #endregion


        #region Mapped Objects

        protected virtual void InitMappedObjects()
        {
            m_mappedObjects = new();

            foreach (var obj in m_objects)
            {
                if (obj != null && obj is IContainerElement elem)
                {
                    if (!m_mappedObjects.TryAdd(elem.UID, new MappedObject(obj, elem)))
                    {
                        Debug.LogError("Could not add IContainerElement : " + obj + " with UID = " + elem.UID + " to mapped objects.", obj);
                    }
                }
                else
                {
                    Debug.LogWarning("Object is null or not a IContainerElement : " + obj, obj);
                }
            }
        }
        protected virtual bool TryGetObjectFromMappedObjects(int uid, out UnityEngine.Object obj)
        {
            if (m_mappedObjects == null)
            {
                InitMappedObjects();
            }

            if (m_mappedObjects.TryGetValue(uid, out var mappedObj))
            {
                obj = mappedObj.obj;
                return true;
            }

            obj = null;
            return false;
        }
        protected virtual bool TryGetElementFromMappedObjects(int uid, out IContainerElement element)
        {
            if (m_mappedObjects == null)
            {
                InitMappedObjects();
            }

            if (m_mappedObjects.TryGetValue(uid, out var mappedObj))
            {
                element = mappedObj.elem;
                return true;
            }

            element = null;
            return false;
        }
        protected virtual IEnumerable<(UnityEngine.Object, IContainerElement)> GetMappedObjects()
        {
            if (m_mappedObjects == null)
            {
                InitMappedObjects();
            }

            foreach (var (_, mappedObj) in m_mappedObjects)
            {
                yield return (mappedObj.obj, mappedObj.elem);
            }
        }

        #endregion

        #region Public Accessors

        // GETTERS
        public virtual UnityEngine.Object GetByUID(int uid)
        {
            if (TryGetByUID(uid, out var obj))
            {
                return obj;
            }
            return null;
        }
        public virtual bool TryGetByUID(int uid, out UnityEngine.Object obj)
        {
            return TryGetObjectFromMappedObjects(uid, out obj);
        }
        
        public virtual T GetByUID<T>(int uid) where T : UnityEngine.Object, IContainerElement
        {
            if (TryGetByUID<T>(uid, out var obj))
            {
                return obj;
            }
            return null;
        }
        public virtual bool TryGetByUID<T>(int uid, out T result) where T : UnityEngine.Object, IContainerElement
        {
            if (TryGetObjectFromMappedObjects(uid, out var obj) && obj is T t)
            {
                result = t;
                return true;
            }
            result = null;
            return false;
        }

        // ITERATORS
        public virtual IEnumerable<UnityEngine.Object> GetObjects()
        {
            return m_objects;
        }

        public virtual IEnumerable<T> GetObjectsOfType<T>() where T : UnityEngine.Object, IContainerElement
        {
            foreach (var obj in m_objects)
            {
                if (obj != null && obj is T t)
                {
                    yield return t;
                }
            }
        }

        // ALIAS
        public virtual bool TryFindObjectByAlias(string alias, out UnityEngine.Object result)
        {
            foreach (var (obj, elem) in GetMappedObjects())
            {
                if (!string.IsNullOrEmpty(elem.Alias)
                    && elem.Alias.Equals(alias, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    result = obj;
                    return true;
                }
            }

            result = null;
            return false;
        }
        public virtual bool TryFindObjectByAlias<T>(string alias, out T result) where T : UnityEngine.Object, IContainerElement
        {
            foreach (var (obj, elem) in GetMappedObjects())
            {
                if (!string.IsNullOrEmpty(elem.Alias)
                    && elem.Alias.Equals(alias, System.StringComparison.InvariantCultureIgnoreCase)
                    && obj is T t)
                {
                    result = t;
                    return true;
                }
            }

            result = null;
            return false;
        }

        #endregion


        // --- EDITOR ---

#if UNITY_EDITOR

        #region List Callbacks

        public virtual void Editor_OnNewObjectInContainer(UnityEngine.Object newObj, IContainerElement newElem)
        {
            Editor_EnsureNewObjectValidUID(newObj, newElem);
        }

        #endregion

        #region Utility

        protected virtual void Editor_EnsureNewObjectValidUID(UnityEngine.Object newObj, IContainerElement newElem)
        {
            if (newElem.UID == 0)
            {
                newElem.Editor_SetUID(IContainerElement.ComputeContainerElementUID(newObj));
            }

            var conflict = false;
            var offset = 0;
            do
            {
                conflict = false;

                foreach (var obj in m_objects)
                {
                    if (obj != null && obj is IContainerElement elem)
                    {
                        if (elem.UID == newElem.UID)
                        {
                            conflict = true;
                            offset++;
                            newElem.Editor_SetUID(IContainerElement.ComputeContainerElementUID(newObj, offset));
                            break;
                        }
                    }
                }
            } while (conflict);
        }

        #endregion

#endif
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(Container), editorForChildClasses: true)]
    public class ContainerEditor : Editor
    {
        #region STRUCT ListEntry

        protected struct ListEntry
        {
            public ListEntry(int index, SerializedProperty property)
            {
                this.index = index;
                this.property = property;
                if (property.objectReferenceValue is IContainerElement elem)
                {
                    this.element = elem;
                }
                else
                {
                    this.element = null;
                }
            }

            public int index;
            public SerializedProperty property;
            public IContainerElement element;
        }

        #endregion


        #region Members

        protected List<ListEntry> m_listEntries;
        protected Dictionary<UnityEngine.Object, Editor> m_editors;

        protected int m_focusedIndex;
        protected UnityEngine.Object m_focusedObject;

        protected Rect m_databaseWindowRect;
        protected string m_searchString;
        protected Vector2 m_listScrollPosition;
        protected GUIStyle m_listScrollViewStyle;
        protected float m_listScrollX;
        protected bool m_isResizing;
        protected Vector2 m_inspectorScrollPosition;

        protected GUIStyle ListScrollViewStyle
        {
            get
            {
                if (m_listScrollViewStyle == null)
                {
                    m_listScrollViewStyle = new GUIStyle(GUI.skin.scrollView);
                    m_listScrollViewStyle.fixedWidth = 0;
                    m_listScrollViewStyle.stretchWidth = true;
                }
                return m_listScrollViewStyle;
            }
        }

        protected Color m_focusedListEntryBackgroundColor = Color.gray2;

        #endregion

        #region Serialized Properties

        protected SerializedProperty p_objects;

        #endregion

        #region STATIC Members

        protected static float _listAreaHeight = 300f;

        #endregion


        #region Core Behaviour

        private void OnEnable()
        {
            p_objects = serializedObject.FindProperty("m_objects");

            RefreshListEntries();
        }

        #endregion


        #region List Entries

        protected void ClearListEntries()
        {
            if (m_listEntries == null) m_listEntries = new();
            else m_listEntries.Clear();
        }
        protected virtual void RefreshListEntries()
        {
            ClearListEntries();

            var isSearching = !string.IsNullOrWhiteSpace(m_searchString);

            int index = 0;
            for (int i = 0; i < p_objects.arraySize; i++)
            {
                var p_entry = p_objects.GetArrayElementAtIndex(i);
                if (p_entry.objectReferenceValue != null)
                {
                    if (!isSearching
                        || p_entry.objectReferenceValue.name.Contains(m_searchString, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        m_listEntries.Add(new ListEntry(index, p_entry));
                        index++;
                    }
                }
            }

            m_focusedIndex = -1;
            m_focusedObject = null;
        }
        protected virtual IEnumerable<ListEntry> GetListEntries()
        {
            return m_listEntries;
        }

        #endregion

        #region Objects Editors

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
        protected Editor GetOrCreateEditor(UnityEngine.Object obj)
        {
            if (obj == null) return null;

            if (m_editors == null) m_editors = new();

            if (m_editors.TryGetValue(obj, out Editor editor)
                && editor != null)
            {
                return editor;
            }

            editor = CreateObjectEditor(obj);
            if (editor != null)
            {
                m_editors[obj] = editor;
            }
            return editor;
        }
        protected virtual Editor CreateObjectEditor(UnityEngine.Object obj)
        {
            if (obj is GameObject go)
            {
                //if (ContainerHasValidDataType
                //    && DataType.IsSubclassOf(typeof(Component))
                //    && go.TryGetComponent(DataType, out Component component))
                //{
                //    return Editor.CreateEditor(component);
                //}
                return Editor.CreateEditor(go.transform);
            }
            return Editor.CreateEditor(obj);
        }

        protected bool ShowObjectEditorIfPossible(UnityEngine.Object obj)
        {
            if (obj == null) return false;

            var editor = GetOrCreateEditor(obj);
            if (editor != null)
            {
                editor.OnInspectorGUI();
                return true;
            }
            return false;
        }

        #endregion


        // --- GUI ---

        #region Temp

        public override void OnInspectorGUI()
        {
            OnDatabaseCoreGUI(new Rect(0f, 0f, Screen.width, Screen.height));
        }

        #endregion

        #region Database Core GUI

        public void OnDatabaseCoreGUI(Rect rect)
        {
            m_databaseWindowRect = rect;

            serializedObject.Update();

            OnDatabaseGUI();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnDatabaseGUI()
        {
            OnToolbarGUI();

            OnListGUI();

            OnResizeGUI();

            OnDatabaseInpsectorGUI();
        }

        #endregion


        #region Toolbar GUI

        protected GUIContent g_content = new GUIContent("Content");

        protected virtual void OnToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(22f), GUILayout.ExpandWidth(true));

            DrawToolbarContentLabel(100f);
            DrawToolbarSearchField(-1f);
            DrawToolbarRefreshButton(40f);
            DrawToolbarAddButton(40f);

            EditorGUILayout.EndHorizontal();
        }

        protected virtual void DrawToolbarContentLabel(float width) => EditorGUILayout.LabelField(g_content, GUILayout.Width(width));
        protected virtual void DrawToolbarSearchField(float width)
        {
            EditorGUI.BeginChangeCheck();
            if (width < 0f) m_searchString = EditorGUILayout.TextField(m_searchString, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            else m_searchString = EditorGUILayout.TextField(m_searchString, EditorStyles.toolbarSearchField, GUILayout.Width(width));
            if (EditorGUI.EndChangeCheck())
            {
                OnSearchStringChanged(m_searchString);
            }

            if (GUILayout.Button(EditorGUIHelper.CrossIcon, EditorStyles.iconButton))
            {
                GUI.FocusControl(null);
                m_searchString = string.Empty;
                OnSearchStringChanged(m_searchString);
            }
        }
        protected virtual void DrawToolbarRefreshButton(float width)
        {
            if (GUILayout.Button(EditorGUIHelper.RefreshIcon, EditorStyles.toolbarButton, GUILayout.Width(width)))
            {
                OnToolbarRefreshButton();
            }
        }
        protected virtual void DrawToolbarAddButton(float width)
        {
            if (GUILayout.Button(EditorGUIHelper.AddMoreIcon, EditorStyles.toolbarButton, GUILayout.Width(width)))
            {
                OnToolbarAddButton();
            }
        }

        #endregion

        #region Toolbar Callbacks

        protected virtual void OnSearchStringChanged(string searchString)
        {
            RefreshListEntries();
        }
        
        protected virtual void OnToolbarRefreshButton()
        {
            RefreshListEntries();
        }
        protected virtual void OnToolbarAddButton()
        {

        }

        #endregion


        #region List GUI

        protected virtual void OnListGUI()
        {
            var rect = EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.Height(_listAreaHeight));

            EditorGUI.DrawRect(rect, Color.gray1);

            m_listScrollPosition = GUILayout.BeginScrollView(m_listScrollPosition, ListScrollViewStyle, GUILayout.ExpandWidth(true));

            var listEntries = GetListEntries();
            if (listEntries != null)
            {
                foreach (var entry in listEntries)
                {
                    var entryRect = EditorGUILayout.GetControlRect(false, GetListEntryHeight());
                    DrawListEntry(entryRect, entry);
                }
            }

            GUILayout.EndScrollView();

            m_listScrollX = GUILayout.HorizontalScrollbar(m_listScrollX, 1f, 0f, 2f);

            EditorGUILayout.EndVertical();
        }
        protected virtual float GetListEntryHeight() => 20f;

        protected virtual void DrawListEntry(Rect rect, ListEntry entry)
        {
            // Background
            if (entry.index == m_focusedIndex)
            {
                EditorGUI.DrawRect(rect, m_focusedListEntryBackgroundColor);
            }

            // Context Button
            var contextButtonRect = new Rect(rect.x + rect.width - 25f, rect.y, 25f, rect.height);
            DrawListEntryContextButton(contextButtonRect, entry);

            var movingRect = rect;

            if (CanDrawListEntryUID(entry))
            {
                movingRect.width = 40f;
                DrawListEntryUID(movingRect, entry);
            }
            movingRect.x += 40f;

            if (CanDrawListEntryName(entry))
            {
                movingRect.width = 50f;
                DrawListEntryName(movingRect, entry);
            }
            movingRect.x += 50f;

            // Selection
            var selectionRect = new Rect(rect.x, rect.y, contextButtonRect.x - rect.x, rect.height);
            DoListEntrySelection(selectionRect, entry);

            DrawListEntryBottomSeparator(rect);
        }

        protected virtual void DrawListEntryContextButton(Rect rect, ListEntry entry)
        {
            if (GUI.Button(rect, EditorGUIHelper.MenuIcon, EditorStyles.iconButton))
            {
                OnListEntryContextButton(entry);
            }
        }

        protected virtual bool CanDrawListEntryName(ListEntry entry) => entry.property != null && entry.property.objectReferenceValue != null;
        protected virtual void DrawListEntryName(Rect rect, ListEntry entry) => EditorGUI.LabelField(rect, entry.property.objectReferenceValue.name);
        protected virtual bool CanDrawListEntryUID(ListEntry entry) => entry.element != null;
        protected virtual void DrawListEntryUID(Rect rect, ListEntry entry) => EditorGUI.LabelField(rect, entry.element.UID.ToString(), EditorStyles.miniLabel);
        protected virtual void DrawListEntryBottomSeparator(Rect rect) => EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), Color.gray5);

        protected virtual void DoListEntrySelection(Rect rect, ListEntry entry)
        {
            if (Event.current.type == EventType.MouseDown
                && rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                OnSelectListEntry(entry);
            }
        }

        #endregion

        #region List Callbacks

        protected virtual void OnListEntryContextButton(ListEntry entry)
        {

        }

        protected virtual void OnSelectListEntry(ListEntry entry)
        {
            m_focusedIndex = entry.index;
            if (entry.property != null && entry.property.objectReferenceValue != null)
            {
                m_focusedObject = entry.property.objectReferenceValue;
            }
        }

        #endregion


        #region Resize GUI

        protected virtual void OnResizeGUI()
        {
            var rect = EditorGUILayout.GetControlRect(false, 5f);

            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 2f, rect.width, 1f), Color.white);

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);

            if (!m_isResizing
                && rect.Contains(Event.current.mousePosition)
                && Event.current.type == EventType.MouseDown)
            {
                m_isResizing = true;
                Event.current.Use();
            }
            else if (m_isResizing
                && Event.current.type == EventType.MouseDrag)
            {
                _listAreaHeight = Mathf.Clamp(_listAreaHeight + Event.current.delta.y, 100f, m_databaseWindowRect.height - 200f);
                Event.current.Use();
            }
        }

        #endregion


        #region Inspector GUI

        protected virtual void OnDatabaseInpsectorGUI()
        {
            if (m_focusedObject != null)
            {
                m_inspectorScrollPosition = EditorGUILayout.BeginScrollView(m_inspectorScrollPosition);

                ShowObjectEditorIfPossible(m_focusedObject);

                EditorGUILayout.EndScrollView();
            }
        }

        #endregion
    }

#endif

    #endregion
}
