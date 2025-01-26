using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public readonly struct ScriptedUpdateTimeline : IUpdateTimeline
    {
        #region Constructors

        public ScriptedUpdateTimeline(EUpdateChannel updateChannel, float duration, bool loop = false, float timescale = 1f, List<IUpdateTimeline.Event> events = null, int uid = 0)
        {
            m_uid = uid;
            m_updateChannel = updateChannel;
            m_duration = duration;
            m_loop = loop;
            m_timescale = timescale;
            m_events = events;
        }

        #endregion

        #region Members

        private readonly int m_uid;
        private readonly EUpdateChannel m_updateChannel;
        private readonly float m_duration;
        private readonly bool m_loop;
        private readonly float m_timescale;
        private readonly List<IUpdateTimeline.Event> m_events;

        #endregion

        #region IUpdateTimeline

        public int UID => m_uid;

        public EUpdateChannel UpdateChannel => m_updateChannel;

        public float Duration => m_duration;

        public bool Loop => m_loop;

        public float Timescale => m_timescale;

        public IEnumerable<IUpdateTimeline.Event> GetSortedEvents()
        {
            if (m_events != null)
            {
                List<IUpdateTimeline.Event> sortedEvents = new(m_events);
                sortedEvents.Sort((e1, e2) => e1.normalizedTime.CompareTo(e2.normalizedTime));
                return sortedEvents;
            }
            return null;
        }

        #endregion
    }
}
