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
        /// <summary>
        /// Commands are filtered by scope depending on play mode : they are registered again when it changes
        /// </summary>
        private static bool _registeredWhilePlaying;

        #endregion


        #region Commands Registration

#if UNITY_EDITOR
        /// <summary>
        /// In the editor, commands are registered on load (fast thanks to the TypeCache)
        /// so that registration errors show up right away, not on the first use of a console
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterCommandsOnLoad()
        {
            RegisterCommands();
        }
#endif

        private static void EnsureCommandsRegistered()
        {
            if (_commands == null || _registeredWhilePlaying != Application.isPlaying)
            {
                RegisterCommands();
            }
        }

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
            _registeredWhilePlaying = Application.isPlaying;

            // Built-in Commands
            foreach (var command in GetBuiltInCommands())
            {
                if (IsScopeValid(command.scope))
                {
                    TryAddCommand(command, "Built-in Command " + command.optionString);
                }
            }

            // User Commands
            foreach (var methodInfo in GetCommandMethods())
            {
                var attribute = methodInfo.GetCustomAttribute<ConsoleCommandAttribute>(inherit: false);
                if (attribute == null || !IsScopeValid(attribute.scope)) continue;

                var source = "ConsoleCommand " + attribute.name + " (" + methodInfo.DeclaringType + "." + methodInfo.Name + ")";
                if (!methodInfo.IsStatic)
                {
                    Debug.LogError("Could not register " + source + " : the method must be static");
                    continue;
                }

                // One invalid command must not prevent the registration of the others
                ConsoleCommand command;
                try
                {
                    command = new ConsoleCommand(attribute.name, attribute.scope, methodInfo);
                }
                catch (Exception e)
                {
                    Debug.LogError("Could not register " + source + " : " + e);
                    continue;
                }
                TryAddCommand(command, source);
            }
        }
        private static void TryAddCommand(ConsoleCommand command, string source)
        {
            if (!_commands.Add(command))
            {
                _commands.TryGetValue(command, out var existingCommand);
                Debug.LogWarning("Could not register " + source + " : it conflicts with the command " + existingCommand?.optionString
                    + " (same name and parameter types, or a default value making the call ambiguous)");
            }
        }

        private static IEnumerable<MethodInfo> GetCommandMethods()
        {
#if UNITY_EDITOR
            foreach (var methodInfo in UnityEditor.TypeCache.GetMethodsWithAttribute<ConsoleCommandAttribute>())
            {
                yield return methodInfo;
            }
#else
            var bindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (var type in types)
                {
                    if (type == null) continue;
                    foreach (var methodInfo in type.GetMethods(bindingFlags))
                    {
                        if (methodInfo.IsDefined(typeof(ConsoleCommandAttribute), false))
                        {
                            yield return methodInfo;
                        }
                    }
                }
            }
#endif
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
            EnsureCommandsRegistered();

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

            // ACCEPTED_MATCH runs too : default values left out, int for a float, 0/1 for a bool, number for an enum...
            // (a PERFECT_MATCH overload is still preferred when there is one)
            if (_closestMatch != null
                && _optionsMatchResult[_closestMatch] is ConsoleCommand.EMatchResult.PERFECT_MATCH or ConsoleCommand.EMatchResult.ACCEPTED_MATCH)
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
