using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Database("Test/Empty", showInDatabaseWindow = false)]
public class TestEmptyDB : BaseDataContainer
{
    [SerializeField] private string emptyStr;

    public override int Count => throw new System.NotImplementedException();

    public override Object GetDataAtIndex(int index)
    {
        throw new System.NotImplementedException();
    }

    public override bool TryGetDataByUID(int uid, out Object obj)
    {
        throw new System.NotImplementedException();
    }

#if UNITY_EDITOR

    protected override bool Editor_OnDeleteElementAtIndex(int index)
    {
        throw new System.NotImplementedException();
    }

    protected override IEnumerable<Object> Editor_GetContainerContent()
    {
        throw new System.NotImplementedException();
    }

    protected override void Editor_CleanUp()
    {
        throw new System.NotImplementedException();
    }

#endif
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