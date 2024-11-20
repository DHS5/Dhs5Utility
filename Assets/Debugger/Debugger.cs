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
        private static DebuggerDatabaseElement GetElement(int key)
        {
            return DebuggerDatabase.I.GetElementAtIndex(key) as DebuggerDatabaseElement;
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
        private static bool CanLog(int key, LogType logType, int level, out DebuggerDatabaseElement element)
        {
            element = GetElement(key);
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
            for (int i = 0; i < MAX_DEBUGGER_LEVEL + 1 - level; i++)
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
                switch (logType)
                {
                    case LogType.Log: Debug.Log(CategorizeMessage(message, level, element), context); break;
                    case LogType.Warning: Debug.LogWarning(CategorizeMessage(message, level, element), context); break;
                    case LogType.Error: Debug.LogError(CategorizeMessage(message, 0, element), context); break;
                    case LogType.Exception: Debug.LogException(new Exception(CategorizeMessage(message, 0, element)), context); break;
                    case LogType.Assert: Debug.LogAssertion(CategorizeMessage(message, 0, element), context); break;
                }
            }
        }

        // --- LOG ---
        public static void Log(Enum e, object message, int level = MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            if (CanLog(e, LogType.Log, level, out DebuggerDatabaseElement element))
            {
                Debug.Log(CategorizeMessage(message, level, element), context);
            }
        }
        public static void Log(int key, object message, int level = MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            if (CanLog(key, LogType.Log, level, out DebuggerDatabaseElement element))
            {
                Debug.Log(CategorizeMessage(message, level, element), context);
            }
        }
        
        // --- WARNING ---
        public static void LogWarning(Enum e, object message, int level = MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            if (CanLog(e, LogType.Warning, level, out DebuggerDatabaseElement element))
            {
                Debug.LogWarning(CategorizeMessage(message, level, element), context);
            }
        }
        public static void LogWarning(int key, object message, int level = MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            if (CanLog(key, LogType.Warning, level, out DebuggerDatabaseElement element))
            {
                Debug.LogWarning(CategorizeMessage(message, level, element), context);
            }
        }
        
        // --- ERROR ---
        public static void LogError(Enum e, object message, bool onScreen = false, UnityEngine.Object context = null)
        {
            if (CanLog(e, LogType.Error, 0, out DebuggerDatabaseElement element))
            {
                Debug.LogError(CategorizeMessage(message, 0, element), context);
            }
        }
        public static void LogError(int key, object message, bool onScreen = true, UnityEngine.Object context = null)
        {
            if (CanLog(key, LogType.Error, 0, out DebuggerDatabaseElement element))
            {
                Debug.LogError(CategorizeMessage(message, 0, element), context);
            }
        }
        
        // --- ALWAYS ---
        public static void LogAlways(Enum e, object message, LogType logType = LogType.Error, bool onScreen = false, UnityEngine.Object context = null)
        {
            var element = GetElement(e);
            if (element != null)
            {
                string categorizedMessage = CategorizeMessage(message, 0, element);
                switch (logType)
                {
                    case LogType.Log: Debug.Log(categorizedMessage, context); break;
                    case LogType.Warning: Debug.LogWarning(categorizedMessage, context); break;
                    case LogType.Error: Debug.LogError(categorizedMessage, context); break;
                    case LogType.Assert: Debug.LogAssertion(categorizedMessage, context); break;
                    case LogType.Exception: Debug.LogException(new Exception(categorizedMessage), context); break;
                }
            }
        }
        public static void LogAlways(int key, object message, LogType logType = LogType.Error, bool onScreen = true, UnityEngine.Object context = null)
        {
            var element = GetElement(key);
            if (element != null)
            {
                string categorizedMessage = CategorizeMessage(message, 0, element);
                switch (logType)
                {
                    case LogType.Log: Debug.Log(categorizedMessage, context); break;
                    case LogType.Warning: Debug.LogWarning(categorizedMessage, context); break;
                    case LogType.Error: Debug.LogError(categorizedMessage, context); break;
                    case LogType.Assert: Debug.LogAssertion(categorizedMessage, context); break;
                    case LogType.Exception: Debug.LogException(new Exception(categorizedMessage), context); break;
                }
            }
        }
        
        // --- ON SCREEN ---
        public static void LogOnScreen(Enum e, object message, LogType logType = LogType.Error)
        {
            var element = GetElement(e);
            if (element != null)
            {
                OnScreenDebugger.Log(CategorizeMessage(message, 0, element), 5f);
            }
        }
        public static void LogOnScreen(int key, object message)
        {
            var element = GetElement(key);
            if (element != null)
            {
                OnScreenDebugger.Log(CategorizeMessage(message, 0, element), 5f);
            }
        }

        #endregion
    }
}
