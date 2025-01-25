using UnityEngine;
using UnityEngine.LowLevel;
using Dhs5.Utility.Editors;

using Unity.VisualScripting;



#if UNITY_EDITOR
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

        private string m_searchString;

        #endregion

        #region Properties

        private bool IsSearching => !string.IsNullOrWhiteSpace(m_searchString);

        #endregion

        #region Styles

        private Color m_elemBackground1;
        private Color m_elemBackground2;

        private void InitStyles()
        {
            m_elemBackground1 = new Color(0f, 0f, 0f, 0.2f);
            m_elemBackground2 = new Color(0f, 0f, 0f, 0.4f);
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
            var toolbarRect = EditorGUILayout.GetControlRect(false, 20f);
            OnToolbarGUI(toolbarRect);

            if (m_playerLoop.subSystemList == null)
            {
                EditorGUILayout.HelpBox("The player Loop is not valid, refresh", MessageType.Info);
                return;
            }

            if (IsSearching)
            {
                OnSearchGUI();
            }
            else
            {

            }
        }

        private void OnSearchGUI()
        {
            int index = 0;
            Rect rect = EditorGUILayout.GetControlRect(false, 20f);
            rect.x = 0; rect.y -= 3f; rect.width = position.width;
            foreach (var mainSystem in m_playerLoop.subSystemList)
            {
                foreach (var system in mainSystem.subSystemList)
                {
                    if (system.type.Name.StartsWith(m_searchString, System.StringComparison.OrdinalIgnoreCase))
                    {
                        DrawSystem(rect, index, system, false);

                        index++;
                        rect.y += rect.height;
                    }
                }
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
            int buttonsCount = 1;

            float labelWidth = 150f;
            var labelRect = new Rect(rect.x + 5f, rect.y, labelWidth - 5f, rect.height);
            EditorGUI.LabelField(labelRect, "Player Loop Visualizer", EditorStyles.boldLabel);

            var searchFieldRect = new Rect(rect.x + labelWidth, rect.y + 2f, rect.width - labelWidth - buttonsWidth * buttonsCount, rect.height);
            m_searchString = EditorGUI.TextField(searchFieldRect, m_searchString, EditorStyles.toolbarSearchField);

            var refreshButtonRect = new Rect(searchFieldRect.x + searchFieldRect.width, rect.y, buttonsWidth, rect.height);
            if (GUI.Button(refreshButtonRect, EditorGUIHelper.RefreshIcon, EditorStyles.toolbarButton))
            {
                Refresh();
                InitStyles();
            }
        }

        private void DrawSystem(Rect rect, int index, PlayerLoopSystem system, bool alinea = true)
        {
            EditorGUI.DrawRect(rect, index % 2 == 0 ? m_elemBackground1 : m_elemBackground2);

            var xSpace = 5f + (alinea ? 15f : 0f);
            var labelRect = new Rect(rect.x + xSpace, rect.y, rect.width - xSpace, rect.height);
            EditorGUI.LabelField(labelRect, system.type.Name);
        }

        #endregion


        #region Utility

        private void Refresh()
        {
            m_playerLoop = PlayerLoop.GetCurrentPlayerLoop();
        }

        #endregion
    }
}

#endif
