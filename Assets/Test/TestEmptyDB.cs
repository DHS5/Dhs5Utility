using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Database("Test/Empty")]
public class TestEmptyDB : BaseDatabase
{
    [SerializeField] private string emptyStr;
}

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
