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
    //[SettingsProvider]
    //public static SettingsProvider GetSettingsProvider() => GetCustomSettingsProvider();
}
