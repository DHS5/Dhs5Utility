using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Toolbars;


#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Debugger
{
    public class DebuggerDatabaseElement : ScriptableObject
    {
        #region Members

        [SerializeField] private Color m_color;
        [SerializeField, Range(-1, 2)] private int m_level;

        [SerializeField] private bool m_showLogs = true;
        [SerializeField] private bool m_showWarnings = true;
        [SerializeField] private bool m_showErrors = true;
        [SerializeField] private bool m_showOnScreen = true;

        #endregion

        #region Properties

        public Color Color => m_color;
        public int Level
        {
            get => m_level;
            set => m_level = value;
        }

        public bool ShowOnScreen => m_showOnScreen;

        #endregion

        #region Accessors

        public bool CanShow(LogType logType, int logLevel)
        {
            return logLevel <= Level && CanShow(logType);
        }
        private bool CanShow(LogType logType)
        {
            switch (logType)
            {
                case LogType.Log: return m_showLogs;
                case LogType.Warning: return m_showWarnings;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return m_showErrors;
                default: return false;
            }
        }

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(DebuggerDatabaseElement), editorForChildClasses:true)]
    public class DebuggerDatabaseElementEditor : Editor
    {
        #region Members

        protected DebuggerDatabaseElement m_element;

        protected SerializedProperty p_script;
        protected SerializedProperty p_color;
        protected SerializedProperty p_level;
        protected SerializedProperty p_showLogs;
        protected SerializedProperty p_showWarnings;
        protected SerializedProperty p_showErrors;
        protected SerializedProperty p_showOnScreen;

        protected List<string> m_excludedProperties;
        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_element = (DebuggerDatabaseElement)target;

            p_script = serializedObject.FindProperty("m_Script");
            p_color = serializedObject.FindProperty("m_color");
            p_level = serializedObject.FindProperty("m_level");
            p_showLogs = serializedObject.FindProperty("m_showLogs");
            p_showWarnings = serializedObject.FindProperty("m_showWarnings");
            p_showErrors = serializedObject.FindProperty("m_showErrors");
            p_showOnScreen = serializedObject.FindProperty("m_showOnScreen");

            m_excludedProperties = new();
            m_excludedProperties.Add(p_script.propertyPath);
            m_excludedProperties.Add(p_showLogs.propertyPath);
            m_excludedProperties.Add(p_showWarnings.propertyPath);
            m_excludedProperties.Add(p_showErrors.propertyPath);
            m_excludedProperties.Add(p_showOnScreen.propertyPath);
        }

        #endregion
        bool hey;
        #region GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Utility buttons
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Enable All", EditorStyles.toolbarButton))
                {
                    p_showLogs.boolValue = true;
                    p_showWarnings.boolValue = true;
                    p_showErrors.boolValue = true;
                    p_showOnScreen.boolValue = true;
                }
                if (GUILayout.Button("Disable All", EditorStyles.toolbarButton))
                {
                    p_showLogs.boolValue = false;
                    p_showWarnings.boolValue = false;
                    p_showErrors.boolValue = false;
                    p_showOnScreen.boolValue = false;
                }
                if (GUILayout.Button("Error Only", EditorStyles.toolbarButton))
                {
                    p_showLogs.boolValue = false;
                    p_showWarnings.boolValue = false;
                    p_showErrors.boolValue = true;
                    p_showOnScreen.boolValue = false;
                }

                EditorGUILayout.EndHorizontal();
            }
            // Log type buttons
            {
                /*
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(p_showLogs.boolValue ? EditorGUIHelper.ConsoleInfoIcon : EditorGUIHelper.ConsoleInfoInactiveIcon, EditorStyles.toolbarButton))
                {
                    p_showLogs.boolValue = !p_showLogs.boolValue;
                }
                if (GUILayout.Button(p_showWarnings.boolValue ? EditorGUIHelper.ConsoleWarningIcon : EditorGUIHelper.ConsoleWarningInactiveIcon, EditorStyles.toolbarButton))
                {
                    p_showWarnings.boolValue = !p_showWarnings.boolValue;
                }
                if (GUILayout.Button(p_showErrors.boolValue ? EditorGUIHelper.ConsoleErrorIcon : EditorGUIHelper.ConsoleErrorInactiveIcon, EditorStyles.toolbarButton))
                {
                    p_showErrors.boolValue = !p_showErrors.boolValue;
                }
                if (GUILayout.Button(p_showOnScreen.boolValue ? EditorGUIHelper.ScreenIcon : EditorGUIHelper.ScreenInactiveIcon, EditorStyles.toolbarButton))
                {
                    p_showOnScreen.boolValue = !p_showOnScreen.boolValue;
                }

                EditorGUILayout.EndHorizontal();
                */
            }

            {
                var rect = EditorGUILayout.GetControlRect(false, 20f);
                rect.x -= 2f;
                rect.width += 4f;
                rect.y -= 2f;

                float width = rect.width / 4;
                rect.width = width;

                p_showLogs.boolValue = EditorGUIHelper.ToolbarToggle(rect, p_showLogs.boolValue ? EditorGUIHelper.ConsoleInfoIcon : EditorGUIHelper.ConsoleInfoInactiveIcon, p_showLogs.boolValue);
                
                rect.x += width;
                p_showWarnings.boolValue = EditorGUIHelper.ToolbarToggle(rect, p_showWarnings.boolValue ? EditorGUIHelper.ConsoleWarningIcon : EditorGUIHelper.ConsoleWarningInactiveIcon, p_showWarnings.boolValue);

                rect.x += width;
                p_showErrors.boolValue = EditorGUIHelper.ToolbarToggle(rect, p_showErrors.boolValue ? EditorGUIHelper.ConsoleErrorIcon : EditorGUIHelper.ConsoleErrorInactiveIcon, p_showErrors.boolValue);

                rect.x += width;
                p_showOnScreen.boolValue = EditorGUIHelper.ToolbarToggle(rect, p_showOnScreen.boolValue ? EditorGUIHelper.ScreenIcon : EditorGUIHelper.ScreenInactiveIcon, p_showOnScreen.boolValue);
            }

            EditorGUILayout.Space(5f);

            DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif

    #endregion
}
