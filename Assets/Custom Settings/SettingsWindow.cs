using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.GUIs;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;

namespace Dhs5.Utility.Settings
{
    public class SettingsWindow : EditorWindow
    {
        #region Static Constructor

        [MenuItem("Window/Dhs5 Utility/Settings", priority = 100)]
        public static void OpenWindow()
        {
            SettingsWindow window = GetWindow<SettingsWindow>();
            window.titleContent = new GUIContent(EditorGUIHelper.SettingsIcon) { text = "Settings" };
        }

        #endregion

        #region Members

        private Editor m_editor;
        private BaseSettings[] m_settings;
        private string[] m_names;
        private string[] m_paths;
        private int[] m_options;

        // --- GUI Members ---
        private Vector2 m_scrollPosition;

        // --- Members ---
        private int m_currentSelection;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            m_currentSelection = EditorPrefs.GetInt("SW_selection");
            GetSettings();
        }
        private void OnDisable()
        {
            EditorPrefs.SetInt("SW_selection", m_currentSelection);
            ClearEditors();
        }

        #endregion


        #region Core GUI

        private void OnGUI()
        {
            var toolbarRect = EditorGUILayout.GetControlRect(false, 20f);
            OnToolbarGUI(toolbarRect);

            if (m_currentSelection >= 0 && m_currentSelection < m_settings.Length)
            {
                EditorGUILayout.Space(5f);
                if (GUILayout.Button(m_names[m_currentSelection], GUIHelper.bigTitleLabel)
                    && m_settings[m_currentSelection] != null)
                {
                    EditorUtils.FullPingObject(m_settings[m_currentSelection]);
                }

                EditorGUILayout.Space(5f);
                var rect = EditorGUILayout.GetControlRect(false, 2f);
                rect.x = 0f; rect.width = position.width;
                EditorGUI.DrawRect(rect, Color.white);
                EditorGUILayout.Space(5f);

                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

                OnSettingsGUI(m_currentSelection);

                EditorGUILayout.EndScrollView();
            }
        }

        #endregion

        #region GUI

        private void OnToolbarGUI(Rect rect)
        {
            rect.x = 0f;
            rect.y = 0f;
            rect.width = position.width;

            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            float buttonsWidth = 40f;
            int buttonsCount = 1;

            var popupRect = new Rect(rect.x, rect.y, rect.width - buttonsWidth * buttonsCount, rect.height);
            m_currentSelection = EditorGUI.IntPopup(popupRect, m_currentSelection, m_paths, m_options, EditorStyles.toolbarDropDown);

            var refreshButtonRect = new Rect(popupRect.x + popupRect.width, rect.y, buttonsWidth, rect.height);
            if (GUI.Button(refreshButtonRect, EditorGUIHelper.RefreshIcon, EditorStyles.toolbarButton))
            {
                GetSettings();
            }
        }

        private void OnSettingsGUI(int settingsIndex)
        {
            BaseSettings settings = m_settings[settingsIndex];
            if (settings != null)
            {
                var editor = GetOrCreateEditor(settings);
                if (editor != null)
                {
                    editor.OnInspectorGUI();
                    return;
                }
                EditorGUILayout.HelpBox("This Settings editor is null", MessageType.Error);
                return;
            }
            EditorGUILayout.HelpBox("This Settings is null", MessageType.Error);
        }

        #endregion


        #region Utility

        private void GetSettings()
        {
            m_editor = null;

            // Settings
            m_settings = BaseSettings.GetAllInstances();// t => BaseSettings.GetScope(t) == SettingsScope.Project);

            // Paths, names & options
            m_names = new string[m_settings.Length];
            m_paths = new string[m_settings.Length];
            m_options = new int[m_settings.Length];
            for (int i = 0; i < m_settings.Length; i++)
            {
                m_names[i] = m_settings[i].name.Contains("Settings") ? m_settings[i].name.Replace("Settings", "") : m_settings[i].name;
                var path = m_settings[i].Editor_GetPath();
                if (path.Contains("Project")) path = path.Replace("Project/", "");
                else if (path.Contains("Preferences")) path = path.Replace("Preferences/", "");
                m_paths[i] = path;
                m_options[i] = i;
            }
        }

        private Editor GetOrCreateEditor(BaseSettings settings)
        {
            if (m_editor != null && m_editor.target == settings) return m_editor;

            // Destroy current
            if (m_editor != null)
            {
                DestroyImmediate(m_editor);
            }

            m_editor = Editor.CreateEditor(settings);
            return m_editor;
        }
        private void ClearEditors()
        {
            if (m_editor != null)
            {
                DestroyImmediate(m_editor);
            }
        }

        #endregion
    }
}

#endif
