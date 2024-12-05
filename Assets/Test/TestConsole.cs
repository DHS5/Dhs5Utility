using Dhs5.Utility.Console;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestConsole : MonoBehaviour
{
    [SerializeField] private List<ConsoleCommandObject> commands;

    private void Start()
    {
        //TestOnScreenConsole.Init();

        foreach (var command in commands)
        {
            TestOnScreenConsole.Register(command, OnConsoleCommandValidated);
        }

        var cmd = new ScriptedConsoleCommand(
            new ConsoleCommandPiece(false, "/Debug time"));
        TestOnScreenConsole.Register(cmd, OnDebugTime);
    }

    private void OnConsoleCommandValidated(ValidCommand command)
    {
        Debug.Log(command.rawCommand);

        for (int i = 0; i < command.parameters.Length; i++)
        {
            if (command.parameters[i] != null)
                Debug.Log("param " + i + " = " + command.parameters[i]);
        }
    }

    private void OnDebugTime(ValidCommand command)
    {
        Debug.Log(Time.time);
    }
}
