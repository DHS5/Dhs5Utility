using Dhs5.Utility.Databases;
using Dhs5.Utility.Debuggers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour, IDataContainerElement
{
    [SerializeField] private int m_uid;
    [SerializeField] private bool _showTexture;
    [SerializeField] private Texture2D _texture;
    [SerializeField] private bool _showName;
    [SerializeField] private string _name;
    [SerializeField] private DataPicker _dataPicker1;
    [SerializeField] private DataPicker<DebuggerDatabase> _dataPicker2;

    public int UID => m_uid;

#if UNITY_EDITOR
    public void Editor_SetUID(int uid)
    {
        m_uid = uid;
    }
    public bool Editor_HasDataContainerElementName(out string name)
    {
        name = _name;
        return _showName;
    }

    public bool Editor_HasDataContainerElementTexture(out Texture2D texture)
    {
        texture = _texture;
        return _showTexture;
    }
#endif

    private ulong m_update1Key;
    private void OnEnable()
    {
        //TestUpdater.Register(true, UpdateCategory.SCREEN_LOG, OnUpdate, ref m_update1Key);
        TestUpdater.OnLateUpdate += OnLateUpdate;
        UpdateCategory.SCREEN_LOG.Register(OnUpdate, ref m_update1Key);
    }
    private void OnDisable()
    {
        //TestUpdater.Register(false, UpdateCategory.SCREEN_LOG, OnUpdate, ref m_update1Key);
        TestUpdater.OnLateUpdate -= OnLateUpdate;
        UpdateCategory.SCREEN_LOG.Unregister(OnUpdate, ref m_update1Key);
    }

    bool done;
    bool done2;
    private void OnUpdate(float deltaTime)
    {
        //TestDebugger.LogOnScreen(0, "Test " + TestUpdater.RealTime, LogType.Log, 0);

        if (!done && TestUpdater.Time > 2f)
        {
            TestDebugger.Log(DebugCategory.GAME, "Frame : " + TestUpdater.Frame, 0);
            done = true;
            TestUpdater.CallOnNextUpdate(OnNextUpdate);
        }
    }
    private void OnLateUpdate(float deltaTime)
    {
        if (!done2 && TestUpdater.Time > 3f)
        {
            done2 = true;
            TestDebugger.Log(DebugCategory.GAME, "on late register, frame : " + TestUpdater.Frame, 0);
            TestUpdater.CallOnLateUpdate((dt) => TestDebugger.Log(DebugCategory.GAME, "on late, frame : " + TestUpdater.Frame, 0));
        }
    }

    private void OnNextUpdate(float deltaTime)
    {
        TestDebugger.Log(DebugCategory.GAME, "On Next update, Frame : " + TestUpdater.Frame, 0);
    }
}
