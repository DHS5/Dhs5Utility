using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public enum EUpdateCondition
    {
        [InspectorName("Always")]
        ALWAYS = 0,
        [InspectorName("Game Playing")]
        GAME_PLAYING,
        [InspectorName("Game Paused")]
        GAME_PAUSED,
        [InspectorName("Game Over")]
        GAME_OVER,
    }
}
