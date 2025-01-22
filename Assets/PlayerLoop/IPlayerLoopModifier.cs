using UnityEngine;
using UnityEngine.LowLevel;

namespace Dhs5.Utility.PlayerLoops
{
    public interface IPlayerLoopModifier
    {
        /// <summary>
        /// Bigger numbers will be processed last<br></br>
        /// Might be important to ensure a precise index in sub systems list
        /// </summary>
        public int Priority { get; }

        PlayerLoopSystem ModifyPlayerLoop(PlayerLoopSystem playerLoopSystem);
    }
}
