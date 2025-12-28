using UnityEngine;
using System.Collections.Generic;
using Dhs5.Utility.Debuggers;



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
        private Vector2 m_selectedLogScrollPosition;

        // COMMANDS
        private string m_commandFieldContent;

        #endregion

        #region Utility

        private Dictionary<LogType, GUIContent> m_logTypeIcons = new();
        private Dictionary<LogType, Color> m_logTypeColors = new();
        private Dictionary<EDebugCategory, Color> m_categoryColors = new();

        private GUIContent GetLogTypeIcon(LogType logType)
        {
            if (m_logTypeIcons.TryGetValue(logType, out var icon)) return icon;

            m_logTypeIcons[logType] = logType switch
            {
                LogType.Log => EditorGUIHelper.ConsoleInfoInactiveIcon,
                LogType.Warning => EditorGUIHelper.ConsoleWarningInactiveIcon,
                LogType.Error => EditorGUIHelper.ConsoleErrorInactiveIcon,
                LogType.Exception => EditorGUIHelper.ConsoleErrorInactiveIcon,
                LogType.Assert => EditorGUIHelper.ConsoleErrorInactiveIcon,
                _ => EditorGUIHelper.ConsoleInfoInactiveIcon
            };
            return m_logTypeIcons[logType];
        }
        private Color GetLogTypeColor(LogType logType)
        {
            if (m_logTypeColors.TryGetValue(logType, out var color)) return color;

            m_logTypeColors[logType] = logType switch
            {
                LogType.Log => Color.white,
                LogType.Warning => Color.yellow,
                LogType.Error => Color.softRed,
                LogType.Exception => Color.red,
                LogType.Assert => Color.red,
                _ => Color.white
            };
            return m_logTypeColors[logType];
        }
        private Color GetCategoryColor(EDebugCategory category)
        {
            if (m_categoryColors.TryGetValue(category, out var color)) return color;

            m_categoryColors[category] = DebuggerDatabase.GetAtIndex((int)category).Color;
            return m_categoryColors[category];
        }

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
            if (IsWritingOnCommandLine())
            {
                var width = Mathf.Min(position.width, 500f);
                var height = Mathf.Min(position.height - 50f, 150f);
                var r_commandLineHints = new Rect(1f, position.height - 30f - height, width, height);
                DrawCommandLineHintsWindow(r_commandLineHints);
            }

            rect = new Rect(1f, position.height - 29f, position.width - 2f, 28f);
            DrawCommandLineGUI(rect);

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

        private void DrawLogsListGUI(float listHeight)
        {
            m_logsScrollPosition = EditorGUILayout.BeginScrollView(m_logsScrollPosition, GUILayout.Height(listHeight));

            int index = 0;
            foreach (var log in ConsoleLogsContainer.GetLogs())
            {
                var selected = m_selectedLogIndex == index;
                var g_message = new GUIContent(log.message);
                var prevWrap = EditorStyles.label.wordWrap;
                EditorStyles.label.wordWrap = true;
                var height = selected ? Mathf.Max(38f, Mathf.Min(100f, EditorStyles.label.CalcHeight(g_message, position.width - 150f) + 4f)) : 19f;
                EditorStyles.label.wordWrap = prevWrap;
                var rect = EditorGUILayout.GetControlRect(false, height);
                rect.width += rect.x + 3f; rect.x = 0f; rect.height++;
                DrawLog(rect, index, selected, g_message, log);
                index++;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLog(Rect rect, int index, bool selected, GUIContent g_message, ConsoleLog log)
        {
            // Background
            EditorGUI.DrawRect(rect, selected ? Color.gray1 : index % 2 == 0 ? Color.gray3 : Color.gray2);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height, rect.width, 1f), Color.black);

            // Button
            if (!selected
                && Event.current.type == EventType.MouseDown 
                && Event.current.button == 0
                && rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                m_selectedLogIndex = index;
                m_selectedLogScrollPosition = Vector2.zero;
                selected = true;
                GUI.FocusControl(null);

                if (log.context != null)
                {
                    Selection.activeObject = log.context;
                }
            }

            // Icon
            var categoryColor = GetCategoryColor(log.category);
            var r_icon = new Rect(rect.x + 2f, rect.y + 1f, 18f, 18f);
            using (new GUIHelper.GUIContentColorScope(categoryColor))
            {
                GUI.Box(r_icon, GetLogTypeIcon(log.type), GUI.skin.label);
            }

            // Level
            var r_levelLabel = new Rect(rect.x + 16f, rect.y + 8f, 10f, 10f);
            EditorGUI.LabelField(r_levelLabel, log.level.ToString(), EditorStyles.miniLabel);

            // Category
            var categoryWidth = 100f;
            var r_categoryLabel = new Rect(rect.x + 26f, rect.y, categoryWidth, 18f);
            using (new GUIHelper.GUIContentColorScope(categoryColor))
            {
                EditorGUI.LabelField(r_categoryLabel, log.category.ToString(), EditorStyles.boldLabel);
            }

            // Message
            var prevClipping = EditorStyles.label.clipping;
            var prevAlignment = EditorStyles.label.alignment;
            var prevWrap = EditorStyles.label.wordWrap;
            EditorStyles.label.clipping = TextClipping.Ellipsis;
            EditorStyles.label.alignment = TextAnchor.UpperLeft;
            EditorStyles.label.wordWrap = true;
            if (selected)
            {
                // Message
                var r_message = new Rect(rect.x + 26f + categoryWidth, rect.y + 2f, rect.width - categoryWidth - 30f, rect.height - 2f);

                var viewWidth = r_message.width - 16f;
                var viewHeight = EditorStyles.label.CalcHeight(g_message, viewWidth) + 2f;
                var r_view = new Rect(0f, 0f, viewWidth, viewHeight);

                m_selectedLogScrollPosition = GUI.BeginScrollView(r_message, m_selectedLogScrollPosition, r_view);
                using (new GUIHelper.GUIContentColorScope(GetLogTypeColor(log.type)))
                {
                    EditorGUI.SelectableLabel(r_view, log.message);
                }
                GUI.EndScrollView(true);

                // Time
                var r_time = new Rect(rect.x + 2f, rect.y + 20f, categoryWidth + 20f, 14f);
                EditorGUI.LabelField(r_time, log.timestamp.ToString(), EditorStyles.miniLabel);
            }
            else
            {
                var r_messageLabel = new Rect(rect.x + 26f + categoryWidth, rect.y + 2f, rect.width - categoryWidth - 30f, EditorStyles.label.fontSize);
                using (new GUIHelper.GUIContentColorScope(GetLogTypeColor(log.type)))
                {
                    EditorGUI.SelectableLabel(r_messageLabel, log.message);
                }
            }
            EditorStyles.label.clipping = prevClipping;
            EditorStyles.label.alignment = prevAlignment;
            EditorStyles.label.wordWrap = prevWrap;
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

            switch (Event.current.type)
            {
                case EventType.KeyDown:
                    if (Event.current.keyCode == KeyCode.Return)
                    {
                        Event.current.Use();
                        ConsoleLogsContainer.AddLog(new ConsoleLog(EDebugCategory.GAME, LogType.Log, 0, m_commandFieldContent, null));
                    }
                    break;
            }
        }

        #endregion
    }
}

#endif