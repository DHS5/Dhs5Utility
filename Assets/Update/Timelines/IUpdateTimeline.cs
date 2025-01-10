using System;
using System.Collections.Generic;
using UnityEngine;

public interface IUpdateTimeline
{
    #region STRUCT Event

    [Serializable]
    public struct Event
    {
        public float normalizedTime;
        public ushort id;
    }

    #endregion

    public int UID { get; }
    public int UpdateKey { get; }
    public float Duration { get; }
    public bool Loop { get; }
    public float Timescale { get; }

    public IEnumerable<Event> GetSortedEvents();
}
