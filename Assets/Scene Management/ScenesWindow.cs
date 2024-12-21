
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

using UnityEditor;
using UnityEditor.SceneManagement;
using Dhs5.Utility.Editors;

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
        private Dictionary<string, Scene> m_loadedScenes;

        // --- GUI VARIABLES ---
        private float m_limit;
        private Vector2 m_activeScenesScrollPos;
        private Vector2 m_sceneBrowserScrollPos;

        #endregion

        #region Styles

        private GUIStyle m_toolbarStyle;
        private GUIStyle m_groupStyle;
        private GUIStyle m_sceneStyle;
        private GUIStyle m_buildIndexStyle;

        private Color m_activeScenesBackgroundColor;
        private Color m_groupOpenBackgroundColor;
        private Color m_groupClosedBackgroundColor;

        private void RefreshStyle()
        {
            m_activeScenesBackgroundColor = new Color(0.4f, 0.4f, 0.4f);
            m_groupOpenBackgroundColor = new Color(0.4f, 0.4f, 0.4f);
            m_groupClosedBackgroundColor = new Color(0.3f, 0.3f, 0.3f);

            m_toolbarStyle = new GUIStyle()
            {
                fontStyle = FontStyle.Normal,
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                },
            };
            m_groupStyle = new GUIStyle()
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                },
            };
            m_sceneStyle = new GUIStyle()
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                },
            };
            m_buildIndexStyle = new GUIStyle()
            {
                fontStyle = FontStyle.Italic,
                fontSize = 12,
                alignment = TextAnchor.MiddleRight,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                },
            };
        }

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            Refresh();

            m_limit = position.height / 3f;

            EditorBuildSettings.sceneListChanged += ComputeSceneFolderStructure;

            EditorSceneManager.sceneLoaded += OnSceneLoaded;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }
        private void OnDisable()
        {
            EditorBuildSettings.sceneListChanged -= ComputeSceneFolderStructure;

            EditorSceneManager.sceneLoaded -= OnSceneLoaded;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
        }


        private void Refresh()
        {
            RefreshStyle();
            ComputeSceneFolderStructure();
            GetLoadedScenes();
        }

        #endregion


        #region Core GUI

        private void OnGUI()
        {
            // Active Scenes
            ActiveScenesToolbar();
            Rect rect = EditorGUILayout.BeginVertical( GUILayout.Height(m_limit - 20f) );
            EditorGUI.DrawRect(rect, m_activeScenesBackgroundColor);

            m_activeScenesScrollPos = EditorGUILayout.BeginScrollView(m_activeScenesScrollPos);

            EditorGUILayout.Space(5f);

            foreach (var group in m_sceneFolderStructure)
            {
                foreach (var scene in group)
                {
                    if (IsSceneOpen(scene))
                    {
                        OnSceneInfosGUI(scene);
                        EditorGUILayout.Space(3f);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Scene Browser
            SceneBrowserToolbar();
            m_sceneBrowserScrollPos = EditorGUILayout.BeginScrollView(m_sceneBrowserScrollPos);

            foreach (var group in m_sceneFolderStructure)
            {
                OnSceneGroupGUI(group);
            }

            EditorGUILayout.EndScrollView();
        }

        private void ActiveScenesToolbar()
        {
            Rect rect = new Rect(0f, 0f, position.width, 20f);
            EditorGUILayout.GetControlRect(false, 18f);

            // Background
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);
            
            // Label
            EditorGUI.LabelField(new Rect(rect.x + 10f, rect.y, rect.width - 10f, rect.height), "Active Scenes", m_toolbarStyle);
        }
        
        private void SceneBrowserToolbar()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 18f);
            rect.x = 0f; rect.height = 20f; rect.width = position.width;

            if (rect.Contains(Event.current.mousePosition)
                && Event.current.type is EventType.MouseDrag)
            {
                m_limit = Event.current.mousePosition.y * 2 - (rect.y + rect.height / 2f);
                Event.current.Use();
            }

            // Background
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            // Buttons
            float buttonsWidth = 40f;
            var refreshButtonRect = new Rect(rect.width + - buttonsWidth, rect.y, buttonsWidth, rect.height);
            if (GUI.Button(refreshButtonRect, EditorGUIHelper.RefreshIcon, EditorStyles.toolbarButton))
            {
                Refresh();
            }

            // Label
            EditorGUI.LabelField(new Rect(rect.x + 10f, rect.y, rect.width - 10f, rect.height), "Scene Browser", m_toolbarStyle);
        }

        #endregion

        #region GUI Elements

        private void OnSceneGroupGUI(SceneGroup group)
        {
            Rect rect = EditorGUILayout.BeginVertical();

            // Background
            rect.x = 0f; rect.width = position.width;
            EditorGUI.DrawRect(rect, group.open ? m_groupOpenBackgroundColor : m_groupClosedBackgroundColor);

            // Custom Foldout
            rect = EditorGUILayout.GetControlRect(false, 25f);
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
                EditorGUILayout.Space(5f);

                foreach (var scene in group)
                {
                    OnSceneInfosGUI(scene);
                    EditorGUILayout.Space(3f);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void OnSceneInfosGUI(SceneInfos infos)
        {
            bool opened = IsSceneOpen(infos);
            bool loaded = IsSceneLoaded(infos);

            Rect rect = EditorGUILayout.GetControlRect(false, 30f);
            rect.x = 10f; rect.width = position.width - 25f;

            // Background
            var guiColor = GUI.backgroundColor;
            GUI.backgroundColor = loaded ? Color.green : opened ? Color.yellow : guiColor;
            GUI.Box(rect, GUIContent.none, GUI.skin.window);
            GUI.backgroundColor = guiColor;

            // Toggle
            float toggleWidth = 20f;
            var toggleRect = new Rect(rect.x + 5f, rect.y + 5f, toggleWidth, 20f);
            EditorGUI.BeginChangeCheck();
            EditorGUI.Toggle(toggleRect, infos.enabledInBuild);
            if (EditorGUI.EndChangeCheck())
            {
                EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
                scenes[infos.settingsIndex].enabled = !infos.enabledInBuild;
                EditorBuildSettings.scenes = scenes;
                ComputeSceneFolderStructure();
            }

            // --- BUTTONS ---
            float buttonsHeight = rect.height - 4f;
            float buttonsY = rect.y + 2f;

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            // Play Button
            float playButtonWidth = 32f;
            var playButtonRect = new Rect(rect.x + rect.width - playButtonWidth - 2f, buttonsY, playButtonWidth, buttonsHeight);
            if (GUI.Button(playButtonRect, EditorGUIUtility.IconContent("d_Animation.Play")))
            {
                EditorSceneManager.OpenScene(infos.path, OpenSceneMode.Single);
                EditorApplication.EnterPlaymode();
            }

            // Open Button
            float openButtonWidth = 60f;
            var openButtonRect = new Rect(playButtonRect.x - openButtonWidth, buttonsY, openButtonWidth, buttonsHeight);
            if (GUI.Button(openButtonRect, opened ? "Remove" : "Open"))
            {
                if (opened) EditorSceneManager.CloseScene(m_loadedScenes[infos.name], true);
                else EditorSceneManager.OpenScene(infos.path, OpenSceneMode.Single);
            }

            EditorGUI.EndDisabledGroup();
            
            // Load Button
            float loadButtonWidth = 60f;
            var loadButtonRect = new Rect(openButtonRect.x - loadButtonWidth, buttonsY, loadButtonWidth, buttonsHeight);
            if (GUI.Button(loadButtonRect, loaded ? "Unload" : opened ? "Load" : "Add"))
            {
                if (loaded) EditorSceneManager.CloseScene(m_loadedScenes[infos.name], false);
                else EditorSceneManager.OpenScene(infos.path, OpenSceneMode.Additive);
            }

            // --- LABELS ---
            float labelsY = rect.y + 5f;
            float labelsHeight = 20f;

            // Build Index Label
            float buildIndexLabelWidth = 0f;
            if (infos.enabledInBuild)
            {
                buildIndexLabelWidth = 20f;
                var buildIndexLabelRect = new Rect(loadButtonRect.x - buildIndexLabelWidth - 5f, labelsY, buildIndexLabelWidth, labelsHeight);
                EditorGUI.LabelField(buildIndexLabelRect, infos.buildIndex, m_buildIndexStyle);
            }
            // Main Label
            float labelRectX = toggleRect.x + toggleWidth + 5f;
            var labelRect = new Rect(labelRectX, labelsY, loadButtonRect.x - labelRectX - buildIndexLabelWidth - 5f, labelsHeight);
            EditorGUI.LabelField(labelRect, infos.name, m_sceneStyle);
        }

        #endregion


        #region Scene Folder Structure

        private void ComputeSceneFolderStructure()
        {
            m_sceneFolderStructure = new();
        }

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
                EditorBuildSettingsScene buildSettingsScene;
                int buildIndex = 0;
                for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
                {
                    buildSettingsScene = EditorBuildSettings.scenes[i];
                    if (buildSettingsScene.enabled)
                    {
                        scene = new(buildSettingsScene, buildIndex, i);
                        buildIndex++;
                    }
                    else
                    {
                        scene = new(buildSettingsScene, -1, i);
                    }

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
                name = sceneInfos.folder;
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

            public readonly int settingsIndex;
            public readonly string buildIndex;
            public readonly bool enabledInBuild;

            #endregion

            #region Constructor

            public SceneInfos(EditorBuildSettingsScene scene, int buildIndex, int settingsIndex)
            {
                this.path = scene.path;
                this.name = path[(path.LastIndexOf('/') + 1)..path.LastIndexOf('.')];
                this.folder = path.Substring(0, path.LastIndexOf('/')).Split('/', System.StringSplitOptions.RemoveEmptyEntries).Last();
                this.enabledInBuild = scene.enabled;

                this.settingsIndex = settingsIndex;
                this.buildIndex = "(" + buildIndex + ")";
            }

            #endregion

            #region Utility

            public override int GetHashCode()
            {
                return settingsIndex;
            }

            #endregion
        }

        #endregion

        #region Loaded Scenes Management

        private bool IsSceneOpen(SceneInfos infos)
        {
            return m_loadedScenes.ContainsKey(infos.name);
        }
        private bool IsSceneLoaded(SceneInfos infos)
        {
            return m_loadedScenes.TryGetValue(infos.name, out var scene) && scene.isLoaded;
        }

        private void GetLoadedScenes()
        {
            m_loadedScenes = new();

            Scene scene;
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                scene = EditorSceneManager.GetSceneAt(i);
                m_loadedScenes.TryAdd(scene.name, scene);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            m_loadedScenes.TryAdd(scene.name, scene);
        }
        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            m_loadedScenes.TryAdd(scene.name, scene);
        }
        private void OnSceneClosing(Scene scene, bool removingScene)
        {
            if (removingScene) m_loadedScenes.Remove(scene.name);
        }
        

        #endregion
    }
}

#endif