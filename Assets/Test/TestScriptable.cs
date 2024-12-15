using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TS", menuName = "TestScriptable")]
public class TestScriptable : ScriptableObject, IDataContainerElement
{
    [SerializeField] private int m_uid;
    [SerializeField] private int m_int0;
    [SerializeField] private int m_int1;
    [SerializeField] private int m_int2;
    [SerializeField] private int m_int3;
    [SerializeField] private int m_int4;
    [SerializeField] private int m_int5;

    public int UID => m_uid;

#if UNITY_EDITOR
    public void Editor_SetUID(int uid)
    {
        m_uid = uid;
    }
    public bool Editor_HasDataContainerElementName(out string name)
    {
        name = this.name;
        return true;
    }

    public bool Editor_HasDataContainerElementTexture(out Texture2D texture)
    {
        texture = null;
        return false;
    }
#endif
}
