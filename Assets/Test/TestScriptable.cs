using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.Databases;

[CreateAssetMenu(fileName = "TS", menuName = "TestScriptable")]
public class TestScriptable : ScriptableObject, IDataContainerElement, IDataContainerNameableElement
{
    [SerializeField] private string m_name;
    [SerializeField] private int m_uid;

    public int UID => m_uid;

    public string DataDisplayName { get => m_name; set => m_name = value; }

#if UNITY_EDITOR

    public void Editor_SetUID(int uid)
    {
        m_uid = uid;
    }

#endif
}
