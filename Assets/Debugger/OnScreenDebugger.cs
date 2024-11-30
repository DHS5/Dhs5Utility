using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.GUIs;

namespace Dhs5.Utility.Debuggers
{
    public class OnScreenDebugger : MonoBehaviour
    {
        #region INSTANCE

        #region Members

        private GUIStyle m_timeStyle;
        private GUIStyle m_logStyle;

        #endregion

        #region Properties

        public bool IsActive => m_activeScreenLogs.IsValid();
        public int LogsCount => m_activeScreenLogs.Count;

        #endregion

        #region Core Behaviour

        private void Start()
        {
            InitStyles();
        }

        #endregion

        #region Styles

        private void InitStyles()
        {
            m_timeStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleRight,
                wordWrap = true,
                fontSize = DebuggerSettings.ScreenLogsTimeSize,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                }
            };
            m_logStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                wordWrap = true,
                fontSize = 20,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                }
            };
        }

        #endregion


        #region STRUCT : ScreenLog

        private struct ScreenLog
        {
            private static int _count;

            public ScreenLog(string message, LogType logType, float time)
            {
                _count++;
                id = _count;
                this.message = message;
                this.logType = logType;
                this.time = time;
            }

            public readonly int id;
            public readonly string message;
            public readonly LogType logType;
            public readonly float time;
        }

        #endregion

        #region STRUCT : LogDisposalTime

        private struct LogDisposalTime : IComparable<LogDisposalTime>
        {
            public LogDisposalTime(int id, float disposalTime)
            {
                this.logID = id;
                this.disposalTime = disposalTime;
            }

            public readonly int logID;
            public readonly float disposalTime;

            public int CompareTo(LogDisposalTime other)
            {
                return disposalTime.CompareTo(other.disposalTime);
            }
        }

        #endregion

        #region ScreenLogs Management

        private List<ScreenLog> m_activeScreenLogs = new();
        private List<LogDisposalTime> m_logsDisposalTime = new();

        private void AddScreenLog(string message, LogType logType, float duration)
        {
            float time = Time.time;

            var screenLog = new ScreenLog(message, logType, time);
            m_activeScreenLogs.Add(screenLog);

            var logDisposalTime = new LogDisposalTime(screenLog.id, time + duration);
            m_logsDisposalTime.Add(logDisposalTime);

            SortDisposalTimes();
        }
        private void RemoveScreenLog(int id)
        {
            int index = m_activeScreenLogs.FindIndex(l => l.id == id);
            if (index != -1)
            {
                m_activeScreenLogs.RemoveAt(index);
            }
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
                        RemoveScreenLog(log.logID);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        #endregion


        #region GUI

        private void OnGUI()
        {
            if (IsActive)
            {
                float logBaseHeight = DebuggerSettings.ScreenLogHeight;
                var rect = DebuggerSettings.ScreenLogsRect;
                var logRect = new Rect(rect.x, rect.y, rect.width, logBaseHeight);

                ScreenLog screenLog;
                for (int i = LogsCount - 1; i >= 0 && logRect.y + logBaseHeight <= rect.y + rect.height; i--)
                {
                    screenLog = m_activeScreenLogs[i];
                    OnScreenLogGUI(logRect, i, screenLog, out var logNecessaryHeight);
                    logRect.y += logNecessaryHeight;
                }
            }
        }

        private void OnScreenLogGUI(Rect rect, int index, ScreenLog log, out float necessaryHeight)
        {
            // PARAMETERS
            float messageRectX = rect.x;
            float messageRectWidth = rect.width;

            float timeRectWidth = 75f;
            float space = 15f;

            if (DebuggerSettings.ShowScreenLogsTime)
            {
                messageRectX = rect.x + timeRectWidth + space;
                messageRectWidth = rect.width - timeRectWidth - space;
            }

            GUIContent messageContent = new GUIContent(log.message);
            necessaryHeight = Mathf.Max(rect.height, m_timeStyle.CalcHeight(messageContent, messageRectWidth));

            rect.height = necessaryHeight;

            // BACKGROUND
            switch (log.logType)
            {
                case LogType.Warning:
                    GUIHelper.DrawRect(rect, new Color(1f, 0.92f, 0.016f, 0.5f)); 
                    break;
                case LogType.Exception:
                case LogType.Assert:
                case LogType.Error:
                    GUIHelper.DrawRect(rect, new Color(1f, 0f, 0f, 0.5f)); 
                    break;
            }
            GUIHelper.DrawRect(rect, index % 2 == 0 ? GUIHelper.transparentBlack05 : GUIHelper.transparentBlack04);

            if (DebuggerSettings.ShowScreenLogsTime)
            {
                var timeRect = new Rect(rect.x, rect.y, timeRectWidth, rect.height);
                GUI.Label(timeRect, log.time.ToString("0.00"), m_timeStyle);
            }

            var messageRect = new Rect(messageRectX, rect.y, messageRectWidth, rect.height);
            GUI.Label(messageRect, messageContent, m_logStyle);
        }

        #endregion

        #endregion

        // ---------- ---------- ---------- 

        #region STATIC

        #region Instance Creation

        private static OnScreenDebugger Instance { get; set; }

        private static void CreateInstance()
        {
            if (Instance != null) return;

            var obj = new GameObject("OnScreen Debugger");
            DontDestroyOnLoad(obj);

            Instance = obj.AddComponent<OnScreenDebugger>();
        }

        private static OnScreenDebugger GetInstance()
        {
            if (Instance == null)
            {
                CreateInstance();
            }
            return Instance;
        }

        #endregion

        #region Log Behaviour

        public static void Log(string message, LogType logType, float duration)
        {
            GetInstance().AddScreenLog(message, logType, duration);
        }

        #endregion

        #endregion
    }
}
