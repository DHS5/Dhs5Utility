using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Database("Test/Scriptable", typeof(TestScriptable))]
public class TestScriptableDatabase : ScriptableDatabase<TestScriptableDatabase>
{
#if UNITY_EDITOR

    internal override void Editor_OnNewElementCreated(Object element)
    {
        base.Editor_OnNewElementCreated(element);

        int index = FindIndexOfElement(element);
        if (index != -1)
        {
            element.name = "Element " + index;
        }
    }

#endif
}
