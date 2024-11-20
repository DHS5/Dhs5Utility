using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.Editors;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Debuggers
{
    public class OnScreenDebugger : MonoBehaviour
    {
        #region INSTANCE

        #region Properties

        public bool IsActive => m_activeScreenLogs.IsValid();
        public int LogsCount => m_activeScreenLogs.Count;

        #endregion

        #region STRUCT : ScreenLog

        private struct ScreenLog
        {
            private static int _count;

            public ScreenLog(string message, float time)
            {
                _count++;
                id = _count;
                this.message = message;
                this.time = time;
            }

            public readonly int id;
            public readonly string message;
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

        private void AddScreenLog(string message, float duration)
        {
            float time = Time.time;

            var screenLog = new ScreenLog(message, time);
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
                var rect = new Rect(10, 10, 500f, 300f);
                GUILayout.BeginArea(rect);
                var backgroundRect = EditorGUILayout.BeginVertical();
                EditorGUI.DrawRect(backgroundRect, EditorGUIHelper.transparentBlack05);

                ScreenLog log;
                for (int i = LogsCount - 1; i >= 0; i--)
                {
                    log = m_activeScreenLogs[i];
                    EditorGUILayout.LabelField(log.time + ": " + log.message);
                }

                EditorGUILayout.EndVertical();
                GUILayout.EndArea();
            }
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

        public static void Log(string message, float duration)
        {
            GetInstance().AddScreenLog(message, duration);
        }

        #endregion

        #endregion
    }
}
