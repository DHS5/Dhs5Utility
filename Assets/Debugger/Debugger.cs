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
        private const float DEFAULT_SCREEN_LOG_DURATION = 5.0f;

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

        private static string CategorizeMessage(object message, int level, DebuggerDatabaseElement element, bool onScreen = false)
        {
            StringBuilder sb = new();

            int size = onScreen ? DebuggerSettings.GetScreenLogSize(level) : DebuggerSettings.GetConsoleLogSize(level);
            sb.Append("<size=");
            sb.Append(size);
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

        #region Public Log Behaviour

        public static void ComplexLog(Enum e, object message, LogType logType, int level = MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            if (CanLog(e, logType, level, out DebuggerDatabaseElement element))
            {
                Internal_Log(message, logType, element, level, onScreen, context);
            }
        }
        public static void ComplexLog(int key, object message, LogType logType, int level = MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            if (CanLog(key, logType, level, out DebuggerDatabaseElement element))
            {
                Internal_Log(message, logType, element, level, onScreen, context);
            }
        }

        // --- LOG ---
        public static void Log(Enum e, object message, int level = MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            var logType = LogType.Log;
            if (CanLog(e, logType, level, out DebuggerDatabaseElement element))
            {
                Internal_Log(message, logType, element, level, onScreen, context);
            }
        }
        public static void Log(int key, object message, int level = MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            var logType = LogType.Log;
            if (CanLog(key, logType, level, out DebuggerDatabaseElement element))
            {
                Internal_Log(message, logType, element, level, onScreen, context);
            }
        }
        
        // --- WARNING ---
        public static void LogWarning(Enum e, object message, int level = MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            var logType = LogType.Warning;
            if (CanLog(e, logType, level, out DebuggerDatabaseElement element))
            {
                Internal_Log(message, logType, element, level, onScreen, context);
            }
        }
        public static void LogWarning(int key, object message, int level = MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            var logType = LogType.Warning;
            if (CanLog(key, logType, level, out DebuggerDatabaseElement element))
            {
                Internal_Log(message, logType, element, level, onScreen, context);
            }
        }
        
        // --- ERROR ---
        public static void LogError(Enum e, object message, bool onScreen = true, UnityEngine.Object context = null)
        {
            var logType = LogType.Error;
            int level = 0;
            if (CanLog(e, logType, level, out DebuggerDatabaseElement element))
            {
                Internal_Log(message, logType, element, level, onScreen, context);
            }
        }
        public static void LogError(int key, object message, bool onScreen = true, UnityEngine.Object context = null)
        {
            var logType = LogType.Error;
            int level = 0;
            if (CanLog(key, logType, level, out DebuggerDatabaseElement element))
            {
                Internal_Log(message, logType, element, level, onScreen, context);
            }
        }
        
        // --- ALWAYS ---
        public static void LogAlways(Enum e, object message, LogType logType = LogType.Error, bool onScreen = true, UnityEngine.Object context = null)
        {
            var element = GetElement(e);
            if (element != null)
            {
                Internal_Log(message, logType, element, 0, onScreen, context);
            }
        }
        public static void LogAlways(int key, object message, LogType logType = LogType.Error, bool onScreen = true, UnityEngine.Object context = null)
        {
            var element = GetElement(key);
            if (element != null)
            {
                Internal_Log(message, logType, element, 0, onScreen, context);
            }
        }
        
        // --- ON SCREEN ---
        public static void LogOnScreen(Enum e, object message, LogType logType = LogType.Log, int level = MAX_DEBUGGER_LEVEL, float duration = DEFAULT_SCREEN_LOG_DURATION)
        {
            var element = GetElement(e);
            if (element != null)
            {
                Internal_LogOnScreen(message, element, logType, level, DebuggerSettings.DefaultScreenLogDuration);
            }
        }
        public static void LogOnScreen(int key, object message, LogType logType = LogType.Log, int level = MAX_DEBUGGER_LEVEL, float duration = DEFAULT_SCREEN_LOG_DURATION)
        {
            var element = GetElement(key);
            if (element != null)
            {
                Internal_LogOnScreen(message, element, logType, level, DebuggerSettings.DefaultScreenLogDuration);
            }
        }

        #endregion

        #region Private Log Behaviour

        private static void Internal_Log(object message, LogType logType, DebuggerDatabaseElement element, int level, bool onScreen, UnityEngine.Object context)
        {
            string consoleMessage = CategorizeMessage(message, level, element, false);

            if (element.ShowInConsole)
            {
                switch (logType)
                {
                    case LogType.Log: Debug.Log(consoleMessage, context); break;
                    case LogType.Warning: Debug.LogWarning(consoleMessage, context); break;
                    case LogType.Exception:
                    case LogType.Assert:
                    case LogType.Error: Debug.LogError(consoleMessage, context); break;
                }
            }

            if (onScreen)
            {
                Internal_LogOnScreen(message, element, logType, level, DebuggerSettings.DefaultScreenLogDuration);
            }
        }

        private static void Internal_LogOnScreen(object message, DebuggerDatabaseElement element, LogType logType, int level, float duration)
        {
            if (element.ShowOnScreen && Application.isPlaying)
            {
                OnScreenDebugger.Log(CategorizeMessage(message, level, element, true), logType, duration);
            }
        }

        #endregion
    }
}
