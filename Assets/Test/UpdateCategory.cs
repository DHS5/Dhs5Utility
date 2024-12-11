using UnityEngine;
using System;
using Dhs5.Utility.Updates;


public enum UpdateCategory
{
    BASE = 0,
    SCREEN_LOG = 1,
}

[Flags]
public enum UpdateCategoryFlags
{
    BASE = 1 << 0,
    SCREEN_LOG = 1 << 1,
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
     
    public static void Register(this UpdateCategory category, UpdateCallback callback, ref ulong key)
    {
        Updater<UpdateCategory>.Register(true, category, callback, ref key);
    }
    public static void Unregister(this UpdateCategory category, UpdateCallback callback, ref ulong key)
    {
        Updater<UpdateCategory>.Register(false, category, callback, ref key);
    }
}
