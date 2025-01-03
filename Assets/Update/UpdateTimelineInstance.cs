using System;
using System.Collections;
using System.Collections.Generic;

namespace Dhs5.Utility.Updates
{
    internal class UpdateTimelineInstance
    {
        #region Constructor

        public UpdateTimelineInstance(UpdateTimeline updateTimeline, int updateCategory)
        {
            IsActive = false;
            Time = 0f;

            this.timelineUID = updateTimeline.UID;
            this.updateCategory = updateCategory;
            this.duration = updateTimeline.Duration;
            this.loop = updateTimeline.Loop;
            Timescale = updateTimeline.Timescale;

            this.eventQueue = new();
            this.customEvents = new();
            foreach (var e in updateTimeline.GetSortedEvents())
            {
                this.customEvents.Add(e);
            }
        }

        #endregion

        #region Members

        public readonly int timelineUID;
        public readonly int updateCategory;
        public readonly float duration;
        public readonly bool loop;

        private readonly List<UpdateTimeline.Event> customEvents;
        private Queue<UpdateTimeline.Event> eventQueue;

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
        public float NormalizedTime => Time / duration;

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
        public event UpdateTimelineCallback Updated;
        /// <summary>
        /// Event triggered when this UpdateTimeline encounters an Event
        /// </summary>
        /// <remarks>
        /// For a Custom Event, this is called JUST AFTER the Update callback
        /// </remarks>
        public event UpdateTimelineEvent EventTriggered;

        #endregion


        #region Methods

        public void SetActive(bool active) => SetActive(active, false);
        private void SetActive(bool active, bool force)
        {
            if (IsActive == active && !force) return;

            IsActive = active;

            if (IsActive) 
                OnSetActive();
            else 
                OnSetInactive();
        }

        public void PlayAtTime(float time)
        {
            if (IsActive) return;

            Time = time;
            FillCustomEventsQueue(time);
            OnSetActive();
        }
        public void Complete(bool triggerCustomEvents)
        {
            if (Time == duration) return;

            Time = duration;

            if (triggerCustomEvents)
            {
                CheckCustomEvents(); // Will trigger all custom events left   
            }
            SetActive(false, true); // Will trigger end event
        }
        public void Restart(bool complete)
        {
            if (Time > 0f && complete)
            {
                Complete(true);
            }
            Time = 0f;
            SetActive(true, true);
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
                    if (loop)
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
                        SetActive(false);
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
            while (eventQueue.TryPeek(out var e) && e.time <= Time)
            {
                TriggerCustomEvent(eventQueue.Dequeue().id);
            }
        }
        private void FillCustomEventsQueue(float time)
        {
            eventQueue.Clear();
            if (customEvents.IsValid())
            {
                foreach (var e in customEvents)
                {
                    if (e.time >= time)
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

        private void OnSetActive()
        {
            if (Time == 0f)
            {
                OnStart();
            }
            else
            {
                EventTriggered?.Invoke(EUpdateTimelineEventType.UNPAUSE, 0);
            }
        }
        private void OnSetInactive()
        {
            if (Time == duration)
            {
                OnEnd();
            }
            else
            {
                EventTriggered?.Invoke(EUpdateTimelineEventType.PAUSE, 0);
            }
        }

        #endregion

        #region Utility

        private void TriggerUpdate(float deltaTime)
        {
            Updated?.Invoke(deltaTime, Time, NormalizedTime);
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

        internal UpdateTimelineInstanceHandle(UpdaterInstance updaterInstance, ulong key)
        {
            this.updater = updaterInstance;
            this.key = key;
        }

        public static UpdateTimelineInstanceHandle Empty = new();

        #endregion

        #region Members

        private readonly UpdaterInstance updater;
        public readonly ulong key;

        #endregion

        #region Accessor

        private readonly bool TryGetInstance(out UpdateTimelineInstance instance)
        {
            if (updater != null) return updater.TryGetUpdateTimelineInstance(key, out instance);

            instance = null;
            return false;
        }

        #endregion


        #region Properties

        /// <summary>
        /// Whether this handle is valid
        /// </summary>
        public readonly bool IsValid => updater != null && updater.TimelineInstanceExist(key);

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
        public event UpdateTimelineCallback Updated 
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
                instance.SetActive(true);
        }
        /// <summary>
        /// Starts or Unpause the UpdateTimeline at <paramref name="time"/>
        /// </summary>
        public readonly void Play(float time)
        {
            if (TryGetInstance(out var instance))
                instance.PlayAtTime(time);
        }
        /// <summary>
        /// Pause the UpdateTimeline
        /// </summary>
        public readonly void Stop()
        {
            if (TryGetInstance(out var instance))
                instance.SetActive(false);
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

        #endregion
    }
}
