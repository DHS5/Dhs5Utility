using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.Databases;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "TS", menuName = "TestScriptable")]
public class TestScriptable : ScriptableObject, IDataContainerElement, IDataContainerPrefixableElement
{
    [SerializeField, FormerlySerializedAs("m_name")] private string m_prefix;
    [SerializeField] private int m_uid;

    public int UID => m_uid;

    public string DataNamePrefix { get => m_prefix; set => m_prefix = value; }

#if UNITY_EDITOR

    public void Editor_SetUID(int uid)
    {
        m_uid = uid;
    }

#endif
}
