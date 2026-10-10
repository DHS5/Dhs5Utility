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

        #region Static Properties

        public static bool ShowSubSettingsReferences { get; set; }

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

        /// <summary>
        /// Selected settings type, saved per project and per user (EditorUserSettings, in the UserSettings folder)
        /// </summary>
        private const string SelectionConfigKey = "Dhs5.SettingsWindow.SelectedType";

        private void OnEnable()
        {
            GetSettings();
            m_currentSelection = GetSettingsIndex(EditorUserSettings.GetConfigValue(SelectionConfigKey));
        }
        private void OnDisable()
        {
            SaveSelection();
            ClearEditors();
        }

        private void SaveSelection()
        {
            if (m_settings != null && m_currentSelection >= 0 && m_currentSelection < m_settings.Length && m_settings[m_currentSelection] != null)
            {
                EditorUserSettings.SetConfigValue(SelectionConfigKey, m_settings[m_currentSelection].GetType().FullName);
            }
        }
        /// <returns>Index of the settings of type <paramref name="typeName"/>, 0 if not found</returns>
        private int GetSettingsIndex(string typeName)
        {
            if (m_settings != null && !string.IsNullOrEmpty(typeName))
            {
                for (int i = 0; i < m_settings.Length; i++)
                {
                    if (m_settings[i] != null && m_settings[i].GetType().FullName == typeName) return i;
                }
            }
            return 0;
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
            int buttonsCount = 2;

            var popupRect = new Rect(rect.x, rect.y, rect.width - buttonsWidth * buttonsCount, rect.height);
            var selection = EditorGUI.IntPopup(popupRect, m_currentSelection, m_paths, m_options, EditorStyles.toolbarDropDown);
            if (selection != m_currentSelection)
            {
                m_currentSelection = selection;
                SaveSelection();
            }

            var subSettingsButtonRect = new Rect(popupRect.x + popupRect.width, rect.y, buttonsWidth, rect.height);
            ShowSubSettingsReferences = EditorGUIHelper.ToolbarToggle(subSettingsButtonRect, EditorGUIHelper.HierarchyIcon, ShowSubSettingsReferences);
            
            var refreshButtonRect = new Rect(popupRect.x + popupRect.width + buttonsWidth, rect.y, buttonsWidth, rect.height);
            if (GUI.Button(refreshButtonRect, EditorGUIHelper.RefreshIcon, EditorStyles.toolbarButton))
            {
                // The list can change : the same settings type stays selected
                SaveSelection();
                GetSettings();
                m_currentSelection = GetSettingsIndex(EditorUserSettings.GetConfigValue(SelectionConfigKey));
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
            // Destroyed, not just dropped : it owns the sub settings editors
            ClearEditors();
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
