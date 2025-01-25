#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.Profiling;
using UnityEditor;

namespace Dhs5.Utility.PlayerLoops
{
    public class PlayerLoopWindow : EditorWindow
    {
        #region Static Constructor

        [MenuItem("Window/Dhs5 Utility/Player Loop", priority = 100)]
        public static void OpenWindow()
        {
            PlayerLoopWindow window = GetWindow<PlayerLoopWindow>();
            window.titleContent = new GUIContent(EditorGUIUtility.IconContent("d_preAudioAutoPlayOff")) { text = "Player loop" };
        }

        #endregion

        #region Members

        private PlayerLoopSystem m_playerLoop;
        private bool[] m_mainSystemsFoldouts;
        private bool m_profilerEnabled;

        private string m_searchString;
        private Vector2 m_scrollPos;

        #endregion

        #region Properties

        private bool IsSearching => !string.IsNullOrWhiteSpace(m_searchString);

        #endregion

        #region Styles

        private Color m_elemBackground1;
        private Color m_elemBackground2;

        private GUIStyle m_centeredIconStyle;

        private void InitStyles()
        {
            m_elemBackground1 = new Color(0f, 0f, 0f, 0.2f);
            m_elemBackground2 = new Color(0f, 0f, 0f, 0.4f);

            m_centeredIconStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageOnly,
                contentOffset = new Vector2(-2f, 2f)
            };
        }

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            Refresh();
            InitStyles();
        }

        #endregion


        #region Core GUI

        private void OnGUI()
        {
            var toolbarRect = EditorGUILayout.GetControlRect(false, 15f);
            OnToolbarGUI(toolbarRect);

            if (m_playerLoop.subSystemList == null)
            {
                EditorGUILayout.HelpBox("The player Loop is not valid, refresh", MessageType.Info);
                return;
            }

            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);
            if (IsSearching)
            {
                OnSearchGUI();
            }
            else
            {
                OnCompleteLoopGUI();
            }
            EditorGUILayout.EndScrollView();
        }

        private void OnSearchGUI()
        {
            int index = 0;
            Rect rect;

            foreach (var mainSystem in m_playerLoop.subSystemList)
            {
                foreach (var system in mainSystem.subSystemList)
                {
                    if (system.type.Name.StartsWith(m_searchString, System.StringComparison.OrdinalIgnoreCase))
                    {
                        rect = EditorGUILayout.GetControlRect(false, 18f);
                        rect.x = 0; rect.width = position.width; rect.height = 20f;

                        DrawSystem(rect, index, mainSystem, system, false);

                        index++;
                    }
                }
            }
        }

        private void OnCompleteLoopGUI()
        {
            int mainSystemIndex = 0;
            int visibleIndex = 0;
            Rect rect;

            foreach (var mainSystem in m_playerLoop.subSystemList)
            {
                rect = EditorGUILayout.GetControlRect(false, 19f);
                rect.x = 0; rect.width = position.width; rect.height = 21f;

                var open = DrawMainSystem(rect, mainSystemIndex, mainSystem);

                if (open)
                {
                    foreach (var system in mainSystem.subSystemList)
                    {
                        rect = EditorGUILayout.GetControlRect(false, 18f);
                        rect.x = 0; rect.width = position.width; rect.height = 20f;

                        DrawSystem(rect, visibleIndex, mainSystem, system);

                        visibleIndex++;
                    }
                }
                mainSystemIndex++;
            }
        }

        #endregion

        #region GUI Elements

        private void OnToolbarGUI(Rect rect)
        {
            rect.x = 0f;
            rect.y = 0f;
            rect.width = position.width;

            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            float buttonsWidth = 40f;
            int buttonsCount = 2;

            float labelWidth = 150f;
            var labelRect = new Rect(rect.x + 5f, rect.y + 2f, labelWidth - 5f, rect.height);
            EditorGUI.LabelField(labelRect, "Player Loop Visualizer", EditorStyles.boldLabel);

            var searchFieldRect = new Rect(rect.x + labelWidth, rect.y + 2f, rect.width - labelWidth - 2f - buttonsWidth * buttonsCount, rect.height);
            m_searchString = EditorGUI.TextField(searchFieldRect, m_searchString, EditorStyles.toolbarSearchField);

            var refreshButtonRect = new Rect(position.width - buttonsWidth, rect.y, buttonsWidth, rect.height);
            if (GUI.Button(refreshButtonRect, EditorGUIUtility.IconContent("d_Refresh"), EditorStyles.toolbarButton))
            {
                Refresh();
                InitStyles();
            }

            var profilerButtonRect = new Rect(position.width - buttonsWidth * buttonsCount, rect.y, buttonsWidth, rect.height);
            m_profilerEnabled = GUI.Toggle(profilerButtonRect, m_profilerEnabled, GUIContent.none, EditorStyles.toolbarButton);
            EditorGUI.LabelField(profilerButtonRect, EditorGUIUtility.IconContent("d_Profiler.CPU"), m_centeredIconStyle);
        }

        private bool DrawMainSystem(Rect rect, int systemIndex, PlayerLoopSystem system)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);

            var xSpace = 5f;
            var foldoutRect = new Rect(rect.x + xSpace, rect.y, rect.width - xSpace, rect.height);
            m_mainSystemsFoldouts[systemIndex] = EditorGUI.Foldout(foldoutRect, m_mainSystemsFoldouts[systemIndex], system.type.Name, true);
            return m_mainSystemsFoldouts[systemIndex];
        }
        private void DrawSystem(Rect rect, int index, PlayerLoopSystem mainSystem, PlayerLoopSystem system, bool alinea = true)
        {
            bool custom = system.updateFunction.ToInt64() == 0
                && system.updateDelegate != null;

            EditorGUI.DrawRect(rect, index % 2 == 0 ? m_elemBackground1 : m_elemBackground2);

            var xSpace = 5f + (alinea ? 20f : 0f);
            float toggleWidth = 20f;
            if (Application.isPlaying)
            {
                var toggleRect = new Rect(rect.x + xSpace, rect.y, toggleWidth, rect.height);
                bool enabled = PlayerLoopManager.IsSystemEnabled(system.type);
                var result = EditorGUI.ToggleLeft(toggleRect, GUIContent.none, enabled);
                if (result && !enabled)
                {
                    PlayerLoopManager.ReenableSystem(system.type);
                }
                else if (!result && enabled)
                {
                    PlayerLoopManager.DisableSystem(system.type);
                }

                xSpace += toggleWidth;
            }

            var labelRect = new Rect(rect.x + xSpace, rect.y, rect.width - xSpace, rect.height);

            var guiColor = GUI.contentColor;
            if (custom)
            {
                GUI.contentColor = Color.cyan;
            }
            EditorGUI.LabelField(labelRect, system.type.Name);
            if (custom)
            {
                GUI.contentColor = guiColor;
            }

            if (m_profilerEnabled)
            {
                float profilerWidth = 90f;
                var profilerRect = new Rect(rect.x + rect.width - profilerWidth, rect.y, profilerWidth, rect.height);

                var sampler = Sampler.Get(mainSystem.type.Name + "." + system.type.Name);

                var info = "0.000000ms";
                var elapsedNanoseconds = sampler.GetRecorder().elapsedNanoseconds;
                if (elapsedNanoseconds != 0)
                {
                    info = (elapsedNanoseconds / 1000000f) + "ms";
                }
                EditorGUI.LabelField(profilerRect, "[" + info + "]", EditorStyles.miniLabel);
            }
        }

        #endregion


        #region Utility

        private void Refresh()
        {
            m_playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            m_mainSystemsFoldouts = new bool[m_playerLoop.subSystemList.Length];
        }

        #endregion
    }
}

#endif
