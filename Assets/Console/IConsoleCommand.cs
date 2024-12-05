using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dhs5.Utility.Console
{
    public interface IConsoleCommand
    {
        public int Count { get; }
        public ConsoleCommandPiece this[int index] { get; }
        public bool IsValid() => Count > 0;
    }

    public static class ConsoleCommandUtility
    {
        #region Consts

        public const string PARAM_INT = "$INT$";
        public const string PARAM_FLOAT = "$FLOAT$";
        public const string PARAM_BOOL = "$BOOL$";
        public const string PARAM_STR = "$STRING$";

        #endregion

        #region Command Options

        internal static List<CommandArray> GetCommandOptions(this IConsoleCommand consoleCommand)
        {
            List<CommandArray> commandArrays = new();

            if (consoleCommand.IsValid())
            {
                Recursive_GetCommand(consoleCommand, commandArrays, new CommandArray(), 0);
            }

            return commandArrays;
        }
        internal static List<CommandArray> GetCommandOptionsStartingWith(this IConsoleCommand consoleCommand, string commandStart)
        {
            List<CommandArray> commandArrays = new();
            var startArray = new CommandArray(commandStart);

            if (consoleCommand.IsValid())
            {
                Recursive_GetCommandStartingWith(consoleCommand, commandArrays, new CommandArray(), 0, startArray);
            }

            return commandArrays;
        }

        private static void Recursive_GetCommand(IConsoleCommand consoleCommand, List<CommandArray> commandArrays, CommandArray currentArray, int pieceIndex)
        {
            CommandArray nextArray;

            foreach (var option in consoleCommand[pieceIndex].GetOptions())
            {
                nextArray = new CommandArray(currentArray);
                nextArray.Push(option);

                if (pieceIndex == consoleCommand.Count - 1)
                {
                    commandArrays.Add(nextArray);
                }
                else
                {
                    Recursive_GetCommand(consoleCommand, commandArrays, nextArray, pieceIndex + 1);
                }
            }
        }
        private static void Recursive_GetCommandStartingWith(IConsoleCommand consoleCommand, List<CommandArray> commandArrays, CommandArray currentArray, int pieceIndex, CommandArray startArray)
        {
            CommandArray nextArray;

            foreach (var option in consoleCommand[pieceIndex].GetOptions())
            {
                nextArray = new CommandArray(currentArray);
                nextArray.Push(option);

                if (startArray.StartsTheSameAs(nextArray))
                {
                    if (pieceIndex == consoleCommand.Count - 1)
                    {
                        if (startArray.Count <= nextArray.Count)
                            commandArrays.Add(nextArray);
                    }
                    else
                    {
                        if (nextArray.Count > startArray.Count) Recursive_GetCommand(consoleCommand, commandArrays, nextArray, pieceIndex + 1);
                        else Recursive_GetCommandStartingWith(consoleCommand, commandArrays, nextArray, pieceIndex + 1, startArray);
                    }
                }
            }
        }

        #endregion

        #region Valid Command

        internal static bool IsCommandValid(this IConsoleCommand consoleCommand, string rawCommand, out ValidCommand validCommand)
        {
            rawCommand = rawCommand.Trim();
            string currentStr = rawCommand;
            object[] parameters = new object[consoleCommand.Count];

            for (int i = 0; i < consoleCommand.Count; i++)
            {
                if (consoleCommand[i].IsCommandValid(currentStr, out object param, out string nextStr))
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

            validCommand = new ValidCommand(consoleCommand, rawCommand, parameters);
            return true;
        }

        #endregion

        #region Utility

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
                if (ConsoleCommandUtility.IsParameterString(m_list[i], out var paramType))
                {
                    sb.Append(ConsoleCommandUtility.ParamDefaultValueAsString(paramType));
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
            if (!ConsoleCommandUtility.IsParameterString(secondStr, out var paramType))
            {
                return false;
            }

            return ConsoleCommandUtility.IsParameterValid(firstStr, paramType, out _);
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
        public ValidCommand(IConsoleCommand command, string rawCommand, object[] parameters)
        {
            this.command = command;
            this.rawCommand = rawCommand;
            this.parameters = parameters;
        }

        public readonly IConsoleCommand command;
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

}
