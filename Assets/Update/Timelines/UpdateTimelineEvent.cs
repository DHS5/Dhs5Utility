using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    /// <summary>
    /// Event triggered when an UpdateTimeline encounters an event
    /// </summary>
    public delegate void UpdateTimelineEvent();

    /// <summary>
    /// Event triggered when an UpdateTimeline encounters a custom event
    /// </summary>
    /// <param name="eventID">ID of the event set in the editor</param>
    public delegate void CustomUpdateTimelineEvent(ushort eventID);
}