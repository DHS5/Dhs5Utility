using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;
using static UnityEditor.Profiling.FrameDataView;

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
                if (!_commands.Add(command))
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
                            if (attribute != null)
                            {
                                if (!Application.isPlaying && attribute.scope == ConsoleCommand.EScope.RUNTIME) continue;
#if !UNITY_EDITOR
                                if (attribute.scope == ConsoleCommand.EScope.EDITOR) continue;
#endif
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

        private static IEnumerable<ConsoleCommand> GetBuiltInCommands()
        {
            #region Time

            yield return new ConsoleCommand(
                "timescale",
                ConsoleCommand.EScope.RUNTIME,
                new ConsoleCommand.EParameter[] { ConsoleCommand.EParameter.FLOAT },
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
                SetCommandLineContent(value);
            }
        }
        public static void SetCommandLineContent(string content)
        {
            _commandLineContent = content;
            _commandLineContentAsArray = content.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (string.IsNullOrEmpty(content)
                || _commandLineContentAsArray.Length == 0)
            {
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

        private static void ClearCommandOptions()
        {
            _currentCommandOptions.Clear();
        }
        private static void ComputeCommandOptions()
        {
            _currentCommandOptions.Clear();

            foreach (var command in _commands)
            {
                var matchResult = command.IsMatch(_commandLineContentAsArray);
                if (matchResult != ConsoleCommand.EMatchResult.NO_MATCH)
                {
                    _currentCommandOptions.Add(command, matchResult);
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
    }
}
