using System;
using System.Collections;
using System.Collections.Generic;

namespace Dhs5.Utility.Updates
{
    internal class UpdateTimelineState
    {
        #region Constructor

        public UpdateTimelineState(UpdateTimelineDatabaseElement updateTimeline, int updateKey)
        {
            IsActive = false;
            Time = 0f;

            this.updateKey = updateKey;
            this.duration = updateTimeline.Duration;
            this.loop = updateTimeline.Loop;
            Timescale = updateTimeline.Timescale;

            this.eventQueue = new();
            foreach (var e in updateTimeline.GetSortedEvents())
            {
                this.eventQueue.Enqueue(e);
            }
        }

        #endregion

        #region Members

        public readonly int updateKey;
        public readonly float duration;
        public readonly bool loop;

        private Queue<UpdateTimelineDatabaseElement.Event> eventQueue;

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
        /// This is called AFTER the Custom Event callback
        /// </remarks>
        public event UpdateTimelineCallback Updated;
        /// <summary>
        /// Event triggered when this UpdateTimeline encounters an Event
        /// </summary>
        /// <remarks>
        /// For a Custom Event, this is called BEFORE the Update callback
        /// </remarks>
        public event UpdateTimelineEvent EventTriggered;

        #endregion


        #region Methods

        public void SetActive(bool active)
        {
            if (IsActive == active) return;

            IsActive = active;

            if (IsActive) 
                OnSetActive();
            else 
                OnSetInactive();
        }

        public void OnUpdate(float deltaTime)
        {
            if (IsActive)
            {
                deltaTime *= Timescale;
                Time += deltaTime; // Increments time

                CheckCustomEvents();

                // Arrive to the end
                if (Time >= duration)
                {
                    float surplus = Time - duration;
                    if (loop)
                    {
                        // End
                        Time = duration;
                        TriggerUpdate(deltaTime - surplus);
                        EventTriggered?.Invoke(EUpdateTimelineEventType.END, 0);

                        // Restart
                        Time = 0f;
                        EventTriggered?.Invoke(EUpdateTimelineEventType.START, 0);

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

        #endregion

        #region Callbacks

        private void OnSetActive()
        {
            if (Time == 0f)
            {
                EventTriggered?.Invoke(EUpdateTimelineEventType.START, 0);
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
                EventTriggered?.Invoke(EUpdateTimelineEventType.END, 0);
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
        }

        private void TriggerCustomEvent(ushort id)
        {
            EventTriggered?.Invoke(EUpdateTimelineEventType.CUSTOM, id);
        }

        #endregion
    }

    public struct UpdateTimelineHandle
    {
        #region Constructors

        internal UpdateTimelineHandle(UpdaterInstance updaterInstance, int updateTimelineUID)
        {
            this.updaterInstance = updaterInstance;
            this.updateTimelineUID = updateTimelineUID;
        }

        public static UpdateTimelineHandle Empty = new();

        #endregion

        #region Members

        private readonly UpdaterInstance updaterInstance;
        private readonly int updateTimelineUID;

        #endregion

        #region Accessor

        private readonly bool TryGetState(out UpdateTimelineState state)
        {
            if (updaterInstance != null) return updaterInstance.TryGetUpdateTimelineState(updateTimelineUID, out state);

            state = null;
            return false;
        }

        #endregion


        #region Properties

        /// <summary>
        /// Whether this handle is valid
        /// </summary>
        public readonly bool IsValid => updaterInstance != null && updaterInstance.IsUpdateTimelineRegistered(updateTimelineUID);

        /// <inheritdoc cref="UpdateTimelineState.IsActive"/>
        public readonly bool IsActive
        {
            get
            {
                if (TryGetState(out var state))
                {
                    return state.IsActive;
                }
                return false;
            }
        }
        /// <inheritdoc cref="UpdateTimelineState.Time"/>
        public readonly float Time
        {
            get
            {
                if (TryGetState(out var state))
                {
                    return state.Time;
                }
                return -1f;
            }
        }
        /// <inheritdoc cref="UpdateTimelineState.NormalizedTime"/>
        public readonly float NormalizedTime
        {
            get
            {
                if (TryGetState(out var state))
                {
                    return state.NormalizedTime;
                }
                return -1f;
            }
        }
        /// <inheritdoc cref="UpdateTimelineState.Timescale"/>
        public readonly float Timescale
        {
            get
            {
                if (TryGetState(out var state))
                {
                    return state.Timescale;
                }
                return -1f;
            }
            set
            {
                if (value >= 0f && TryGetState(out var state))
                {
                    state.Timescale = value;
                }
            }
        }

        #endregion

        #region Events

        /// <inheritdoc cref="UpdateTimelineState.Updated"/>
        public event UpdateTimelineCallback Updated 
        { 
            add { if (TryGetState(out var state)) state.Updated += value; } 
            remove { if (TryGetState(out var state)) state.Updated -= value; } 
        }
        /// <inheritdoc cref="UpdateTimelineState.EventTriggered"/>
        public event UpdateTimelineEvent EventTriggered
        {
            add { if (TryGetState(out var state)) state.EventTriggered += value; }
            remove { if (TryGetState(out var state)) state.EventTriggered -= value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts or Unpause the UpdateTimeline
        /// </summary>
        public readonly void Start()
        {
            if (TryGetState(out var state))
                state.SetActive(true);
        }
        /// <summary>
        /// Pause the UpdateTimeline
        /// </summary>
        public readonly void Stop()
        {
            if (TryGetState(out var state))
                state.SetActive(false);
        }

        /// <summary>
        /// Unregisters this UpdateTimeline from the Updater
        /// </summary>
        public readonly void Unregister()
        {
            if (updaterInstance != null)
            {
                updaterInstance.UnregisterUpdateTimeline(updateTimelineUID);
            }
        }

        #endregion
    }
}
