using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
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
        private Type[] m_types;
        private string[] m_names;
        private string[] m_paths;
        private int[] m_options;

        // --- GUI Members ---
        private Vector2 m_scrollPosition;
        /// <summary>
        /// Label width used by the settings editor, 0 for Unity's default
        /// </summary>
        private float m_labelWidth;

        // --- Members ---
        private int m_currentSelection;

        #endregion

        #region Core Behaviour

        /// <summary>
        /// Selected settings type, saved per project and per user (EditorUserSettings, in the UserSettings folder)
        /// </summary>
        private const string SelectionConfigKey = "Dhs5.SettingsWindow.SelectedType";
        /// <summary>
        /// Label width, saved per project and per user (EditorUserSettings, in the UserSettings folder)
        /// </summary>
        private const string LabelWidthConfigKey = "Dhs5.SettingsWindow.LabelWidth";

        private void OnEnable()
        {
            GetSettings(false);
            m_currentSelection = GetSettingsIndex(EditorUserSettings.GetConfigValue(SelectionConfigKey));
            float.TryParse(EditorUserSettings.GetConfigValue(LabelWidthConfigKey), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out m_labelWidth);
        }

        private void SaveLabelWidth()
        {
            EditorUserSettings.SetConfigValue(LabelWidthConfigKey, m_labelWidth.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        private void OnDisable()
        {
            SaveSelection();
            ClearEditors();
        }

        private void SaveSelection()
        {
            if (m_types != null && m_currentSelection >= 0 && m_currentSelection < m_types.Length)
            {
                EditorUserSettings.SetConfigValue(SelectionConfigKey, m_types[m_currentSelection].FullName);
            }
        }
        /// <returns>Index of the settings of type <paramref name="typeName"/>, 0 if not found</returns>
        private int GetSettingsIndex(string typeName)
        {
            if (m_types != null && !string.IsNullOrEmpty(typeName))
            {
                for (int i = 0; i < m_types.Length; i++)
                {
                    if (m_types[i].FullName == typeName) return i;
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

                var handleRect = EditorGUILayout.GetControlRect(false, LabelWidthHandleHeight);
                bool dragging = OnLabelWidthHandleGUI(handleRect);

                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

                using (new EditorGUIHelper.LabelWidthScope(GetLabelWidth()))
                {
                    OnSettingsGUI(m_currentSelection);
                }

                EditorGUILayout.EndScrollView();

                // Guide line over the content while dragging
                if (dragging && Event.current.type == EventType.Repaint)
                {
                    float x = handleRect.x + GetLabelWidth();
                    EditorGUI.DrawRect(new Rect(x - 0.5f, handleRect.yMax, 1f, position.height - handleRect.yMax), new Color(0.3f, 0.6f, 1f, 0.6f));
                }
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
                // Missing settings assets are only created here
                SaveSelection();
                GetSettings(true);
                m_currentSelection = GetSettingsIndex(EditorUserSettings.GetConfigValue(SelectionConfigKey));
            }
        }

        private const float LabelWidthHandleHeight = 8f;
        private const float MinLabelWidth = 50f;

        /// <returns>Label width to use, clamped to the window width</returns>
        private float GetLabelWidth()
        {
            float labelWidth;
            if (m_labelWidth > 0f)
            {
                labelWidth = m_labelWidth;
            }
            else
            {
                // 0 makes Unity compute its default width
                using (new EditorGUIHelper.LabelWidthScope(0f))
                {
                    labelWidth = EditorGUIUtility.labelWidth;
                }
            }
            return Mathf.Clamp(labelWidth, MinLabelWidth, Mathf.Max(MinLabelWidth, position.width - 60f));
        }

        /// <summary>
        /// Handle to drag the label width, double click to reset it to Unity's default
        /// </summary>
        /// <returns>Whether the handle is being dragged</returns>
        private bool OnLabelWidthHandleGUI(Rect rect)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            float labelWidth = GetLabelWidth();
            var handleRect = new Rect(rect.x + labelWidth - 5f, rect.y, 10f, rect.height);

            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal, id);

            var current = Event.current;
            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (current.button == 0 && handleRect.Contains(current.mousePosition))
                    {
                        if (current.clickCount == 2)
                        {
                            m_labelWidth = 0f;
                            SaveLabelWidth();
                        }
                        else
                        {
                            m_labelWidth = labelWidth;
                            GUIUtility.hotControl = id;
                        }
                        current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        m_labelWidth = Mathf.Clamp(current.mousePosition.x - rect.x, MinLabelWidth, Mathf.Max(MinLabelWidth, position.width - 60f));
                        current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        SaveLabelWidth();
                        current.Use();
                    }
                    break;

                case EventType.Repaint:
                    bool active = GUIUtility.hotControl == id;
                    EditorGUI.DrawRect(new Rect(rect.x, rect.center.y - 0.5f, rect.width, 1f), new Color(0.5f, 0.5f, 0.5f, 0.25f));
                    EditorGUI.DrawRect(new Rect(rect.x + labelWidth - 2f, rect.y, 4f, rect.height),
                        active ? new Color(0.3f, 0.6f, 1f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f));
                    break;
            }

            return GUIUtility.hotControl == id;
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
            EditorGUILayout.HelpBox("No asset exists for these settings : press Refresh to create it", MessageType.Info);
        }

        #endregion


        #region Utility

        private void GetSettings(bool createIfMissing)
        {
            // Destroyed, not just dropped : it owns the sub settings editors
            ClearEditors();
            m_editor = null;

            // Settings
            m_settings = BaseSettings.GetAllInstances(createIfMissing, out m_types);// t => BaseSettings.GetScope(t) == SettingsScope.Project);

            // Paths, names & options
            m_names = new string[m_settings.Length];
            m_paths = new string[m_settings.Length];
            m_options = new int[m_settings.Length];
            for (int i = 0; i < m_settings.Length; i++)
            {
                // Missing assets are named after their type
                var name = m_settings[i] != null ? m_settings[i].name : m_types[i].Name;
                m_names[i] = name.Contains("Settings") ? name.Replace("Settings", "") : name;
                var path = BaseSettings.GetPath(m_types[i]);
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
