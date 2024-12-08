using Dhs5.Utility.Debuggers;
using Dhs5.Utility.Console;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour, IDatabaseElement
{
    [SerializeField] private bool _showTexture;
    [SerializeField] private Texture2D _texture;
    [SerializeField] private bool _showName;
    [SerializeField] private string _name;

    public bool HasDatabaseElementName(out string name)
    {
        name = _name;
        return _showName;
    }

    public bool HasDatabaseElementTexture(out Texture2D texture)
    {
        texture = _texture;
        return _showTexture;
    }

    private ulong m_updateKey;
    private void OnEnable()
    {
        TestUpdater.Register(true, UpdateCategory.SCREEN_LOG, OnUpdate, ref m_updateKey);
    }
    private void OnDisable()
    {
        TestUpdater.Register(false, UpdateCategory.SCREEN_LOG, OnUpdate, ref m_updateKey);
    }

    bool done;
    private void OnUpdate(float deltaTime)
    {
        TestDebugger.LogOnScreen(0, "Test " + TestUpdater.RealTime, LogType.Log, 0);

        if (!done && TestUpdater.Time > 2f)
        {
            TestDebugger.Log(DebugCategory.GAME, "Frame : " + TestUpdater.Frame, 0);
            done = true;
            TestUpdater.CallOnNextUpdate(OnNextUpdate);
        }
    }

    private void OnNextUpdate(float deltaTime)
    {
        TestDebugger.Log(DebugCategory.GAME, "On Next update, Frame : " + TestUpdater.Frame, 0);
    }
}
