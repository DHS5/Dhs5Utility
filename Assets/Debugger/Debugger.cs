using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Dhs5.Utility.Debuggers
{
    public static class Debugger
    {
        public const int MAX_DEBUGGER_LEVEL = 2;

        #region Database Access

        private static DebuggerDatabaseElement GetElement(Enum e)
        {
            return DebuggerDatabase.I.GetElementAtIndex(Convert.ToInt32(e)) as DebuggerDatabaseElement;
        }

        #endregion

        #region Log Verification

        private static bool CanLog(Enum e, LogType logType, int level, out DebuggerDatabaseElement element)
        {
            element = GetElement(e);
            if (element != null)
            {
                return element.CanLog(logType, level);
            }
            return false;
        }

        #endregion

        #region Log Enhancement

        private static string CategorizeMessage(object message, int level, DebuggerDatabaseElement element)
        {
            StringBuilder sb = new();

            sb.Append("<size=");
            sb.Append(14 - 2*level);
            sb.Append(">");
            sb.Append("<color=#");
            sb.Append(element.ColorString);
            sb.Append("><b>");
            sb.Append(element.name);
            sb.Append(" ");
            for (int i = 0; i < 3 - level; i++)
                sb.Append(">");
            sb.Append("</b></color> ");
            sb.Append(message);
            sb.Append("</size>");

            return sb.ToString();
        }

        #endregion

        #region Log Behaviour

        public static void Log(Enum e, object message, LogType logType, int level = MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            if (CanLog(e, logType, level, out DebuggerDatabaseElement element))
            {
                var categorizedMessage = CategorizeMessage(message, level, element);
                switch (logType)
                {
                    case LogType.Log: Debug.Log(categorizedMessage, context); break;
                    case LogType.Warning: Debug.LogWarning(categorizedMessage, context); break;
                    case LogType.Error: Debug.LogError(categorizedMessage, context); break;
                    case LogType.Exception: Debug.LogException(new Exception(categorizedMessage), context); break;
                    case LogType.Assert: Debug.LogAssertion(categorizedMessage, context); break;
                }
            }
        }

        public static void Log(Enum e, object message, int level = MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            if (CanLog(e, LogType.Log, level, out DebuggerDatabaseElement element))
            {
                Debug.Log(CategorizeMessage(message, level, element), context);
            }
        }

        #endregion
    }
}
