using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace Dhs5.Utility.Debugger
{
    public static class ConsoleCommandsRegister
    {
        #region Members

        private static HashSet<ConsoleCommand> _commands;

        #endregion


        #region Commands Registration

        private static void RegisterCommands()
        {
            // Init Commands
            if (_commands != null)
            {
                _commands.Clear();
            }
            else
            {
                _commands = new();
            }

            // Built-in Commands
            foreach (var command in GetBuiltInCommands())
            {
                if (IsScopeValid(command.scope) && !_commands.Add(command))
                {
                    Debug.LogWarning("Could not register Built-in Command " + command.optionString);
                }
            }

            // User Commands
            var bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var methodInfo in type.GetMethods(bindingFlags))
                        {
                            var attribute = methodInfo.GetCustomAttribute<ConsoleCommandAttribute>(inherit:false);
                            if (attribute != null && IsScopeValid(attribute.scope))
                            {
                                if (!_commands.Add(new ConsoleCommand(attribute.name, attribute.scope, methodInfo)))
                                {
                                    Debug.LogWarning("Could not register ConsoleCommand with name " + attribute.name
                                        + " from Method " + methodInfo.Name + " on " + methodInfo.DeclaringType);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
        private static bool IsScopeValid(ConsoleCommand.EScope scope)
        {
            if (!Application.isPlaying && scope == ConsoleCommand.EScope.RUNTIME) return false;
#if !UNITY_EDITOR
            if (scope == ConsoleCommand.EScope.EDITOR) return false;
#endif
            return true;
        }

        private static IEnumerable<ConsoleCommand> GetBuiltInCommands()
        {
            #region Time

            yield return new ConsoleCommand(
                "timescale",
                ConsoleCommand.EScope.RUNTIME,
                new ConsoleCommand.Parameter[] { new(ConsoleCommand.EParameterType.FLOAT, typeof(float)) },
                (parameters) => { Time.timeScale = (float)parameters[0]; });

            #endregion

            #region Test

            yield return new ConsoleCommand(
                "test",
                ConsoleCommand.EScope.EDITOR,
                null,
                (parameters) => { Debug.Log("test"); });
            yield return new ConsoleCommand(
                "testing",
                ConsoleCommand.EScope.EDITOR,
                null,
                (parameters) => { Debug.Log("testing"); });
            yield return new ConsoleCommand(
                "test",
                ConsoleCommand.EScope.EDITOR,
                new ConsoleCommand.Parameter[] { new(ConsoleCommand.EParameterType.BOOL, typeof(bool)) },
                (parameters) => { Debug.Log("test " + parameters[0]); });
            yield return new ConsoleCommand(
                "test",
                ConsoleCommand.EScope.EDITOR,
                new ConsoleCommand.Parameter[] { new(ConsoleCommand.EParameterType.INT, typeof(int), 5) },
                (parameters) => { Debug.Log("test " + parameters[0]); });
            yield return new ConsoleCommand(
                "test",
                ConsoleCommand.EScope.EDITOR,
                new ConsoleCommand.Parameter[] { new(ConsoleCommand.EParameterType.INT, typeof(int)) },
                (parameters) => { Debug.Log("test " + parameters[0]); });
            yield return new ConsoleCommand(
                "test",
                ConsoleCommand.EScope.EDITOR,
                new ConsoleCommand.Parameter[] { new(ConsoleCommand.EParameterType.FLOAT, typeof(float)), new(ConsoleCommand.EParameterType.FLOAT, typeof(float), 0.2f) },
                (parameters) => { Debug.Log("test " + parameters[0] + " " + parameters[1]); });
            yield return new ConsoleCommand(
                "test",
                ConsoleCommand.EScope.EDITOR,
                new ConsoleCommand.Parameter[] { new(ConsoleCommand.EParameterType.VECTOR3, typeof(Vector3)) },
                (parameters) => { Debug.Log("test " + parameters[0]); });
            yield return new ConsoleCommand(
                "test_enum",
                ConsoleCommand.EScope.EDITOR,
                new ConsoleCommand.Parameter[] { new(ConsoleCommand.EParameterType.ENUM, typeof(EDebugCategory)) },
                (parameters) => { Debug.Log("test " + parameters[0]); });
            yield return new ConsoleCommand(
                "testdef",
                ConsoleCommand.EScope.EDITOR,
                new ConsoleCommand.Parameter[] { new(ConsoleCommand.EParameterType.INT, typeof(int), 5) },
                (parameters) => { Debug.Log("test default " + parameters[0]); });

            #endregion
        }

        #endregion


        #region Command Line

        private static string _commandLineContent;
        private static string[] _commandLineContentAsArray;
        public static string CommandLineContent
        {
            get => _commandLineContent;
            set
            {
                if (value != _commandLineContent)
                {
                    SetCommandLineContent(value);
                }
            }
        }
        public static void SetCommandLineContent(string content)
        {
            _commandLineContent = string.IsNullOrWhiteSpace(content) ? content : content.TrimStart();
            _commandLineContentAsArray = string.IsNullOrEmpty(content) ? null : content.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (string.IsNullOrEmpty(content))
            {
                _commandLineContentAsArray = null;
                ClearCommandOptions();
            }
            else
            {
                ComputeCommandOptions();
            }
        }
        public static void ClearCommandLineContent()
        {
            _commandLineContent = null;
            ClearCommandOptions();
        }

        #endregion

        #region Command Options

        private static List<ConsoleCommand> _currentCommandOptions = new();
        private static Dictionary<ConsoleCommand, ConsoleCommand.EMatchResult> _optionsMatchResult = new();
        private static ConsoleCommand _closestMatch;
        public static int SelectedOptionIndex { get; private set; } = -1;
        public static int CurrentOptionsCount => _currentCommandOptions.Count;

        private static void ClearCommandOptions()
        {
            _currentCommandOptions.Clear();
            _optionsMatchResult.Clear();
            _closestMatch = null;
            SelectedOptionIndex = -1;
            _commandsHistoryIndex = -1;
        }
        private static void ComputeCommandOptions()
        {
            if (_commands == null)
            {
                RegisterCommands();
            }

            _currentCommandOptions.Clear();
            _optionsMatchResult.Clear();
            _closestMatch = null;

            int index = 0;
            foreach (var command in _commands)
            {
                var matchResult = command.IsMatch(_commandLineContentAsArray);
                if (matchResult != ConsoleCommand.EMatchResult.NO_MATCH)
                {
                    _currentCommandOptions.Add(command);
                    _optionsMatchResult.Add(command, matchResult);

                    switch (matchResult)
                    {
                        case ConsoleCommand.EMatchResult.PERFECT_MATCH:
                            _closestMatch = command;
                            SelectedOptionIndex = index;
                            break;

                        case ConsoleCommand.EMatchResult.ACCEPTED_MATCH:
                            if (_closestMatch == null
                                || _optionsMatchResult[_closestMatch] != ConsoleCommand.EMatchResult.PERFECT_MATCH)
                            {
                                _closestMatch = command;
                                SelectedOptionIndex = index;
                            }
                            break;

                        case ConsoleCommand.EMatchResult.PARTIAL_MATCH:
                            if (_closestMatch == null
                                || _optionsMatchResult[_closestMatch] == ConsoleCommand.EMatchResult.NAME_MATCH)
                            {
                                _closestMatch = command;
                                SelectedOptionIndex = index;
                            }
                            break;

                        case ConsoleCommand.EMatchResult.NAME_MATCH:
                            if (_closestMatch == null)
                            {
                                _closestMatch = command;
                                SelectedOptionIndex = index;
                            }
                            break;
                    }

                    index++;
                }
            }
        }

        public static IEnumerable<KeyValuePair<string, ConsoleCommand.EMatchResult>> GetCurrentOptions()
        {
            if (_currentCommandOptions.IsValid())
            {
                foreach (var command in _currentCommandOptions)
                {
                    yield return new KeyValuePair<string, ConsoleCommand.EMatchResult>(command.optionString, _optionsMatchResult[command]);
                }
            }
        }
        public static string GetHintString()
        {
            if (string.IsNullOrEmpty(CommandLineContent))
            {
                return "Type command here..."; 
            }
            if (_closestMatch != null
                && _commandLineContentAsArray.Length == 1)
            {
                return _closestMatch.hintString;
            }
            return string.Empty;
        }

        public static void SelectNextOption()
        {
            if (_currentCommandOptions.IsValid())
            {
                if (SelectedOptionIndex < _currentCommandOptions.Count - 1)
                    SelectedOptionIndex++;
                else
                    SelectedOptionIndex = 0;
            }
            else
            {
                SelectedOptionIndex = -1;
            }
        }
        public static void SelectPreviousOption()
        {
            if (_currentCommandOptions.IsValid())
            {
                if (SelectedOptionIndex > 0)
                    SelectedOptionIndex--;
                else
                    SelectedOptionIndex = _currentCommandOptions.Count - 1;
            }
            else
            {
                SelectedOptionIndex = -1;
            }
        }

        public static void FillFromOption()
        {
            if (_currentCommandOptions.IsIndexValid(SelectedOptionIndex, out var command))
            {
                if (!_commandLineContentAsArray.IsValid()
                    || _commandLineContentAsArray.Length == 1
                    || _optionsMatchResult[command] is ConsoleCommand.EMatchResult.NO_MATCH or ConsoleCommand.EMatchResult.NAME_MATCH)
                {
                    CommandLineContent = command.hintString;
                }
            }
        }

        #endregion

        #region Command History

        private static List<string> _commandsHistory = new();
        private static int _commandsHistoryIndex = -1;

        private static void AddToCommandHistory(string rawCommand)
        {
            _commandsHistory.Insert(0, rawCommand);

            if (_commandsHistory.Count > 20) // TODO
            {
                _commandsHistory.RemoveAt(_commandsHistory.Count - 1);
            }
        }

        public static void SelectPreviousCommandInHistory()
        {
            if (_commandsHistoryIndex < _commandsHistory.Count - 1)
            {
                _commandsHistoryIndex++;
                CommandLineContent = _commandsHistory[_commandsHistoryIndex];
            }
        }
        public static void SelectNextCommandInHistory()
        {
            if (_commandsHistoryIndex > 0)
            {
                _commandsHistoryIndex--;
                CommandLineContent = _commandsHistory[_commandsHistoryIndex];
            }
            else
            {
                CommandLineContent = string.Empty;
            }
        }

        #endregion

        #region Command Validation

        public static void ValidateCommand()
        {
            CommandLineContent = CommandLineContent.Trim();
            AddToCommandHistory(CommandLineContent);

            if (_closestMatch != null
                && _optionsMatchResult[_closestMatch] == ConsoleCommand.EMatchResult.PERFECT_MATCH)
            {
                // Run command
                if (_commandLineContentAsArray.Length > 1)
                {
                    string[] stringParameters = new string[_commandLineContentAsArray.Length - 1];
                    for (int i = 0; i < stringParameters.Length; i++)
                    {
                        stringParameters[i] = _commandLineContentAsArray[i + 1];
                    }
                    _closestMatch.Run(stringParameters);
                }
                else
                {
                    _closestMatch.Run(commandParameters:null);
                }
            }
            else
            {
                Debug.LogWarning("Could not run " + CommandLineContent);
            }

            ClearCommandLineContent();
        }

        #endregion
    }
}
