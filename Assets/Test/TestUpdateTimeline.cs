using Dhs5.Utility.Databases;
using Dhs5.Utility.Updates;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestUpdateTimeline : MonoBehaviour
{
    [SerializeField] private UpdateTimelinePicker m_updateTimeline;

    private UpdateTimelineHandle m_timelineHandle;

    private void Start()
    {
        if (m_updateTimeline.TryGetUpdateTimelineHandle(out m_timelineHandle))
        {
            m_timelineHandle.Updated += OnUpdateTimeline;
            m_timelineHandle.EventTriggered += OnTimelineEvent;
            m_timelineHandle.Start();
        }
    }

    private void OnUpdateTimeline(float deltaTime, float time, float normalizedTime)
    {
        Debug.Log("deltaTime : " + deltaTime + " time : " + time + " normalizedTime : " + normalizedTime);
    }
    private void OnTimelineEvent(EUpdateTimelineEventType type, ushort id)
    {
        Debug.Log("Timeline Event received at " + m_timelineHandle.Time + " : " + type + " (id = " + id + ")");

        if (type == EUpdateTimelineEventType.CUSTOM && id == 1 && m_timelineHandle.Time > 15f)
        {
            m_timelineHandle.Stop();
        }
        else if (type == EUpdateTimelineEventType.PAUSE)
        {
            m_timelineHandle.Timescale = 2f;
            m_timelineHandle.Start();
        }
    }
}
