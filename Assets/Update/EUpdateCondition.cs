using System;

namespace Dhs5.Utility.Updates
{
    public enum EUpdateCondition : Byte
    {
        ALWAYS = 0,
        GAME_PLAYING = 1,
        GAME_PAUSED = 2,
    }
}
