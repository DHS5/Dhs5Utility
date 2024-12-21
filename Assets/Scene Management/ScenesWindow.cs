
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

using UnityEditor;
using UnityEditor.SceneManagement;

namespace Dhs5.Utility.Scenes
{
    public class ScenesWindow : EditorWindow
    {
        #region Window Creator

        [MenuItem("Tools/Windows/Scenes")]
        public static void OpenSceneWindow()
        {
            ScenesWindow window = CreateWindow<ScenesWindow>();
            window.titleContent = new GUIContent(EditorGUIUtility.IconContent("d_Scene")) { text = "Scenes" };
        }

        #endregion


        #region Members

        // --- DATA ---
        private SceneFolderStructure m_sceneFolderStructure;

        // --- GUI VARIABLES ---
        private Vector2 m_sceneBrowserScrollPos;

        #endregion

        #region Styles

        private GUIStyle m_groupStyle;
        private GUIStyle m_sceneStyle;

        private Color m_groupOpenBackgroundColor;
        private Color m_groupClosedBackgroundColor;

        private Color m_sceneBackgroundColor;
        private Color m_sceneBackgroundFilterColor;

        private void RefreshStyle()
        {
            m_groupOpenBackgroundColor = new Color(0.4f, 0.4f, 0.4f);
            m_groupClosedBackgroundColor = new Color(0.3f, 0.3f, 0.3f);

            m_sceneBackgroundColor = new Color(0.7f, 0.7f, 0.7f);
            m_sceneBackgroundFilterColor = new Color(0f, 0f, 0f, 0.1f);

            m_groupStyle = new GUIStyle()
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                },
                onNormal = new GUIStyleState()
                {
                    textColor = Color.white,
                }
            };
        }

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            Refresh();

            EditorBuildSettings.sceneListChanged += ComputeSceneFolderStructure;
        }
        private void OnDisable()
        {
            EditorBuildSettings.sceneListChanged -= ComputeSceneFolderStructure;
        }


        private void Refresh()
        {
            RefreshStyle();
            ComputeSceneFolderStructure();
        }

        #endregion


        #region Core GUI

        private void OnGUI()
        {
            // Active Scenes

            // Scene Browser
            m_sceneBrowserScrollPos = EditorGUILayout.BeginScrollView(m_sceneBrowserScrollPos);

            foreach (var group in m_sceneFolderStructure)
            {
                OnSceneGroupGUI(group);
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region GUI Elements

        private void OnSceneGroupGUI(SceneGroup group)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 30f);
            rect.x = 0f; rect.width = position.width;
            
            // Background
            EditorGUI.DrawRect(rect, group.open ? m_groupOpenBackgroundColor : m_groupClosedBackgroundColor);

            // Custom Foldout
            EditorGUI.indentLevel++;
            {
                if (Event.current.type == EventType.MouseDown
                    && rect.Contains(Event.current.mousePosition))
                {
                    group.open = !group.open;
                    Event.current.Use();
                }
                GUIContent content = group.open ? EditorGUIUtility.IconContent("d_icon dropdown open") : EditorGUIUtility.IconContent("d_icon dropdown");
                content.text = group.name;
                EditorGUI.LabelField(rect, content, m_groupStyle);
            }
            EditorGUI.indentLevel--;

            // Group Content
            if (group.open)
            {
                rect.y += rect.height;

                int i = 0;
                foreach (var scene in group)
                {
                    OnSceneInfosGUI(rect, scene, i);
                    rect.y += rect.height;
                    i++;
                }
            }
        }

        private void OnSceneInfosGUI(Rect rect, SceneInfos infos, int index)
        {
            rect.x = 0f; rect.width = position.width;

            // Background
            EditorGUI.DrawRect(rect, m_sceneBackgroundColor);
            if (index % 2 == 0) EditorGUI.DrawRect(rect, m_sceneBackgroundFilterColor);

            EditorGUI.LabelField(rect, infos.name, m_sceneStyle);
        }

        #endregion


        #region Scene Folder Structure

        public class SceneFolderStructure : IEnumerable<SceneGroup>
        {
            #region Members

            private readonly Dictionary<string, SceneGroup> m_groups;
            private readonly List<string> m_orderedGroupList;

            #endregion

            #region Constructor

            public SceneFolderStructure()
            {
                m_groups = new();
                m_orderedGroupList = new();

                SceneInfos scene;
                for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
                {
                    scene = new(EditorBuildSettings.scenes[i], i);

                    if (m_groups.TryGetValue(scene.folder, out var group))
                    {
                        group.Add(scene);
                    }
                    else
                    {
                        m_groups.Add(scene.folder, new SceneGroup(scene));
                        m_orderedGroupList.Add(scene.folder);
                    }
                }

                m_orderedGroupList.Sort();
            }

            #endregion

            #region Accessors

            public IEnumerator<SceneGroup> GetEnumerator()
            {
                foreach (var groupName in m_orderedGroupList)
                {
                    yield return m_groups[groupName];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        public class SceneGroup : IEnumerable<SceneInfos>
        {
            #region Members

            public readonly string name;
            public bool open;

            private HashSet<SceneInfos> m_scenes;

            #endregion

            #region Constructor

            public SceneGroup(SceneInfos sceneInfos)
            {
                name = sceneInfos.name;
                open = false;

                m_scenes = new HashSet<SceneInfos>() { sceneInfos };
            }

            #endregion

            #region Accessors

            public IEnumerator<SceneInfos> GetEnumerator()
            {
                foreach (var scene in m_scenes)
                {
                    yield return scene;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            #region Setters

            public void Add(SceneInfos scene)
            {
                m_scenes.Add(scene);
            }

            #endregion
        }

        public struct SceneInfos
        {
            #region Members

            public readonly string name;
            public readonly string folder;
            public readonly string path;

            public readonly int buildIndex;
            public readonly bool enabledInBuild;

            #endregion

            #region Constructor

            public SceneInfos(EditorBuildSettingsScene scene, int buildIndex)
            {
                this.path = scene.path;
                this.name = path[(path.LastIndexOf('/') + 1)..path.LastIndexOf('.')];
                this.folder = path.Substring(0, path.LastIndexOf('/')).Split('/', System.StringSplitOptions.RemoveEmptyEntries).Last();
                this.enabledInBuild = scene.enabled;
                //this.name = path.Substring(path.LastIndexOf('/') + 1).Replace(".unity", "");
                //string temp = path.Replace("/" + name + ".unity", "");
                //folder = temp.Substring(temp.LastIndexOf('/') + 1);

                this.buildIndex = buildIndex;
            }

            #endregion

            #region Utility

            public override int GetHashCode()
            {
                return buildIndex;
            }

            #endregion
        }

        #endregion

        #region Scenes Computation

        private void ComputeSceneFolderStructure()
        {
            m_sceneFolderStructure = new();
        }

        #endregion
    }
}

#endif