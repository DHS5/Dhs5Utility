using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dhs5.Utility.Console
{
    public class ScriptedConsoleCommand : IConsoleCommand
    {
        #region Constructor

        public ScriptedConsoleCommand(params ConsoleCommandPiece[] commandPieces)
        {
            m_commandPieces = commandPieces.ToList();
        }
        public ScriptedConsoleCommand(string singleInput)
        {
            m_commandPieces = new();
            m_commandPieces.Add(new ConsoleCommandPiece(false, singleInput));
        }

        #endregion

        #region Members

        private readonly List<ConsoleCommandPiece> m_commandPieces;

        #endregion

        #region IConsoleCommand

        public ConsoleCommandPiece this[int index] => m_commandPieces[index];

        public int Count => m_commandPieces.Count;

        #endregion
    }
}
