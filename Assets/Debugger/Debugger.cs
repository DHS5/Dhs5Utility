using System;
using UnityEngine;

namespace Dhs5.Utility.Debuggers
{
    public class Debugger<DebugEnum> where DebugEnum : Enum
    {
        #region Public Log Behaviour

        public static void ComplexLog(DebugEnum e, object message, LogType logType, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            BaseDebugger.ComplexLog(e, message, logType, level, onScreen, context);
        }

        // --- LOG ---
        public static void Log(DebugEnum e, object message, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            BaseDebugger.Log(e, message, level, onScreen, context);
        }
        
        // --- WARNING ---
        public static void LogWarning(DebugEnum e, object message, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
        {
            BaseDebugger.LogWarning(e, message, level, onScreen, context);
        }
        
        // --- ERROR ---
        public static void LogError(DebugEnum e, object message, bool onScreen = true, UnityEngine.Object context = null)
        {
            BaseDebugger.LogError(e, message, onScreen, context);
        }
        
        // --- ALWAYS ---
        public static void LogAlways(DebugEnum e, object message, LogType logType = LogType.Error, bool onScreen = true, UnityEngine.Object context = null)
        {
            BaseDebugger.LogAlways(e, message, logType, onScreen, context);
        }
        
        // --- ON SCREEN ---
        public static void LogOnScreen(DebugEnum e, object message, LogType logType = LogType.Log, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, float duration = BaseDebugger.DEFAULT_SCREEN_LOG_DURATION)
        {
            BaseDebugger.LogOnScreen(e, message, logType, level, duration);
        }

        #endregion
    }
}
