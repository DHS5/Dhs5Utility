using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.GUIs;

namespace Dhs5.Utility.Debugger
{
    public class OnScreenLogger : MonoBehaviour
    {
        #region STRUCT : LogDisposalTime

        private struct LogDisposalTime : IComparable<LogDisposalTime>
        {
            public LogDisposalTime(int index, float disposalTime)
            {
                this.logIndex = index;
                this.disposalTime = disposalTime;
            }

            public readonly int logIndex;
            public readonly float disposalTime;

            public int CompareTo(LogDisposalTime other)
            {
                return disposalTime.CompareTo(other.disposalTime);
            }
        }

        #endregion


        #region INSTANCE

        #region Properties

        public bool IsActive => m_activeOnScreenLogs.IsValid();
        public int LogsCount => m_activeOnScreenLogs.Count;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            DebuggerLogsContainer.Cleared += OnLogsCleared;
        }
        private void OnDisable()
        {
            DebuggerLogsContainer.Cleared -= OnLogsCleared;
        }

        #endregion


        #region ScreenLogs Management

        private readonly List<int> m_activeOnScreenLogs = new();
        private readonly List<LogDisposalTime> m_logsDisposalTime = new();

        private void AddScreenLog(int logIndex, float duration)
        {
            m_activeOnScreenLogs.Add(logIndex);

            var logDisposalTime = new LogDisposalTime(logIndex, Time.time + duration);
            m_logsDisposalTime.Add(logDisposalTime);

            SortDisposalTimes();
        }
        private void RemoveScreenLog(int logIndex)
        {
            m_activeOnScreenLogs.Remove(logIndex);
        }

        private void SortDisposalTimes()
        {
            m_logsDisposalTime.Sort((l1,l2) => l1.CompareTo(l2));
        }

        #endregion

        #region Update

        private void LateUpdate()
        {
            if (IsActive) 
            {
                float time = Time.time;

                foreach (var log in m_logsDisposalTime)
                {
                    if (time >= log.disposalTime)
                    {
                        RemoveScreenLog(log.logIndex);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        #endregion

        #region Callbacks

        private void OnLogsCleared()
        {
            m_activeOnScreenLogs.Clear();
            m_logsDisposalTime.Clear();
        }

        #endregion


        #region GUI

        private void OnGUI()
        {
            if (IsActive)
            {
                float logMinHeight = 50f;//TODO settings
                var rect = new Rect(0f, 0f, 800f, 500f);
                var logRect = new Rect(rect.x, rect.y, rect.width, logMinHeight);

                for (int i = LogsCount - 1; i >= 0 && logRect.y + logMinHeight <= rect.y + rect.height; i--)
                {
                    var logIndex = m_activeOnScreenLogs[i];
                    OnScreenLogGUI(logRect, i, DebuggerLogsContainer.GetLogAtIndex(logIndex), out var logNecessaryHeight);
                    logRect.y += logNecessaryHeight;
                }
            }
        }

        private void OnScreenLogGUI(Rect rect, int index, DebuggerLog log, out float necessaryHeight)
        {
            var prevLabelFontSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = 22;

            GUIContent messageContent = new GUIContent(log.message);
            necessaryHeight = Mathf.Max(rect.height, GUI.skin.label.CalcHeight(messageContent, rect.width - 150f));
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
            var r_categoryLabel = new Rect(rect.x + 2f, rect.y, 144f, 30f);
            GUI.skin.label.fontStyle = FontStyle.Bold;
            using (new GUIHelper.GUIContentColorScope(categoryColor))
            {
                GUI.Label(r_categoryLabel, log.category.ToString());
            }
            GUI.skin.label.fontStyle = FontStyle.Normal;

            var messageRect = new Rect(150f, rect.y, rect.width - 150f, rect.height);
            GUI.Label(messageRect, messageContent);

            GUI.skin.label.fontSize = 18;

            var timeRect = new Rect(rect.x + 2f, rect.y + 26f, 144f, rect.height);
            GUI.Label(timeRect, log.timestamp.ToString("0.00"));

            GUI.skin.label.fontSize = prevLabelFontSize;
        }

        #endregion

        #endregion

        // ---------- ---------- ---------- 

        #region STATIC

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

        #region Log Behaviour

        public static void Log(int logIndex, float duration = DebuggerAsset.DEFAULT_SCREEN_LOG_DURATION)
        {
            GetInstance().AddScreenLog(logIndex, duration);
        }

        #endregion

        #endregion
    }
}
