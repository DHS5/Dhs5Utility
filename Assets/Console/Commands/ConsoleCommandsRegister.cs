using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace Dhs5.Utility.Console
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
                    Debug.LogWarning("Could not register Built-in Command with name " + command.name);
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
                new ConsoleCommand.EParameter[] { ConsoleCommand.EParameter.FLOAT },
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
                new ConsoleCommand.EParameter[] { ConsoleCommand.EParameter.BOOL },
                (parameters) => { Debug.Log("test " + parameters[0]); });
            yield return new ConsoleCommand(
                "test",
                ConsoleCommand.EScope.EDITOR,
                new ConsoleCommand.EParameter[] { ConsoleCommand.EParameter.FLOAT, ConsoleCommand.EParameter.FLOAT },
                (parameters) => { Debug.Log("test " + parameters[0] + " " + parameters[1]); });
            yield return new ConsoleCommand(
                "test",
                ConsoleCommand.EScope.EDITOR,
                new ConsoleCommand.EParameter[] { ConsoleCommand.EParameter.VECTOR3 },
                (parameters) => { Debug.Log("test " + parameters[0]); });

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
            _commandLineContent = content;
            _commandLineContentAsArray = string.IsNullOrEmpty(content) ? null : content.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (string.IsNullOrEmpty(content)
                || !_commandLineContentAsArray.IsValid())
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

        private static Dictionary<ConsoleCommand, ConsoleCommand.EMatchResult> _currentCommandOptions = new();
        private static ConsoleCommand _closestMatch;

        private static void ClearCommandOptions()
        {
            _currentCommandOptions.Clear();
            _closestMatch = null;
        }
        private static void ComputeCommandOptions()
        {
            if (_commands == null)
            {
                RegisterCommands();
            }

            _currentCommandOptions.Clear();
            _closestMatch = null;

            foreach (var command in _commands)
            {
                var matchResult = command.IsMatch(_commandLineContentAsArray);
                if (matchResult != ConsoleCommand.EMatchResult.NO_MATCH)
                {
                    _currentCommandOptions.Add(command, matchResult);

                    switch (matchResult)
                    {
                        case ConsoleCommand.EMatchResult.PERFECT_MATCH:
                            _closestMatch = command;
                            break;

                        case ConsoleCommand.EMatchResult.PARTIAL_MATCH:
                            if (_closestMatch == null
                                || _currentCommandOptions[_closestMatch] == ConsoleCommand.EMatchResult.NAME_MATCH)
                            {
                                _closestMatch = command;
                            }
                            break;

                        case ConsoleCommand.EMatchResult.NAME_MATCH:
                            _closestMatch ??= command;
                            break;
                    }
                }
            }
        }

        public static IEnumerable<string> GetCurrentOptions()
        {
            if (_currentCommandOptions.IsValid())
            {
                foreach (var (command, _) in _currentCommandOptions)
                {
                    yield return command.optionString;
                }
            }
        }

        #endregion

        #region Command History

        private static List<string> _commandsHistory = new();

        private static void AddToCommandHistory(string rawCommand)
        {
            _commandsHistory.Add(rawCommand);

            if (_commandsHistory.Count > 20) // TODO
            {
                _commandsHistory.RemoveAt(0);
            }
        }

        #endregion

        #region Command Validation

        public static void ValidateCommand()
        {
            AddToCommandHistory(CommandLineContent);

            if (_closestMatch != null
                && _currentCommandOptions[_closestMatch] == ConsoleCommand.EMatchResult.PERFECT_MATCH)
            {
                // Run command
                Debug.Log("run " + CommandLineContent + " as " + _closestMatch.optionString);
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
