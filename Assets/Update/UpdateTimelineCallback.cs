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

    /// <summary>
    /// Event triggered when an UpdateTimeline encounters an event
    /// </summary>
    /// <param name="type">Type of the event</param>
    /// <param name="eventID">ID of the event set in the editor</param>
    public delegate void UpdateTimelineEvent(EUpdateTimelineEventType type, ushort eventID);

    public enum EUpdateTimelineEventType
    {
        START = 0,
        END = 1,
        PAUSE = 2,
        UNPAUSE = 3,
        CUSTOM = 4,
    }
}