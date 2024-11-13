using UnityEngine;
using System;

namespace Dhs5.Test
{
    public enum TestEnum
    {
        T1 = 0,
        T2 = 1,
    }

    [Flags]
    public enum TestEnumFlags
    {
        T1 = 1 << 0,
        T2 = 1 << 1,
    }
    
    public static class TestEnumExtension
    {
        public static TestScriptable GetValue(this TestEnum e)
        {
            return TestEnumDB.I
.GetValueAtIndex<TestScriptable>((int)e);        }

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
