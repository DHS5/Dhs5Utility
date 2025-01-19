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

        [Tooltip("Custom conditions should be overriden in a custom updater")]
        CUSTOM1,

        [Tooltip("Custom conditions should be overriden in a custom updater")]
        CUSTOM2,

        [Tooltip("Custom conditions should be overriden in a custom updater")]
        CUSTOM3,

        [Tooltip("Custom conditions should be overriden in a custom updater")]
        CUSTOM4,

        [Tooltip("Custom conditions should be overriden in a custom updater")]
        CUSTOM5,

        [Tooltip("Custom conditions should be overriden in a custom updater")]
        CUSTOM6,

        [Tooltip("Custom conditions should be overriden in a custom updater")]
        CUSTOM7,

        [Tooltip("Custom conditions should be overriden in a custom updater")]
        CUSTOM8,

        [Tooltip("Custom conditions should be overriden in a custom updater")]
        CUSTOM9,

        [Tooltip("Custom conditions should be overriden in a custom updater")]
        CUSTOM10,
    }
}
