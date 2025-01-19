using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
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