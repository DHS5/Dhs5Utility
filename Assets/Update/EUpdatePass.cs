using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public enum EUpdatePass
    {
        [InspectorName("After Early Update"), Tooltip("Updated after the Early Update of the PlayerLoop")]
        AFTER_EARLY_UPDATE,
        
        [InspectorName("Classic Update"), Tooltip("Updated just before the Classic Update of the PlayerLoop")]
        CLASSIC_UPDATE,
        
        [InspectorName("After Update"), Tooltip("Updated after the Classic Update of the PlayerLoop")]
        AFTER_UPDATE,
        
        [InspectorName("After Late Update"), Tooltip("Updated after the Late Update of the PlayerLoop")]
        AFTER_LATE_UPDATE,
        
        [InspectorName("Before Fixed Update"), Tooltip("Updated inside the Fixed Update of the PlayerLoop, before the FixedUpdate method")]
        BEFORE_FIXED_UPDATE,
        
        [InspectorName("After Physics Fixed Update"), Tooltip("Updated inside the Fixed Update of the PlayerLoop, after the Physics FixedUpdate")]
        AFTER_PHYSICS_FIXED_UPDATE,
    }

}
