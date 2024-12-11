using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.Databases;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Debuggers
{
    public class DebuggerDatabaseElement : ScriptableObject, IEnumDatabaseElement
    {
        #region Members

        [SerializeField] private int m_enumIndex;

        [SerializeField] private Color m_color;
        [SerializeField] private string m_colorString;
        [SerializeField, Range(-1, BaseDebugger.MAX_DEBUGGER_LEVEL)] private int m_level;

        [SerializeField] private bool m_showLogs = true;
        [SerializeField] private bool m_showWarnings = true;
        [SerializeField] private bool m_showErrors = true;

        [SerializeField] private bool m_showInConsole = true;
        [SerializeField] private bool m_showOnScreen = true;

        #endregion

        #region Properties

        public Color Color
        {
            get => m_color;
            internal set => m_color = value;
        }
        public string ColorString => m_colorString;

        public bool Active => Level >= 0;
        public int Level
        {
            get => m_level;
            internal set => m_level = value;
        }

        public bool ShowInConsole => m_showInConsole;
        public bool ShowOnScreen => m_showOnScreen;

        #endregion

        #region Accessors

        public bool CanLog(LogType logType, int logLevel)
        {
            switch (logType)
            {
                case LogType.Log: return Active && m_showLogs && logLevel <= Level;
                case LogType.Warning: return Active && m_showWarnings && logLevel <= Level;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return m_showErrors;
                default: return false;
            }
        }

        #endregion

        #region IEnumDatabaseElement

        public int EnumIndex => m_enumIndex;

#if UNITY_EDITOR
        public void Editor_SetIndex(int index)
        {
            m_enumIndex = index;
        }
#endif

        public bool HasDatabaseElementName(out string name)
        {
            name = null;
            return false;
        }

        public bool HasDatabaseElementTexture(out Texture2D texture)
        {
            texture = null;
            return false;
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
        protected SerializedProperty p_enumIndex;
        protected SerializedProperty p_color;
        protected SerializedProperty p_colorString;
        protected SerializedProperty p_level;
        protected SerializedProperty p_showLogs;
        protected SerializedProperty p_showWarnings;
        protected SerializedProperty p_showErrors;
        protected SerializedProperty p_showInConsole;
        protected SerializedProperty p_showOnScreen;

        protected List<string> m_excludedProperties;


        protected bool m_testLogOpen;
        protected string m_testString;
        protected int m_testLevel;
        protected LogType m_testLogType;

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_element = (DebuggerDatabaseElement)target;

            p_script = serializedObject.FindProperty("m_Script");
            p_enumIndex = serializedObject.FindProperty("m_enumIndex");
            p_color = serializedObject.FindProperty("m_color");
            p_colorString = serializedObject.FindProperty("m_colorString");
            p_level = serializedObject.FindProperty("m_level");
            p_showLogs = serializedObject.FindProperty("m_showLogs");
            p_showWarnings = serializedObject.FindProperty("m_showWarnings");
            p_showErrors = serializedObject.FindProperty("m_showErrors");
            p_showInConsole = serializedObject.FindProperty("m_showInConsole");
            p_showOnScreen = serializedObject.FindProperty("m_showOnScreen");

            m_excludedProperties = new();
            m_excludedProperties.Add(p_script.propertyPath);
            m_excludedProperties.Add(p_enumIndex.propertyPath);
            m_excludedProperties.Add(p_color.propertyPath);
            m_excludedProperties.Add(p_colorString.propertyPath);
            m_excludedProperties.Add(p_showLogs.propertyPath);
            m_excludedProperties.Add(p_showWarnings.propertyPath);
            m_excludedProperties.Add(p_showErrors.propertyPath);
            m_excludedProperties.Add(p_showInConsole.propertyPath);
            m_excludedProperties.Add(p_showOnScreen.propertyPath);
        }

        #endregion
        
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
                }
                if (GUILayout.Button("Disable All", EditorStyles.toolbarButton))
                {
                    p_showLogs.boolValue = false;
                    p_showWarnings.boolValue = false;
                    p_showErrors.boolValue = false;
                }
                if (GUILayout.Button("Error Only", EditorStyles.toolbarButton))
                {
                    p_showLogs.boolValue = false;
                    p_showWarnings.boolValue = false;
                    p_showErrors.boolValue = true;
                }

                EditorGUILayout.EndHorizontal();
            }
            // Log type buttons
            {
                var rect = EditorGUILayout.GetControlRect(false, 40f);
                float startX = rect.x - 2f;
                rect.x = startX;
                rect.width += 4f;
                rect.y -= 1f;
                rect.height = 20f;

                // First row

                float totalWidth = rect.width;
                float width = totalWidth / 3;
                rect.width = width;

                p_showLogs.boolValue = EditorGUIHelper.ToolbarToggle(rect, p_showLogs.boolValue ? EditorGUIHelper.ConsoleInfoIcon : EditorGUIHelper.ConsoleInfoInactiveIcon, p_showLogs.boolValue);
                
                rect.x += width;
                p_showWarnings.boolValue = EditorGUIHelper.ToolbarToggle(rect, p_showWarnings.boolValue ? EditorGUIHelper.ConsoleWarningIcon : EditorGUIHelper.ConsoleWarningInactiveIcon, p_showWarnings.boolValue);

                rect.x += width;
                p_showErrors.boolValue = EditorGUIHelper.ToolbarToggle(rect, p_showErrors.boolValue ? EditorGUIHelper.ConsoleErrorIcon : EditorGUIHelper.ConsoleErrorInactiveIcon, p_showErrors.boolValue);

                // Second row

                width = totalWidth / 2;
                rect.width = width;
                rect.y += 21f;

                rect.x = startX;
                p_showInConsole.boolValue = EditorGUIHelper.ToolbarToggle(rect, p_showInConsole.boolValue ? EditorGUIHelper.ConsoleIcon : EditorGUIHelper.ConsoleIcon, p_showInConsole.boolValue);

                rect.x += width;
                p_showOnScreen.boolValue = EditorGUIHelper.ToolbarToggle(rect, p_showOnScreen.boolValue ? EditorGUIHelper.ScreenIcon : EditorGUIHelper.ScreenInactiveIcon, p_showOnScreen.boolValue);
            }

            EditorGUILayout.Space(5f);

            // Color
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.ColorField(new GUIContent(p_color.displayName), p_color.colorValue, true, false, false);
                if (EditorGUI.EndChangeCheck())
                {
                    p_colorString.stringValue = ColorUtility.ToHtmlStringRGB(p_color.colorValue);
                }

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.PropertyField(p_colorString);
                EditorGUI.EndDisabledGroup();
            }

            DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());

            EditorGUILayout.Space(15f);

            // Test Log
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                m_testLogOpen = EditorGUILayout.Foldout(m_testLogOpen, "Test Log", true);

                if (m_testLogOpen)
                {
                    EditorGUILayout.Space(5f);
                    m_testString = EditorGUILayout.TextField(m_testString);
                    m_testLevel = EditorGUILayout.IntSlider(m_testLevel, 0, BaseDebugger.MAX_DEBUGGER_LEVEL);
                    m_testLogType = (LogType)EditorGUILayout.EnumPopup(m_testLogType);
                    if (GUILayout.Button("Log"))
                    {
                        BaseDebugger.ComplexLog(m_element.EnumIndex, m_testString, m_testLogType, m_testLevel);
                    }
                }
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif

    #endregion
}
