using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Dhs5.Utility.Console
{
    [CreateAssetMenu(fileName = "CCMD_", menuName = "Dhs5 Utility/Console/Command")]
    public class ConsoleCommandObject : ScriptableObject, IConsoleCommand
    {
        #region Members

        [SerializeField] private List<ConsoleCommandPiece> m_commandPieces;

        #endregion

        #region IConsoleCommand

        public int Count => m_commandPieces.Count;

        public ConsoleCommandPiece this[int index] => m_commandPieces[index];

        #endregion

        #region Protected Access

        protected void SetCommandPieces(List<ConsoleCommandPiece> commandPieces)
        {
            if (m_commandPieces == null) m_commandPieces = new();
            else m_commandPieces.Clear();

            m_commandPieces.AddRange(commandPieces);
        }

        #endregion
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(ConsoleCommandObject))]
    public class ConsoleCommandEditor : Editor
    {
        #region Members

        // COMMAND
        protected ConsoleCommandObject m_command;

        // PROPERTIES
        protected SerializedProperty p_script;
        protected SerializedProperty p_commandPieces;
        protected List<string> m_excludedProperties;

        // PARAMETERS
        protected ReorderableList m_piecesList;
        protected string m_previewCommandStart;
        protected bool m_previewOpen;
        protected Vector2 m_previewScrollPos;

        // GUI
        protected Color m_transparentWhite01 = new Color(1f, 1f, 1f, 0.1f);
        protected Color m_transparentBlack02 = new Color(0f, 0f, 0f, 0.2f);
        protected Color m_transparentBlack04 = new Color(0f, 0f, 0f, 0.4f);

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_command = (ConsoleCommandObject)target;

            p_script = serializedObject.FindProperty("m_Script");
            p_commandPieces = serializedObject.FindProperty("m_commandPieces");

            m_excludedProperties = new();
            m_excludedProperties.Add(p_script.propertyPath);
            m_excludedProperties.Add(p_commandPieces.propertyPath);

            CreatePiecesList();
        }

        #endregion

        #region GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());

            EditorGUILayout.Space(15f);

            m_piecesList.DoLayoutList();

            EditorGUILayout.Space(15f);

            OnPreviewGUI();

            serializedObject.ApplyModifiedProperties();
        }

        protected void OnPreviewGUI()
        {
            m_previewOpen = EditorGUILayout.Foldout(m_previewOpen, "Preview", true);
            if (m_previewOpen)
            {
                Color guiColor = GUI.color;
                if (!string.IsNullOrWhiteSpace(m_previewCommandStart))
                {
                    GUI.color = m_command.IsCommandValid(m_previewCommandStart, out _) ? Color.green : Color.red;
                }
                m_previewCommandStart = EditorGUILayout.TextField(m_previewCommandStart);
                GUI.color = guiColor;

                var validCommands =
                    string.IsNullOrWhiteSpace(m_previewCommandStart) ?
                    m_command.GetCommandOptions() :
                    m_command.GetCommandOptionsStartingWith(m_previewCommandStart);

                var previewRect = EditorGUILayout.GetControlRect(false, 150f);
                previewRect.x += 4f;
                previewRect.width -= 8f;

                float scrollbarSpace = validCommands.Count > 7 ? 15f : 0f;
                var viewRect = new Rect(0, 0, previewRect.width - scrollbarSpace, validCommands.Count * 20f);

                EditorGUI.DrawRect(previewRect, m_transparentBlack02);
                m_previewScrollPos = GUI.BeginScrollView(previewRect, m_previewScrollPos, viewRect);

                var commandRect = new Rect(0, 0, viewRect.width, 20f);
                for (int i = 0; i < validCommands.Count; i++)
                {
                    EditorGUI.DrawRect(commandRect, i % 2 == 0 ? m_transparentBlack02 : m_transparentBlack04);
                    EditorGUI.LabelField(commandRect, validCommands[i].ToString());
                    commandRect.y += 20f;
                }

                GUI.EndScrollView();
            }
        }

        #endregion

        #region Command Pieces List

        protected virtual void CreatePiecesList()
        {
            m_piecesList = new ReorderableList(serializedObject, p_commandPieces, true, true, true, true)
            {
                // HEADER
                headerHeight = 20f,
                drawHeaderCallback = OnHeader,
                
                // ELEMENT
                elementHeightCallback = OnElementHeight,
                drawElementCallback = OnDrawElement,

                // BACKGROUND
                drawElementBackgroundCallback = OnDrawBackground,
            };
        }

        // HEADER
        protected virtual void OnHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Command Pieces");
        }

        // ELEMENT
        protected virtual float OnElementHeight(int index)
        {
            return EditorGUI.GetPropertyHeight(p_commandPieces.GetArrayElementAtIndex(index), true) + 4f;
        }
        protected virtual void OnDrawElement(Rect rect, int index, bool active, bool focused)
        {
            rect.y += 3f;
            EditorGUI.PropertyField(rect, p_commandPieces.GetArrayElementAtIndex(index), true);
        }

        // BACKGROUND
        protected virtual void OnDrawBackground(Rect rect, int index, bool active, bool focused)
        {
            if (focused) EditorGUI.DrawRect(rect, m_transparentWhite01);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
        }

        #endregion
    }

#endif
}
