using Dhs5.Utility.Debugger;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RuntimeDebuggerTest : MonoBehaviour
{
    [SerializeField] private EDebugCategory m_category;

    [RuntimeDebug]
    [SerializeField] private int m_runtimeTest1;

    [RuntimeDebug]
    [SerializeField] private ScriptableObject m_runtimeTest2;

    [RuntimeDebug]
    public float RuntimeTest3 => m_runtimeTest1;

    [RuntimeDebug]
    private float RuntimeTest4 { get; set; }

    [RuntimeDebug]
    public float RuntimeTest5
    {
        get
        {
            return 5f;
        }
    }

    [RuntimeDebug]
    public static string RuntimeTest6 => "Test 6";

    [RuntimeDebug]
    private Vector3 m_runtimeTest7 = new Vector3(0.1f, 54f, 89.3f);
    
    [RuntimeDebug]
    private static AnimationCurve m_runtimeTest8;
    
    [RuntimeDebug]
    [SerializeField] private ulong m_runtimeTest9;

    private void OnEnable()
    {
        RuntimeDebugger.Register(true, m_category, this);
        RuntimeDebuggerStaticTest.Register(true);
    }
    private void OnDisable()
    {
        RuntimeDebugger.Register(false, m_category, this);
        RuntimeDebuggerStaticTest.Register(false);
    }

#if UNITY_EDITOR
    [RuntimeDebug]
    public void Editor_DrawRuntimeDebugExtensionGUI()
    {
        EditorGUILayout.LabelField("Extension");
    }
#endif
}
