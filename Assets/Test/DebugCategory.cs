using UnityEngine;
using System;
using Dhs5.Utility.Debuggers;


public enum DebugCategory
{
    NONE = 0,
    GAME = 1,
    FLOW = 2,
    UI = 3,
}

[Flags]
public enum DebugCategoryFlags
{
    NONE = 1 << 0,
    GAME = 1 << 1,
    FLOW = 1 << 2,
    UI = 1 << 3,
}

public static class DebugCategoryExtension
{
    public static DebuggerDatabaseElement GetValue(this DebugCategory e)
    {
        return DebuggerDatabase.I.GetValueAtIndex<DebuggerDatabaseElement>((int)e);    }

    public static bool Contains(this DebugCategoryFlags flag, DebugCategory e)
    {
        return (flag & ((DebugCategoryFlags)(1 << (int)e))) != 0;
    }

    public static bool Contains(this DebugCategoryFlags flag, DebugCategoryFlags other)
    {
        return (flag & other) != 0;
    }
    public static void Log(this DebugCategory category, object message, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
    {
        Debugger<DebugCategory>.Log(category, message, level, onScreen, context);
    }
    public static void LogWarning(this DebugCategory category, object message, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
    {
        Debugger<DebugCategory>.LogWarning(category, message, level, onScreen, context);
    }
    public static void LogError(this DebugCategory category, object message, bool onScreen = true, UnityEngine.Object context = null)
    {
        Debugger<DebugCategory>.LogError(category, message, onScreen, context);
    }
    public static void LogAlways(this DebugCategory category, object message, LogType logType = LogType.Error, bool onScreen = true, UnityEngine.Object context = null)
    {
        Debugger<DebugCategory>.LogAlways(category, message, logType, onScreen, context);
    }
    public static void LogOnScreen(this DebugCategory category, object message, LogType logType = LogType.Log, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, float duration = BaseDebugger.DEFAULT_SCREEN_LOG_DURATION)
    {
        Debugger<DebugCategory>.LogOnScreen(category, message, logType, level, duration);
    }
}
