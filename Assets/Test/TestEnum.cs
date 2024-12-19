using UnityEngine;
using System;
using Dhs5.Utility.Databases;

namespace Dhs5.Test
{
    public enum TestEnum
    {
        T1 = 0,
        T2 = 1,
        T3 = 2,
        T4 = 3,
        T5 = 4,
    }

    [Flags]
    public enum TestEnumFlags
    {
        T1 = 1 << 0,
        T2 = 1 << 1,
        T3 = 1 << 2,
        T4 = 1 << 3,
        T5 = 1 << 4,
    }
    
    public static class TestEnumExtension
    {
        public static TestScriptable GetValue(this TestEnum e)
        {
            return BaseDatabase.Get<TestEnumDB>().GetValueAtIndex<TestScriptable>((int)e);        
        }

        public static bool Contains(this TestEnumFlags flag, TestEnum e)
        {
            return (flag & ((TestEnumFlags)(1 << (int)e))) != 0;
        }

        public static bool Contains(this TestEnumFlags flag, TestEnumFlags other)
        {
            return (flag & other) != 0;
        }
    }
}
