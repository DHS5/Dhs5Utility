using Dhs5.Utility.Debugger;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RuntimeDebuggerStaticTest
{
    public static void Register(bool register)
    {
        RuntimeDebugger.RegisterStaticClass(register, EDebugCategory.GAME, typeof(RuntimeDebuggerStaticTest));
    }

    [RuntimeDebug] private static int _static1 = 5;

    [RuntimeDebug] private static int _static2 => _static1;

    [RuntimeDebug] private static float _static3 { get; set; } = 8.2f;

#if UNITY_EDITOR

    [RuntimeDebug]
    private static void StaticMethod1()
    {
        EditorGUILayout.LabelField("Method 1");
    }
    
    [RuntimeDebug]
    public static void StaticMethod2()
    {
        EditorGUILayout.LabelField("Method 2");
    }
    
    [RuntimeDebug]
    public static bool StaticMethod3()
    {
        EditorGUILayout.LabelField("Method 3");
        return true;
    }
    
    [RuntimeDebug]
    public static void StaticMethod4(int param)
    {
        EditorGUILayout.LabelField("Method 4");
    }

#endif
}
