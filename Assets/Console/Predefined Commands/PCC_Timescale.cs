using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Console
{
    [CreateAssetMenu(fileName = "PCC_Timescale", menuName = "Dhs5 Utility/Console/Predefined Commands/Timescale")]
    public class PCC_Timescale : PredefinedConsoleCommand
    {
        #region Command Creation

        protected override List<ConsoleCommandPiece> OnCreateCommand()
        {
            var result = new List<ConsoleCommandPiece>();

            result.Add(new ConsoleCommandPiece(false, "/timescale"));
            result.Add(new ConsoleCommandPiece(ParamType.FLOAT));

            return result;
        }

        #endregion

        #region Callbacks

        protected override void OnCommandValidated(ValidCommand validCommand)
        {
            if (validCommand.parameters.Length == 2
                && validCommand.parameters[1] is float timeScale)
            {
                Time.timeScale = timeScale;
            }
        }

        #endregion
    }
}
