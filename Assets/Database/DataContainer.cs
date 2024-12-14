using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dhs5.Utility.Databases
{
    public abstract class BaseDataContainer : ScriptableObject
    {
#if UNITY_EDITOR

        #region Instance Content Management

        internal abstract bool Editor_ContainerHasValidDataType();

        internal virtual IEnumerable<UnityEngine.Object> Editor_GetContainerContent()
        {
            yield return null;
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
            foreach (var obj in Editor_GetContainerContent())
            {
                if (obj is IDatabaseElement elem && elem.UID == 0)
                {
                    elem.Editor_SetUID(Editor_GenerateUID());
                }
            }

            Editor_ContainerContentChanged?.Invoke();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        /// <summary>
        /// Callback triggered when a new element has been created for the database<br></br>
        /// At this moment, the database content has already been recomputed so the element is already in it
        /// </summary>
        /// <param name="element"></param>
        internal virtual void Editor_OnNewElementCreated(UnityEngine.Object element) { }

        // --- UIDs ---
        protected int Editor_GenerateUID()
        {
            int max = 0;
            foreach (var obj in Editor_GetContainerContent())
            {
                if (obj is IDatabaseElement elem &&
                    elem.UID > max)
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

        #endregion

#endif
    }

    public abstract class DataContainer : BaseDataContainer
    {

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

        // Events
        protected Event CurrentEvent { get; private set; }
        protected bool EventReceived { get; private set; }
        protected Vector2 MousePosition { get; private set; }

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_container = (BaseDataContainer)target;
            ContainerHasValidDataType = m_container.Editor_ContainerHasValidDataType();

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
        protected abstract Editor CreateEditorFor(UnityEngine.Object element);

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
            MousePosition = CurrentEvent.mousePosition;
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

        protected abstract void OnEventReceived(Event e);

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
    }

    [CustomEditor(typeof(DataContainer), editorForChildClasses: true)]
    public class DataContainerEditor : BaseDataContainerEditor
    {
        #region Editors Handling

        protected override Editor CreateEditorFor(UnityEngine.Object element)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected override string ContainerInvalidDataTypeMessage()
        {
            return "The data type of this DataContainer is not valid.\n\n" +
                    "- Add the DatabaseAttribute to the top of your script.\n" +
                    "- Make sure the dataType parameter implements at least the IDatabaseElement interface.";
        }

        protected override void OnEventReceived(Event e)
        {
            throw new NotImplementedException();
        }
    }

#endif

    #endregion
}
