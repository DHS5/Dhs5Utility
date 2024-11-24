using Dhs5.Utility.Console;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestConsole : MonoBehaviour
{
    [SerializeField] private List<ConsoleCommand> commands;

    private void Start()
    {
        foreach (var command in commands)
        {
            OnScreenConsole.Register(command, OnConsoleCommandValidated);
        }

        OnScreenConsole.Open();
    }

    private void OnConsoleCommandValidated(ConsoleCommand.ValidCommand command)
    {
        Debug.Log(command.rawCommand);

        for (int i = 0; i < command.parameters.Length; i++)
        {
            if (command.parameters[i] != null)
                Debug.Log("param " + i + " = " + command.parameters[i]);
        }
    }
}
