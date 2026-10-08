using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    [Serializable]
    public struct ScriptedUpdateTimeline : IUpdateTimeline
    {
        #region Constructors

        public ScriptedUpdateTimeline(EUpdateChannel updateChannel, float duration, bool loop = false, float timescale = 1f, List<IUpdateTimeline.Event> events = null)
        {
            m_updateChannel = updateChannel;
            m_duration = duration;
            m_loop = loop;
            m_timescale = timescale;
            m_events = events;
        }

        #endregion

        #region Members

        [SerializeField] private EUpdateChannel m_updateChannel;
        [SerializeField] private float m_duration;
        [SerializeField] private bool m_loop;
        [SerializeField] private float m_timescale;
        [SerializeField] private List<IUpdateTimeline.Event> m_events;

        #endregion

        #region IUpdateTimeline

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
