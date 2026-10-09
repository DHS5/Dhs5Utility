using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;


#if UNITY_EDITOR
using Dhs5.Utility.GUIs;
using UnityEditor;
using Dhs5.Utility.Editors;

namespace Dhs5.Utility.Debugger
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

        // TOOLBAR
        private bool m_filtersOpen;
        private Vector2 m_filtersScrollPosition;
        private int[] m_filtersValues;

        // LOGS
        private int m_selectedLogId = -1;
        private Vector2 m_logsScrollPosition;
        private Vector2 m_selectedLogScrollPosition;

        // VISIBLE LOGS CACHE
        /// <summary>
        /// IDs of the logs passing the filters, ascending
        /// </summary>
        private readonly List<int> m_visibleLogIds = new();
        private int m_cacheVersion = -1;
        private int m_cacheNextLogId;
        private int[] m_cacheFiltersValues;

        // AUTO REPAINT & SCROLL
        private int m_repaintVersion = -1;
        private int m_repaintNextLogId = -1;
        private float m_lastMaxLogsScroll;

        // COMMAND OPTIONS SCROLL
        private int m_lastScrolledOptionIndex = -1;
        private int m_lastScrolledOptionsCount = -1;

        // COMMANDS
        private bool m_isWritingOnCommandLine;
        private Vector2 m_commandsOptionsScrollPosition;

        #endregion

        #region GUI Content

        private GUIContent g_clearButton = new GUIContent("Clear");
        private GUIContent g_filtersButton = new GUIContent("Filters");

        #endregion

        #region Utility

        private Dictionary<LogType, GUIContent> m_logTypeIcons = new();
        private Dictionary<LogType, Color> m_logTypeColors = new();

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

        #endregion


        #region Core Behaviour

        /// <summary>
        /// Called ~10 times per second : repaints the window when logs are added or cleared
        /// </summary>
        private void OnInspectorUpdate()
        {
            if (m_repaintVersion != DebuggerLogsContainer.Version || m_repaintNextLogId != DebuggerLogsContainer.NextLogId)
            {
                m_repaintVersion = DebuggerLogsContainer.Version;
                m_repaintNextLogId = DebuggerLogsContainer.NextLogId;
                Repaint();
            }
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

            EditorGUILayout.BeginHorizontal();
            DrawLogsListGUI(logsListHeight);

            // FILTERS
            if (m_filtersOpen)
            {
                DrawFiltersGUI(logsListHeight);
            }
            EditorGUILayout.EndHorizontal();

            // COMMAND LINE
            if (m_isWritingOnCommandLine)
            {
                // Events
                HandleCommandLineEvents();
            }

            // Command Line
            rect = new Rect(1f, position.height - 29f, position.width - 2f, 28f);
            DrawCommandLineGUI(rect);

            CheckIsWritingOnCommandLine();

            // Options
            if (m_isWritingOnCommandLine)
            {
                var width = Mathf.Min(position.width, 500f);
                var height = Mathf.Min(position.height - 50f, 150f);
                var r_commandLineOptions = new Rect(1f, position.height - 30f - height, width, height);
                DrawCommandLineOptionsWindow(r_commandLineOptions);
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Event.current.Use();
                m_selectedLogId = -1;
                GUI.FocusControl(null);
            }
        }

        #endregion

        #region TOOLBAR GUI

        private void DrawToolbarGUI(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            // Clear
            var r_clearButton = new Rect(rect.x, rect.y, 70f, rect.height);
            if (GUI.Button(r_clearButton, g_clearButton, EditorStyles.toolbarButton))
            {
                DebuggerLogsContainer.ClearLogs();
            }

            // Filters
            var r_filtersButton = new Rect(rect.x + rect.width - 250f, rect.y, 250f, rect.height);
            m_filtersOpen = EditorGUIHelper.ToolbarToggleDropdown(r_filtersButton, g_filtersButton, m_filtersOpen);
        }

        #endregion

        #region FILTERS GUI

        private void DrawFiltersGUI(float height)
        {
            // Separator
            EditorGUI.DrawRect(new Rect(position.width - 251f, 20f, 1f, position.height - 48f), Color.gray1);

            m_filtersScrollPosition = EditorGUILayout.BeginScrollView(m_filtersScrollPosition, GUILayout.Width(250f), GUILayout.Height(height - 2f));

            var values = Enum.GetValues(typeof(EDebugCategory));
            if (m_filtersValues == null || values.Length != m_filtersValues.Length)
            {
                int[] newFiltersValues = new int[values.Length];
                newFiltersValues.InitWithConstantValue(DebuggerAsset.MAX_DEBUGGER_LEVEL);

                for (int i = 0; i < values.Length; i++)
                {
                    if (m_filtersValues.IsIndexValid(i, out var currentValue))
                    {
                        newFiltersValues[i] = currentValue;
                    }
                }
                m_filtersValues = newFiltersValues;
            }

            int index = 0;
            foreach (var obj in values)
            {
                var value = (EDebugCategory)obj;
                var color = DebuggerAsset.GetCategoryColor(value);
                var r_color = EditorGUILayout.GetControlRect(false, 2f);
                EditorGUI.DrawRect(r_color, color);

                var r_firstLine = EditorGUILayout.GetControlRect(false, 18f);

                // Light rim
                var r_lightRim = new Rect(r_firstLine.x, r_firstLine.y, 22f, 22f);
                EditorGUI.LabelField(r_lightRim, m_filtersValues[index] > -1 ? EditorGUIHelper.GreenLightIcon : EditorGUIHelper.RedLightIcon);

                // Label
                using (new GUIHelper.GUIContentColorScope(color))
                {
                    EditorGUI.LabelField(new Rect(r_firstLine.x + 30f, r_firstLine.y, r_firstLine.width - 30f, r_firstLine.height), value.ToString(), EditorStyles.boldLabel);
                }

                // Slider
                m_filtersValues[index] = EditorGUILayout.IntSlider(m_filtersValues[index], -1, DebuggerAsset.MAX_DEBUGGER_LEVEL);
                EditorGUILayout.Space(2f);

                index++;
            }

            EditorGUILayout.EndScrollView();
        }

        private bool DoesLogPassFilters(DebuggerLog log)
        {
            var categoryIndex = (int)log.category;

            if (m_filtersValues.IsIndexValid(categoryIndex, out var level))
            {
                return level >= log.level;
            }

            return true;
        }

        #endregion

        #region LOGS GUI

        private const float LOG_ROW_HEIGHT = 19f;
        private const float LOG_ROW_SPACING = 2f;

        /// <summary>
        /// Keeps <see cref="m_visibleLogIds"/> up to date : only new and removed logs are processed,
        /// the whole list is rebuilt only when the logs are cleared or the filters change
        /// </summary>
        private void UpdateVisibleLogsCache()
        {
            var filtersChanged = m_filtersValues != null
                && (m_cacheFiltersValues == null || !m_filtersValues.SequenceEqual(m_cacheFiltersValues));

            if (m_cacheVersion != DebuggerLogsContainer.Version || filtersChanged)
            {
                m_visibleLogIds.Clear();
                m_cacheVersion = DebuggerLogsContainer.Version;
                m_cacheNextLogId = DebuggerLogsContainer.FirstLogId;
                m_cacheFiltersValues = m_filtersValues != null ? (int[])m_filtersValues.Clone() : null;
            }

            // Logs removed by the logs limit
            var firstLogId = DebuggerLogsContainer.FirstLogId;
            int removedCount = 0;
            while (removedCount < m_visibleLogIds.Count && m_visibleLogIds[removedCount] < firstLogId) removedCount++;
            if (removedCount > 0) m_visibleLogIds.RemoveRange(0, removedCount);

            // New logs
            var nextLogId = DebuggerLogsContainer.NextLogId;
            for (int id = Mathf.Max(m_cacheNextLogId, firstLogId); id < nextLogId; id++)
            {
                if (DebuggerLogsContainer.TryGetLog(id, out var log) && DoesLogPassFilters(log))
                {
                    m_visibleLogIds.Add(id);
                }
            }
            m_cacheNextLogId = nextLogId;
        }

        private void DrawLogsListGUI(float listHeight)
        {
            // Scrolled to the bottom before the update : follow the new logs
            var wasAtBottom = m_logsScrollPosition.y >= m_lastMaxLogsScroll - 1f;
            var previousVisibleCount = m_visibleLogIds.Count;

            UpdateVisibleLogsCache();

            var listRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(listHeight));
            // Sizes computed from known values, not from listRect : during the Layout event, GetRect returns a placeholder rect,
            // and the same rows must be drawn in every event
            var viewWidth = position.width - (m_filtersOpen ? 251f : 0f) - 14f;
            var rowPitch = LOG_ROW_HEIGHT + LOG_ROW_SPACING;
            var count = m_visibleLogIds.Count;

            // Selected log : the only row with a different height
            var selectedPosition = m_selectedLogId >= 0 ? m_visibleLogIds.BinarySearch(m_selectedLogId) : -1;
            GUIContent g_selectedMessage = null;
            float selectedExtraHeight = 0f;
            if (selectedPosition >= 0 && DebuggerLogsContainer.TryGetLog(m_selectedLogId, out var selectedLog))
            {
                g_selectedMessage = new GUIContent(selectedLog.message);
                var prevWrap = EditorStyles.label.wordWrap;
                EditorStyles.label.wordWrap = true;
                var selectedHeight = Mathf.Max(38f, Mathf.Min(100f, EditorStyles.label.CalcHeight(g_selectedMessage, position.width - 150f) + 4f));
                EditorStyles.label.wordWrap = prevWrap;
                selectedExtraHeight = selectedHeight - LOG_ROW_HEIGHT;
            }
            else
            {
                selectedPosition = -1;
            }

            float RowY(int row) => row * rowPitch + (selectedPosition >= 0 && row > selectedPosition ? selectedExtraHeight : 0f);

            var viewRect = new Rect(0f, 0f, viewWidth, count * rowPitch + selectedExtraHeight);
            var maxScroll = Mathf.Max(0f, viewRect.height - listHeight);
            if (wasAtBottom && count != previousVisibleCount)
            {
                m_logsScrollPosition.y = maxScroll;
            }
            m_lastMaxLogsScroll = maxScroll;
            m_logsScrollPosition = GUI.BeginScrollView(listRect, m_logsScrollPosition, viewRect);

            // Only the rows inside the scroll view are drawn
            var scrollY = m_logsScrollPosition.y;
            int first = Mathf.Max(0, Mathf.FloorToInt(scrollY / rowPitch));
            if (selectedPosition >= 0 && first > selectedPosition)
            {
                first = Mathf.Max(selectedPosition, Mathf.FloorToInt((scrollY - selectedExtraHeight) / rowPitch));
            }
            for (int i = first; i < count && RowY(i) < scrollY + listHeight; i++)
            {
                var logId = m_visibleLogIds[i];
                if (!DebuggerLogsContainer.TryGetLog(logId, out var log)) continue;

                var selected = i == selectedPosition;
                var height = selected ? LOG_ROW_HEIGHT + selectedExtraHeight : LOG_ROW_HEIGHT;
                var rect = new Rect(0f, RowY(i), viewWidth + 3f, height + 1f);
                DrawLog(rect, i, logId, selected, selected ? g_selectedMessage : new GUIContent(log.message), log);
            }

            GUI.EndScrollView();
        }

        private void DrawLog(Rect rect, int index, int logId, bool selected, GUIContent g_message, DebuggerLog log)
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
                m_selectedLogId = logId;
                m_selectedLogScrollPosition = Vector2.zero;
                selected = true;
                GUI.FocusControl(null);

                if (log.context != null)
                {
                    EditorGUIUtility.PingObject(log.context);
                }
            }

            // Icon
            var categoryColor = DebuggerAsset.GetCategoryColor(log.category);
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

        private void CheckIsWritingOnCommandLine()
        {
            if (Event.current.type != EventType.Repaint)
            {
                m_isWritingOnCommandLine = GUI.GetNameOfFocusedControl() == ConsoleCommandTextFieldControl;
            }
        }
        private void DrawCommandLineGUI(Rect rect)
        {
            GUI.skin.textField.fontSize += 4;
            EditorStyles.label.fontSize += 4;
            GUI.SetNextControlName(ConsoleCommandTextFieldControl);
            ConsoleCommandsRegister.CommandLineContent = GUI.TextField(rect, ConsoleCommandsRegister.CommandLineContent);
            using (new GUIHelper.GUIContentColorScope(new Color(1f, 1f, 1f, 0.3f)))
            {
                EditorGUI.LabelField(new Rect(rect.x + 2f, rect.y - 2f, rect.width, rect.height), ConsoleCommandsRegister.GetHintString());
            }
            GUI.skin.textField.fontSize -= 4;
            EditorStyles.label.fontSize -= 4;
        }
        private void DrawCommandLineOptionsWindow(Rect rect)
        {
            // Draw options
            var options = ConsoleCommandsRegister.GetCurrentOptions().ToList();
            if (options.IsValid())
            {
                // Background
                GUI.Box(rect, GUIContent.none, GUI.skin.window);

                // Options
                var r_view = new Rect(0f, 0f, rect.width - 16f, options.Count * 15f);
                m_commandsOptionsScrollPosition = GUI.BeginScrollView(rect, m_commandsOptionsScrollPosition, r_view);
                
                var r_command = new Rect(2f, 0f, r_view.width + 12f, 15f);
                int index = 0;
                foreach (var (command, matchResult) in options)
                {
                    var selected = ConsoleCommandsRegister.SelectedOptionIndex == index;
                    var color = matchResult switch
                    {
                        ConsoleCommand.EMatchResult.NAME_MATCH => Color.red,
                        ConsoleCommand.EMatchResult.PARTIAL_MATCH => Color.white,
                        ConsoleCommand.EMatchResult.ACCEPTED_MATCH => Color.greenYellow,
                        ConsoleCommand.EMatchResult.PERFECT_MATCH => Color.green,
                        _ => Color.white
                    };

                    if (selected)
                    {
                        EditorGUI.DrawRect(r_command, Color.gray1);
                        // Only when the selection changes : the list can be scrolled freely otherwise
                        if (index != m_lastScrolledOptionIndex || options.Count != m_lastScrolledOptionsCount)
                        {
                            m_lastScrolledOptionIndex = index;
                            m_lastScrolledOptionsCount = options.Count;
                            GUI.ScrollTo(r_command);
                        }
                    }
                    using (new GUIHelper.GUIContentColorScope(color))
                    {
                        EditorGUI.LabelField(r_command, command);
                    }
                    r_command.y += 15f;
                    index++;
                }

                GUI.EndScrollView(handleScrollWheel:false);
            }
        }

        private void HandleCommandLineEvents()
        {
            var commandLineContentEmpty = string.IsNullOrWhiteSpace(ConsoleCommandsRegister.CommandLineContent);
            switch (Event.current.type)
            {
                case EventType.KeyDown:
                    if (Event.current.keyCode == KeyCode.Return
                        && !commandLineContentEmpty)
                    {
                        Event.current.Use();
                        ConsoleCommandsRegister.ValidateCommand();
                    }
                    else if (Event.current.keyCode == KeyCode.Tab || Event.current.character == '\t')
                    {
                        Event.current.Use();
                        ConsoleCommandsRegister.FillFromOption();
                        ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl))?.MoveLineEnd();
                    }
                    else if (Event.current.keyCode == KeyCode.UpArrow)
                    {
                        Event.current.Use();
                        if (Event.current.modifiers.HasFlag(EventModifiers.Control))
                        {
                            ConsoleCommandsRegister.SelectPreviousCommandInHistory();
                            ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl))?.MoveLineEnd();
                        }
                        else
                        {
                            ConsoleCommandsRegister.SelectPreviousOption();
                        }
                    }
                    else if (Event.current.keyCode == KeyCode.DownArrow)
                    {
                        Event.current.Use();
                        if (Event.current.modifiers.HasFlag(EventModifiers.Control))
                        {
                            ConsoleCommandsRegister.SelectNextCommandInHistory();
                            ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl))?.MoveLineEnd();
                        }
                        else
                        {
                            ConsoleCommandsRegister.SelectNextOption();
                        }
                    }
                    break;
            }
        }

        #endregion
    }
}

#endif