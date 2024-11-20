using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

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
            m_timeStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleRight,
                wordWrap = false,
                fontSize = 18,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                }
            };
            m_logStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                richText = true,
                wordWrap = false,
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
                float logHeight = 20f;
                int maxLogCount = 25;

                var rect = new Rect(10, 10, 700f, 0f);
                var logRect = new Rect(rect.x, rect.y, rect.width, logHeight);

                for (int i = LogsCount - 1; i >= Mathf.Max(0, LogsCount - maxLogCount); i--)
                {
                    OnScreenLogGUI(logRect, i, m_activeScreenLogs[i]);
                    logRect.y += logHeight;
                }
            }
        }

        private void OnScreenLogGUI(Rect rect, int index, ScreenLog log)
        {
            switch (log.logType)
            {
                case LogType.Warning:
                    EditorGUI.DrawRect(rect, new Color(1f, 0.92f, 0.016f, 0.5f)); 
                    break;
                case LogType.Exception:
                case LogType.Assert:
                case LogType.Error:
                    EditorGUI.DrawRect(rect, new Color(1f, 0f, 0f, 0.5f)); 
                    break;
            }
            EditorGUI.DrawRect(rect, index % 2 == 0 ? EditorGUIHelper.transparentBlack05 : EditorGUIHelper.transparentBlack04);

            float timeRectWidth = 75f;
            float space = 15f;
            var timeRect = new Rect(rect.x, rect.y, timeRectWidth, rect.height);
            EditorGUI.LabelField(timeRect, log.time.ToString("0.00"), m_timeStyle);

            var messageRect = new Rect(rect.x + timeRectWidth + space, rect.y, rect.width - timeRectWidth - space, rect.height);
            EditorGUI.LabelField(messageRect, log.message, m_logStyle);
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
