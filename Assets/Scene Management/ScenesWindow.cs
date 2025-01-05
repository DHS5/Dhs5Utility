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

        [MenuItem("Window/Dhs5 Utility/Scenes", priority = 100)]
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
        private bool m_activeScenesOpened;
        private float m_limit;
        private bool m_isResizing;
        private Vector2 m_activeScenesScrollPos;
        private Vector2 m_sceneBrowserScrollPos;
        private string m_searchString;

        #endregion

        #region Consts

        private const string EditorPref_ActiveScenesOpenedKey = "ScW_activeScenesOpened";
        private const string EditorPref_LimitKey = "ScW_limit";
        private const string EditorPref_GroupKey = "ScW_group_";

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

            m_activeScenesOpened = EditorPrefs.GetBool(EditorPref_ActiveScenesOpenedKey, true);
            m_limit = EditorPrefs.GetFloat(EditorPref_LimitKey, position.height / 3f);

            EditorBuildSettings.sceneListChanged += ComputeSceneFolderStructure;

            EditorSceneManager.sceneLoaded += OnSceneLoaded;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }
        private void OnDisable()
        {
            EditorPrefs.SetBool(EditorPref_ActiveScenesOpenedKey, m_activeScenesOpened);
            EditorPrefs.SetFloat(EditorPref_LimitKey, m_limit);

            EditorBuildSettings.sceneListChanged -= ComputeSceneFolderStructure;

            EditorSceneManager.sceneLoaded -= OnSceneLoaded;
            EditorSceneManager.sceneClosing -= OnSceneClosing;
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
            // Events
            if (m_isResizing && Event.current.type == EventType.MouseUp)
            {
                m_isResizing = false;
                Event.current.Use();
            }

            // Active Scenes
            OnActiveScenesGUI();

            // Scene Browser
            OnSceneBrowserGUI();
        }

        private void OnActiveScenesGUI()
        {
            ActiveScenesToolbar();

            if (m_activeScenesOpened)
            {
                Rect rect = EditorGUILayout.BeginVertical(GUILayout.Height(m_limit - 20f));
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
            }
        }
        private void OnSceneBrowserGUI()
        {
            SceneBrowserToolbar();
            m_sceneBrowserScrollPos = EditorGUILayout.BeginScrollView(m_sceneBrowserScrollPos);

            if (string.IsNullOrWhiteSpace(m_searchString))
            {
                foreach (var group in m_sceneFolderStructure)
                {
                    OnSceneGroupGUI(group);
                }
            }
            else // Searching
            {
                foreach (var group in m_sceneFolderStructure)
                {
                    foreach (var scene in group)
                    {
                        if (scene.name.Contains(m_searchString.Trim(), System.StringComparison.OrdinalIgnoreCase))
                        {
                            OnSceneInfosGUI(scene);
                            EditorGUILayout.Space(3f);
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Toolbars GUI

        private void ActiveScenesToolbar()
        {
            Rect rect = new Rect(0f, 0f, position.width, 20f);
            EditorGUILayout.GetControlRect(false, 18f);

            // Background
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);
            
            // Label
            if (GUI.Button(rect, GUIContent.none, EditorStyles.toolbarButton)) m_activeScenesOpened = !m_activeScenesOpened;
            EditorGUI.LabelField(new Rect(rect.x + 10f, rect.y, rect.width - 10f, rect.height), "Active Scenes", m_toolbarStyle);
        }
        
        private void SceneBrowserToolbar()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 18f);
            rect.x = 0f; rect.height = 20f; rect.width = position.width;

            // Background
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            // Buttons
            float buttonsWidth = 40f;
            var refreshButtonRect = new Rect(rect.width + - buttonsWidth, rect.y, buttonsWidth, rect.height);
            if (GUI.Button(refreshButtonRect, EditorGUIUtility.IconContent("d_Refresh"), EditorStyles.toolbarButton))
            {
                Refresh();
            }

            // Label
            var labelRect = new Rect(rect.x + 10f, rect.y, 100f, rect.height);
            EditorGUI.LabelField(labelRect, "Scene Browser", m_toolbarStyle);

            // Search Field
            float searchFieldRectX = labelRect.x + labelRect.width;
            var searchFieldRect = new Rect(searchFieldRectX, rect.y + 2f, refreshButtonRect.x - searchFieldRectX - 2f, rect.height - 4f);
            m_searchString = EditorGUI.TextField(searchFieldRect, m_searchString, EditorStyles.toolbarSearchField);

            // Resize
            if (m_activeScenesOpened)
            {
                var resizeRect = new Rect(rect.x, rect.y - 2f, rect.width - buttonsWidth, 4f);
                EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeVertical);
                if (!m_isResizing
                    && resizeRect.Contains(Event.current.mousePosition)
                    && Event.current.type is EventType.MouseDown)
                {
                    m_isResizing = true;
                    Event.current.Use();
                }
                else if (m_isResizing && Event.current.type is EventType.MouseDrag)
                {
                    m_limit = Mathf.Clamp(m_limit + Event.current.delta.y, 100f, position.height - 100f);
                    Event.current.Use();
                }
            }
        }

        #endregion

        #region GUI Elements

        private void OnSceneGroupGUI(SceneGroup group)
        {
            Rect rect = EditorGUILayout.BeginVertical();

            // Background
            rect.x = 0f; rect.width = position.width;
            EditorGUI.DrawRect(rect, group.Open ? m_groupOpenBackgroundColor : m_groupClosedBackgroundColor);

            // Custom Foldout
            rect = EditorGUILayout.GetControlRect(false, 25f);
            EditorGUI.indentLevel++;
            {
                if (Event.current.type == EventType.MouseDown
                    && rect.Contains(Event.current.mousePosition))
                {
                    group.SetOpen(!group.Open);
                    Event.current.Use();
                }
                GUIContent content = group.Open ? EditorGUIUtility.IconContent("d_icon dropdown open") : EditorGUIUtility.IconContent("d_icon dropdown");
                content.text = group.name;
                EditorGUI.LabelField(rect, content, m_groupStyle);
            }
            EditorGUI.indentLevel--;

            // Group Content
            if (group.Open)
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
            float buildIndexLabelWidth = 20f;
            if (infos.enabledInBuild)
            {
                var buildIndexLabelRect = new Rect(loadButtonRect.x - buildIndexLabelWidth - 5f, labelsY, buildIndexLabelWidth, labelsHeight);
                EditorGUI.LabelField(buildIndexLabelRect, infos.buildIndex, m_buildIndexStyle);
            }
            // Main Label
            float labelRectX = toggleRect.x + toggleWidth + 5f;
            var labelRect = new Rect(labelRectX, labelsY, loadButtonRect.x - labelRectX - buildIndexLabelWidth - 5f, labelsHeight);
            if (GUI.Button(labelRect, infos.name, m_sceneStyle))
            {
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(infos.Asset);
            }
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

            private HashSet<SceneInfos> m_scenes;

            public bool Open { get; private set; }

            #endregion

            #region Constructor

            public SceneGroup(SceneInfos sceneInfos)
            {
                name = sceneInfos.folder;
                Open = EditorPrefs.GetBool(EditorPref_GroupKey + name, false);

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
            public void SetOpen(bool open)
            {
                this.Open = open;
                EditorPrefs.SetBool(EditorPref_GroupKey + name, open);
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

            #region Properties

            public SceneAsset Asset => AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            #endregion

            #region Constructor

            public SceneInfos(EditorBuildSettingsScene scene, int settingsIndex)
            {
                this.path = scene.path;
                this.name = path[(path.LastIndexOf('/') + 1)..path.LastIndexOf('.')];
                this.folder = path.Substring(0, path.LastIndexOf('/')).Split('/', System.StringSplitOptions.RemoveEmptyEntries).Last();
                this.enabledInBuild = scene.enabled;

                this.settingsIndex = settingsIndex;
                this.buildIndex = "(" + SceneUtility.GetBuildIndexByScenePath(path) + ")";
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