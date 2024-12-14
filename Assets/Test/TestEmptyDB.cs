using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Database("Test/Empty", showInDatabaseWindow = false)]
public class TestEmptyDB : BaseDatabase
{
    [SerializeField] private string emptyStr;
}

#if UNITY_EDITOR
[CustomEditor(typeof(TestEmptyDB))]
public class EmptyDBEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }

    public override bool HasPreviewGUI()
    {
        return true;
    }

    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        
    }
}
#endif