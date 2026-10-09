using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
namespace Dhs5.Utility.Debugger
{
    /// <summary>
    /// EDITOR ONLY<br></br>
    /// Keeps the logs in memory for the D5 Console (all of them, or up to <see cref="DebuggerAsset.MaxLogsCount"/>).<br></br>
    /// Logs are identified by an ID that stays valid when the oldest logs are removed :
    /// IDs are consecutive, from <see cref="FirstLogId"/> (oldest log kept) to <see cref="NextLogId"/> - 1 (newest log).
    /// </summary>
    public static class DebuggerLogsContainer
    {
        #region Engine Callbacks

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _logs.Clear();
            _firstLogId = 0;
            Version++;
            Cleared = null;
        }

        #endregion

        #region Members

        private readonly static List<DebuggerLog> _logs = new();
        /// <summary>
        /// ID of _logs[0]
        /// </summary>
        private static int _firstLogId;

        #endregion

        #region Properties

        /// <summary>
        /// ID of the oldest log kept
        /// </summary>
        public static int FirstLogId => _firstLogId;
        /// <summary>
        /// ID the next log will get (newest log ID + 1)
        /// </summary>
        public static int NextLogId => _firstLogId + _logs.Count;
        public static int Count => _logs.Count;
        /// <summary>
        /// Changes when all logs are cleared : lets viewers know their cached data is outdated
        /// </summary>
        public static int Version { get; private set; }

        #endregion

        #region Events

        public static event Action Cleared;

        #endregion


        #region List Methods

        /// <returns>The ID of the added log</returns>
        public static int AddLog(DebuggerLog log)
        {
            _logs.Add(log);
            var id = NextLogId - 1;
            TrimIfNeeded();
            return id;
        }
        public static void ClearLogs()
        {
            // IDs keep increasing : an ID never refers to two different logs
            _firstLogId += _logs.Count;
            _logs.Clear();
            Version++;
            Cleared?.Invoke();
        }

        private static void TrimIfNeeded()
        {
            var maxCount = DebuggerAsset.MaxLogsCount;
            if (maxCount > 0 && _logs.Count > maxCount)
            {
                // Remove the oldest 10% at once, so that the list isn't shifted on every new log
                var removeCount = Mathf.Min(_logs.Count, _logs.Count - maxCount + Mathf.Max(1, maxCount / 10));
                _logs.RemoveRange(0, removeCount);
                _firstLogId += removeCount;
            }
        }

        #endregion

        #region Accessors

        public static IEnumerable<DebuggerLog> GetLogs() => _logs;
        public static bool TryGetLog(int logId, out DebuggerLog log)
        {
            return _logs.IsIndexValid(logId - _firstLogId, out log);
        }
        /// <returns>The log with <paramref name="logId"/>, or default if it was removed or cleared</returns>
        public static DebuggerLog GetLog(int logId)
        {
            TryGetLog(logId, out var log);
            return log;
        }

        #endregion
    }
}
#endif
