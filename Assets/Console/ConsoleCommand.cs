using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;


#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
using UnityEditorInternal;
#endif

namespace Dhs5.Utility.Console
{
    [CreateAssetMenu(fileName = "CCMD_", menuName = "Dhs5 Utility/Console/Command")]
    public class ConsoleCommand : ScriptableObject
    {
        #region STRUCT : CommandArray

        public struct CommandArray
        {
            #region Constructors

            public CommandArray(string str)
            {
                m_list = new();
                Push(str);
            }
            public CommandArray(CommandArray other)
            {
                if (other.IsValid) m_list = new(other.m_list);
                else m_list = new();
            }

            #endregion

            #region Members

            private List<string> m_list;

            #endregion

            #region Properties

            public bool IsValid => m_list.IsValid();
            public int Count => m_list.Count;

            #endregion

            #region Creation Methods

            public void Push(string input)
            {
                if (string.IsNullOrWhiteSpace(input)) return;
                if (m_list == null) m_list = new();

                var array = input.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < array.Length; i++)
                {
                    m_list.Add(array[i]);
                }
            }

            #endregion

            #region Accessors

            public string GetAtIndex(int index)
            {
                if (m_list.IsIndexValid(index)) return m_list[index];
                return null;
            }

            public override string ToString()
            {
                StringBuilder sb = new();

                for (int i = 0; i < m_list.Count; i++)
                {
                    sb.Append(m_list[i]);
                    if (i < Count - 1) sb.Append(' ');
                }

                return sb.ToString();
            }

            #endregion

            #region Utility

            public bool StartsTheSameAs(CommandArray other)
            {
                if (IsValid && other.IsValid)
                {
                    int count = Mathf.Min(Count, other.Count);
                    string ownStr, otherStr;

                    for (int i = 0; i < count - 1; i++)
                    {
                        ownStr = GetAtIndex(i);
                        otherStr = other.GetAtIndex(i);
                        if (string.Compare(ownStr, otherStr, true) != 0 && 
                            otherStr != PARAMETER)
                        {
                            return false;
                        }
                    }

                    ownStr = GetAtIndex(count - 1);
                    otherStr = other.GetAtIndex(count - 1);
                    if (otherStr != PARAMETER &&
                        !otherStr.StartsWith(ownStr, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }

            #endregion

            #region STATIC Utility

            public static bool StartTheSame(CommandArray a, CommandArray b)
            {
                if (a.IsValid && b.IsValid)
                {
                    int count = Mathf.Min(a.Count, b.Count);

                    for (int i = 0; i < count; i++)
                    {
                        if (string.Compare(a.GetAtIndex(i), b.GetAtIndex(i), true) != 0) 
                            return false;
                    }
                }
                return true;
            }

            #endregion
        }

        #endregion

        #region Members

        public const string PARAMETER = "$PARAM$";

        [SerializeField] private List<ConsoleCommandPiece> m_commandPieces;

        #endregion

        #region Properties

        public int PieceCount => m_commandPieces.Count;

        #endregion


        #region Valid Command Strings

        public List<CommandArray> GetValidCommandStrings()
        {
            List<CommandArray> commandArrays = new();

            if (m_commandPieces.IsValid())
            {
                Recursive_GetValidCommand(commandArrays, new CommandArray(), 0);
            }

            return commandArrays;
        }
        public List<CommandArray> GetValidCommandStringsStartingWith(string commandStart)
        {
            List<CommandArray> commandArrays = new();
            var startArray = new CommandArray(commandStart);

            if (m_commandPieces.IsValid())
            {
                Recursive_GetValidCommandStartingWith(commandArrays, new CommandArray(), 0, startArray);
            }

            return commandArrays;
        }

        private void Recursive_GetValidCommand(List<CommandArray> commandArrays, CommandArray currentArray, int pieceIndex)
        {
            CommandArray nextArray;

            foreach (var option in m_commandPieces[pieceIndex].GetOptions())
            {
                nextArray = new CommandArray(currentArray);
                nextArray.Push(option);

                if (pieceIndex == PieceCount - 1)
                {
                    commandArrays.Add(nextArray);
                }
                else
                {
                    Recursive_GetValidCommand(commandArrays, nextArray, pieceIndex + 1);
                }
            }
        }
        private void Recursive_GetValidCommandStartingWith(List<CommandArray> commandArrays, CommandArray currentArray, int pieceIndex, CommandArray startArray)
        {
            CommandArray nextArray;

            foreach (var option in m_commandPieces[pieceIndex].GetOptions())
            {
                nextArray = new CommandArray(currentArray);
                nextArray.Push(option);

                if (startArray.StartsTheSameAs(nextArray))
                {
                    if (pieceIndex == PieceCount - 1)
                    {
                        if (startArray.Count <= nextArray.Count)
                            commandArrays.Add(nextArray);
                    }
                    else
                    {
                        if (nextArray.Count > startArray.Count) Recursive_GetValidCommand(commandArrays, nextArray, pieceIndex + 1);
                        else Recursive_GetValidCommandStartingWith(commandArrays, nextArray, pieceIndex + 1, startArray);
                    }
                }
            }
        }

        #endregion
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(ConsoleCommand))]
    public class ConsoleCommandEditor : Editor
    {
        #region Members

        protected ConsoleCommand m_command;

        protected SerializedProperty p_commandPieces;
        protected List<string> m_excludedProperties;

        protected ReorderableList m_piecesList;
        protected string m_previewCommandStart;
        protected bool m_previewOpen;
        protected Vector2 m_previewScrollPos;

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_command = (ConsoleCommand)target;

            p_commandPieces = serializedObject.FindProperty("m_commandPieces");

            m_excludedProperties = new();
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

            //EditorGUILayout.PropertyField(p_commandPieces);
            m_piecesList.DoLayoutList();

            EditorGUILayout.Space(15f);

            m_previewOpen = EditorGUILayout.Foldout(m_previewOpen, "Preview", true);
            if (m_previewOpen)
            {
                m_previewCommandStart = EditorGUILayout.TextField(m_previewCommandStart);

                var validCommands = 
                    string.IsNullOrWhiteSpace(m_previewCommandStart) ? 
                    m_command.GetValidCommandStrings() : 
                    m_command.GetValidCommandStringsStartingWith(m_previewCommandStart);

                var previewRect = EditorGUILayout.GetControlRect(false, 150f);
                previewRect.x += 4f;
                previewRect.width -= 8f;

                float scrollbarSpace = validCommands.Count > 7 ? 15f : 0f;
                var viewRect = new Rect(0, 0, previewRect.width - scrollbarSpace, validCommands.Count * 20f);

                EditorGUI.DrawRect(previewRect, EditorGUIHelper.transparentBlack02);
                m_previewScrollPos = GUI.BeginScrollView(previewRect, m_previewScrollPos, viewRect);

                var commandRect = new Rect(0, 0, viewRect.width, 20f);
                for (int i = 0; i < validCommands.Count; i++)
                {
                    EditorGUI.DrawRect(commandRect, i % 2 == 0 ? EditorGUIHelper.transparentBlack02 : EditorGUIHelper.transparentBlack04);
                    EditorGUI.LabelField(commandRect, validCommands[i].ToString());
                    commandRect.y += 20f;
                }

                GUI.EndScrollView();
            }

            serializedObject.ApplyModifiedProperties();
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
            if (focused) EditorGUI.DrawRect(rect, EditorGUIHelper.transparentWhite01);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
        }

        #endregion
    }

#endif
}
