using Dhs5.Utility.Debugger;
using UnityEngine;

public class DebuggerTest : MonoBehaviour
{
    [RuntimeDebug]
    private int m_runtimeTest1;
    
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

    private void Start()
    {
        Logger.Log(EDebugCategory.BASE, "Test 1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111", level: 0, onScreen: true, this);
        Logger.Log(EDebugCategory.GAME, "Test 2\n2\n2\n2\n2", level: 0, onScreen: true, this);
        Logger.LogWarning(EDebugCategory.GAME, "Test 3", level: 1, onScreen: true, this);
        Logger.LogError(EDebugCategory.GAME, "Test 4", onScreen: true, this);
        Logger.Log(EDebugCategory.UI, "Test 5", level:2, onScreen: true, this);
        Logger.LogWarning(EDebugCategory.UI, "Test 6", level:0, onScreen: true, this);
        Logger.Log(EDebugCategory.FLOW, "Test 7", level:0, onScreen: true, this);
        Logger.Log(EDebugCategory.FLOW, "Test 8", level:1, onScreen: true, this);
    }

    private void OnEnable()
    {
        RuntimeDebugger.Register(true, EDebugCategory.GAME, this);
    }
    private void OnDisable()
    {
        RuntimeDebugger.Register(false, EDebugCategory.GAME, this);
    }
}
