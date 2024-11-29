using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;

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
        private bool m_previewOpen;
        private bool m_previewWasOpen;
        private float m_inspectorHeight;
        private float m_inspectorY;

        // --- Parameters ---

        private float m_previewWindowHeight = 200f;
        private float m_previewButtonHeight = 20f;
        private float m_previewMargin = 8f;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            GetDatabases();
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

            if (m_currentSelection >= 0)
            {
                EditorGUILayout.Space(5f);
                if (GUILayout.Button(m_names[m_currentSelection], EditorGUIHelper.bigTitleLabel)
                    && m_databases[m_currentSelection] != null)
                {
                    EditorUtils.PingObject(m_databases[m_currentSelection]);
                }

                EditorGUILayout.Space(5f);
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 2f), Color.white);
                EditorGUILayout.Space(5f);

                if (TryGetEditorAtIndex(m_currentSelection, out Editor editor))
                {
                    bool hasPreview = editor.HasPreviewGUI();
                    float previewHeight = m_previewOpen ? m_previewWindowHeight + m_previewButtonHeight : m_previewButtonHeight;

                    if (Event.current.type != EventType.Layout)
                    {
                        m_inspectorY = GUILayoutUtility.GetLastRect().y;
                        m_inspectorHeight = position.height - m_inspectorY - previewHeight - m_previewMargin;
                    }

                    m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.Height(m_inspectorHeight));

                    editor.OnInspectorGUI();

                    EditorGUILayout.EndScrollView();

                    if (hasPreview)
                    {
                        Rect previewRect = new Rect(0, m_inspectorY + m_inspectorHeight + m_previewMargin, position.width, previewHeight);
                        
                        if (m_previewOpen)
                        {
                            OnPreviewButtonGUI(new Rect(previewRect.x, previewRect.y, previewRect.width, m_previewButtonHeight));
                            editor.DrawPreview(new Rect(previewRect.x, previewRect.y + m_previewButtonHeight, previewRect.width, m_previewWindowHeight));
                            if (!m_previewWasOpen)
                            {
                                GUI.changed = true;
                                m_previewWasOpen = true;
                            }
                        }
                        else
                        {
                            OnPreviewButtonGUI(previewRect);
                            if (m_previewWasOpen)
                            {
                                GUI.changed = true;
                                m_previewWasOpen = false;
                            }
                        }
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
            int buttonsCount = 1;

            var popupRect = new Rect(rect.x, rect.y, rect.width - buttonsWidth * buttonsCount, rect.height);
            m_currentSelection = EditorGUI.IntPopup(popupRect, m_currentSelection, m_paths, m_options, EditorStyles.toolbarDropDown);

            var refreshButtonRect = new Rect(popupRect.x + popupRect.width, rect.y, buttonsWidth, rect.height);
            if (GUI.Button(refreshButtonRect, EditorGUIHelper.RefreshIcon, EditorStyles.toolbarButton))
            {
                GetDatabases();
            }
        }

        private void OnPreviewButtonGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, EditorGUIHelper.grey015);
            EditorGUI.LabelField(rect, "Preview", EditorGUIHelper.centeredBoldLabel);

            if (Event.current.type == EventType.MouseDown
                && rect.Contains(Event.current.mousePosition))
            {
                m_previewOpen = !m_previewOpen;
                Event.current.Use();
                GUI.changed = true;
            }
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

        private bool TryGetEditorAtIndex(int index, out Editor editor)
        {
            BaseDatabase database = m_databases[index];
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
            EditorGUILayout.HelpBox("This Database is null", MessageType.Error);
            editor = null;
            return false;
        }

        #endregion
    }
}

#endif
