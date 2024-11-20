using Dhs5.Utility.Debuggers;
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
            Debugger.LogOnScreen(0, "Test", LogType.Log);
            Debugger.LogOnScreen(0, "Test", LogType.Warning);
            Debugger.LogOnScreen(0, "Test", LogType.Error);
        }
    }
}
