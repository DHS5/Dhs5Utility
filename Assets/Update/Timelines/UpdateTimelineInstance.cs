using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public class UpdateTimelineInstance
    {
        #region Constructor

        public UpdateTimelineInstance(IUpdateTimeline updateTimeline)
        {
            IsActive = false;
            Time = 0f;

            this.timelineUID = updateTimeline.UID;
            this.updateChannel = updateTimeline.UpdateChannel;
            this.duration = updateTimeline.Duration;
            Loop = updateTimeline.Loop;
            Timescale = updateTimeline.Timescale;

            this.eventQueue = new();
            this.customEvents = new();
            var sortedEvents = updateTimeline.GetSortedEvents();
            if (sortedEvents != null)
            {
                foreach (var e in sortedEvents)
                {
                    this.customEvents.Add(e);
                }
            }
        }

        #endregion

        #region Members

        public readonly int timelineUID;
        public readonly EUpdateChannel updateChannel;
        public readonly float duration;

        private readonly List<IUpdateTimeline.Event> customEvents;
        private Queue<IUpdateTimeline.Event> eventQueue;

        #endregion

        #region Properties

        /// <summary>
        /// Is this UpdateTimeline currently active
        /// </summary>
        public bool IsActive { get; private set; }
        /// <summary>
        /// Current time relative to this UpdateTimeline
        /// </summary>
        public float Time { get; private set; }
        /// <summary>
        /// Current normalized time relative to this UpdateTimeline
        /// </summary>
        public float NormalizedTime
        {
            get => Time / duration;
            set
            {
                Time = value * duration;
            }
        }

        /// <summary>
        /// Whether this UpdateTimeline loops
        /// </summary>
        public bool Loop { get; set; }
        /// <summary>
        /// Timescale of this UpdateTimeline
        /// </summary>
        public float Timescale { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Callback triggered when this UpdateTimeline is updated
        /// </summary>
        /// <remarks>
        /// This is called BEFORE the Custom Event callback
        /// </remarks>
        public event UpdateCallback Updated;
        /// <summary>
        /// Event triggered when this UpdateTimeline encounters an Event
        /// </summary>
        /// <remarks>
        /// For a Custom Event, this is called JUST AFTER the Update callback
        /// </remarks>
        public event UpdateTimelineEvent EventTriggered;

        #endregion


        #region Methods

        private void SetActive(bool active, bool force, bool triggerPauseEvents)
        {
            if (IsActive == active && !force) return;

            IsActive = active;

            if (IsActive) 
                OnSetActive(triggerPauseEvents);
            else 
                OnSetInactive(triggerPauseEvents);
        }

        public void Play()
        {
            SetActive(true, false, true);
        }
        public void Pause()
        {
            SetActive(true, false, true);
        }

        public void SetTime(float time, bool triggerCustomEvents)
        {
            Time = Mathf.Clamp(time, 0f, duration);
            if (triggerCustomEvents)
            {
                CheckCustomEvents(); // Will trigger all custom events left   
            }
            else
            {
                FillCustomEventsQueue(NormalizedTime);
            }
        }
        public void SetNormalizedTime(float normalizedTime, bool triggerCustomEvents)
        {
            NormalizedTime = Mathf.Clamp01(normalizedTime);
            if (triggerCustomEvents)
            {
                CheckCustomEvents(); // Will trigger all custom events left   
            }
            else
            {
                FillCustomEventsQueue(NormalizedTime);
            }
        }

        public void Complete(bool triggerCustomEvents)
        {
            if (Time == duration) return;

            Time = duration;

            if (triggerCustomEvents)
            {
                CheckCustomEvents(); // Will trigger all custom events left   
            }
            SetActive(false, true, false); // Will trigger end event
        }
        public void Restart(bool complete)
        {
            if (complete && Time > 0f)
            {
                Complete(true);
            }
            Time = 0f;
            SetActive(true, true, false);
        }
        public void Reset()
        {
            Time = 0f;
            SetActive(false, false, false);
        }

        #endregion

        #region Update Method

        public void OnUpdate(float deltaTime)
        {
            if (IsActive)
            {
                deltaTime *= Timescale;
                Time += deltaTime; // Increments time

                // Arrive to the end
                if (Time >= duration)
                {
                    float surplus = Time - duration;
                    if (Loop)
                    {
                        // End
                        Time = duration;
                        TriggerUpdate(deltaTime - surplus);
                        OnEnd();

                        // Restart
                        Time = 0f;
                        OnStart();

                        // Surplus update
                        if (surplus > 0f)
                        {
                            Time = surplus;
                            TriggerUpdate(surplus);
                        }
                    }
                    else
                    {
                        Time = duration;
                        TriggerUpdate(deltaTime - surplus);
                        SetActive(false, false, false);
                    }
                }
                else
                {
                    TriggerUpdate(deltaTime);
                }
            }
        }

        #endregion

        #region Custom Events

        private void CheckCustomEvents()
        {
            while (eventQueue.TryPeek(out var e) && e.normalizedTime <= NormalizedTime)
            {
                TriggerCustomEvent(eventQueue.Dequeue().id);
            }
        }
        private void FillCustomEventsQueue(float normalizedTime)
        {
            eventQueue.Clear();
            if (customEvents.IsValid())
            {
                foreach (var e in customEvents)
                {
                    if (e.normalizedTime >= normalizedTime)
                    {
                        eventQueue.Enqueue(e);
                    }
                }
            }
        }

        #endregion

        #region Callbacks

        private void OnStart()
        {
            FillCustomEventsQueue(0f);
            EventTriggered?.Invoke(EUpdateTimelineEventType.START, 0);
        }
        private void OnEnd()
        {
            EventTriggered?.Invoke(EUpdateTimelineEventType.END, 0);
        }

        private void OnSetActive(bool triggerPauseEvents)
        {
            // If complete, restart
            if (Mathf.Approximately(Time, duration))
            {
                Time = 0f;
            }

            if (Time == 0f)
            {
                OnStart();
            }
            else if (triggerPauseEvents)
            {
                EventTriggered?.Invoke(EUpdateTimelineEventType.UNPAUSE, 0);
            }
        }
        private void OnSetInactive(bool triggerPauseEvents)
        {
            if (Mathf.Approximately(Time, duration))
            {
                OnEnd();
            }
            else if (triggerPauseEvents)
            {
                EventTriggered?.Invoke(EUpdateTimelineEventType.PAUSE, 0);
            }
        }

        #endregion

        #region Utility

        private void TriggerUpdate(float deltaTime)
        {
            Updated?.Invoke(deltaTime);
            CheckCustomEvents();
        }

        private void TriggerCustomEvent(ushort id)
        {
            EventTriggered?.Invoke(EUpdateTimelineEventType.CUSTOM, id);
        }

        #endregion
    }

    public struct UpdateTimelineInstanceHandle
    {
        #region Constructors

        public UpdateTimelineInstanceHandle(ulong key)
        {
            this.key = key;
        }

        public static UpdateTimelineInstanceHandle Empty = new();

        #endregion

        #region Members

        public readonly ulong key;

        #endregion

        #region Accessor

        private readonly bool TryGetInstance(out UpdateTimelineInstance instance)
        {
            return Updater.Instance.TryGetUpdateTimelineInstance(key, out instance);
        }

        #endregion


        #region Properties

        /// <summary>
        /// Whether this handle is valid
        /// </summary>
        public readonly bool IsValid => Updater.Instance.TimelineInstanceExist(key);

        /// <inheritdoc cref="UpdateTimelineInstance.IsActive"/>
        public readonly bool IsActive
        {
            get
            {
                if (TryGetInstance(out var instance))
                {
                    return instance.IsActive;
                }
                return false;
            }
        }
        /// <inheritdoc cref="UpdateTimelineInstance.Time"/>
        public readonly float Time
        {
            get
            {
                if (TryGetInstance(out var instance))
                {
                    return instance.Time;
                }
                return -1f;
            }
        }
        /// <inheritdoc cref="UpdateTimelineInstance.NormalizedTime"/>
        public readonly float NormalizedTime
        {
            get
            {
                if (TryGetInstance(out var instance))
                {
                    return instance.NormalizedTime;
                }
                return -1f;
            }
        }
        /// <inheritdoc cref="UpdateTimelineInstance.duration"/>
        public readonly float Duration
        {
            get
            {
                if (TryGetInstance(out var instance))
                {
                    return instance.duration;
                }
                return 0f;
            }
        }
        /// <inheritdoc cref="UpdateTimelineInstance.Loop"/>
        public readonly bool Loop
        {
            get
            {
                if (TryGetInstance(out var instance))
                {
                    return instance.Loop;
                }
                return false;
            }
            set
            {
                if (TryGetInstance(out var instance))
                {
                    instance.Loop = value;
                }
            }
        }
        /// <inheritdoc cref="UpdateTimelineInstance.Timescale"/>
        public readonly float Timescale
        {
            get
            {
                if (TryGetInstance(out var instance))
                {
                    return instance.Timescale;
                }
                return -1f;
            }
            set
            {
                if (value >= 0f && TryGetInstance(out var instance))
                {
                    instance.Timescale = value;
                }
            }
        }

        #endregion

        #region Events

        /// <inheritdoc cref="UpdateTimelineInstance.Updated"/>
        public event UpdateCallback Updated
        { 
            add { if (TryGetInstance(out var instance)) instance.Updated += value; } 
            remove { if (TryGetInstance(out var instance)) instance.Updated -= value; } 
        }
        /// <inheritdoc cref="UpdateTimelineInstance.EventTriggered"/>
        public event UpdateTimelineEvent EventTriggered
        {
            add { if (TryGetInstance(out var instance)) instance.EventTriggered += value; }
            remove { if (TryGetInstance(out var instance)) instance.EventTriggered -= value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts or Unpause the UpdateTimeline
        /// </summary>
        public readonly void Play()
        {
            if (TryGetInstance(out var instance))
                instance.Pause();
        }
        /// <summary>
        /// Pause the UpdateTimeline
        /// </summary>
        public readonly void Pause()
        {
            if (TryGetInstance(out var instance))
                instance.Pause();
        }
        /// <summary>
        /// Sets the Time to <paramref name="time"/>
        /// </summary>
        /// <remarks>
        /// It won't change the playing state
        /// </remarks>
        /// <param name="triggerCustomEvents">Whether to trigger the custom events in between current time and <paramref name="time"/></param>
        public readonly void SetTime(float time, bool triggerCustomEvents = false)
        {
            if (TryGetInstance(out var instance))
                instance.SetTime(time, triggerCustomEvents);
        }
        /// <summary>
        /// Sets the NormalizedTime to <paramref name="normalizedTime"/>
        /// </summary>
        /// <remarks>
        /// It won't change the playing state
        /// </remarks>
        /// <param name="triggerCustomEvents">Whether to trigger the custom events in between current time and <paramref name="normalizedTime"/></param>
        public readonly void SetNormalizedTime(float normalizedTime, bool triggerCustomEvents = false)
        {
            if (TryGetInstance(out var instance))
                instance.SetNormalizedTime(normalizedTime, triggerCustomEvents);
        }

        /// <summary>
        /// Complete this UpdateTimeline
        /// </summary>
        /// <remarks>
        /// The END event is always triggered on complete
        /// </remarks>
        /// <param name="triggerCustomEvents">Whether to trigger the remaining custom events</param>
        public readonly void Complete(bool triggerCustomEvents = false)
        {
            if (TryGetInstance(out var instance))
            {
                instance.Complete(triggerCustomEvents);
            }
        }
        /// <summary>
        /// Restart this UpdateTimeline
        /// </summary>
        /// <param name="complete">Whether to complete the UpdateTimeline first, thus triggering the remaining custom events and the END event</param>
        public readonly void Restart(bool complete = false)
        {
            if (TryGetInstance(out var instance))
            {
                instance.Restart(complete);
            }
        }
        /// <summary>
        /// Resets this UpdateTimeline (set <see cref="Time"/> to 0f & stop)
        /// </summary>
        public readonly void Reset()
        {
            if (TryGetInstance(out var instance))
            {
                instance.Reset();
            }
        }
        /// <summary>
        /// Kills the corresponding UpdateTimelineInstance if it exists
        /// </summary>
        public readonly void Kill()
        {
            if (key > 0) Updater.KillTimelineInstance(this);
        }

        #endregion
    }
}
