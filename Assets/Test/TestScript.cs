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

    float lastUpdate;
    private void Update()
    {
        if (Time.time > lastUpdate + 0.75f)
        {
            lastUpdate = Time.time;
            TestDebugger.LogOnScreen(0, "Test reogj trj grop trjl hp tr khpr tr khp  typh k typ hkph yp hy y pypy jp trpok z ^lf, ap dal dapd ka fk", LogType.Log, 2);
            TestDebugger.LogOnScreen(0, "Test", LogType.Warning, 1);
            TestDebugger.LogOnScreen(0, "Test", LogType.Error, 0);
        }
    }
}
