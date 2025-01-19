using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public enum EUpdatePass
    {
        [Tooltip("Called on the Update() method")]
        CLASSIC = 0,

        [Tooltip("Called on the Update() method just before CLASSIC")]
        EARLY,

        [Tooltip("Called on the LateUpdate() method")]
        LATE,

        [Tooltip("Called on the FixedUpdate() method, usually used for Physics")]
        FIXED,

        [Tooltip("Custom passes should be called at your convenience in a custom updater")]
        CUSTOM1,
        
        [Tooltip("Custom passes should be called at your convenience in a custom updater")]
        CUSTOM2,

        [Tooltip("Custom passes should be called at your convenience in a custom updater")]
        CUSTOM3,

        [Tooltip("Custom passes should be called at your convenience in a custom updater")]
        CUSTOM4,

        [Tooltip("Custom passes should be called at your convenience in a custom updater")]
        CUSTOM5,

        [Tooltip("Custom passes should be called at your convenience in a custom updater")]
        CUSTOM6,

        [Tooltip("Custom passes should be called at your convenience in a custom updater")]
        CUSTOM7,

        [Tooltip("Custom passes should be called at your convenience in a custom updater")]
        CUSTOM8,

        [Tooltip("Custom passes should be called at your convenience in a custom updater")]
        CUSTOM9,

        [Tooltip("Custom passes should be called at your convenience in a custom updater")]
        CUSTOM10,
    }

}
