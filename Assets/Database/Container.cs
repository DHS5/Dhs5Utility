using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
using System.IO;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Databases
{
    public class Container : ScriptableObject, ISerializationCallbackReceiver
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

        [NonSerialized] protected Dictionary<int, MappedObject> m_mappedObjects;

        #endregion

        #region Serialization Callbacks

        public virtual void OnBeforeSerialize() { }
        /// <summary>
        /// m_objects is only changed through serialization (editor changes, undo, reimport) : the mapped objects are rebuilt on next access
        /// </summary>
        public virtual void OnAfterDeserialize()
        {
            m_mappedObjects = null;
        }

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

        public virtual int Count => m_objects.Count;

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

        // INDEXED GETTERS
        public virtual UnityEngine.Object GetAtIndex(int index)
        {
            if (TryGetAtIndex(index, out var obj))
            {
                return obj;
            }
            return null;
        }
        public virtual bool TryGetAtIndex(int index, out UnityEngine.Object obj)
        {
            return m_objects.IsIndexValid(index, out obj);
        }
        public virtual T GetAtIndex<T>(int index) where T : UnityEngine.Object, IContainerElement
        {
            if (TryGetAtIndex<T>(index, out var obj))
            {
                return obj;
            }
            return null;
        }
        public virtual bool TryGetAtIndex<T>(int index, out T result) where T : UnityEngine.Object, IContainerElement
        {
            if (m_objects.IsIndexValid(index, out var obj) && obj is T t)
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

        public virtual bool Editor_CanAddNewObjectInContainer(UnityEngine.Object newObj, IContainerElement newElem)
        {
            if (newObj == null || newElem == null || m_objects.Contains(newObj)) return false;

            return true;
        }
        public virtual void Editor_OnNewObjectAddedInContainer(UnityEngine.Object newObj, IContainerElement newElem) { }
        /// <param name="obj">May be null</param>
        /// <param name="elem">May be null</param>
        public virtual void Editor_OnObjectRemovedFromContainer(UnityEngine.Object obj, IContainerElement elem) { }

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
            public ListEntry(int index, int propertyIndex, SerializedProperty property, bool isValid)
            {
                this.displayIndex = index;
                this.propertyIndex = propertyIndex;
                this.property = property;
                this.isValid = isValid;
                var obj = property.objectReferenceValue;
                if (obj is IContainerElement elem)
                {
                    this.element = elem;
                }
                else
                {
                    this.element = null;
                }
                this.g_name = obj != null ? new GUIContent(obj.name, obj.name) : new GUIContent(MissingEntryName, "The referenced object doesn't exist anymore");
            }

            public const string MissingEntryName = "Missing";

            public int displayIndex;
            public int propertyIndex;
            public SerializedProperty property;
            /// <summary>
            /// False if the object is missing or its type isn't accepted by the container anymore
            /// </summary>
            public bool isValid;
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

        protected Container m_container;

        protected List<Type> m_objectTypes;
        protected List<ListEntry> m_listEntries;
        protected List<ListDisplayedProperty> m_listDisplayedProperties;
        protected Dictionary<UnityEngine.Object, Editor> m_editors;
        protected Dictionary<UnityEngine.Object, SerializedObject> m_serializedObjects;

        protected int m_focusedIndex;
        protected UnityEngine.Object m_focusedObject;
        protected ListEntry m_contextEntry;

        protected Rect m_databaseWindowRect;
        protected string m_searchString;
        protected float m_listTotalWidth;
        protected int m_listSize;
        protected Vector2 m_listScrollPosition;
        protected float m_listScrollX;
        protected bool m_isResizing;
        protected Vector2 m_inspectorScrollPosition;
        protected bool m_listHasFocus;
        protected GenericMenu m_entryContextMenu;
        protected GenericMenu m_addMenu;
        protected bool m_projectObjectPickerRequested;
        protected int m_projectObjectPickerControlID = -1;

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
        protected Color m_invalidListEntryTextColor = new Color(1f, 0.35f, 0.35f);

        protected GUIStyle m_invalidEntryNameStyle;
        protected GUIStyle InvalidEntryNameStyle
        {
            get
            {
                if (m_invalidEntryNameStyle == null)
                {
                    m_invalidEntryNameStyle = new GUIStyle(EditorStyles.boldLabel);
                    SetStyleTextColor(m_invalidEntryNameStyle, m_invalidListEntryTextColor);
                }
                return m_invalidEntryNameStyle;
            }
        }
        protected GUIStyle m_invalidEntryUIDStyle;
        protected GUIStyle InvalidEntryUIDStyle
        {
            get
            {
                if (m_invalidEntryUIDStyle == null)
                {
                    m_invalidEntryUIDStyle = new GUIStyle(EditorStyles.miniLabel);
                    SetStyleTextColor(m_invalidEntryUIDStyle, m_invalidListEntryTextColor);
                }
                return m_invalidEntryUIDStyle;
            }
        }
        private static void SetStyleTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
        }

        #endregion

        #region Serialized Properties

        protected SerializedProperty p_objects;
        protected SerializedProperty p_lastCreateFolderPath;

        protected virtual void FindSerializedProperties()
        {
            p_objects = serializedObject.FindProperty("m_objects");
            p_lastCreateFolderPath = serializedObject.FindProperty("m_lastCreateFolderPath");
        }

        #endregion

        #region STATIC Members

        /// <summary>
        /// List area height, saved per project and per user (EditorUserSettings, in the UserSettings folder)
        /// </summary>
        private const string ListAreaHeightConfigKey = "Dhs5.ContainerEditor.ListAreaHeight";
        private const float DefaultListAreaHeight = 300f;

        private static float? _listAreaHeight;
        protected static float ListAreaHeight
        {
            get
            {
                if (!_listAreaHeight.HasValue)
                {
                    _listAreaHeight = float.TryParse(EditorUserSettings.GetConfigValue(ListAreaHeightConfigKey),
                        System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var height)
                        ? height : DefaultListAreaHeight;
                }
                return _listAreaHeight.Value;
            }
            set => _listAreaHeight = value;
        }
        protected static void SaveListAreaHeight()
        {
            EditorUserSettings.SetConfigValue(ListAreaHeightConfigKey, ListAreaHeight.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        #endregion


        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_container = target as Container;

            FindSerializedProperties();

            // Invalid entries are only shown : their removal is offered by the refresh button
            serializedObject.Update();
            RefreshObjectTypes();
            RefreshListEntries();
            RefreshListDisplayedProperties();

            m_entryContextMenu = CreateEntryContextMenu();
        }
        protected virtual void OnDisable()
        {
            ClearSerializedObjects();
            ClearEditors();
        }

        #endregion


        #region Object Types

        protected virtual void RefreshObjectTypes()
        {
            if (m_objectTypes == null) m_objectTypes = new();
            else m_objectTypes.Clear();

            if (m_container != null)
            {
                var containerType = m_container.GetType();
                if (containerType != null)
                {
                    var typesAttribute = containerType.GetCustomAttribute<ContainerTypesAttribute>();
                    if (typesAttribute != null && typesAttribute.types != null)
                    {
                        var containerElementType = typeof(IContainerElement);
                        var unityObjType = typeof(UnityEngine.Object);
                        foreach (var type in typesAttribute.types)
                        {
                            if (type != null 
                                && containerElementType.IsAssignableFrom(type)
                                && unityObjType.IsAssignableFrom(type))
                            {
                                if (!type.IsAbstract && !m_objectTypes.Contains(type))
                                    m_objectTypes.Add(type);
                                foreach (var subType in TypeCache.GetTypesDerivedFrom(type))
                                    if (!subType.IsAbstract && !m_objectTypes.Contains(subType))
                                        m_objectTypes.Add(subType);
                            }
                        }
                    }
                }
            }

            m_addMenu = CreateAddMenuFromObjectTypes();
        }

        /// <returns>False if <paramref name="obj"/> is missing or its type isn't accepted by the container anymore, or if it has no UID</returns>
        protected virtual bool IsEntryValid(UnityEngine.Object obj)
        {
            return IsEntryTypeValid(obj) && HasEntryValidUID(obj);
        }
        /// <returns>False if <paramref name="obj"/> is missing or its type isn't accepted by the container anymore : the entry can only be removed</returns>
        protected virtual bool IsEntryTypeValid(UnityEngine.Object obj)
        {
            return obj != null && m_objectTypes.Contains(obj.GetType());
        }
        /// <returns>False if <paramref name="obj"/> is an element with a UID of 0 : the entry is kept, its UID can be recomputed</returns>
        protected virtual bool HasEntryValidUID(UnityEngine.Object obj)
        {
            return obj is not IContainerElement elem || elem.UID != 0;
        }

        /// <summary>
        /// Shows a dialog listing the invalid entries and removes them if accepted (can be undone)
        /// </summary>
        protected virtual void OfferInvalidEntriesRemoval()
        {
            // Called after the GUI pass : the editor can be gone
            if (this == null || target == null || p_objects == null) return;

            serializedObject.Update();

            List<string> invalidEntries = new();
            for (int i = 0; i < p_objects.arraySize; i++)
            {
                var obj = p_objects.GetArrayElementAtIndex(i).objectReferenceValue;
                if (!IsEntryTypeValid(obj))
                {
                    var name = obj != null ? obj.name + " (" + obj.GetType().Name + ")" : ListEntry.MissingEntryName;
                    invalidEntries.Add("- " + name + (obj is IContainerElement elem ? " [UID " + elem.UID + "]" : string.Empty));
                }
            }
            if (invalidEntries.Count == 0) return;

            const int maxListed = 15;
            var list = string.Join("\n", invalidEntries.Take(maxListed));
            if (invalidEntries.Count > maxListed) list += "\n... and " + (invalidEntries.Count - maxListed) + " more";

            if (EditorUtility.DisplayDialog("Invalid entries in " + target.name,
                invalidEntries.Count + " entries are missing or have a type this container doesn't accept anymore :\n\n" + list
                + "\n\nRemove them from the container ? The assets themselves are kept.",
                "Remove", "Keep"))
            {
                RemoveInvalidEntries();
                serializedObject.ApplyModifiedProperties();
                RefreshListEntries();
                RefreshListDisplayedProperties();
                Repaint();
            }
        }
        protected virtual void RemoveInvalidEntries()
        {
            List<UnityEngine.Object> removedObjects = new();
            bool removed = false;

            for (int i = p_objects.arraySize - 1; i >= 0; i--)
            {
                var obj = p_objects.GetArrayElementAtIndex(i).objectReferenceValue;
                if (!IsEntryTypeValid(obj))
                {
                    removed = true;
                    if (obj != null) removedObjects.Add(obj);
                    p_objects.DeleteArrayElementAtIndex(i);                    
                }
            }

            if (removed) serializedObject.ApplyModifiedProperties();
            if (removedObjects.Count > 0)
            {
                foreach (var obj in removedObjects)
                {
                    if (obj != null) m_container.Editor_OnObjectRemovedFromContainer(obj, obj as IContainerElement);
                }
            }
        }

        /// <summary>
        /// Shows a dialog listing the elements with a UID of 0 and recomputes their UID if accepted
        /// </summary>
        protected virtual void OfferMissingUIDsRecomputation()
        {
            // Called after the GUI pass : the editor can be gone
            if (this == null || target == null || p_objects == null) return;

            serializedObject.Update();

            List<(UnityEngine.Object, IContainerElement)> missingUIDs = new();
            for (int i = 0; i < p_objects.arraySize; i++)
            {
                var obj = p_objects.GetArrayElementAtIndex(i).objectReferenceValue;
                // Entries to remove are left to the removal dialog
                if (IsEntryTypeValid(obj) && !HasEntryValidUID(obj))
                {
                    missingUIDs.Add((obj, obj as IContainerElement));
                }
            }
            if (missingUIDs.Count == 0) return;

            const int maxListed = 15;
            var list = string.Join("\n", missingUIDs.Take(maxListed).Select(e => "- " + e.Item1.name));
            if (missingUIDs.Count > maxListed) list += "\n... and " + (missingUIDs.Count - maxListed) + " more";

            if (EditorUtility.DisplayDialog("Missing UIDs in " + target.name,
                missingUIDs.Count + " elements have no UID (0) :\n\n" + list
                + "\n\nRecompute their UID ?",
                "Recompute", "Keep"))
            {
                foreach (var (obj, elem) in missingUIDs)
                {
                    EnsureNewObjectValidUID(obj, elem);
                }
                RefreshListEntries();
                Repaint();
            }
        }
        protected virtual bool IsObjectTypeValid(UnityEngine.Object obj)
        {
            if (obj is IContainerElement)
            {
                return m_objectTypes.Contains(obj.GetType());
            }
            if (obj is GameObject go && go.TryGetComponent<IContainerElement>(out var elem))
            {
                return m_objectTypes.Contains(elem.GetType());
            }
            return false;
        }

        protected virtual GenericMenu CreateAddMenuFromObjectTypes()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Find in project"), false, OnTryAddFromProject);
            menu.AddSeparator(string.Empty);
            
            foreach (var type in m_objectTypes)
            {
                if (type != null)
                {
                    menu.AddItem(new GUIContent(type.Name), false, OnTryAddFromTypeButton, type);
                }
            }

            return menu;
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
            int focusedIndex = -1;
            for (int i = 0; i < m_listSize; i++)
            {
                // Invalid entries (missing object or type not accepted) are listed too
                var p_entry = p_objects.GetArrayElementAtIndex(i);
                var obj = p_entry.objectReferenceValue;
                var name = obj != null ? obj.name : ListEntry.MissingEntryName;
                if (!isSearching
                    || name.Contains(m_searchString, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    // Keeps the focus on the focused object if still in the list
                    if (m_focusedObject != null && obj == m_focusedObject)
                        focusedIndex = index;

                    m_listEntries.Add(new ListEntry(index, i, p_entry, IsEntryValid(obj)));
                    index++;
                }
            }

            PruneObjectsCaches();

            if (focusedIndex != -1) m_focusedIndex = focusedIndex;
            else OnDeselectListEntry();
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
        /// Disposes the SerializedObjects and destroys the Editors of objects that are no longer in the container
        /// </summary>
        protected void PruneObjectsCaches()
        {
            if (m_serializedObjects == null && m_editors == null) return;

            HashSet<UnityEngine.Object> alive = new();
            for (int i = 0; i < p_objects.arraySize; i++)
            {
                var obj = p_objects.GetArrayElementAtIndex(i).objectReferenceValue;
                if (obj != null) alive.Add(obj);
            }

            List<UnityEngine.Object> toRemove = new();

            if (m_serializedObjects != null)
            {
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

            if (m_editors != null)
            {
                toRemove.Clear();
                foreach (var kvp in m_editors)
                {
                    if (kvp.Key == null || !alive.Contains(kvp.Key))
                    {
                        if (kvp.Value != null) DestroyImmediate(kvp.Value);
                        toRemove.Add(kvp.Key);
                    }
                }
                foreach (var key in toRemove)
                {
                    m_editors.Remove(key);
                }
            }
        }

        #endregion

        #region Entry Context Menu

        protected virtual GenericMenu CreateEntryContextMenu()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Ping"), false, OnContextObjectPing);
            menu.AddItem(new GUIContent("Open Asset"), false, OnContextObjectOpenAsset);
            menu.AddItem(new GUIContent("Remove"), false, OnContextObjectRemove);
            menu.AddItem(new GUIContent("Delete Asset"), false, OnContextObjectDeleteAsset);

            return menu;
        }

        protected virtual void OnContextObjectPing()
        {
            if (m_contextEntry.property.objectReferenceValue != null)
            {
                EditorUtils.FullPingObject(m_contextEntry.property.objectReferenceValue);
            }
        }
        protected virtual void OnContextObjectOpenAsset()
        {
            if (m_contextEntry.property.objectReferenceValue != null)
            {
                AssetDatabase.OpenAsset(m_contextEntry.property.objectReferenceValue);
            }
        }
        protected virtual void OnContextObjectRemove()
        {
            OnTryDeleteListEntry(m_contextEntry);
        }
        protected virtual void OnContextObjectDeleteAsset()
        {
            var obj = m_contextEntry.property.objectReferenceValue;
            if (obj != null)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (EditorUtility.DisplayDialog("Delete Asset",
                    "Are you sure you want to delete the asset " + obj.name + " ?\n\n" + path
                    + "\n\nThe file will be deleted from the project. You cannot undo this action.",
                    "Delete", "Cancel"))
                {
                    OnTryDeleteListEntry(m_contextEntry);
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        #endregion


        // --- GUI ---

        #region Core GUI

        public override void OnInspectorGUI()
        {
            if (Database.DebugModeEnabled)
                OnDefaultInspectorGUI();
            else
                OnDatabaseCoreGUI(new Rect(0f, 0f, Screen.width, Screen.height));
        }
        public virtual void OnDefaultInspectorGUI()
        {
            base.OnInspectorGUI();
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

            OnDatabaseInspectorGUI();
        }

        #endregion

        #region Database Core Events & Checks

        protected virtual void OnBeforeDatabaseGUI()
        {
            if (m_listHasFocus && GUIUtility.keyboardControl != 0)
            {
                m_listHasFocus = false;
            }

            HandleProjectObjectPicker();
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

            // Aligned on the top of the search field, like the D5 Console (GetLastRect is a placeholder during the Layout event, where the rect is unused)
            var r_searchField = GUILayoutUtility.GetLastRect();
            var r_clearButton = GUILayoutUtility.GetRect(16f, 16f, EditorStyles.iconButton, GUILayout.Width(16f));
            r_clearButton.y = r_searchField.y;
            r_clearButton.height = 16f;
            if (GUI.Button(r_clearButton, EditorGUIHelper.CrossIcon, EditorStyles.iconButton))
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
            RefreshObjectTypes();
            RefreshListEntries();
            RefreshListDisplayedProperties();

            // A modal dialog during the GUI pass breaks the layout : shown right after
            EditorApplication.delayCall -= OfferInvalidEntriesRemoval;
            EditorApplication.delayCall += OfferInvalidEntriesRemoval;
            EditorApplication.delayCall -= OfferMissingUIDsRecomputation;
            EditorApplication.delayCall += OfferMissingUIDsRecomputation;
        }

        protected virtual void OnToolbarAddButton()
        {
            m_addMenu.ShowAsContext();
        }
        protected virtual void OnTryAddFromProject()
        {
            // The picker is opened on the next GUI pass
            m_projectObjectPickerRequested = true;
            Repaint();
        }
        protected virtual bool OnAddFromProject(UnityEngine.Object obj)
        {
            if (m_container != null && obj != null)
            {
                if (!TryAddObjectToContainer(obj))
                {
                    Debug.LogError("Could not add object " + obj + " to " + m_container 
                        + "\nThe type is probably incorrect, make sure the object inherits from IContainerElement " +
                        "or is a GameObject with a component inheriting from IContainerElement on the root");
                    return false;
                }
                return true;
            }
            return false;
        }
        protected virtual void OnTryAddFromTypeButton(object typeObj)
        {
            if (typeObj is Type type && !type.IsAbstract)
            {
                var extension = typeof(Component).IsAssignableFrom(type) || type == typeof(GameObject) ? "prefab" : "asset";

                var folder = p_lastCreateFolderPath != null && AssetDatabase.IsValidFolder(p_lastCreateFolderPath.stringValue)
                    ? p_lastCreateFolderPath.stringValue
                    : Path.GetDirectoryName(AssetDatabase.GetAssetPath(m_container)).Replace('\\', '/');

                var path = EditorUtility.SaveFilePanelInProject("Create " + type.Name, "New " + type.Name, extension,
                    "Choose the name and location of the new " + type.Name, folder);

                // Empty when cancelled
                if (!string.IsNullOrEmpty(path))
                {
                    if (p_lastCreateFolderPath != null)
                    {
                        serializedObject.Update();
                        p_lastCreateFolderPath.stringValue = Path.GetDirectoryName(path).Replace('\\', '/');
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }

                    OnAddFromTypeButton(type, path);
                }
            }
        }
        protected virtual void OnAddFromTypeButton(Type type, string path)
        {
            if (m_container != null)
            {
                var newAsset = EditorDataUtility.CreateAssetOfType(type, path);

                if (!TryAddObjectToContainer(newAsset))
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(newAsset));
                    Debug.LogError("Could not create object of type " + type.FullName + " and add to " + m_container
                        + "\nThe type is probably incorrect, make sure it inherits from IContainerElement");
                }
            }
        }

        protected virtual bool TryAddObjectToContainer(UnityEngine.Object obj)
        {
            var result = false;
            var objToAdd = obj;
            IContainerElement newElem = null;
            if (obj is IContainerElement elem)
            {
                newElem = elem;
                result = m_container.Editor_CanAddNewObjectInContainer(objToAdd, newElem);
            }
            else if (obj is GameObject go && go.TryGetComponent(out newElem))
            {
                objToAdd = newElem as UnityEngine.Object;
                result = m_container.Editor_CanAddNewObjectInContainer(objToAdd, newElem);
            }

            if (result)
            {
                serializedObject.Update();

                EnsureNewObjectValidUID(objToAdd, newElem);

                if (p_objects != null)
                {
                    var newIndex = p_objects.arraySize;
                    p_objects.InsertArrayElementAtIndex(newIndex);
                    var p_newEntry = p_objects.GetArrayElementAtIndex(newIndex);
                    if (p_newEntry != null)
                    {
                        p_newEntry.objectReferenceValue = objToAdd;
                        serializedObject.ApplyModifiedProperties();

                        m_container.Editor_OnNewObjectAddedInContainer(objToAdd, newElem);
                    }
                }
            }

            return result;
        }

        protected virtual void EnsureNewObjectValidUID(UnityEngine.Object newObj, IContainerElement newElem)
        {
            var dirty = false;

            if (newElem.UID == 0)
            {
                var newUID = IContainerElement.Editor_ComputeContainerElementUID(newObj);
                if (newUID == 0)
                {
                    Debug.LogError("Computed new UID for " + newObj + " is 0", newObj);
                    return;
                }

                newElem.Editor_SetUID(newUID);
                dirty = true;
            }

            var conflict = false;
            var offset = 0;
            do
            {
                conflict = false;

                for (int i = 0; i < p_objects.arraySize; i++)
                {
                    var p_entry = p_objects.GetArrayElementAtIndex(i);
                    var obj = p_entry != null ? p_entry.objectReferenceValue : null;
                    if (obj != null && obj != newObj && obj is IContainerElement elem)
                    {
                        if (elem.UID == newElem.UID)
                        {
                            conflict = true;
                            offset++;
                            var newUID = IContainerElement.Editor_ComputeContainerElementUID(newObj, offset);
                            if (newUID == 0)
                            {
                                Debug.LogError("Computed new UID for " + newObj + " is 0", newObj);
                                return;
                            }

                            newElem.Editor_SetUID(newUID);
                            dirty = true;
                            break; 
                        }
                    }
                }
            } while (conflict);

            if (dirty)
            {
                Debug.Log("Set " + newObj.name + " UID to " + newElem.UID, newObj);
                EditorUtility.SetDirty(newObj);
                AssetDatabase.SaveAssetIfDirty(newObj);
            }
        }

        #endregion


        #region List GUI

        protected virtual void OnListGUI()
        {
            var rect = EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.Height(ListAreaHeight));

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
            if (rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    OnClickOnListArea();
                }

                else if (Event.current.type == EventType.DragUpdated)
                {
                    OnDragUpdatedOverListArea();
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    OnDragPerformedOverListArea();
                }
            }

            if (m_listHasFocus)
            {
                if (Event.current.type == EventType.KeyDown)
                {
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.Backspace or KeyCode.Delete:
                            OnTryDeleteEntryFromKeyboard();
                            break;

                        case KeyCode.Return or KeyCode.KeypadEnter:
                            OnTryFocusEntryFromKeyboard();
                            break;
                    }
                }
            }
        }

        protected virtual void OnClickOnListArea()
        {
            Event.current.Use();
            OnDeselectListEntry();
            GUI.FocusControl(null);
        }
        protected virtual void OnDragUpdatedOverListArea()
        {
            if (DragAndDrop.objectReferences != null
                && DragAndDrop.objectReferences.Length > 0)
            {
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj != null && IsObjectTypeValid(obj))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        break;
                    }
                }
            }
            Event.current.Use();
        }
        protected virtual void OnDragPerformedOverListArea()
        {
            if (DragAndDrop.objectReferences != null
                && DragAndDrop.objectReferences.Length > 0)
            {
                var valid = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj != null && IsObjectTypeValid(obj))
                    {
                        OnAddFromProject(obj);
                        valid = true;
                    }
                }

                if (valid)
                {
                    DragAndDrop.AcceptDrag();
                    Event.current.Use();
                }
            }
        }
        protected virtual void OnTryDeleteEntryFromKeyboard()
        {
            if (TryGetFocusedListEntry(out var focusedEntry))
            {
                Event.current.Use();
                OnTryDeleteListEntry(focusedEntry);
            }
        }
        protected virtual void OnTryFocusEntryFromKeyboard()
        {
            if (TryGetFocusedListEntry(out var focusedEntry))
            {
                Event.current.Use();
                OnDoubleClickListEntry(focusedEntry);
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

        protected virtual bool CanDrawListEntryName(ListEntry entry) => entry.property != null;
        protected virtual void DrawListEntryName(Rect rect, ListEntry entry) => GUI.Label(rect, entry.g_name, entry.isValid ? EditorStyles.boldLabel : InvalidEntryNameStyle);
        protected virtual bool CanDrawListEntryUID(ListEntry entry) => entry.element != null;
        protected virtual void DrawListEntryUID(Rect rect, ListEntry entry) => EditorGUI.LabelField(rect, entry.element.UID.ToString(), entry.isValid ? EditorStyles.miniLabel : InvalidEntryUIDStyle);
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

                if (Event.current.button == 0)
                {
                    // Unity's double-click detection : OS delay, same position
                    if (Event.current.clickCount == 2)
                    {
                        OnDoubleClickListEntry(entry);
                        if (m_focusedIndex != entry.displayIndex)
                        {
                            OnSelectListEntry(entry);
                        }
                    }
                    else
                    {
                        if (m_focusedIndex == entry.displayIndex)
                        {
                            OnDeselectListEntry();
                            GUI.FocusControl(null);
                        }
                        else
                        {
                            OnSelectListEntry(entry);
                        }
                    }
                }
                else if (Event.current.button == 1)
                {
                    OnListEntryContextButton(entry);
                }
            }
        }

        #endregion

        #region List Callbacks

        protected virtual void OnListEntryContextButton(ListEntry entry)
        {
            m_contextEntry = entry;

            if (m_entryContextMenu != null)
            {
                m_entryContextMenu.ShowAsContext();
            }
        }

        protected virtual void OnSelectListEntry(ListEntry entry)
        {
            m_focusedIndex = entry.displayIndex;
            GUI.FocusControl(null);
            m_listHasFocus = true;
            // Null for a missing entry : no inspector shown
            m_focusedObject = entry.property != null ? entry.property.objectReferenceValue : null;
        }
        protected virtual void OnDeselectListEntry()
        {
            m_focusedIndex = -1;
            m_focusedObject = null;
            m_listHasFocus = false;
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
            serializedObject.Update();
            if (p_objects != null && p_objects.arraySize > entry.propertyIndex)
            {
                var obj = entry.property.objectReferenceValue;
                p_objects.DeleteArrayElementAtIndex(entry.propertyIndex);
                serializedObject.ApplyModifiedProperties();
                if (m_container != null && obj != null)
                {
                    m_container.Editor_OnObjectRemovedFromContainer(obj, entry.element);
                }
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
                ListAreaHeight = Mathf.Clamp(ListAreaHeight + Event.current.delta.y, 100f, m_databaseWindowRect.height - 200f);
                Event.current.Use();
            }
            else if (m_isResizing && Event.current.rawType == EventType.MouseUp)
            {
                m_isResizing = false;
                SaveListAreaHeight();
            }
        }

        #endregion


        #region Inspector GUI

        protected virtual void OnDatabaseInspectorGUI()
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

        #region Project Object Picker

        protected virtual string GetProjectObjectPickerSearchFilter()
        {
            // Several "t:" filters are combined with OR by the object picker
            return string.Join(" ", m_objectTypes.Where(t => t != null).Select(t => "t:" + t.Name));
        }

        protected virtual void HandleProjectObjectPicker()
        {
            if (m_projectObjectPickerRequested)
            {
                m_projectObjectPickerRequested = false;
                if (m_objectTypes != null && m_objectTypes.Count > 0)
                {
                    m_projectObjectPickerControlID = GUIUtility.GetControlID(FocusType.Passive);
                    EditorGUIUtility.ShowObjectPicker<UnityEngine.Object>(null, false, GetProjectObjectPickerSearchFilter(), m_projectObjectPickerControlID);
                }
            }

            if (m_projectObjectPickerControlID != -1
                && Event.current.type == EventType.ExecuteCommand
                && Event.current.commandName == "ObjectSelectorClosed"
                && EditorGUIUtility.GetObjectPickerControlID() == m_projectObjectPickerControlID)
            {
                m_projectObjectPickerControlID = -1;
                var foundObject = EditorGUIUtility.GetObjectPickerObject();
                Event.current.Use();

                // The search filter can be edited by the user inside the picker window
                if (foundObject != null)
                {
                    var foundObjectType = foundObject.GetType();
                    if (m_objectTypes.Contains(foundObjectType))
                    {
                        OnAddFromProject(foundObject);
                    }
                    else if (foundObject is GameObject go)
                    {
                        foreach (var type in m_objectTypes)
                        {
                            if (type != null && go.TryGetComponent(type, out var component))
                            {
                                OnAddFromProject(component);
                                break;
                            }
                        }
                    }
                }
            }
        }

        #endregion

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
