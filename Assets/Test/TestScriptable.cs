using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TS", menuName = "TestScriptable")]
public class TestScriptable : ScriptableObject, IDatabaseElement
{
    [SerializeField] private int m_int0;
    [SerializeField] private int m_int1;
    [SerializeField] private int m_int2;
    [SerializeField] private int m_int3;
    [SerializeField] private int m_int4;
    [SerializeField] private int m_int5;

    public bool HasDatabaseElementName(out string name)
    {
        name = this.name;
        return true;
    }

    public bool HasDatabaseElementTexture(out Texture2D texture)
    {
        texture = null;
        return false;
    }
}
