using UnityEngine;
using System;
using Dhs5.Utility.Debugger;

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
}
