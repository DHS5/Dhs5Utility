using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{

    public enum UpdateCondition
    {
        ALWAYS = 0,
        GAME_PLAYING,
        GAME_PAUSED,
    }

    public static class UpdateConditionExtension
    {
        public static bool IsFullfilled(this UpdateCondition condition)
        {
            switch (condition)
            {
                case UpdateCondition.ALWAYS: return true;
                case UpdateCondition.GAME_PLAYING: return true;
                case UpdateCondition.GAME_PAUSED: return false;
                default: throw new NotImplementedException();
            }
        }
    }

}
