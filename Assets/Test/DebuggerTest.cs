using UnityEngine;

public class DebuggerTest : MonoBehaviour
{
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
}
