using UnityEngine;
using System;
using Dhs5.Utility.Updates;


public enum UpdateCategory
{
    BASE = 0,
}

[Flags]
public enum UpdateCategoryFlags
{
    BASE = 1 << 0,
}

public static class UpdateCategoryExtension
{
    public static UpdaterDatabaseElement GetValue(this UpdateCategory e)
    {
        return UpdaterDatabase.I.GetValueAtIndex<UpdaterDatabaseElement>((int)e);    }

    public static bool Contains(this UpdateCategoryFlags flag, UpdateCategory e)
    {
        return (flag & ((UpdateCategoryFlags)(1 << (int)e))) != 0;
    }

    public static bool Contains(this UpdateCategoryFlags flag, UpdateCategoryFlags other)
    {
        return (flag & other) != 0;
    }
}
