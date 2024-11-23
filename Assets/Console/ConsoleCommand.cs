using Dhs5.Utility.Console;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CCMD_", menuName = "Dhs5 Utility/Console/Command")]
public class ConsoleCommand : ScriptableObject
{
    #region Members

    [SerializeField] private List<ConsoleCommandPiece> m_commandPieces;

    #endregion

    #region Properties

    #endregion
}
