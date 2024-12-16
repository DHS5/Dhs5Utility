using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.GUIs;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Databases
{
    public abstract class BaseDataContainer : ScriptableObject
    {
        #region Accessors

        public abstract bool TryGetObjectByUID(int uid, out IDataContainerElement obj);

        #endregion

#if UNITY_EDITOR

        #region Instance Content Management

        internal abstract bool Editor_ContainerHasValidDataType(out Type dataType);
        internal abstract bool Editor_IsElementValid(UnityEngine.Object element);

        protected abstract IEnumerable<UnityEngine.Object> Editor_GetContainerContent();
        protected IEnumerable<T> Editor_GetContainerElements<T>() where T : class, IDataContainerElement
        {
            foreach (var obj in Editor_GetContainerContent())
            {
                yield return obj as T;
            }
        }

        internal bool Editor_DeleteElementAtIndex(int index)
        {
            if (Editor_OnDeleteElementAtIndex(index))
            {
                Editor_ContainerContentChanged?.Invoke();
                return true;
            }
            return false;
        }
        protected abstract bool Editor_OnDeleteElementAtIndex(int index);

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

            foreach (var elem in Editor_GetContainerElements<IDataContainerElement>())
            {
                // Name
                if (elem is IDataContainerNameableElement nameableElem) names.Add(nameableElem.DataDisplayName);
                else names.Add(elem.name);
                // UID
                uids.Add(elem.UID);
            }

            return (names.ToArray(), uids.ToArray());
        }

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
    }

    public abstract class DataContainer : BaseDataContainer
    {
        #region Instance Editor Methods

#if UNITY_EDITOR

        internal override bool Editor_ContainerHasValidDataType(out Type dataType)
        {
            return HasDataType(GetType(), out dataType) &&
                typeof(IDataContainerElement).IsAssignableFrom(dataType);
        }

#endif

        #endregion

        #region Static Editor Methods

#if UNITY_EDITOR

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

        #endregion

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    public abstract class BaseDataContainerEditor : Editor
    {
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

        // Events
        protected Event CurrentEvent { get; private set; }
        protected bool EventReceived { get; private set; }

        // Database Informations
        protected bool DatabaseInformationsFoldoutOpen { get; set; }

        // Content List Window
        protected abstract int ContentListCount { get; }
        protected int ContainerSelectionIndex { get; set; } = -1;
        protected Rect ContentListRect { get; set; }
        protected Vector2 ContentListWindowScrollPos { get; set; }
        protected float ContentListElementHeight { get; set; } = 20f;
        protected float ContentListElementContextButtonWidth { get; set; } = 30f;
        protected float ContentListButtonsHeight { get; set; } = 20f;
        private double m_lastSelectionTime;
        private double m_doubleClickDelay = 0.2d;

        // Element Display
        protected Vector2 ElementDisplayScrollPos { get; set; }

        // Renaming
        protected bool IsRenamingElement { get; set; }
        protected string RenamingString { get; set; }
        protected UnityEngine.Object RenamingElement { get; set; }
        private bool m_justStartedRename;

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_container = (BaseDataContainer)target;
            ContainerHasValidDataType = m_container.Editor_ContainerHasValidDataType(out var dataType);
            if (ContainerHasValidDataType) DataType = dataType;

            m_container.Editor_ContainerContentChanged += OnContainerContentChanged;

            p_script = serializedObject.FindProperty("m_Script");

            m_excludedProperties = new()
            {
                p_script.propertyPath,
            };
        }
        protected virtual void OnDisable()
        {
            ClearEditors();

            m_container.Editor_ContainerContentChanged -= OnContainerContentChanged;
        }

        #endregion

        #region Callbacks

        protected virtual void OnContainerContentChanged()
        {
            serializedObject.Update();
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
                    BeginRenaming(GetContainerCurrentSelection());
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
            var obj = GetContainerCurrentSelection();
            if (obj != null)
            {
                EditorUtils.FullPingObject(obj);
                UseCurrentEvent();
            }
        }


        protected virtual void OnEventReceived_Up(Event e)
        {
            if (CanSelectUp())
            {
                ContainerSelectionIndex--;
                UseCurrentEvent();
            }
        }
        protected virtual void OnEventReceived_Down(Event e)
        {
            if (CanSelectDown())
            {
                ContainerSelectionIndex++;
                UseCurrentEvent();
            }
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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DatabaseInformationsFoldoutOpen = EditorGUIHelper.Foldout(EditorGUILayout.GetControlRect(false, 20f), title, DatabaseInformationsFoldoutOpen);
            if (DatabaseInformationsFoldoutOpen)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5f);
                OnContainerInformationsContentGUI();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
        protected virtual void OnContainerInformationsContentGUI() { }

        #endregion

        #region Container Content

        protected bool CanSelectUp() => ContainerSelectionIndex > 0;
        protected bool CanSelectDown() => ContainerSelectionIndex < ContentListCount - 1;

        protected UnityEngine.Object GetContainerCurrentSelection()
        {
            return GetContainerElementAtIndex(ContainerSelectionIndex);
        }
        protected virtual UnityEngine.Object GetContainerElementAtIndex(int index)
        {
            throw new NotImplementedException();
        }

        protected void ForceContainerContentRefresh() { m_container.Editor_ShouldRecomputeContainerContent(); }

        #endregion

        #region Selection

        protected bool Select(UnityEngine.Object obj)
        {
            for (int i = 0; i < ContentListCount; i++)
            {
                if (GetContainerElementAtIndex(i) == obj)
                {
                    ContainerSelectionIndex = i;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Data Renaming

        protected void BeginRenaming(UnityEngine.Object obj)
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
                GUI.changed = true;
                return true;
            }
            return false;
        }
        protected virtual void OnCompleteRenaming(UnityEngine.Object obj)
        {
            BaseDatabase.RenameAsset(obj, RenamingString);
            ForceContainerContentRefresh();
        }

        #endregion

        #region Decoratives

        protected void Separator(float height, Color color)
        {
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, height), color);
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
        protected virtual void PopulateContainerElementContextMenu(int index, GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Ping"), false, () => EditorUtils.FullPingObject(GetContainerElementAtIndex(index)));
            menu.AddItem(new GUIContent("Remove"), false, () => OnTryDeleteElementAtIndex(index));
        }

        #endregion


        #region Database Content List

        protected virtual bool IsContainerContentListInteractable()
        {
            return !IsRenamingElement;
        }

        protected virtual void OnContainerContentListWindowGUI(Rect rect, bool refreshButton = false, bool addButton = false, bool contextButtons = false)
        {
            bool hasAtLeastOneButton = (refreshButton || addButton);
            ContentListRect = new Rect(rect.x, rect.y, rect.width, rect.height - (hasAtLeastOneButton ? ContentListButtonsHeight : 0f));
            EditorGUI.DrawRect(ContentListRect, GUIHelper.transparentBlack01);

            bool needScrollRect = ContentListCount * ContentListElementHeight > ContentListRect.height;
            if (needScrollRect)
            {
                Rect viewRect = new Rect(0, 0, ContentListRect.width - 15f, ContentListCount * ContentListElementHeight);
                ContentListWindowScrollPos = GUI.BeginScrollView(ContentListRect, ContentListWindowScrollPos, viewRect);

                Rect dataRect = new Rect(0, 0, viewRect.width, ContentListElementHeight);
                for (int i = 0; i < ContentListCount; i++)
                {
                    OnContentListElementGUI(dataRect, i, GetContainerElementAtIndex(i), contextButtons);
                    dataRect.y += ContentListElementHeight;
                }

                GUI.EndScrollView();
            }
            else
            {
                Rect dataRect = new Rect(ContentListRect.x, ContentListRect.y, ContentListRect.width, ContentListElementHeight);
                for (int i = 0; i < ContentListCount; i++)
                {
                    OnContentListElementGUI(dataRect, i, GetContainerElementAtIndex(i), contextButtons);
                    dataRect.y += ContentListElementHeight;
                }
            }

            if (hasAtLeastOneButton)
            {
                EditorGUI.BeginDisabledGroup(!IsContainerContentListInteractable());
                if (refreshButton)
                {
                    // Refresh Button
                    Rect refreshButtonRect = new Rect(rect.x, rect.y + rect.height - ContentListButtonsHeight, rect.width * (addButton ? 0.5f : 1f), ContentListButtonsHeight);
                    if (GUI.Button(refreshButtonRect, new GUIContent(EditorGUIHelper.RefreshIcon) { text = "Refresh" }))
                    {
                        ForceContainerContentRefresh();
                    }
                }
                if (addButton)
                {
                    // Add Button
                    Rect addButtonRect = new Rect(rect.x + (refreshButton ? rect.width * 0.5f : 0f), rect.y + rect.height - ContentListButtonsHeight, rect.width * (refreshButton ? 0.5f : 1f), ContentListButtonsHeight);
                    if (GUI.Button(addButtonRect, new GUIContent(EditorGUIHelper.AddIcon) { text = "Add" }))
                    {
                        CreateNewData();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        protected virtual Rect GetButtonRectForContentListElement(Rect rect, int index, UnityEngine.Object element, bool contextButton)
        {
            return new Rect(rect.x, rect.y, rect.width - (contextButton ? ContentListElementContextButtonWidth : 0f), rect.height);
        }
        protected virtual void OnContentListElementGUI(Rect rect, int index, UnityEngine.Object element, bool contextButton)
        {
            bool selected = ContainerSelectionIndex == index;

            Rect elementRect = new Rect(rect.x, rect.y, rect.width - (contextButton ? ContentListElementContextButtonWidth : 0f), rect.height);
            Rect buttonRect = GetButtonRectForContentListElement(rect, index, element, contextButton);
            bool isHovered = buttonRect.Contains(CurrentEvent.mousePosition);
            bool clicked = false, doubleClicked = false, contextClicked = false;
            if (isHovered && EventReceived)
            {
                switch (CurrentEvent.type)
                {
                    case EventType.MouseDown:
                        if (CurrentEvent.button == 0)
                        {
                            clicked = true;
                            doubleClicked = EditorApplication.timeSinceStartup <= m_lastSelectionTime + m_doubleClickDelay;
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

            if (IsContainerContentListInteractable())
            {
                if (clicked)
                {
                    m_lastSelectionTime = EditorApplication.timeSinceStartup;
                    if (doubleClicked)
                    {
                        // Double click
                        Select();
                        EditorUtils.FullPingObject(element);
                    }
                    else if (selected)
                    {
                        Deselect();
                    }
                    else
                    {
                        Select();
                    }
                }
                else if (contextClicked)
                {
                    ShowContainerElementContextMenu(index);
                }
            }

            OnContentListElementBackgroundGUI(rect, index, selected, element);

            if (element == null)
            {
                OnContentListNullElementGUI(elementRect, index, selected);
                return;
            }
            switch (element)
            {
                case GameObject go:
                    OnContentListGameObjectElementGUI(elementRect, index, selected, go); break;
                case ScriptableObject so:
                    OnContentListScriptableObjectElementGUI(elementRect, index, selected, so); break;
                default:
                    OnContentListOtherObjectElementGUI(elementRect, index, selected, element); break;
            }

            if (contextButton)
            {
                EditorGUI.BeginDisabledGroup(!IsContainerContentListInteractable());
                Rect contextButtonRect = new Rect(rect.x + rect.width - ContentListElementContextButtonWidth, rect.y, ContentListElementContextButtonWidth, rect.height);
                OnContentListElementContextButtonGUI(contextButtonRect, index, selected, element);
                EditorGUI.EndDisabledGroup();
            }

            void Select()
            {
                ContainerSelectionIndex = index;
                OnSelectContentListElement(element);
                selected = true;
            }
            void Deselect()
            {
                ContainerSelectionIndex = -1;
                OnDeselectContentListElement(element);
                selected = false;
            }
        }

        protected virtual void OnContentListElementBackgroundGUI(Rect rect, int index, bool selected, UnityEngine.Object element)
        {
            EditorGUI.DrawRect(rect, selected ? GUIHelper.transparentWhite01 : (index % 2 == 0 ? GUIHelper.transparentBlack02 : GUIHelper.transparentBlack04));
        }

        protected virtual void OnContentListNullElementGUI(Rect rect, int index, bool selected)
        {
            ForceContainerContentRefresh();
        }
        protected virtual void OnContentListGameObjectElementGUI(Rect rect, int index, bool selected, GameObject element)
        {
            string elementName;
            if (element.TryGetComponent(out IDataContainerNameableElement nameableElem)) elementName = nameableElem.DataDisplayName;
            else elementName = element.name;

            Texture2D elementTexture;
            if (element.TryGetComponent(out IDataContainerTexturableElement texturableElem)) elementTexture = texturableElem.DataTexture;
            else elementTexture = AssetPreview.GetAssetPreview(element);
            if (elementTexture == null) elementTexture = AssetPreview.GetMiniThumbnail(element);

            OnContentListElementWithNameAndTextureGUI(rect, index, selected, element, elementName, elementTexture);
        }
        protected virtual void OnContentListScriptableObjectElementGUI(Rect rect, int index, bool selected, ScriptableObject element)
        {
            string elementName;
            if (element is IDataContainerNameableElement nameableElem) elementName = nameableElem.DataDisplayName;
            else elementName = element.name;

            Texture2D elementTexture;
            if (element is IDataContainerTexturableElement texturableElem) elementTexture = texturableElem.DataTexture;
            else elementTexture = AssetPreview.GetAssetPreview(element);
            if (elementTexture == null) elementTexture = AssetPreview.GetMiniThumbnail(element);

            OnContentListElementWithNameAndTextureGUI(rect, index, selected, element, elementName, elementTexture);
        }
        protected virtual void OnContentListOtherObjectElementGUI(Rect rect, int index, bool selected, UnityEngine.Object obj)
        {
            OnContentListElementWithNameAndTextureGUI(rect, index, selected, obj, obj.name, AssetPreview.GetMiniThumbnail(obj));
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

        protected virtual void OnContentListElementContextButtonGUI(Rect rect, int index, bool selected, UnityEngine.Object element)
        {
            bool hover = false;
            if (element != null
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


        protected virtual void OnSelectContentListElement(UnityEngine.Object element) { }
        protected virtual void OnDeselectContentListElement(UnityEngine.Object element) { }

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

        protected bool CreateNewData()
        {
            if (OnCreateNewData(out var obj))
            {
                m_container.Editor_OnNewElementCreated(obj);
                OnAddNewDataToContainer(obj);
                return true;
            }
            return false;
        }
        protected abstract bool OnCreateNewData(out UnityEngine.Object obj);

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

        protected virtual void OnAddNewDataToContainer(UnityEngine.Object obj)
        {
            m_container.Editor_OnAddingNewElement(obj);
            ForceContainerContentRefresh();
            ContentListWindowScrollPos = Vector2.zero;
            var root = GetRootElement(obj);
            if (Select(root))
            {
                BeginRenaming(root);
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
            m_container.Editor_DeleteElementAtIndex(index);
        }

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

    public abstract class DataContainerEditor : BaseDataContainerEditor
    {
        #region Base GUI

        protected override string ContainerInvalidDataTypeMessage()
        {
            return "The data type of this DataContainer is not valid.\n\n" +
                    "- Add the DataContainerAttribute to the top of your script.\n" +
                    "- Make sure the dataType parameter implements at least the IDataContainerElement interface.";
        }

        #endregion
    }

#endif

    #endregion
}
