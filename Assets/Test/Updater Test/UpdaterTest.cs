using Dhs5.Utility.Updates;
using UnityEngine;

public class UpdaterTest : MonoBehaviour
{
    private bool m_startedDelayedCall;
    private UpdateTimelineInstanceHandle m_timelineHandle;
    private bool m_timelinePaused;
    private bool m_timelineExceptioned;

    private void OnEnable()
    {
        Updater.RegisterChannelCallback(true, EUpdateChannel.BASE, OnUpdate1);
        Updater.RegisterChannelCallback(true, EUpdateChannel.BASE, OnUpdate2);
        Updater.RegisterChannelCallback(true, EUpdateChannel.SCREEN_LOG, OnUpdate3);
    }
    private void OnDisable()
    {
        Updater.RegisterChannelCallback(false, EUpdateChannel.BASE, OnUpdate1);
        Updater.RegisterChannelCallback(false, EUpdateChannel.BASE, OnUpdate2);
        Updater.RegisterChannelCallback(false, EUpdateChannel.SCREEN_LOG, OnUpdate3);
    }

    private void OnUpdate1(float deltaTime)
    {
        Debug.Log("update 1 " + deltaTime);

        if (Time.time > 5f) throw new System.Exception("Test exception");
    }
    private void OnUpdate2(float deltaTime)
    {
        Debug.Log("update 2 " + deltaTime);

        if (Time.time > 8f && !m_startedDelayedCall)
        {
            m_startedDelayedCall = true;
            Time.timeScale = 2f;
            Debug.Log("init delayed calls");
            Updater.CallInXSeconds(2f, OnDelayedCall, out _);
            Updater.CallInXRealtimeSeconds(2f, OnRealtimeDelayedCall, out _);
            Updater.CreateTimelineInstance(EUpdateChannel.BASE, 5f, out m_timelineHandle);
            m_timelineHandle.Updated += OnTimelineUpdated;
            m_timelineHandle.Started += OnTimelineStarted;
            m_timelineHandle.Ended += OnTimelineEnded;
            m_timelineHandle.Paused += OnTimelinePaused;
            m_timelineHandle.Resumed += OnTimelineResumed;
            m_timelineHandle.Play();
        }

        if (m_timelineHandle.IsPaused)
        {
            m_timelineHandle.Resume();
        }
    }
    private void OnUpdate3(float deltaTime)
    {
        Debug.Log("update 3 " + deltaTime);

        if (m_timelineHandle.IsPlaying && !m_timelinePaused)
        {
            Debug.Log("try pause timeline");
            m_timelinePaused = true;
            m_timelineHandle.Pause();
        }
    }


    private void OnDelayedCall()
    {
        Time.timeScale = 1f;
        Debug.Log("CALLED");
        throw new System.Exception("Delayed call exception");
    }
    private void OnRealtimeDelayedCall()
    {
        Debug.Log("REALTIME CALLED");
    }

    private void OnTimelineStarted()
    {
        Debug.Log("timeline started");
    }
    private void OnTimelineEnded()
    {
        Debug.Log("timeline ended");
    }
    private void OnTimelinePaused()
    {
        Debug.Log("timeline paused");
        throw new System.Exception("Timeline pause exception");
    }
    private void OnTimelineResumed()
    {
        Debug.Log("timeline resumed");
    }
    private void OnTimelineUpdated(float deltaTime)
    {
        Debug.Log("timeline updated");
        if (m_timelineHandle.NormalizedTime > 0.5f && !m_timelineExceptioned)
        {
            m_timelineExceptioned = true;
            throw new System.Exception("Timeline exception");
        }
    }
}
