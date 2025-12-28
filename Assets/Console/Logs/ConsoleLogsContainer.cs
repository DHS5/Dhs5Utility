using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Console
{
    public static class ConsoleLogsContainer
    {
        #region Members

        private readonly static List<ConsoleLog> _logs = new();

        #endregion


        #region Activation

        public static void SetActive()
        {
            //Application.logMessageReceived

            //UnityEngine.StackTraceUtility.ExtractStackTrace();
        }

        #endregion


        #region List Methods

        public static void AddLog(ConsoleLog log) => _logs.Add(log);
        public static void ClearLogs() => _logs.Clear();

        #endregion

        #region Accessors

        public static IEnumerable<ConsoleLog> GetLogs() => _logs;
        internal static IEnumerable<ConsoleLog> GetTestLogs()
        {
            yield return new ConsoleLog(EDebugCategory.GAME, LogType.Log, 0, "Test 1 aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" +
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" +
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", null);
            yield return new ConsoleLog(EDebugCategory.GAME, LogType.Warning, 1, "Test 2.1\nTest 2.2\nTest 2.3\nTest 2.4", null);
            yield return new ConsoleLog(EDebugCategory.GAME, LogType.Error, 0, "Test 3", null);
            yield return new ConsoleLog(EDebugCategory.UI, LogType.Log, 2, "Test 4", null);
            yield return new ConsoleLog(EDebugCategory.UI, LogType.Exception, 0, "Test 5", null);
            yield return new ConsoleLog(EDebugCategory.UI, LogType.Exception, 0, "Test 5", null);
            yield return new ConsoleLog(EDebugCategory.UI, LogType.Exception, 0, "Test 5", null);
            yield return new ConsoleLog(EDebugCategory.UI, LogType.Exception, 0, "Test 5", null);
            yield return new ConsoleLog(EDebugCategory.UI, LogType.Exception, 0, "Test 5", null);
        }

        #endregion
    }
}
