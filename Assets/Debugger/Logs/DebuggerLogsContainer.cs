using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Debugger
{
    public static class DebuggerLogsContainer
    {
        #region Members

        private readonly static List<DebuggerLog> _logs = new();

        #endregion

        #region Events

        public static event Action Cleared;

        #endregion


        #region List Methods

        public static int AddLog(DebuggerLog log)
        {
            _logs.Add(log);
            return _logs.Count - 1;
        }
        public static void ClearLogs()
        {
            _logs.Clear();
            Cleared?.Invoke();
        }

        #endregion

        #region Accessors

        public static IEnumerable<DebuggerLog> GetLogs() => _logs;
        public static DebuggerLog GetLogAtIndex(int index)
        {
            if (_logs.IsIndexValid(index, out var log)) return log;
            return default;
        }

        #endregion
    }
}
