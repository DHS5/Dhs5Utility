using System;

public enum EDebugCategory : UInt16
{
    BASE = 0,
    GAME = 1,
    UI = 2,
    FLOW = 3,
}

[Flags]
public enum EDebugCategoryFlags : Int32
{
    BASE = 1 << 0,
    GAME = 1 << 1,
    UI = 1 << 2,
    FLOW = 1 << 3,
}

public static class DebugCategoryExtensions
{
    public static Boolean HasCategory(this EDebugCategoryFlags flags, EDebugCategory category)
    {
        return (flags & ((EDebugCategoryFlags)(1 << ((int)category)))) != 0;
    }
}
