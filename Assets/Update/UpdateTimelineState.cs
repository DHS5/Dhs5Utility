using System;

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
        }

        #endregion

        #region Members

        public readonly int updateKey;
        public readonly float duration;
        public readonly bool loop;

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

        #endregion

        #region Events

        public event UpdateTimelineCallback Updated;

        public event Action OnStart;
        public event Action OnEnd;

        public event Action Paused;
        public event Action Unpaused;

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
                        OnEnd?.Invoke();

                        // Restart
                        Time = 0f;
                        OnStart?.Invoke();

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

        #region Callbacks

        private void OnSetActive()
        {
            if (Time == 0f)
            {
                OnStart?.Invoke();
            }
            else
            {
                Unpaused?.Invoke();
            }
        }
        private void OnSetInactive()
        {
            if (Time == duration)
            {
                OnEnd?.Invoke();
            }
            else
            {
                Paused?.Invoke();
            }
        }

        #endregion

        #region Utility

        private void TriggerUpdate(float deltaTime)
        {
            Updated?.Invoke(deltaTime, Time, NormalizedTime);
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

        #endregion

        #region Events

        public event UpdateTimelineCallback Updated 
        { 
            add { if (TryGetState(out var state)) state.Updated += value; } 
            remove { if (TryGetState(out var state)) state.Updated -= value; } 
        }

        public event Action OnStart
        {
            add { if (TryGetState(out var state)) state.OnStart += value; }
            remove { if (TryGetState(out var state)) state.OnStart -= value; }
        }
        public event Action OnEnd
        {
            add { if (TryGetState(out var state)) state.OnEnd += value; }
            remove { if (TryGetState(out var state)) state.OnEnd -= value; }
        }

        public event Action Paused
        {
            add { if (TryGetState(out var state)) state.Paused += value; }
            remove { if (TryGetState(out var state)) state.Paused -= value; }
        }
        public event Action Unpaused
        {
            add { if (TryGetState(out var state)) state.Unpaused += value; }
            remove { if (TryGetState(out var state)) state.Unpaused -= value; }
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
