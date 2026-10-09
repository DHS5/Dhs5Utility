using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.GUIs;

namespace Dhs5.Utility.Debugger
{
    /// <summary>
    /// Displays logs on screen for a few seconds.<br></br>
    /// Only compiled in the editor and development builds : in release builds, <see cref="Log"/> does nothing.
    /// </summary>
    public class OnScreenLogger : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        #region STRUCT : ScreenLog

        private readonly struct ScreenLog
        {
            public ScreenLog(DebuggerLog log, float disposalTime)
            {
                this.log = log;
                this.disposalTime = disposalTime;
            }

            public readonly DebuggerLog log;
            public readonly float disposalTime;
        }

        #endregion


        #region INSTANCE

        #region Properties

        public bool IsActive => m_screenLogs.Count > 0;
        public int LogsCount => m_screenLogs.Count;

        #endregion

        #region Core Behaviour

#if UNITY_EDITOR
        // The D5 Console's Clear button also clears the on screen logs
        private void OnEnable()
        {
            DebuggerLogsContainer.Cleared += OnLogsCleared;
        }
        private void OnDisable()
        {
            DebuggerLogsContainer.Cleared -= OnLogsCleared;
        }
        private void OnLogsCleared()
        {
            m_screenLogs.Clear();
        }
#endif

        #endregion


        #region ScreenLogs Management

        /// <summary>
        /// Logs currently displayed, oldest first : they are stored here, not looked up in the logs container (editor only)
        /// </summary>
        private readonly List<ScreenLog> m_screenLogs = new();

        private void AddScreenLog(DebuggerLog log, float duration)
        {
            // Unscaled : logs must disappear even when the game is paused (timescale 0)
            m_screenLogs.Add(new ScreenLog(log, Time.unscaledTime + duration));
        }

        #endregion

        #region Update

        private void LateUpdate()
        {
            if (IsActive)
            {
                float time = Time.unscaledTime;
                m_screenLogs.RemoveAll(screenLog => time >= screenLog.disposalTime);
            }
        }

        #endregion


        #region GUI

        private void OnGUI()
        {
            if (IsActive)
            {
                var sizes = DebuggerAsset.OnScreenGUI;
                var scale = sizes.Scale;
                float logMinHeight = sizes.logMinHeight * scale;
                var rect = new Rect(0f, 0f, sizes.logsAreaSize.x * scale, sizes.logsAreaSize.y * scale);
                var logRect = new Rect(rect.x, rect.y, rect.width, logMinHeight);

                // Newest first
                for (int i = LogsCount - 1; i >= 0 && logRect.y + logMinHeight <= rect.y + rect.height; i--)
                {
                    OnScreenLogGUI(logRect, i, m_screenLogs[i].log, sizes, out var logNecessaryHeight);
                    logRect.y += logNecessaryHeight;
                }
            }
        }

        private void OnScreenLogGUI(Rect rect, int index, DebuggerLog log, DebuggerAsset.OnScreenGUISettings sizes, out float necessaryHeight)
        {
            var prevLabelFontSize = GUI.skin.label.fontSize;
            var scale = sizes.Scale;
            var fontSize = sizes.ScaledFont(sizes.logFontSize);
            GUI.skin.label.fontSize = fontSize;

            var categoryWidth = sizes.logCategoryWidth * scale;
            GUIContent messageContent = new GUIContent(log.message);
            necessaryHeight = Mathf.Max(rect.height, GUI.skin.label.CalcHeight(messageContent, rect.width - categoryWidth));
            rect.height = necessaryHeight;

            // BACKGROUND
            switch (log.type)
            {
                case LogType.Warning:
                    GUIHelper.DrawRect(rect, Color.yellowNice);
                    break;
                case LogType.Exception:
                case LogType.Assert:
                case LogType.Error:
                    GUIHelper.DrawRect(rect, Color.darkRed);
                    break;
            }
            GUIHelper.DrawRect(rect, index % 2 == 0 ? GUIHelper.transparentBlack07 : GUIHelper.transparentBlack06);

            // Icon
            var categoryColor = DebuggerAsset.GetCategoryColor(log.category);
            var r_categoryLabel = new Rect(rect.x + 2f, rect.y, categoryWidth - 6f, fontSize + 8f * scale);
            GUI.skin.label.fontStyle = FontStyle.Bold;
            using (new GUIHelper.GUIContentColorScope(categoryColor))
            {
                GUI.Label(r_categoryLabel, log.category.ToString());
            }
            GUI.skin.label.fontStyle = FontStyle.Normal;

            var messageRect = new Rect(rect.x + categoryWidth, rect.y, rect.width - categoryWidth, rect.height);
            GUI.Label(messageRect, messageContent);

            GUI.skin.label.fontSize = sizes.ScaledFont(sizes.logTimeFontSize);

            var timeRect = new Rect(rect.x + 2f, rect.y + fontSize + 4f * scale, categoryWidth - 6f, rect.height);
            GUI.Label(timeRect, log.timestamp.ToString("0.00"));

            GUI.skin.label.fontSize = prevLabelFontSize;
        }

        #endregion

        #endregion
#else
        // Release build : the on screen logger is not compiled
        public bool IsActive => false;
        public int LogsCount => 0;
#endif

        // ---------- ---------- ----------

        #region STATIC

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        #region Instance Creation

        private static OnScreenLogger Instance { get; set; }

        private static void CreateInstance()
        {
            if (Instance != null) return;

            var obj = new GameObject("OnScreen Debugger");
            DontDestroyOnLoad(obj);

            Instance = obj.AddComponent<OnScreenLogger>();
        }

        private static OnScreenLogger GetInstance()
        {
            if (Instance == null)
            {
                CreateInstance();
            }
            return Instance;
        }

        #endregion
#endif

        #region Log Behaviour

        /// <summary>
        /// Displays <paramref name="log"/> on screen for <paramref name="duration"/> seconds (unscaled time).<br></br>
        /// Does nothing in release builds.
        /// </summary>
        public static void Log(DebuggerLog log, float duration = DebuggerAsset.DEFAULT_SCREEN_LOG_DURATION)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GetInstance().AddScreenLog(log, duration);
#endif
        }

        #endregion

        #endregion
    }
}
