using System.Collections.Generic;
using UnityEngine;
using System;
using Dhs5.Utility.GUIs;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;

namespace Dhs5.Utility.NewDatabase
{
    public class DatabaseWindow : EditorWindow
    {
        #region Static Constructor

        [MenuItem("Window/Dhs5 Utility/Databases", priority = 100)]
        public static void OpenWindow()
        {
            DatabaseWindow window = GetWindow<DatabaseWindow>();
            window.titleContent = new GUIContent(EditorGUIHelper.DatabaseIcon) { text = "Databases" };
        }

        #endregion

        #region Members

        private ContainerEditor m_editor;
        private Container[] m_databases;
        private Type[] m_types;
        private string[] m_names;
        private string[] m_paths;
        private int[] m_options;

        // --- GUI Members ---

        private Vector2 m_scrollPosition;
        private float m_inspectorHeight;
        private float m_inspectorY;

        // --- STATIC Members ---

        private int m_currentSelection;

        #endregion

        #region Core Behaviour

        /// <summary>
        /// Selected database type, saved per project and per user (EditorUserSettings, in the UserSettings folder)
        /// </summary>
        private const string SelectionConfigKey = "Dhs5.DatabaseWindow.SelectedType";

        private void OnEnable()
        {
            GetDatabases(false);
            m_currentSelection = GetDatabaseIndex(EditorUserSettings.GetConfigValue(SelectionConfigKey));
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
        /// <returns>Index of the database of type <paramref name="typeName"/>, 0 if not found</returns>
        private int GetDatabaseIndex(string typeName)
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

            if (m_currentSelection >= 0 && m_currentSelection < m_databases.Length)
            {
                EditorGUILayout.Space(5f);
                if (GUILayout.Button(m_names[m_currentSelection], GUIHelper.bigTitleLabel)
                    && m_databases[m_currentSelection] != null)
                {
                    EditorUtils.FullPingObject(m_databases[m_currentSelection]);
                }

                EditorGUILayout.Space(5f);
                var rect = EditorGUILayout.GetControlRect(false, 2f);
                rect.x = 0f; rect.width = position.width;
                EditorGUI.DrawRect(rect, Color.white);

                if (TryGetEditorAtIndex(m_currentSelection, out var editor))
                {
                    // Area left below the separator
                    if (Event.current.type != EventType.Layout)
                    {
                        m_inspectorY = GUILayoutUtility.GetLastRect().yMax;
                        m_inspectorHeight = position.height - m_inspectorY;
                    }

                    if (Database.DebugModeEnabled)
                    {
                        m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.Height(m_inspectorHeight));
                        editor.OnDefaultInspectorGUI();
                        EditorGUILayout.EndScrollView();
                    }
                    else
                    {
                        // Not inside a scroll view : the container GUI handles its own scroll views
                        editor.OnDatabaseCoreGUI(new Rect(0f, m_inspectorY, position.width, m_inspectorHeight));
                    }
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

            // Popup
            var popupRect = new Rect(rect.x, rect.y, rect.width - buttonsWidth * buttonsCount, rect.height);
            var selection = EditorGUI.IntPopup(popupRect, m_currentSelection, m_paths, m_options, EditorStyles.toolbarDropDown);
            if (selection != m_currentSelection)
            {
                m_currentSelection = selection;
                SaveSelection();
            }

            // Debug Button
            var debugButtonRect = new Rect(rect.x + rect.width - buttonsWidth * 2, rect.y, buttonsWidth, rect.height);
            Database.DebugModeEnabled = EditorGUIHelper.ToolbarToggle(debugButtonRect, EditorGUIHelper.DebugIcon, Database.DebugModeEnabled);

            // Refresh Button
            var refreshButtonRect = new Rect(rect.x + rect.width - buttonsWidth, rect.y, buttonsWidth, rect.height);
            if (GUI.Button(refreshButtonRect, EditorGUIHelper.RefreshIcon, EditorStyles.toolbarButton))
            {
                // The list can change : the same database type stays selected
                // Missing database assets are only created here
                SaveSelection();
                Database.ClearInstances();
                GetDatabases(true);
                m_currentSelection = GetDatabaseIndex(EditorUserSettings.GetConfigValue(SelectionConfigKey));
            }
        }

        #endregion


        #region Utility

        private void GetDatabases(bool createIfMissing)
        {
            // Destroyed, not just dropped
            ClearEditors();

            m_databases = Database.GetAllDatabaseInstances((da) => da.showInDatabaseWindow, createIfMissing, out m_types);

            // Paths, names & options
            m_names = new string[m_databases.Length];
            m_paths = new string[m_databases.Length];
            m_options = new int[m_databases.Length];
            for (int i = 0; i < m_databases.Length; i++)
            {
                // Missing assets are named after their type
                var name = m_databases[i] != null ? m_databases[i].name : m_types[i].Name;
                m_names[i] = name.Contains("Database") ? name.Replace("Database", "") : name;
                m_paths[i] = Database.Editor_GetDatabaseWindowPath(m_types[i]);
                m_options[i] = i;
            }
        }

        private ContainerEditor GetOrCreateEditor(Container database)
        {
            if (m_editor != null && m_editor.target == database) return m_editor;

            // Destroy current
            if (m_editor != null)
            {
                DestroyImmediate(m_editor);
            }

            var editor = Editor.CreateEditor(database);
            if (editor is ContainerEditor containerEditor)
            {
                m_editor = containerEditor;
            }
            else
            {
                // Custom editor not deriving from ContainerEditor : destroyed, the window always uses a ContainerEditor
                if (editor != null) DestroyImmediate(editor);
                m_editor = Editor.CreateEditor(database, typeof(ContainerEditor)) as ContainerEditor;
            }
            return m_editor;
        }
        private void ClearEditors()
        {
            if (m_editor != null)
            {
                DestroyImmediate(m_editor);
            }
            m_editor = null;
        }

        private bool TryGetEditorAtIndex(int index, out ContainerEditor editor)
        {
            Container database = m_databases[index];
            if (database != null)
            {
                editor = GetOrCreateEditor(database);
                if (editor != null)
                {
                    return true;
                }
                EditorGUILayout.HelpBox("This Database's editor is null", MessageType.Error);
                return false;
            }
            EditorGUILayout.HelpBox("No asset exists for this Database : press Refresh to create it", MessageType.Info);
            editor = null;
            return false;
        }

        #endregion
    }
}

#endif
