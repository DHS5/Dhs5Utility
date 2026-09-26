using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
using Dhs5.Utility.Editors;
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
                    if (obj != null && obj != newObj && obj is IContainerElement elem)
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
            public ListEntry(int index, int propertyIndex, SerializedProperty property)
            {
                this.displayIndex = index;
                this.propertyIndex = propertyIndex;
                this.property = property;
                if (property.objectReferenceValue is IContainerElement elem)
                {
                    this.element = elem;
                }
                else
                {
                    this.element = null;
                }
                this.g_name = new GUIContent(property.objectReferenceValue.name, property.objectReferenceValue.name);
            }

            public int displayIndex;
            public int propertyIndex;
            public SerializedProperty property;
            public IContainerElement element;
            public GUIContent g_name;
        }

        #endregion

        #region STRUCT ListDisplayedProperty

        protected struct ListDisplayedProperty
        {
            #region Constructors

            public ListDisplayedProperty(string propertyName, float width, int priority)
            {
                this.propertyName = propertyName;
                this.displayName = ObjectNames.NicifyVariableName(propertyName);
                this.width = width;
                this.priority = priority;
            }
            
            public ListDisplayedProperty(string propertyName, ContainerDisplayAttribute attribute)
            {
                this.propertyName = propertyName;
                this.displayName = ObjectNames.NicifyVariableName(propertyName);
                this.width = attribute.width;
                this.priority = attribute.priority;
            }

            #endregion

            #region Members

            public string propertyName;
            public string displayName;
            public float width;
            public int priority;

            #endregion

            #region Methods

            public ListDisplayedProperty Combine(ListDisplayedProperty property)
            {
                return new ListDisplayedProperty(propertyName, Mathf.Max(width, property.width), Mathf.Min(priority, property.priority));
            }

            #endregion
        }

        #endregion


        #region Members

        protected List<ListEntry> m_listEntries;
        protected List<ListDisplayedProperty> m_listDisplayedProperties;
        protected Dictionary<UnityEngine.Object, Editor> m_editors;
        protected Dictionary<UnityEngine.Object, SerializedObject> m_serializedObjects;

        protected int m_focusedIndex;
        protected UnityEngine.Object m_focusedObject;

        protected Rect m_databaseWindowRect;
        protected string m_searchString;
        protected float m_listTotalWidth;
        protected int m_listSize;
        protected Vector2 m_listScrollPosition;
        protected float m_listScrollX;
        protected bool m_isResizing;
        protected Vector2 m_inspectorScrollPosition;
        protected double m_lastListSelectionTime;
        protected bool m_listHasFocus;

        protected GUIStyle m_listScrollViewStyle;
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

        protected GUIContent g_uid = new GUIContent("UID", "Unique Identifier");
        protected GUIContent g_name = new GUIContent("Name", "Actual object's name");

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
            RefreshListDisplayedProperties();
        }
        private void OnDisable()
        {
            ClearSerializedObjects();
            ClearEditors();
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

            m_listSize = p_objects.arraySize;
            int index = 0;
            for (int i = 0; i < m_listSize; i++)
            {
                var p_entry = p_objects.GetArrayElementAtIndex(i);
                if (p_entry.objectReferenceValue != null)
                {
                    if (!isSearching
                        || p_entry.objectReferenceValue.name.Contains(m_searchString, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        m_listEntries.Add(new ListEntry(index, i, p_entry));
                        index++;
                    }
                }
            }

            PruneSerializedObjects();

            OnDeselectListEntry();
        }
        protected virtual void RefreshListEntriesOnlyIfNecessary()
        {
            var actualSize = p_objects.arraySize;
            if (actualSize != m_listSize)
            {
                RefreshListEntries();
            }
        }
        protected virtual IEnumerable<ListEntry> GetListEntries()
        {
            return m_listEntries;
        }

        protected virtual bool TryGetFocusedListEntry(out ListEntry focusedEntry)
        {
            if (m_focusedIndex > -1
                && m_listEntries.IsIndexValid(m_focusedIndex, out focusedEntry))
            {
                return true;
            }
            focusedEntry = default;
            return false;
        }

        #endregion

        #region List Displayed Properties

        protected void ClearListDisplayedProperties()
        {
            if (m_listDisplayedProperties == null) m_listDisplayedProperties = new();
            else m_listDisplayedProperties.Clear();
        }
        protected virtual void RefreshListDisplayedProperties()
        {
            ClearListDisplayedProperties();

            List<Type> types = new();

            for (int i = 0; i < p_objects.arraySize; i++)
            {
                var p_entry = p_objects.GetArrayElementAtIndex(i);
                if (p_entry.objectReferenceValue != null)
                {
                    var type = p_entry.objectReferenceValue.GetType();
                    if (!types.Contains(type))
                    {
                        types.Add(type);
                    }
                }
            }

            if (types.Count > 0)
            {
                Dictionary<string, ListDisplayedProperty> mappedProperties = new();
                
                foreach (var type in types)
                {
                    var fields = GetAllInstanceFields(type);
                    if (fields != null)
                    {
                        foreach (var field in fields)
                        {
                            var attribute = field.GetCustomAttribute<ContainerDisplayAttribute>();
                            if (attribute != null)
                            {
                                if (!mappedProperties.TryAdd(field.Name, new ListDisplayedProperty(field.Name, attribute)))
                                {
                                    mappedProperties[field.Name] = mappedProperties[field.Name].Combine(new ListDisplayedProperty(field.Name, attribute));
                                }
                            }
                        }
                    }
                }

                m_listDisplayedProperties.AddRange(mappedProperties.Values);
            }

            InsertCustomListDisplayedProperties();
            SortListDisplayedProperties();
            m_listTotalWidth = ComputeListTotalWidth();
        }
        protected virtual IEnumerable<ListDisplayedProperty> GetListDisplayedProperties()
        {
            return m_listDisplayedProperties;
        }

        protected virtual void InsertCustomListDisplayedProperties() { }
        protected virtual void SortListDisplayedProperties()
        {
            m_listDisplayedProperties.Sort((p1, p2) => p1.priority.CompareTo(p2.priority));
        }

        protected virtual float ComputeListTotalWidth()
        {
            var total = GetListBasePropertyUIDWidth() + GetListBasePropertyNameWidth();

            foreach (var property in m_listDisplayedProperties)
            {
                total += property.width;
            }

            return total + 5f;
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

        #region Objects Serialized Objects

        protected void ClearSerializedObjects()
        {
            if (m_serializedObjects != null)
            {
                foreach (var so in m_serializedObjects.Values)
                {
                    so?.Dispose();
                }
                m_serializedObjects.Clear();
            }
        }
        protected SerializedObject GetOrCreateSerializedObject(UnityEngine.Object obj)
        {
            if (obj == null) return null;

            if (m_serializedObjects == null) m_serializedObjects = new();

            if (m_serializedObjects.TryGetValue(obj, out SerializedObject so))
            {
                if (so != null && so.targetObject != null)
                {
                    return so;
                }
                so?.Dispose();
            }

            so = new SerializedObject(obj);
            m_serializedObjects[obj] = so;
            return so;
        }
        /// <summary>
        /// Disposes the SerializedObjects of objects that are no longer in the container
        /// </summary>
        protected void PruneSerializedObjects()
        {
            if (m_serializedObjects == null) return;

            HashSet<UnityEngine.Object> alive = new();
            for (int i = 0; i < p_objects.arraySize; i++)
            {
                var obj = p_objects.GetArrayElementAtIndex(i).objectReferenceValue;
                if (obj != null) alive.Add(obj);
            }

            List<UnityEngine.Object> toRemove = new();
            foreach (var kvp in m_serializedObjects)
            {
                if (kvp.Key == null || !alive.Contains(kvp.Key))
                {
                    kvp.Value?.Dispose();
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var key in toRemove)
            {
                m_serializedObjects.Remove(key);
            }
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

            OnBeforeDatabaseGUI();

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

        #region Database Core Events & Checks

        protected virtual void OnBeforeDatabaseGUI()
        {
            if (m_listHasFocus && GUIUtility.keyboardControl != 0)
            {
                m_listHasFocus = false;
            }
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
            RefreshListDisplayedProperties();
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

            var actualWidth = rect.width - GetListEntryContextButtonWidth();
            var delta = m_listTotalWidth - actualWidth;
            var scrollOffset = delta > 0f ? -m_listScrollX * delta : 0f;

            // Headers
            var headerRect = EditorGUILayout.GetControlRect(false, GetListHeaderHeight());
            DrawListHeader(headerRect, scrollOffset);

            // Entries ScrollView
            m_listScrollPosition = GUILayout.BeginScrollView(m_listScrollPosition, ListScrollViewStyle, GUILayout.ExpandWidth(true));

            RefreshListEntriesOnlyIfNecessary();
            var listEntries = GetListEntries();
            if (listEntries != null)
            {
                foreach (var entry in listEntries)
                {
                    var entryRect = EditorGUILayout.GetControlRect(false, GetListEntryHeight());
                    DrawListEntry(entryRect, scrollOffset, entry);
                }
            }

            GUILayout.EndScrollView();

            m_listScrollX = delta > 0f ? GUILayout.HorizontalScrollbar(m_listScrollX, 1f, 0f, 2f) : 0f;

            EditorGUILayout.EndVertical();

            HandleListAreaEvents(rect);
        }
        protected virtual void HandleListAreaEvents(Rect rect)
        {
            if (Event.current.type == EventType.MouseDown
                && rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                OnDeselectListEntry();
            }

            if (m_listHasFocus)
            {
                if (Event.current.type == EventType.KeyDown)
                {
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.Backspace or KeyCode.Delete:
                            if (TryGetFocusedListEntry(out var focusedEntry))
                            {
                                Event.current.Use();
                                OnTryDeleteListEntry(focusedEntry);
                            }
                            break;
                        
                        case KeyCode.Return or KeyCode.KeypadEnter:
                            if (TryGetFocusedListEntry(out focusedEntry))
                            {
                                Event.current.Use();
                                OnDoubleClickListEntry(focusedEntry);
                            }
                            break;
                    }
                }
            }
        }

        #endregion

        #region List Header GUI

        protected virtual float GetListHeaderHeight() => 25f;
        protected virtual float GetListBasePropertyUIDWidth() => 70f;
        protected virtual float GetListBasePropertyNameWidth() => 150f;

        protected virtual void DrawListHeader(Rect rect, float scrollOffset)
        {
            GUI.BeginClip(rect);

            var movingRect = new Rect(scrollOffset, 0f, 0f, rect.height);

            var width = GetListBasePropertyUIDWidth();
            movingRect.width = width;
            if (movingRect.x + movingRect.width > 0f) GUI.Label(movingRect, g_uid, EditorStyles.boldLabel);
            movingRect.x += width;

            width = GetListBasePropertyNameWidth();
            movingRect.width = width;
            if (movingRect.x + movingRect.width > 0f) GUI.Label(movingRect, g_name, EditorStyles.boldLabel);
            movingRect.x += width;

            if (m_listDisplayedProperties != null)
            {
                foreach (var displayedProperty in m_listDisplayedProperties)
                {
                    movingRect.width = displayedProperty.width;
                    if (movingRect.x + movingRect.width > 0f) GUI.Label(movingRect, displayedProperty.displayName, EditorStyles.boldLabel);
                    movingRect.x += displayedProperty.width;
                }
            }

            GUI.EndClip();

            DrawListHeaderBottomSeparator(rect);
        }

        protected virtual void DrawListHeaderBottomSeparator(Rect rect) => EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - 2f, rect.width, 2f), Color.white);

        #endregion

        #region List Entry GUI

        protected virtual float GetListEntryHeight() => 20f;
        protected virtual float GetListEntryContextButtonWidth() => 25f;

        protected virtual void DrawListEntry(Rect rect, float scrollOffset, ListEntry entry)
        {
            // Background
            if (entry.displayIndex == m_focusedIndex)
            {
                EditorGUI.DrawRect(rect, m_focusedListEntryBackgroundColor);
            }

            // Context Button
            var contextButtonWidth = GetListEntryContextButtonWidth();
            var contextButtonRect = new Rect(rect.x + rect.width - contextButtonWidth + 5f, rect.y, contextButtonWidth, rect.height);
            DrawListEntryContextButton(contextButtonRect, entry);

            // Starts Clipping
            GUI.BeginClip(new Rect(rect.x, rect.y, rect.width - contextButtonWidth, rect.height));

            // Create MovingRect
            var movingRect = new Rect(scrollOffset, 0f, 0f, rect.height);

            // Draw Base Properties
            var width = GetListBasePropertyUIDWidth();
            if (movingRect.x + width > 0f && CanDrawListEntryUID(entry))
            {
                movingRect.width = width;
                DrawListEntryUID(movingRect, entry);
            }
            movingRect.x += width;

            width = GetListBasePropertyNameWidth();
            if (movingRect.x + width > 0f && CanDrawListEntryName(entry))
            {
                movingRect.width = width;
                DrawListEntryName(movingRect, entry);
            }
            movingRect.x += width;

            // Draw Displayed Properties
            DrawListEntryDisplayedProperties(movingRect, entry);

            // End Clipping
            GUI.EndClip();

            // Selection
            var selectionRect = new Rect(rect.x, rect.y, contextButtonRect.x - rect.x, rect.height);
            HandleListEntryEvents(selectionRect, entry);

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
        protected virtual void DrawListEntryName(Rect rect, ListEntry entry) => GUI.Label(rect, entry.g_name, EditorStyles.boldLabel);
        protected virtual bool CanDrawListEntryUID(ListEntry entry) => entry.element != null;
        protected virtual void DrawListEntryUID(Rect rect, ListEntry entry) => EditorGUI.LabelField(rect, entry.element.UID.ToString(), EditorStyles.miniLabel);
        protected virtual void DrawListEntryBottomSeparator(Rect rect) => EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), Color.gray5);

        protected virtual void DrawListEntryDisplayedProperties(Rect movingRect, ListEntry entry)
        {
            if (m_listDisplayedProperties == null) return;

            var so = GetOrCreateSerializedObject(entry.property.objectReferenceValue);
            if (so == null) return;
            so.UpdateIfRequiredOrScript();

            foreach (var displayedProperty in m_listDisplayedProperties)
            {
                if (movingRect.x + displayedProperty.width > 0f 
                    && CanDrawListEntryDisplayedProperty(entry, so, displayedProperty, out var property))
                {
                    movingRect.width = displayedProperty.width;
                    DrawListEntryDisplayedProperty(movingRect, entry, so, displayedProperty, property);
                }
                movingRect.x += displayedProperty.width;
            }

            so.ApplyModifiedProperties();
        }
        protected virtual bool CanDrawListEntryDisplayedProperty(ListEntry entry, SerializedObject so, ListDisplayedProperty displayedProperty, out SerializedProperty property)
        {
            property = so.FindProperty(displayedProperty.propertyName);
            return property != null;
        }
        protected virtual void DrawListEntryDisplayedProperty(Rect rect, ListEntry entry, SerializedObject so, ListDisplayedProperty displayedProperty, SerializedProperty property)
        {
            rect.x += 2f;
            rect.width -= 2f;
            PropertyFieldWithoutDecorators(rect, property, GUIContent.none);
        }

        protected virtual void HandleListEntryEvents(Rect rect, ListEntry entry)
        {
            if (Event.current.type == EventType.MouseDown
                && rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();

                if (EditorApplication.timeSinceStartup - m_lastListSelectionTime < 0.5f)
                {
                    OnDoubleClickListEntry(entry);
                    if (m_focusedIndex != entry.displayIndex)
                    {
                        OnSelectListEntry(entry);
                    }
                }
                else
                {
                    m_lastListSelectionTime = EditorApplication.timeSinceStartup;
                    if (m_focusedIndex == entry.displayIndex)
                    {
                        OnDeselectListEntry();
                    }
                    else
                    {
                        OnSelectListEntry(entry);
                    }
                }
            }
        }

        #endregion

        #region List Callbacks

        protected virtual void OnListEntryContextButton(ListEntry entry)
        {

        }

        protected virtual void OnSelectListEntry(ListEntry entry)
        {
            m_focusedIndex = entry.displayIndex;
            GUI.FocusControl(null);
            m_listHasFocus = true;
            if (entry.property != null && entry.property.objectReferenceValue != null)
            {
                m_focusedObject = entry.property.objectReferenceValue;
            }
        }
        protected virtual void OnDeselectListEntry()
        {
            m_focusedIndex = -1;
            m_focusedObject = null;
            m_listHasFocus = false;
            GUI.FocusControl(null);
        }
        protected virtual void OnDoubleClickListEntry(ListEntry entry)
        {
            if (entry.property != null && entry.property.objectReferenceValue != null)
            {
                EditorUtils.FullPingObject(entry.property.objectReferenceValue);
            }
        }
        protected virtual void OnTryDeleteListEntry(ListEntry entry)
        {
            if (p_objects != null && p_objects.arraySize > entry.propertyIndex)
            {
                Undo.RecordObject(target, "Delete list entry");
                p_objects.DeleteArrayElementAtIndex(entry.propertyIndex);
            }
            else
            {
                Debug.LogError("Could not delete entry " + entry.displayIndex + " corresponding to index " + entry.propertyIndex + " in objects list.");
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
            else if (m_isResizing && Event.current.rawType == EventType.MouseUp)
            {
                m_isResizing = false;
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


        // --- UTILITY ---

        #region Type Fields

        private static IEnumerable<FieldInfo> GetAllInstanceFields(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            while (type != null
                && type != typeof(ScriptableObject)
                && type != typeof(MonoBehaviour)
                && type != typeof(UnityEngine.Object))
            {
                foreach (var field in type.GetFields(flags))
                    yield return field;

                type = type.BaseType;
            }
        }

        #endregion

        #region Property Field Without Decorators

        private static Func<Rect, SerializedProperty, GUIContent, bool> _defaultPropertyField;
        private static bool _defaultPropertyFieldResolved;

        /// <summary>
        /// Draws the property without its DecoratorDrawers (Header, Space...) nor custom PropertyDrawers
        /// </summary>
        protected static void PropertyFieldWithoutDecorators(Rect rect, SerializedProperty property, GUIContent label)
        {
            if (!_defaultPropertyFieldResolved)
            {
                _defaultPropertyFieldResolved = true;
                var method = typeof(EditorGUI).GetMethod("DefaultPropertyField", BindingFlags.NonPublic | BindingFlags.Static, null,
                    new[] { typeof(Rect), typeof(SerializedProperty), typeof(GUIContent) }, null);
                if (method != null)
                {
                    _defaultPropertyField = (Func<Rect, SerializedProperty, GUIContent, bool>)
                        Delegate.CreateDelegate(typeof(Func<Rect, SerializedProperty, GUIContent, bool>), method);
                }
            }

            if (_defaultPropertyField != null) _defaultPropertyField(rect, property, label);
            else EditorGUI.PropertyField(rect, property, label); // Fallback if Unity changes the internal API
        }

        #endregion
    }

#endif

    #endregion
}
