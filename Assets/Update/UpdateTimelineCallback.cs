using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    /// <summary>
    /// Callback triggered when an UpdateTimeline is updated
    /// </summary>
    /// <param name="deltaTime">Delta time since last update</param>
    /// <param name="time">Current time relative to the timeline</param>
    /// <param name="normalizedTime">Normalized time relative to the timeline</param>
    public delegate void UpdateTimelineCallback(float deltaTime, float time, float normalizedTime);
}