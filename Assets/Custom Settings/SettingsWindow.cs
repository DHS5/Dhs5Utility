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

        [MenuItem("Tools/Windows/Settings")]
        public static void OpenWindow()
        {
            SettingsWindow window = GetWindow<SettingsWindow>();
            window.titleContent = new GUIContent(EditorGUIHelper.SettingsIcon) { text = "Settings" };
        }

        #endregion

        #region Members

        private Dictionary<BaseSettings, Editor> m_editors;
        private BaseSettings[] m_settings;
        private string[] m_names;
        private string[] m_paths;
        private int[] m_options;

        // --- GUI Members ---
        private Vector2 m_scrollPosition;

        // --- STATIC Members ---
        private static int _currentSelection;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            GetSettings();
        }
        private void OnDisable()
        {
            ClearEditors();
        }

        #endregion


        #region Core GUI

        private void OnGUI()
        {
            var toolbarRect = EditorGUILayout.GetControlRect(false, 20f);
            OnToolbarGUI(toolbarRect);

            if (_currentSelection >= 0)
            {
                EditorGUILayout.Space(5f);
                if (GUILayout.Button(m_names[_currentSelection], GUIHelper.bigTitleLabel)
                    && m_settings[_currentSelection] != null)
                {
                    EditorUtils.PingObject(m_settings[_currentSelection]);
                }

                EditorGUILayout.Space(5f);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 2f), Color.white);
                EditorGUILayout.Space(5f);

                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

                OnSettingsGUI(_currentSelection);

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
            _currentSelection = EditorGUI.IntPopup(popupRect, _currentSelection, m_paths, m_options, EditorStyles.toolbarDropDown);

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
            m_editors = new();

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
            if (m_editors.TryGetValue(settings, out var editor)
                && editor != null)
            {
                return editor;
            }

            m_editors[settings] = Editor.CreateEditor(settings);
            return m_editors[settings];
        }
        private void ClearEditors()
        {
            if (m_editors != null)
            {
                foreach (var editor in m_editors.Values)
                {
                    if (editor != null)
                    {
                        DestroyImmediate(editor);
                    }
                }
            }
        }

        #endregion
    }
}

#endif
