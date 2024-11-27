using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;

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

        internal struct CommandArray
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
            public string ToStringWithoutParams()
            {
                StringBuilder sb = new();

                for (int i = 0; i < m_list.Count; i++)
                {
                    if (ConsoleCommand.IsParameterString(m_list[i], out var paramType))
                    {
                        sb.Append(ConsoleCommand.ParamDefaultValueAsString(paramType));
                    }
                    else
                    {
                        sb.Append(m_list[i]);
                    }

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
                            !IsFirstStrStartingLikeParameterOfTypeSecondStr(ownStr, otherStr))
                            //!ConsoleCommand.IsParameterString(otherStr, out _))
                        {
                            return false;
                        }
                    }

                    ownStr = GetAtIndex(count - 1);
                    otherStr = other.GetAtIndex(count - 1);
                    if (!IsFirstStrStartingLikeParameterOfTypeSecondStr(ownStr, otherStr) &&
                        !otherStr.StartsWith(ownStr, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }

            #endregion

            #region STATIC Utility

            private static bool IsFirstStrStartingLikeParameterOfTypeSecondStr(string firstStr, string secondStr)
            {
                if (!ConsoleCommand.IsParameterString(secondStr, out var paramType))
                {
                    return false;
                }

                return ConsoleCommand.IsParameterValid(firstStr, paramType, out _);
            }

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

        #region STRUCT : ValidCommand

        public struct ValidCommand
        {
            public ValidCommand(ConsoleCommand command, string rawCommand, object[] parameters)
            {
                this.command = command;
                this.rawCommand = rawCommand;
                this.parameters = parameters;
            }

            public readonly ConsoleCommand command;
            public readonly string rawCommand;
            public readonly object[] parameters;


            public static ValidCommand Invalid()
            {
                return new ValidCommand(null, null, null);
            }
        }

        #endregion

        #region ENUM : Parameter Type

        public enum ParamType
        {
            BOOL,
            INT,
            FLOAT,
            STRING
        }

        #endregion


        #region Consts

        public const string PARAM_INT = "$INT$";
        public const string PARAM_FLOAT = "$FLOAT$";
        public const string PARAM_BOOL = "$BOOL$";
        public const string PARAM_STR = "$STRING$";

        #endregion

        #region Members

        [SerializeField] private List<ConsoleCommandPiece> m_commandPieces;

        #endregion

        #region Properties

        private int PieceCount => m_commandPieces.Count;

        #endregion


        #region Command Options

        internal List<CommandArray> GetCommandOptions()
        {
            List<CommandArray> commandArrays = new();

            if (m_commandPieces.IsValid())
            {
                Recursive_GetCommand(commandArrays, new CommandArray(), 0);
            }

            return commandArrays;
        }
        internal List<CommandArray> GetCommandOptionsStartingWith(string commandStart)
        {
            List<CommandArray> commandArrays = new();
            var startArray = new CommandArray(commandStart);

            if (m_commandPieces.IsValid())
            {
                Recursive_GetCommandStartingWith(commandArrays, new CommandArray(), 0, startArray);
            }

            return commandArrays;
        }

        private void Recursive_GetCommand(List<CommandArray> commandArrays, CommandArray currentArray, int pieceIndex)
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
                    Recursive_GetCommand(commandArrays, nextArray, pieceIndex + 1);
                }
            }
        }
        private void Recursive_GetCommandStartingWith(List<CommandArray> commandArrays, CommandArray currentArray, int pieceIndex, CommandArray startArray)
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
                        if (nextArray.Count > startArray.Count) Recursive_GetCommand(commandArrays, nextArray, pieceIndex + 1);
                        else Recursive_GetCommandStartingWith(commandArrays, nextArray, pieceIndex + 1, startArray);
                    }
                }
            }
        }

        #endregion

        #region Valid Command

        public bool IsCommandValid(string rawCommand, out ValidCommand validCommand)
        {
            rawCommand = rawCommand.Trim();
            string currentStr = rawCommand;
            object[] parameters = new object[m_commandPieces.Count];

            for (int i = 0; i < m_commandPieces.Count; i++)
            {
                if (m_commandPieces[i].IsCommandValid(currentStr, out object param, out string nextStr))
                {
                    parameters[i] = param;
                    currentStr = nextStr;
                }
                else
                {
                    validCommand = ValidCommand.Invalid();
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(currentStr))
            {
                validCommand = ValidCommand.Invalid();
                return false;
            }

            validCommand = new ValidCommand(this, rawCommand, parameters);
            return true;
        }

        #endregion


        #region STATIC Utility

        public static bool IsParameterString(string str, out ParamType paramType)
        {
            switch (str)
            {
                case PARAM_BOOL:
                    paramType = ParamType.BOOL;
                    return true;
                case PARAM_INT:
                    paramType = ParamType.INT;
                    return true;
                case PARAM_FLOAT:
                    paramType = ParamType.FLOAT;
                    return true;
                case PARAM_STR:
                    paramType = ParamType.STRING;
                    return true;
            }
            paramType = ParamType.BOOL;
            return false;
        }
        public static string GetParameterString(ParamType paramType)
        {
            switch (paramType)
            {
                case ParamType.BOOL: return PARAM_BOOL;
                case ParamType.INT: return PARAM_INT;
                case ParamType.FLOAT: return PARAM_FLOAT;
                case ParamType.STRING: return PARAM_STR;
            }
            return null;
        }

        public static bool IsParameterValid(string paramStr, ParamType paramType, out object param)
        {
            param = null;
            switch (paramType)
            {
                case ParamType.BOOL:
                    if (paramStr == "T")
                    {
                        param = true;
                        return true;
                    }
                    if (paramStr == "F")
                    {
                        param = false;
                        return true;
                    }
                    return false;

                case ParamType.INT:
                    if (int.TryParse(paramStr, out var intVal))
                    {
                        param = intVal;
                        return true;
                    }
                    return false;

                case ParamType.FLOAT:
                    if (float.TryParse(paramStr, out var floatVal))
                    {
                        param = floatVal;
                        return true;
                    }
                    return false;

                case ParamType.STRING:
                    param = paramStr;
                    return true;

                default:
                    throw new NotImplementedException();
            }
        }

        public static string ParamDefaultValueAsString(ParamType paramType)
        {
            switch (paramType)
            {
                case ParamType.BOOL: return "F";
                case ParamType.INT: return "0";
                case ParamType.FLOAT: return "0.0";
                case ParamType.STRING: return "_";
                default:
                    throw new NotImplementedException();
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

        protected SerializedProperty p_script;
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
