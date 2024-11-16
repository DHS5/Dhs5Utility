using UnityEngine;
using System;
using Dhs5.Utility.Debugger;

public enum DebugCategory
{
    UI = 0,
    GAME = 1,
    NONE = 2,
}

[Flags]
public enum DebugCategoryFlags
{
    UI = 1 << 0,
    GAME = 1 << 1,
    NONE = 1 << 2,
}

public static class DebugCategoryExtension
{
    public static DebuggerDatabaseElement GetValue(this DebugCategory e)
    {
        return DebuggerDatabase.I
.GetValueAtIndex<DebuggerDatabaseElement>((int)e);    }

    public static bool Contains(this DebugCategoryFlags flag, DebugCategory e)
    {
        return (flag & ((DebugCategoryFlags)(1 << (int)e))) != 0;
    }

    public static bool Contains(this DebugCategoryFlags flag, DebugCategoryFlags other)
    {
        return (flag & other) != 0;
    }
}
