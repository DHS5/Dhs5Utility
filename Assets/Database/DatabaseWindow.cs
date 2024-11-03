using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dhs5.Utility.Databases
{
    public class DatabaseWindow : EditorWindow
    {
        #region Static Constructor

        [MenuItem("Tools/Windows/Databases")]
        public static void OpenWindow()
        {
            DatabaseWindow window = GetWindow<DatabaseWindow>();
            window.titleContent = new GUIContent(EditorGUIHelper.DatabaseIcon) { text = "Databases" };
        }

        #endregion

        #region Members

        private Dictionary<BaseDatabase, Editor> m_editors;
        private BaseDatabase[] m_databases;
        private string[] m_names;
        private string[] m_paths;
        private int[] m_options;

        // --- GUI Members ---

        private Vector2 m_scrollPosition;
        private int m_currentSelection;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            GetDatabases();
        }

        #endregion


        #region Core GUI

        private void OnGUI()
        {
            m_currentSelection = EditorGUILayout.IntPopup(m_currentSelection, m_paths, m_options);

            if (m_currentSelection >= 0)
            {
                EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField(m_names[m_currentSelection], EditorGUIHelper.bigTitleLabel);

                EditorGUILayout.Space(5f);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 2f), Color.white);
                EditorGUILayout.Space(5f);

                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

                OnDatabaseGUI(m_currentSelection);

                EditorGUILayout.EndScrollView();
            }
        }

        #endregion

        #region GUI

        private void OnDatabaseGUI(int index)
        {
            BaseDatabase database = m_databases[index];
            if (database != null)
            {
                var editor = GetOrCreateEditor(database);
                if (editor != null)
                {
                    editor.OnInspectorGUI();
                    return;
                }
                EditorGUILayout.HelpBox("This Database's editor is null", MessageType.Error);
                return;
            }
            EditorGUILayout.HelpBox("This Database is null", MessageType.Error);
        }

        #endregion


        #region Utility

        private void GetDatabases()
        {
            m_editors = new();

            // Settings
            m_databases = BaseDatabase.GetAllInstances();

            // Paths, names & options
            m_names = new string[m_databases.Length];
            m_paths = new string[m_databases.Length];
            m_options = new int[m_databases.Length];
            for (int i = 0; i < m_databases.Length; i++)
            {
                m_names[i] = m_databases[i].name.Contains("Database") ? m_databases[i].name.Replace("Database", "") : m_databases[i].name;
                m_paths[i] = m_databases[i].Editor_GetPath();
                m_options[i] = i;
            }
        }

        private Editor GetOrCreateEditor(BaseDatabase database)
        {
            if (m_editors.TryGetValue(database, out var editor)
                && editor != null)
            {
                return editor;
            }

            m_editors[database] = Editor.CreateEditor(database);
            return m_editors[database];
        }

        #endregion
    }
}
