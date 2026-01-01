using UnityEngine;
using System;
using Dhs5.Utility.Databases;
using Dhs5.Utility.Debuggers;


public enum EDebugCategory
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
    public static DebugCategoryElement GetValue(this EDebugCategory e)
    {
        return Database.Get<DebuggerDatabase>().GetDataAtIndex<DebugCategoryElement>((int)e);
    }

    public static bool Contains(this DebugCategoryFlags flag, EDebugCategory e)
    {
        return (flag & ((DebugCategoryFlags)(1 << (int)e))) != 0;
    }

    public static bool Contains(this DebugCategoryFlags flag, DebugCategoryFlags other)
    {
        return (flag & other) != 0;
    }
    public static void Log(this EDebugCategory category, object message, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
    {
        Debugger<EDebugCategory>.Log(category, message, level, onScreen, context);
    }
    public static void LogWarning(this EDebugCategory category, object message, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)
    {
        Debugger<EDebugCategory>.LogWarning(category, message, level, onScreen, context);
    }
    public static void LogError(this EDebugCategory category, object message, bool onScreen = true, UnityEngine.Object context = null)
    {
        Debugger<EDebugCategory>.LogError(category, message, onScreen, context);
    }
    public static void LogAlways(this EDebugCategory category, object message, LogType logType = LogType.Error, bool onScreen = true, UnityEngine.Object context = null)
    {
        Debugger<EDebugCategory>.LogAlways(category, message, logType, onScreen, context);
    }
    public static void LogOnScreen(this EDebugCategory category, object message, LogType logType = LogType.Log, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, float duration = BaseDebugger.DEFAULT_SCREEN_LOG_DURATION)
    {
        Debugger<EDebugCategory>.LogOnScreen(category, message, logType, level, duration);
    }
}
