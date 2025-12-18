using Dhs5.Utility.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

[Settings(path: "Test", Scope.Project)]
public class TestSettings : CustomSettings<TestSettings>
{
    [SerializeField] private string m_word;
    [SerializeField] private PlayerPrefInt m_playerPrefInt;
    [SerializeField] private PlayerPrefVector3 m_playerPrefVector3;
    [SerializeField] private PlayerPrefVector3Int m_playerPrefVector3Int;
    [SerializeField] private PlayerPrefVector2 m_playerPrefVector2;
    [SerializeField] private PlayerPrefVector2Int m_playerPrefVector2Int;
    [SerializeField] private PlayerPrefFloat m_playerPrefFloat;
    [SerializeField] private PlayerPrefString m_playerPrefString;
    [SerializeField] private PlayerPrefBool m_playerPrefBool;
    [SerializeField, SubSettings] private TestSubSettings m_subSetTest;
    [SerializeField, SubSettings] private ScriptableObject m_subSetTest2;
    [SerializeField, SubSettings] private ScriptableObject m_subSetTest3;

    public static PlayerPrefInt PlayerPrefInt => I.m_playerPrefInt;
}
