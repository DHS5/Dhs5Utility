using Dhs5.Utility.Databases;
using Dhs5.Utility.Debuggers;
using Dhs5.Utility.Scenes;
using Dhs5.Utility.Updates;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.Tags;

#if UNITY_EDITOR
using UnityEditorInternal;
#endif

public class TestScript : MonoBehaviour, IDataContainerElement
{
    [Header("Header 1")]
    [SerializeField] private int m_uid;
    [SerializeField] private bool _showTexture;
    [SerializeField] private Texture2D _texture;
    [Header("Header 1")]
    [HelpBox("Be carefull this boolean is used to show another property", HelpBoxAttribute.EType.WARNING)]
    [SerializeField] private bool _showName;
    [HelpBox("It's a read only property", HelpBoxAttribute.EType.INFO)]
    [SerializeField, ReadOnly()] private string _name;
    [Header("Header 1")]
    [SerializeField, Show(nameof(_showName))] private string _name2;
    [SerializeField] private DataPicker _dataPicker1;
    [SerializeField] private DataPicker<DebuggerDatabase> _dataPicker2;
    [SerializeField] private SceneReference m_sceneRef;
    [SerializeField, FolderPicker] private string m_folder;
    [SerializeField, FolderPicker("Assets/Test")] private string m_folder2;
    [SerializeField, FolderPicker("Assets/Test")] private UnityEngine.Object m_folder3;
    [SerializeField, Layer] private int m_layer;
    [SerializeField, Tag] private string m_tag;
    [SerializeField, VectorRange(5f, 45f)] private Vector2 m_ranges;
    [SerializeField, VectorRange(12, 38)] private Vector2Int m_intRange;
    [SerializeField, Creator] private BaseEnumDatabaseElement m_creator;
    [SerializeField, FoldoutContent] private UpdaterDatabaseElement m_foldoutElem;
    [SerializeField] private EnumValues<EUpdateTimelineEventType, int> m_enumValues;
    [SerializeField] private EnumFlagValues<DebugCategory, DebugCategoryFlags, int> m_enumFlagValues;
    [SerializeField] private GameplayTagsList m_tags;
    [SerializeField] private GameplayTagsList m_tags2;

    public int UID => m_uid;

#if UNITY_EDITOR

    public void Editor_SetUID(int uid)
    {
        m_uid = uid;
    }

#endif

    private void OnEnable()
    {
        //TestUpdater.Register(true, UpdateCategory.SCREEN_LOG, OnUpdate, ref m_update1Key);
        TestUpdater.OnLateUpdate += OnLateUpdate;
        TestUpdater.Register(true, EUpdateChannel.SCREEN_LOG, OnUpdate);
    }
    private void OnDisable()
    {
        //TestUpdater.Register(false, UpdateCategory.SCREEN_LOG, OnUpdate, ref m_update1Key);
        TestUpdater.OnLateUpdate -= OnLateUpdate;
        TestUpdater.Register(false, EUpdateChannel.SCREEN_LOG, OnUpdate);
    }

    bool done;
    bool done2;
    private void OnUpdate(float deltaTime)
    {
        //TestDebugger.LogOnScreen(0, "Test " + TestUpdater.RealTime, LogType.Log, 0);

        if (!done && TestUpdater.Time > 2f)
        {
            TestDebugger.Log(DebugCategory.GAME, "Frame : " + TestUpdater.Frame, 0);
            done = true;
            TestUpdater.CallInXFrames(1, OnNextUpdate, out _);
        }
    }
    private void OnLateUpdate(float deltaTime)
    {
        if (!done2 && TestUpdater.Time > 3f)
        {
            done2 = true;
            TestDebugger.Log(DebugCategory.GAME, "on late register, frame : " + TestUpdater.Frame, 0);
            TestUpdater.CallInXFrames(0, () => TestDebugger.Log(DebugCategory.GAME, "on late, frame : " + TestUpdater.Frame, 0), out _, EUpdatePass.AFTER_LATE_UPDATE);
        }
    }

    private void OnNextUpdate()
    {
        TestDebugger.Log(DebugCategory.GAME, "On Next update, Frame : " + TestUpdater.Frame, 0);
    }

    private void OnInputTest()
    {
        
    }
}
