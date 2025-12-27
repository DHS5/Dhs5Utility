using Dhs5.Utility.Databases;
using Dhs5.Utility.Updates;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestUpdateTimeline : MonoBehaviour
{
    [SerializeField] private UpdateTimelinePicker m_updateTimeline;

    private UpdateTimelineInstanceHandle m_timelineHandle;

    private void Start()
    {
        if (m_updateTimeline.TryGetUpdateTimeline(out var updateTimeline))
        {
            if (Updater.TryGetUpdateTimelineInstanceHandle(updateTimeline.UID, out m_timelineHandle))
            {
                Debug.Log("Timeline already active");
            }
            else if (Updater.CreateTimelineInstance(updateTimeline, out m_timelineHandle))
            {
                m_timelineHandle.Updated += OnUpdateTimeline;
                m_timelineHandle.CustomEventTriggered += OnCustomTimelineEvent;
                m_timelineHandle.Play();
            }
        }
    }

    private void OnUpdateTimeline(float deltaTime)
    {
        Debug.Log("deltaTime : " + deltaTime + " time : " + m_timelineHandle.Time + " normalizedTime : " + m_timelineHandle.NormalizedTime);
    }
    private void OnCustomTimelineEvent(ushort id)
    {
        Debug.Log("Timeline Event received at " + m_timelineHandle.Time + " (id = " + id + ")");

        if (id == 0)
        {
            Updater.KillTimelineInstance(m_timelineHandle);
        }
    }
}
