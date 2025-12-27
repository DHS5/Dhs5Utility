using UnityEngine;

#if UNITY_EDITOR
using Dhs5.Utility.GUIs;
using UnityEditor;
using Dhs5.Utility.Editors;

namespace Dhs5.Utility.Console
{
    public class ConsoleWindow : EditorWindow
    {
        #region Static Constructor

        [MenuItem("Window/Dhs5 Utility/Console", priority = 100)]
        public static void OpenWindow()
        {
            ConsoleWindow window = GetWindow<ConsoleWindow>();
            window.titleContent = new GUIContent(EditorGUIHelper.ConsoleIcon) { text = "D5 Console" };
        }

        #endregion


        #region Consts

        private const string ConsoleCommandTextFieldControl = "ConsoleCommandTextField";

        #endregion

        #region Members

        // LOGS
        private int m_selectedLogIndex = -1;
        private Vector2 m_logsScrollPosition;

        // COMMANDS
        private string m_commandFieldContent;

        #endregion


        #region Core GUI

        private void OnGUI()
        {
            // TOOLBAR
            EditorGUILayout.GetControlRect(false, 17f);
            var rect = new Rect(0f, 0f, position.width, 20f);
            DrawToolbarGUI(rect);

            // LOGS
            var logsListHeight = position.height - 50f;
            DrawLogsListGUI(logsListHeight);

            // COMMAND LINE
            rect = new Rect(1f, position.height - 29f, position.width - 2f, 28f);
            DrawCommandLineGUI(rect);

            if (IsWritingOnCommandLine())
            {
                var width = Mathf.Min(position.width, 500f);
                var height = Mathf.Min(position.height - 50f, 150f);
                var r_commandLineHints = new Rect(1f, position.height - 30f - height, width, height);
                DrawCommandLineHintsWindow(r_commandLineHints);
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Event.current.Use();
                m_selectedLogIndex = -1;
                GUI.FocusControl(null);
            }
        }

        #endregion

        #region TOOLBAR GUI

        private void DrawToolbarGUI(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);
        }

        #endregion

        #region LOGS GUI

        private void DrawLogsListGUI(float height)
        {
            m_logsScrollPosition = EditorGUILayout.BeginScrollView(m_logsScrollPosition, GUILayout.Height(height));

            int index = 0;
            foreach (var log in ConsoleLogsContainer.GetTestLogs())
            {
                var selected = m_selectedLogIndex == index;
                var rect = EditorGUILayout.GetControlRect(false, selected ? 49f : 19f);
                rect.x = 0f; rect.width = position.width; rect.height++;
                DrawLog(rect, index, selected, log);
                index++;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLog(Rect rect, int index, bool selected, ConsoleLog log)
        {
            // Background
            EditorGUI.DrawRect(rect, selected ? Color.gray1 : index % 2 == 0 ? Color.gray3 : Color.gray2);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height, rect.width, 1f), Color.black);

            // Button
            if (Event.current.type == EventType.MouseDown 
                && Event.current.button == 0
                && rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                m_selectedLogIndex = index;
                GUI.FocusControl(null);
            }

            // Icon
            var r_icon = new Rect(rect.x + 4f, rect.y + 2f, 16f, 16f);
            var g_icon = log.type switch
            {
                LogType.Log => EditorGUIHelper.ConsoleInfoInactiveIcon,
                LogType.Warning => EditorGUIHelper.ConsoleWarningInactiveIcon,
                LogType.Error => EditorGUIHelper.ConsoleErrorInactiveIcon,
                LogType.Exception => EditorGUIHelper.ConsoleErrorInactiveIcon,
                _ => EditorGUIHelper.ConsoleInfoInactiveIcon
            };
            using (new GUIHelper.GUIContentColorScope(Color.white))
            {
                GUI.Box(r_icon, g_icon, EditorStyles.iconButton);
            }
        }

        #endregion

        #region COMMAND LINE GUI

        private bool IsWritingOnCommandLine()
        {
            return EditorGUIUtility.editingTextField
                && GUI.GetNameOfFocusedControl() == ConsoleCommandTextFieldControl
                && !string.IsNullOrWhiteSpace(m_commandFieldContent);
        }
        private void DrawCommandLineGUI(Rect rect)
        {
            var previous = GUI.skin.textField.fontSize;
            GUI.skin.textField.fontSize = previous + 4;
            GUI.SetNextControlName(ConsoleCommandTextFieldControl);
            m_commandFieldContent = EditorGUI.TextField(rect, m_commandFieldContent);
            GUI.skin.textField.fontSize = previous;
        }
        private void DrawCommandLineHintsWindow(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, GUI.skin.window);
        }

        #endregion
    }
}

#endif