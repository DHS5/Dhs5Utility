using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        [SerializeField] private bool m_showLogs;
        [SerializeField] private bool m_showWarnings;
        [SerializeField] private bool m_showErrors;

        #endregion

        #region Properties

        public Color Color => m_color;
        public int Level
        {
            get => m_level;
            set => m_level = value;
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

            m_excludedProperties = new();
            m_excludedProperties.Add(p_script.propertyPath);
            m_excludedProperties.Add(p_showLogs.propertyPath);
            m_excludedProperties.Add(p_showWarnings.propertyPath);
            m_excludedProperties.Add(p_showErrors.propertyPath);
        }

        #endregion

        #region GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

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

            EditorGUILayout.EndHorizontal();

            DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif

    #endregion
}
