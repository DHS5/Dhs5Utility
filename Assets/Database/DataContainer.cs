using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;

#if UNITY_EDITOR
using Dhs5.Utility.GUIs;
using System.Reflection;
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Databases
{
    public abstract class BaseDataContainer : ScriptableObject, IEnumerable
    {
        #region Accessors

        public abstract int Count { get; }

        public abstract UnityEngine.Object GetDataAtIndex(int index); 
        public T GetDataAtIndex<T>(int index) where T : UnityEngine.Object, IDataContainerElement
        {
            if (GetDataAtIndex(index) is T elem) return elem;
            return null;
        }

        public abstract bool TryGetDataByUID(int uid, out UnityEngine.Object obj);
        public bool TryGetDataByUID<T>(int uid, out T data) where T : UnityEngine.Object, IDataContainerElement
        {
            if (TryGetDataByUID(uid, out UnityEngine.Object obj) && obj is T elem)
            {
                data = elem;
                return true;
            }
            data = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return GetDataAtIndex(i);
            }
        }
        public IEnumerable<T> GetDataEnumerator<T>() where T : UnityEngine.Object, IDataContainerElement
        {
            for (int i = 0; i < Count; i++)
            {
                yield return GetDataAtIndex<T>(i);
            }
        }

        #endregion

        #region Context Menu

        [ContextMenu("Clean")]
        private void Clean()
        {
#if UNITY_EDITOR
            Editor_CleanUp();
#endif
        }

        #endregion

#if UNITY_EDITOR

        #region Instance Content Management

        internal virtual bool Editor_IsTypeValidForContainer(Type type)
        {
            return typeof(IDataContainerElement).IsAssignableFrom(type);
        }
        internal virtual bool Editor_ContainerHasValidDataType(out Type dataType)
        {
            return HasDataType(GetType(), out dataType) &&
                Editor_IsTypeValidForContainer(dataType);
        }
        internal virtual bool Editor_IsElementValid(UnityEngine.Object element)
        {
            if (element == null || element == this) return false;

            if (Editor_ContainerHasValidDataType(out var type))
            {
                Type elementType = element.GetType();

                // Scriptable
                if (type.IsSubclassOf(typeof(ScriptableObject)))
                {
                    return elementType == type || elementType.IsSubclassOf(type);
                }
                // Component
                if (type.IsSubclassOf(typeof(Component)))
                {
                    return element is GameObject go && go.TryGetComponent(type, out _);
                }

                return false;
            }
            return !HasDataType(GetType(), out _); // True if anyType and false if has data type cause the type is invalid
        }

        protected abstract IEnumerable<UnityEngine.Object> Editor_GetContainerContent();
        protected IEnumerable<T> Editor_GetContainerElements<T>() where T : class, IDataContainerElement
        {
            foreach (var obj in Editor_GetContainerContent())
            {
                if (obj is ScriptableObject)
                    yield return obj as T;
                else if (obj is GameObject go)
                    yield return go.GetComponent<T>();
            }
        }
        public Dictionary<int, UnityEngine.Object> Editor_GetContainerDicoContent()
        {
            Dictionary<int, UnityEngine.Object> dico = new();

            IDataContainerElement elem = null;
            foreach (var obj in Editor_GetContainerContent())
            {
                if (obj is ScriptableObject)
                {
                    elem = obj as IDataContainerElement;
                }
                else if (obj is GameObject go)
                {
                    elem = go.GetComponent<IDataContainerElement>();
                }
                dico[elem.UID] = obj;
            }
            return dico;
        }

        internal bool Editor_DeleteElementByUID(int uid)
        {
            if (Editor_OnDeleteElementByUID(uid))
            {
                Editor_ShouldRecomputeContainerContent();
                return true;
            }
            return false;
        }
        protected abstract bool Editor_OnDeleteElementByUID(int uid);

        internal event Action Editor_ContainerContentChanged;

        internal virtual void Editor_ShouldRecomputeContainerContent()
        {
            // Ensure UIDs unicity
            HashSet<int> uids = new();
            foreach (var elem in Editor_GetContainerElements<IDataContainerElement>())
            {
                if (elem.UID == 0 || uids.Contains(elem.UID))
                {
                    elem.Editor_SetUID(Editor_GenerateUID());
                }
                uids.Add(elem.UID);
            }

            // Trigger event and save asset
            Editor_ContainerContentChanged?.Invoke();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        /// <summary>
        /// Callback triggered when a new element has been created for the database<br></br>
        /// At this moment, the database content has not been recomputed yet so the element is not in it
        /// </summary>
        internal virtual void Editor_OnNewElementCreated(UnityEngine.Object element) { }
        /// <summary>
        /// Callback triggered when a new element has been added to the database<br></br>
        /// At this moment, the database content has not been recomputed yet so the element is not in it
        /// </summary>
        internal virtual void Editor_OnAddingNewElement(UnityEngine.Object element)
        {
            IDataContainerElement dataContainerElem = element as IDataContainerElement;
            if (dataContainerElem.UID == 0 || Editor_DoesUIDExistIn(dataContainerElem.UID))
            {
                dataContainerElem.Editor_SetUID(Editor_GenerateUID());
            }
        }

        // --- UIDs ---
        protected int Editor_GenerateUID()
        {
            int max = 0;
            foreach (var elem in Editor_GetContainerElements<IDataContainerElement>())
            {
                if (elem.UID > max)
                {
                    max = elem.UID;
                }
            }

            if (max == int.MaxValue)
            {
                throw new Exception("You reached the max number of elements created in this DB, congratulations !");
            }
            return max + 1;
        }
        protected bool Editor_DoesUIDExistIn(int uid)
        {
            foreach (var elem in Editor_GetContainerElements<IDataContainerElement>())
            {
                if (elem.UID == uid) return true;
            }
            return false;
        }

        public (string[], int[]) Editor_GetContainerDisplayContent()
        {
            List<string> names = new();
            List<int> uids = new();

            foreach (var (uid, obj) in Editor_GetContainerDicoContent())
            {
                // Name
                names.Add(Editor_GetDataPrefixedName(obj));
                // UID
                uids.Add(uid);
            }

            return (names.ToArray(), uids.ToArray());
        }

        // --- NAMING ---
        internal virtual string Editor_GetDataPrefixedName(UnityEngine.Object obj)
        {
            if (obj is IDataContainerPrefixableElement prefixableElement && !string.IsNullOrWhiteSpace(prefixableElement.DataNamePrefix))
            {
                StringBuilder sb = new();
                sb.Append(prefixableElement.DataNamePrefix);
                if (!prefixableElement.DataNamePrefix.EndsWith("/")) sb.Append("/");
                sb.Append(obj.name);
                return sb.ToString();
            }
            return obj != null ? obj.name : null;
        }

        // --- CLEAN ---
        protected abstract void Editor_CleanUp();

        #endregion

        #region Type Informations

        /// <summary>
        /// The path will be null if the DataContainer doesn't have the DatabaseAttribute
        /// </summary>
        /// <returns></returns>
        internal string Editor_GetPath()
        {
            return GetPath(GetType());
        }

        #endregion

        #region Sort Functions

        internal static int Sort_ByName(UnityEngine.Object obj1, UnityEngine.Object obj2)
        {
            return obj1.name.CompareTo(obj2.name);
        }
        internal static int Sort_ByType(UnityEngine.Object obj1, UnityEngine.Object obj2)
        {
            return obj1.GetType().FullName.CompareTo(obj2.GetType().FullName);
        }

        #endregion

        #region Attributes

        private static Dictionary<Type, DataContainerAttribute> _attributes = new();
        protected static bool TryGetAttribute(Type type, out DataContainerAttribute attribute)
        {
            if (_attributes.TryGetValue(type, out attribute))
            {
                return true;
            }

            attribute = type.GetCustomAttribute<DataContainerAttribute>(inherit: true);

            if (attribute != null)
            {
                _attributes.Add(type, attribute);
                return true;
            }
            return false;
        }
        internal static bool TryGetDatabaseAttribute(Type type, out DatabaseAttribute attribute)
        {
            if (TryGetAttribute(type, out var dataContainerAtt) && dataContainerAtt is DatabaseAttribute databaseAtt)
            {
                attribute = databaseAtt;
                return true;
            }
            attribute = null;
            return false;
        }
        protected static bool HasDataType(Type type, out Type dataType)
        {
            if (TryGetAttribute(type, out var attribute))
            {
                dataType = attribute.dataType;
                return !attribute.anyType;
            }
            dataType = null;
            return false;
        }
        protected static string GetPath(Type type)
        {
            if (TryGetDatabaseAttribute(type, out var attribute))
            {
                return attribute.path;
            }
            return null;
        }

        #endregion

#endif
    }

    #region Editor

#if UNITY_EDITOR

    public abstract class BaseDataContainerEditor : Editor
    {
        #region ENUM : ContentListDisplayType

        protected enum EContentListDisplayType
        {
            INDEX = 0,
            FOLDERS = 1,
        }

        #endregion

        #region Consts

        protected const string RenamingControlName = "RenameControl";

        #endregion

        #region Members

        protected BaseDataContainer m_container;
        protected Dictionary<UnityEngine.Object, Editor> m_editors = new();

        protected SerializedProperty p_script;

        protected List<string> m_excludedProperties;

        #endregion

        #region Properties

        // Valid Data Type
        protected bool ContainerHasValidDataType { get; private set; }
        protected Type DataType { get; private set; }
        protected Type[] ChildDataTypes { get; private set; }

        // Events
        protected Event CurrentEvent { get; private set; }
        protected bool EventReceived { get; private set; }

        // Database Informations
        protected bool DatabaseInformationsFoldoutOpen { get; set; }

        // Content List Window
        protected int ContainerSelectionIndex { get; private set; } = -1;
        protected float ContentListRectHeight { get; private set; } = 170f;
        protected bool IsResizingContentList { get; private set; }
        protected string ContentListSearchString { get; private set; }
        protected bool ContentListFiltered => !string.IsNullOrWhiteSpace(ContentListSearchString);
        protected Rect ContentListRect { get; set; }
        protected bool ContentListNeedsScrollRect { get; private set; }
        protected Vector2 ContentListWindowScrollPos { get; set; }
        protected float ContentListElementAlinea { get; set; } = 20f;
        protected float ContentListElementHeight { get; set; } = 20f;
        protected float ContentListElementContextButtonWidth { get; set; } = 30f;
        private double m_lastSelectionTime;
        private double m_doubleClickDelay = 0.2d;

        // Element Display
        protected Vector2 ElementDisplayScrollPos { get; set; }

        // Renaming
        protected bool IsRenamingElement { get; set; }
        protected string RenamingString { get; set; }
        protected UnityEngine.Object RenamingElement { get; set; }
        protected int RenamingElementIndex { get; set; }
        private bool m_justStartedRename;

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_container = (BaseDataContainer)target;
            // --- DATA TYPE ---
            ContainerHasValidDataType = m_container.Editor_ContainerHasValidDataType(out var dataType);
            if (ContainerHasValidDataType)
            {
                DataType = dataType;
                if (DataType.IsAbstract)
                {
                    ChildDataTypes = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => a.GetTypes())
                        .Where(t => t.IsSubclassOf(DataType))
                        .ToArray();
                }
                else
                {
                    ChildDataTypes = null;
                }
            }

            // --- EDITOR PREFS ---
            ContentListRectHeight = EditorPrefs.GetFloat(m_container.GetType().Name + "_contentListRectHeight", 170f);

            // --- CALLBACKS ---
            m_container.Editor_ContainerContentChanged += OnContainerContentChanged;

            // --- PROPERTIES ---
            p_script = serializedObject.FindProperty("m_Script");

            m_excludedProperties = new()
            {
                p_script.propertyPath,
            };

            // --- CONTENT UPDATE ---
            OnContainerContentChanged();
        }
        protected virtual void OnDisable()
        {
            ClearEditors();

            EditorPrefs.SetFloat(m_container.GetType().Name + "_contentListRectHeight", ContentListRectHeight);

            m_container.Editor_ContainerContentChanged -= OnContainerContentChanged;
        }

        #endregion

        #region Callbacks

        protected virtual void OnContainerContentChanged()
        {
            SetSelectionIndex(-1, false);

            serializedObject.Update();

            m_containerContent = m_container.Editor_GetContainerDicoContent();

            m_folderStructure.Clear();
            ComputeFolderStructure();
        }

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
            if (element == null) return null;

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
                if (ContainerHasValidDataType
                    && DataType.IsSubclassOf(typeof(Component))
                    && go.TryGetComponent(DataType, out Component component))
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

        #region Events

        protected virtual void HandleEvent()
        {
            CurrentEvent = Event.current;
            CheckEventReceived();
        }
        protected bool CheckEventReceived()
        {
            EventReceived =
                CurrentEvent.type != EventType.Ignore &&
                CurrentEvent.type != EventType.Used &&
                CurrentEvent.type != EventType.Repaint &&
                CurrentEvent.type != EventType.Layout;
            return EventReceived;
        }

        protected void UseCurrentEvent()
        {
            EventReceived = false;
            CurrentEvent.Use();
        }

        protected virtual void OnEventReceived(Event e)
        {
            switch (e.type)
            {
                case EventType.KeyDown:
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
                            case KeyCode.Return:
                                OnEventReceived_Validate(e); break;
                        }
                        break;
                    }
                case EventType.MouseDown:
                    {
                        if (CompleteRenaming())
                        {
                            UseCurrentEvent();
                        }
                        break;
                    }
                case EventType.DragUpdated:
                    {
                        OnEventReceived_DragUpdated(e);
                        break;
                    }
                case EventType.DragPerform:
                    {
                        OnEventReceived_DragPerformed(e);
                        break;
                    }
            }
        }

        protected virtual void OnEventReceived_Delete(Event e)
        {
            if (TryDeleteCurrentSelection())
            {
                UseCurrentEvent();
            }
        }

        protected virtual void OnEventReceived_Rename(Event e)
        {
            if (!IsRenamingElement)
            {
                if (ContainerSelectionIndex >= 0)
                {
                    BeginRenaming(GetContainerCurrentSelection(), ContainerSelectionIndex);
                    UseCurrentEvent();
                }
            }
            else if (CompleteRenaming())
            {
                UseCurrentEvent();
            }
        }

        protected virtual void OnEventReceived_Validate(Event e)
        {
            var entry = GetCurrentSelectionEntry();
            if (entry is FolderStructureGroupEntry group)
            {
                group.open = !group.open;
                UseCurrentEvent();
            }
        }


        protected virtual void OnEventReceived_Up(Event e)
        {
            TrySelectUp();
            UseCurrentEvent();
        }
        protected virtual void OnEventReceived_Down(Event e)
        {
            TrySelectDown();
            UseCurrentEvent();
        }

        #endregion


        #region Base GUI

        public sealed override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (ContainerFailedDataTypeCheck())
            {
                return;
            }

            HandleEvent();

            OnGUI();

            if (CheckEventReceived())
            {
                OnEventReceived(CurrentEvent);
            }

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

        protected abstract string ContainerInvalidDataTypeMessage();
        protected virtual bool ContainerFailedDataTypeCheck()
        {
            if (!ContainerHasValidDataType)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(p_script);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.HelpBox(ContainerInvalidDataTypeMessage(), MessageType.Error);
                return true;
            }
            return false;
        }

        #endregion

        #region Helper GUI

        #region Container Informations

        protected virtual void OnContainerInformationsGUI(string title)
        {
            Rect rect = new Rect(0f, 0f, EditorGUIUtility.currentViewWidth, 20f);
            EditorGUILayout.GetControlRect(false, 17f);

            // Background
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            // Label
            if (GUI.Button(rect, GUIContent.none, EditorStyles.toolbarButton)) DatabaseInformationsFoldoutOpen = !DatabaseInformationsFoldoutOpen;
            EditorGUI.LabelField(new Rect(rect.x + 10f, rect.y, rect.width - 10f, rect.height), title);

            // Content
            if (DatabaseInformationsFoldoutOpen)
            {
                var backgroundRect = EditorGUILayout.BeginVertical();
                backgroundRect.x = 0f; backgroundRect.width = EditorGUIUtility.currentViewWidth;
                EditorGUI.DrawRect(backgroundRect, GUIHelper.transparentBlack02);
                EditorGUI.indentLevel++;
            
                EditorGUILayout.Space(5f);
                OnContainerInformationsContentGUI();
                EditorGUILayout.Space(2f);
                EditorGUI.DrawRect(new Rect(backgroundRect.x, backgroundRect.y + backgroundRect.height - 1f, backgroundRect.width, 1f), Color.black);

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
        }
        protected virtual void OnContainerInformationsContentGUI() 
        {
            DrawDefault();
        }

        #endregion

        #region Container Folder Structure

        private FolderStructure m_folderStructure = new();
        protected int ContentListCount => m_folderStructure.Count;

        protected FolderStructureEntry GetEntryAtIndex(int index) => m_folderStructure.GetEntryAtIndex(index);
        protected FolderStructureEntry GetCurrentSelectionEntry() => m_folderStructure.GetEntryAtIndex(ContainerSelectionIndex);

        private IEnumerable<int> GetValidEntriesIndexes()
        {
            if (!ContentListFiltered)
            {
                return m_folderStructure.GetValidEntriesIndexes();
            }
            else
            {
                return m_folderStructure.GetFilteredEntriesIndexes(ContentListSearchString);
            }
        }

        protected int GetFirstValidEntryIndexUp()
        {
            int validIndex = 0;
            foreach (var index in GetValidEntriesIndexes())
            {
                if (index < ContainerSelectionIndex)
                {
                    validIndex = index;
                }
                else
                {
                    break;
                }
            }
            return validIndex;
        }
        protected int GetFirstValidEntryIndexDown()
        {
            foreach (var index in GetValidEntriesIndexes())
            {
                if (index > ContainerSelectionIndex)
                {
                    return index;
                }
            }
            return ContainerSelectionIndex;
        }

        protected int GetVisibleIndex(int containerIndex)
        {
            if (containerIndex < 0) return -1;

            int index = 0;
            foreach (var i in GetValidEntriesIndexes())
            {
                if (i == containerIndex)
                    return index;
                index++;
            }
            return 0;
        }

        // --- COMPUTATION ---
        private void ComputeFolderStructure()
        {
            switch (GetContentListDisplayType())
            {
                case EContentListDisplayType.INDEX:
                    OnComputeFolderStructure_ByINDEX(m_folderStructure, m_containerContent);
                    break;
                case EContentListDisplayType.FOLDERS:
                    OnComputeFolderStructure_ByFOLDERS(m_folderStructure, m_containerContent);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected virtual void OnComputeFolderStructure_ByINDEX(FolderStructure structure, Dictionary<int, UnityEngine.Object> contentDico)
        {
            foreach (var (uid, obj) in contentDico)
            {
                structure.Add(new FolderStructureEntry(m_container.Editor_GetDataPrefixedName(obj), data: uid));
            }
        }
        protected virtual void OnComputeFolderStructure_ByFOLDERS(FolderStructure structure, Dictionary<int, UnityEngine.Object> contentDico)
        {
            Dictionary<string, object> folderStructureDatas = new(); 
            foreach (var (uid, obj) in contentDico)
            {
                if (!folderStructureDatas.TryAdd(m_container.Editor_GetDataPrefixedName(obj), uid))
                {
                    folderStructureDatas.Add(m_container.Editor_GetDataPrefixedName(obj) + "_" + uid, uid);
                }
            }

            structure.FillFromNamesAndDatas(folderStructureDatas);
        }

        #endregion

        #region Content List Display Type

        protected abstract EContentListDisplayType GetContentListDisplayType();

        #endregion

        #region Container Content

        private Dictionary<int, UnityEngine.Object> m_containerContent;

        protected UnityEngine.Object GetDataContainerElementByUID(int uid)
        {
            if (m_containerContent.TryGetValue(uid, out var element)) return element;
            return null;
        }

        protected UnityEngine.Object GetDataContainerElementAtIndex(int index)
        {
            var entry = GetEntryAtIndex(index);
            if (entry != null && entry.data is int uid && uid > 0)
            {
                return GetDataContainerElementByUID(uid);
            }
            return null;
        }
        protected UnityEngine.Object GetContainerCurrentSelection()
        {
            return GetDataContainerElementAtIndex(ContainerSelectionIndex);
        }        

        protected void ForceContainerContentRefresh() { m_container.Editor_ShouldRecomputeContainerContent(); }

        #endregion

        #region Content List Navigation

        protected void TrySelectUp()
        {
            if (CanSelectUp())
            {
                SetSelectionIndex(GetFirstValidEntryIndexUp(), true);
            }
        }
        protected void TrySelectDown()
        {
            if (CanSelectDown())
            {
                SetSelectionIndex(GetFirstValidEntryIndexDown(), true);
            }
        }

        protected bool CanSelectUp() => ContainerSelectionIndex > 0;
        protected bool CanSelectDown() => ContainerSelectionIndex < ContentListCount - 1;

        #endregion

        #region Selection

        protected void SetSelectionIndex(int index, bool ensureVisibility)
        {
            var entry = GetEntryAtIndex(ContainerSelectionIndex);
            if (entry != null)
            {
                OnDeselectContentListElement(entry);
            }

            ContainerSelectionIndex = index;

            entry = GetEntryAtIndex(ContainerSelectionIndex);
            if (entry != null)
            {
                OnSelectContentListElement(entry, ContainerSelectionIndex, ensureVisibility);
            }
        }

        protected bool Select(UnityEngine.Object obj)
        {
            for (int i = 0; i < ContentListCount; i++)
            {
                if (GetDataContainerElementAtIndex(i) == obj)
                {
                    SetSelectionIndex(i, true);
                    return true;
                }
            }
            return false;
        }

        protected virtual void OnSelectContentListElement(FolderStructureEntry entry, int index, bool ensureVisibility) 
        {
            GUI.FocusControl(null);
            if (ensureVisibility)
            {
                m_folderStructure.EnsureVisibilityOfEntryAtIndex(index);
                if (ContentListNeedsScrollRect)
                {
                    int visibleIndex = GetVisibleIndex(index);

                    float elementYPos = visibleIndex * ContentListElementHeight;
                    if (elementYPos < ContentListWindowScrollPos.y)
                    {
                        ContentListWindowScrollPos = new(ContentListWindowScrollPos.x, elementYPos);
                    }
                    else
                    {
                        elementYPos += ContentListElementHeight;
                        if (elementYPos > ContentListWindowScrollPos.y + ContentListRect.height)
                        {
                            ContentListWindowScrollPos = new(ContentListWindowScrollPos.x, elementYPos - ContentListRect.height);
                        }
                    }
                }
            }
        }
        protected virtual void OnDeselectContentListElement(FolderStructureEntry entry) { }

        #endregion


        #region Data Renaming

        protected void BeginRenaming(UnityEngine.Object obj, int index)
        {
            RenamingElement = obj;
            RenamingString = obj.name;
            RenamingElementIndex = index;
            IsRenamingElement = true;
            m_justStartedRename = true;
        }
        private bool CompleteRenaming()
        {
            if (IsRenamingElement)
            {
                OnCompleteRenaming(RenamingElement, RenamingElementIndex);
                IsRenamingElement = false;
                RenamingElement = null;
                GUI.changed = true;
                return true;
            }
            return false;
        }
        protected virtual bool OnCompleteRenaming(UnityEngine.Object obj, int index)
        {
            if (obj.name != RenamingString)
            {
                Database.RenameAsset(obj, RenamingString);
                GetEntryAtIndex(index).content = RenamingString;
                return true;
            }
            return false;
        }

        #endregion

        #region Decoratives

        protected void Separator(float height, Color color)
        {
            var rect = EditorGUILayout.GetControlRect(false, height);
            rect.x = 0f; rect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(rect, color);
        }

        #endregion

        #region Drag & Drop

        protected virtual void OnEventReceived_DragUpdated(Event e)
        {
            if (ContentListRect.Contains(e.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                e.Use();
            }
        }
        protected virtual void OnEventReceived_DragPerformed(Event e)
        {
            if (ContentListRect.Contains(e.mousePosition))
            {
                for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    if (m_container.Editor_IsElementValid(DragAndDrop.objectReferences[i]))
                    {
                        OnAddNewDataToContainer(DragAndDrop.objectReferences[i]);
                    }
                    else
                    {
                        Debug.LogWarning(DragAndDrop.objectReferences[i] + " is not a valid element for " + m_container.name);
                    }
                }
                e.Use();
            }
        }

        #endregion

        #region Context Menu

        protected void ShowContainerElementContextMenu(int index)
        {
            var menu = new GenericMenu();
            PopulateContainerElementContextMenu(index, menu);
            menu.ShowAsContext();
        }
        protected void PopulateContainerElementContextMenu(int index, GenericMenu menu)
        {
            var obj = GetDataContainerElementAtIndex(index);

            if (obj != null)
            {
                PopulateContainerDataContextMenu(obj, index, menu);
            }
            else if (GetEntryAtIndex(index) is FolderStructureGroupEntry group)
            {
                PopulateGroupContextMenu(group, index, menu);
            }
        }
        protected virtual void PopulateContainerDataContextMenu(UnityEngine.Object obj, int index, GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Ping"), false, () => EditorUtils.FullPingObject(obj));
            menu.AddItem(new GUIContent("Open Asset"), false, () => { if (AssetDatabase.CanOpenAssetInEditor(obj.GetInstanceID())) AssetDatabase.OpenAsset(obj); });
            menu.AddItem(new GUIContent("Remove"), false, () => OnTryDeleteElementAtIndex(index));
        }
        protected virtual void PopulateGroupContextMenu(FolderStructureGroupEntry group, int index, GenericMenu menu)
        {
            menu.AddDisabledItem(new GUIContent("Nothing yet"));
        }

        // --- GUI ---
        protected virtual void OnContentListElementContextButtonGUI(Rect rect, int index, bool selected)
        {
            bool hover = false;
            if (IsContainerContentListInteractable()
                && rect.Contains(CurrentEvent.mousePosition))
            {
                hover = true;
                if (EventReceived
                    && CurrentEvent.type == EventType.MouseDown)
                {
                    ShowContainerElementContextMenu(index);
                    GUI.changed = true;
                    UseCurrentEvent();
                }
            }
            if (CurrentEvent.type == EventType.Repaint)
            {
                GUIHelper.simpleIconButton.Draw(rect, EditorGUIHelper.MenuIcon, 0, false, hover);
            }
        }

        #endregion


        #region Content List GUI

        protected virtual bool IsContainerContentListInteractable()
        {
            return !IsRenamingElement;
        }

        protected virtual void OnContainerContentListWindowGUI(Rect rect, string listName)
        {
            float screenWidth = EditorGUIUtility.currentViewWidth;
            rect.x = 0f; rect.width = screenWidth;

            // --- TOOLBAR ---
            float toolbarHeight = 21f;
            Rect toolbarRect = new Rect(0f, rect.y, screenWidth, toolbarHeight);
            // Background
            GUI.Box(toolbarRect, GUIContent.none, EditorStyles.toolbar);
            // Label
            EditorGUI.LabelField(new Rect(toolbarRect.x + 10f, toolbarRect.y, toolbarRect.width - 10f, toolbarHeight), listName);
            // Buttons
            int buttonsCount = 2;
            float buttonsWidth = 40f;
            Rect addButtonRect = new Rect(toolbarRect.width - buttonsWidth, toolbarRect.y, buttonsWidth, toolbarHeight);
            EditorGUI.BeginDisabledGroup(!EnableAddButton());
            if (GUI.Button(addButtonRect, DataType.IsAbstract ? EditorGUIHelper.AddMoreIcon : EditorGUIHelper.AddIcon, EditorStyles.toolbarButton))
            {
                if (DataType.IsAbstract)
                {
                    TryCreateDataFromAbstractType();
                }
                else
                {
                    CreateNewDataOfType(DataType);
                }
            }
            EditorGUI.EndDisabledGroup();
            Rect refreshButtonRect = new Rect(toolbarRect.width - buttonsWidth * buttonsCount, toolbarRect.y, buttonsWidth, toolbarHeight);
            EditorGUI.BeginDisabledGroup(!EnableRefreshButton());
            if (GUI.Button(refreshButtonRect, EditorGUIHelper.RefreshIcon, EditorStyles.toolbarButton))
            {
                ForceContainerContentRefresh();
            }
            EditorGUI.EndDisabledGroup();
            // Search Bar
            Rect searchFieldRect = new Rect(toolbarRect.x + (toolbarRect.width / 3), toolbarRect.y + 2f, (toolbarRect.width * 2 / 3) - (buttonsWidth * buttonsCount) - 2f, toolbarHeight - 2f);
            ContentListSearchString = EditorGUI.TextField(searchFieldRect, ContentListSearchString, EditorStyles.toolbarSearchField);

            // --- CONTENT LIST ---
            ContentListRect = new Rect(rect.x, rect.y + toolbarHeight, rect.width, rect.height - 7f - toolbarHeight);
            EditorGUI.DrawRect(ContentListRect, GUIHelper.transparentBlack01);

            var validIndexes = GetValidEntriesIndexes().ToList();
            int visibleContentListCount = validIndexes.Count;
            ContentListNeedsScrollRect = visibleContentListCount * ContentListElementHeight > ContentListRect.height;

            Rect dataRect;
            if (ContentListNeedsScrollRect)
            {
                Rect viewRect = new Rect(0, 0, ContentListRect.width - 15f, visibleContentListCount * ContentListElementHeight);
                ContentListWindowScrollPos = GUI.BeginScrollView(ContentListRect, ContentListWindowScrollPos, viewRect);
                dataRect = new Rect(0, 0, viewRect.width, ContentListElementHeight);
            }
            else
            {
                dataRect = new Rect(ContentListRect.x, ContentListRect.y, ContentListRect.width, ContentListElementHeight);
            }

            int visibleIndex = 0;
            foreach (var index in validIndexes)
            {
                OnContentListElementGUI(dataRect, index, visibleIndex, ShowContextButtons());
                dataRect.y += ContentListElementHeight;
                visibleIndex++;
            }

            if (ContentListNeedsScrollRect)
            {
                GUI.EndScrollView();
            }

            ContentListResize(new Rect(rect.x, rect.y + rect.height - 7f, rect.width, 7f));
        }

        protected virtual bool EnableRefreshButton() => true;
        protected virtual bool EnableAddButton() => true;
        protected virtual bool ShowContextButtons() => true;

        #endregion

        #region Content List Resize

        protected void ContentListResize(Rect rect)
        {
            rect.x = 0f;
            rect.width = EditorGUIUtility.currentViewWidth;
            EditorGUI.DrawRect(rect, GUIHelper.grey01);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 1, rect.width, 1f), Color.white);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - 2, rect.width, 1f), Color.white);

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
            if (!IsResizingContentList
                && rect.Contains(Event.current.mousePosition)
                && Event.current.type == EventType.MouseDown)
            {
                IsResizingContentList = true;
                Event.current.Use();
            }
            else if (IsResizingContentList
                && Event.current.type == EventType.MouseDrag)
            {
                ContentListRectHeight = Mathf.Clamp(ContentListRectHeight + Event.current.delta.y, 100f, 500f);
                Event.current.Use();
            }
        }

        #endregion

        #region Content List Elements GUI

        protected virtual void OnContentListElementGUI(Rect rect, int index, int visibleIndex, bool contextButton)
        {
            FolderStructureEntry entry = GetEntryAtIndex(index);
            bool selected = ContainerSelectionIndex == index;

            float alinea = ContentListFiltered ? 0f : ContentListElementAlinea * entry.level;
            Rect elementRect = new Rect(rect.x + alinea, rect.y, rect.width - alinea - (contextButton ? ContentListElementContextButtonWidth : 0f), rect.height);
            Rect buttonRect = GetButtonRectForContentListElement(rect, index, entry, contextButton);
            bool isHovered = buttonRect.Contains(CurrentEvent.mousePosition);
            bool clicked = false, doubleClicked = false, contextClicked = false;
            if (IsContainerContentListInteractable() && isHovered && EventReceived)
            {
                switch (CurrentEvent.type)
                {
                    case EventType.MouseDown:
                        if (CurrentEvent.button == 0)
                        {
                            clicked = true;
                            doubleClicked = EditorApplication.timeSinceStartup <= m_lastSelectionTime + m_doubleClickDelay;
                            GUI.FocusControl(null);
                            UseCurrentEvent();
                            GUI.changed = true;
                        }
                        break;
                    case EventType.ContextClick:
                        contextClicked = true;
                        UseCurrentEvent();
                        GUI.changed = true;
                        break;
                }
            }

            // Background
            OnContentListElementBackgroundGUI(rect, visibleIndex, selected);
            
            // Element GUI
            if (entry is FolderStructureGroupEntry group)
            {
                OnContentListFolderGUI(rect, elementRect, index, group, selected, clicked, doubleClicked, contextClicked);
            }
            else
            {
                var element = GetDataContainerElementAtIndex(index);
                OnContentListDataGUI(rect, elementRect, index, entry, contextButton, element, selected, clicked, doubleClicked, contextClicked);
            }

            // Context Button
            if (contextButton)
            {
                Rect contextButtonRect = new Rect(rect.x + rect.width - ContentListElementContextButtonWidth, rect.y, ContentListElementContextButtonWidth, rect.height);
                OnContentListElementContextButtonGUI(contextButtonRect, index, selected);
            }
        }
        protected virtual Rect GetButtonRectForContentListElement(Rect rect, int index, FolderStructureEntry entry, bool contextButton)
        {
            return new Rect(rect.x, rect.y, rect.width - (contextButton ? ContentListElementContextButtonWidth : 0f), rect.height);
        }

        #region Folder

        protected virtual void OnContentListFolderGUI(Rect totalRect, Rect elementRect, int index, FolderStructureGroupEntry group,
            bool selected, bool clicked, bool doubleClicked, bool contextClicked)
        {
            if (clicked)
            {
                group.open = !group.open;
            }
            else if (contextClicked)
            {
                ShowContainerElementContextMenu(index);
            }
            if (Event.current.type == EventType.Repaint)
            {
                GUIContent content = new GUIContent(group.content);
                EditorStyles.foldout.Draw(elementRect, content, 0, group.open);
            }
        }

        #endregion

        protected virtual void OnContentListDataGUI(Rect totalRect, Rect elementRect, int index, FolderStructureEntry entry, bool contextButton, 
            UnityEngine.Object element, bool selected, bool clicked, bool doubleClicked, bool contextClicked)
        {
            if (clicked)
            {
                m_lastSelectionTime = EditorApplication.timeSinceStartup;
                if (doubleClicked)
                {
                    // Double click
                    SetSelectionIndex(index, false);
                    selected = true;
                    EditorUtils.FullPingObject(element);
                }
                else if (selected)
                {
                    SetSelectionIndex(-1, false);
                    selected = false;
                }
                else
                {
                    SetSelectionIndex(index, false);
                    selected = true;
                }
            }
            else if (contextClicked)
            {
                ShowContainerElementContextMenu(index);
            }

            if (element == null)
            {
                OnContentListNullElementGUI(elementRect, entry, index, selected);
                return;
            }
            switch (element)
            {
                case GameObject go:
                    OnContentListGameObjectElementGUI(elementRect, entry, index, selected, go); break;
                case ScriptableObject so:
                    OnContentListScriptableObjectElementGUI(elementRect, entry, index, selected, so); break;
                default:
                    OnContentListOtherObjectElementGUI(elementRect, entry, index, selected, element); break;
            }
        }

        protected virtual void OnContentListElementBackgroundGUI(Rect rect, int visibleIndex, bool selected)
        {
            EditorGUI.DrawRect(rect, selected ? GUIHelper.transparentWhite01 : (visibleIndex % 2 == 0 ? GUIHelper.transparentBlack02 : GUIHelper.transparentBlack04));
        }

        protected virtual void OnContentListNullElementGUI(Rect rect, FolderStructureEntry entry, int index, bool selected)
        {
            ForceContainerContentRefresh();
        }
        protected virtual void OnContentListGameObjectElementGUI(Rect rect, FolderStructureEntry entry, int index, bool selected, GameObject element)
        {
            Texture2D elementTexture;
            if (element.TryGetComponent(out IDataContainerTexturableElement texturableElem)) elementTexture = texturableElem.DataTexture;
            else elementTexture = AssetPreview.GetAssetPreview(element);
            if (elementTexture == null) elementTexture = AssetPreview.GetMiniThumbnail(element);

            OnContentListElementWithNameAndTextureGUI(rect, index, selected, element, entry.content, elementTexture);
        }
        protected virtual void OnContentListScriptableObjectElementGUI(Rect rect, FolderStructureEntry entry, int index, bool selected, ScriptableObject element)
        {
            Texture2D elementTexture;
            if (element is IDataContainerTexturableElement texturableElem) elementTexture = texturableElem.DataTexture;
            else elementTexture = AssetPreview.GetAssetPreview(element);
            if (elementTexture == null) elementTexture = AssetPreview.GetMiniThumbnail(element);

            OnContentListElementWithNameAndTextureGUI(rect, index, selected, element, entry.content, elementTexture);
        }
        protected virtual void OnContentListOtherObjectElementGUI(Rect rect, FolderStructureEntry entry, int index, bool selected, UnityEngine.Object obj)
        {
            OnContentListElementWithNameAndTextureGUI(rect, index, selected, obj, entry.content, AssetPreview.GetMiniThumbnail(obj));
        }

        protected virtual void OnContentListElementWithNameAndTextureGUI(Rect rect, int index, bool selected, UnityEngine.Object obj, string name, Texture2D texture)
        {
            bool hasTexture = texture != null;
            if (hasTexture)
            {
                Rect textureRect = new Rect(rect.x + 5f, rect.y + 1f, rect.height - 2f, rect.height - 2f);
                GUI.DrawTexture(textureRect, texture, ScaleMode.ScaleToFit);
            }

            Rect labelRect = new Rect(rect.x + 5f + (hasTexture ? 5f + rect.height : 0f), rect.y, rect.width - 5f - (hasTexture ? 5f + rect.height : 0f), rect.height);
            OnContentListElementNameGUI(labelRect, index, selected, obj, name);
        }
        protected virtual void OnContentListElementNameGUI(Rect rect, int index, bool selected, UnityEngine.Object obj, string name)
        {
            if (IsRenamingElement && RenamingElement == obj)
            {
                GUI.SetNextControlName(RenamingControlName);

                EditorGUI.BeginChangeCheck();
                RenamingString = EditorGUI.DelayedTextField(rect, RenamingString);
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
                EditorGUI.LabelField(rect, name, selected ? EditorStyles.boldLabel : EditorStyles.label);
            }
        }

        #endregion

        #region Database Element Display

        protected bool DisplayContainerCurrentSelection()
        {
            if (ContainerSelectionIndex != -1)
            {
                ElementDisplayScrollPos = EditorGUILayout.BeginScrollView(ElementDisplayScrollPos);

                DisplayContainerElement(GetContainerCurrentSelection());

                EditorGUILayout.EndScrollView();
                return true;
            }
            return false;
        }
        protected virtual void DisplayContainerElement(UnityEngine.Object element)
        {
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

        protected void TryCreateDataFromAbstractType()
        {
            var menu = new GenericMenu();

            foreach (var type in ChildDataTypes)
            {
                if (!type.IsAbstract)
                    menu.AddItem(new GUIContent(type.Name), false, OnTryCreateDataFromAbstractType, type);
            }

            menu.ShowAsContext();
        }
        private void OnTryCreateDataFromAbstractType(object obj)
        {
            if (obj is Type type)
            {
                CreateNewDataOfType(type);
            }
        }

        protected bool CreateNewDataOfType(Type type)
        {
            if (m_container.Editor_IsTypeValidForContainer(type) 
                && OnCreateNewDataOfType(type, out var obj))
            {
                m_container.Editor_OnNewElementCreated(obj);
                OnAddNewDataToContainer(obj);
                return true;
            }
            return false;
        }
        protected abstract bool OnCreateNewDataOfType(Type type, out UnityEngine.Object obj);

        protected virtual GameObject OnCreateNewEmptyPrefab(string path)
        {
            return Database.CreateEmptyPrefab(path);
        }
        protected virtual Component OnCreateNewPrefabWithComponent(string path, Type componentType)
        {
            return Database.CreatePrefabWithComponent(componentType, path);
        }
        protected virtual ScriptableObject OnCreateNewScriptableObject(string path, Type scriptableType)
        {
            return Database.CreateScriptableAsset(scriptableType, path);
        }

        protected virtual void OnAddNewDataToContainer(UnityEngine.Object obj)
        {
            m_container.Editor_OnAddingNewElement(obj);
            ForceContainerContentRefresh();
            ContentListWindowScrollPos = Vector2.zero;
            var root = GetRootElement(obj);
            if (Select(root))
            {
                BeginRenaming(root, ContainerSelectionIndex);
            }
        }

        #endregion

        #region Data Deletion

        protected bool TryDeleteCurrentSelection()
        {
            if (ContainerSelectionIndex != -1)
            {
                OnTryDeleteElementAtIndex(ContainerSelectionIndex);
                return true;
            }
            return false;
        }
        protected virtual void OnTryDeleteElementAtIndex(int index)
        {
            var entry = GetEntryAtIndex(index);
            if (entry != null)
            {
                if (entry is FolderStructureGroupEntry)
                {

                }
                else if (entry.data is int uid)
                {
                    m_container.Editor_DeleteElementByUID(uid);
                    OnDeletedElementWithUID(uid);
                }
            }
        }
        protected virtual void OnDeletedElementWithUID(int uid) { }

        #endregion

        #region Utility

        protected UnityEngine.Object GetRootElement(UnityEngine.Object obj)
        {
            if (obj is Component component)
            {
                return component.transform.root.gameObject;
            }
            return obj;
        }

        #endregion

        #endregion
    }

#endif

    #endregion
}
