using Dhs5.Utility.Databases;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public class Updater : MonoBehaviour
    {
        #region Instance

        protected internal static Updater Instance { get; private set; }
        internal void SetAsInstance()
        {
            Instance = this;
        }

        #endregion

        #region Properties

        // TIME
        public float Time { get; private set; }
        public float DeltaTime { get; private set; }
        // REALTIME
        public float RealTime { get; private set; }
        public float RealDeltaTime { get; private set; }
        // FRAME
        public int Frame { get; private set; }

        // GAME STATE
        public bool TimePaused { get; private set; }

        #endregion

        #region Events

        public event UpdateCallback OnEarlyUpdate;
        public event UpdateCallback OnUpdate;
        public event UpdateCallback OnLateUpdate;

        public event UpdateCallback OnFixedUpdate;

        #endregion

        #region Core Behaviour

        protected virtual void Awake()
        {
            InitChannels();
        }
        protected virtual void OnEnable()
        {
            
        }
        protected virtual void OnDisable()
        {

        }

        #endregion


        #region Time Management

        protected virtual void ComputeTimeState()
        {
            Time = UnityEngine.Time.time;
            DeltaTime = UnityEngine.Time.deltaTime;

            RealTime = UnityEngine.Time.realtimeSinceStartup;
            RealDeltaTime = UnityEngine.Time.unscaledDeltaTime;

            Frame = UnityEngine.Time.frameCount;

            TimePaused = DeltaTime != 0f;
        }

        #endregion

        #region Update Methods

        // UNITY
        protected virtual void Update()
        {
            ResetCurrentFramePasses();
            ComputeTimeState();

            EarlyUpdate();
            ClassicUpdate();
        }
        protected virtual void LateUpdate()
        {
            InvokePassEvents(EUpdatePass.LATE, DeltaTime, RealDeltaTime);
        }
        protected virtual void FixedUpdate()
        {
            InvokePassEvents(EUpdatePass.FIXED, UnityEngine.Time.fixedDeltaTime, UnityEngine.Time.fixedUnscaledDeltaTime);
        }

        // CUSTOM
        protected virtual void EarlyUpdate()
        {
            InvokePassEvents(EUpdatePass.EARLY, DeltaTime, RealDeltaTime);
        }
        protected virtual void ClassicUpdate()
        {
            InvokePassEvents(EUpdatePass.CLASSIC, DeltaTime, RealDeltaTime);
        }

        #endregion

        #region Pass Management

        private List<EUpdatePass> m_currentFramePasses = new();

        protected void InvokePassEvents(EUpdatePass pass, float deltaTime, float realDeltaTime)
        {
            UpdateValidChannels(pass, deltaTime, realDeltaTime);
            UpdateDelayedCalls(pass, deltaTime);
            InvokeDefaultEvents(pass, deltaTime);

            m_currentFramePasses.Add(pass);
        }

        private void ResetCurrentFramePasses()
        {
            m_currentFramePasses.Clear();
            m_delayedCallPasses.Clear();
        }
        protected bool PassHasBeenTriggeredThisFrame(EUpdatePass pass)
        {
            return m_currentFramePasses.Contains(pass);
        }

        #endregion


        #region Channels

        #region CLASS UpdateChannel

        protected class UpdateChannel
        {
            #region Constructors

            public UpdateChannel(int index, bool enabled, EUpdatePass pass, ushort order, EUpdateCondition condition, float frequency, float timescale, bool realtime)
            {
                this.index = index;
                this.pass = pass;
                this.order = order;
                this.condition = condition;
                this.customFrequency = frequency > 0f;
                this.frequency = frequency;
                this.realtime = realtime;

                Enabled = enabled;
                Timescale = timescale;

                TimeSinceLastUpdate = 0f;
            }

            #endregion

            #region Members

            public readonly int index;
            public readonly EUpdatePass pass;
            public readonly ushort order;
            public readonly EUpdateCondition condition;
            public readonly bool customFrequency;
            public readonly float frequency;
            public readonly bool realtime;

            public bool Enabled { get; set; }
            public float Timescale { get; set; }

            #endregion

            #region Properties

            public float TimeSinceLastUpdate { get; private set; }

            #endregion

            #region Behaviour

            public bool Update(float deltaTime, out float actualDeltaTime)
            {
                if (!customFrequency)
                {
                    actualDeltaTime = deltaTime;
                    return true;
                }
                else
                {
                    TimeSinceLastUpdate += deltaTime * Timescale;
                    actualDeltaTime = TimeSinceLastUpdate;
                    if (TimeSinceLastUpdate >= frequency)
                    {
                        TimeSinceLastUpdate -= frequency;
                        return true;
                    }
                }
                return false;
            }

            #endregion
        }

        #endregion

        #region Creation & Deletion

        private readonly Dictionary<EUpdatePass, List<UpdateChannel>> m_channels = new();

        private UpdateChannel CreateChannel(UpdaterDatabaseElement elem)
        {
            return new UpdateChannel(elem.EnumIndex, elem.EnabledByDefault, elem.Pass, elem.Order, elem.Condition, elem.Frequency, elem.TimeScale, elem.Realtime);
        }
        protected void InitChannels()
        {
            // Fill channels dico from database elements
            foreach (var updaterElement in Database.Enumerate<UpdaterDatabase, UpdaterDatabaseElement>())
            {
                var channel = CreateChannel(updaterElement);
                if (m_channels.TryGetValue(channel.pass, out var list))
                {
                    list.Add(channel);

                    // Sort list by order
                    list.Sort((c1, c2) => c1.order.CompareTo(c2.order));
                }
                else
                {
                    m_channels[channel.pass] = new() { channel };
                }
            }
        }

        protected void ClearChannels()
        {
            m_channels.Clear();
            m_channelCallbacks.Clear();
        }

        #endregion

        #region Accessors

        protected bool GetChannelByIndex(int index, out UpdateChannel channel)
        {
            foreach (var (pass, list) in m_channels)
            {
                foreach (var chan in list)
                {
                    if (chan.index == index)
                    {
                        channel = chan;
                        return true;
                    }
                }
            }
            channel = null;
            return false;
        }

        #endregion

        #region Callbacks

        private readonly Dictionary<int, UpdateCallback> m_channelCallbacks = new();

        public void RegisterChannelCallback(int channelIndex, UpdateCallback callback)
        {
            if (m_channelCallbacks.ContainsKey(channelIndex))
            {
                m_channelCallbacks[channelIndex] += callback;
            }
            else
            {
                m_channelCallbacks.Add(channelIndex, callback);
            }
        }
        public void UnregisterChannelCallback(int channelIndex, UpdateCallback callback)
        {
            if (m_channelCallbacks.ContainsKey(channelIndex))
            {
                m_channelCallbacks[channelIndex] -= callback;
            }
        }

        protected void TriggerChannelCallback(int channelIndex, float deltaTime)
        {
            if (m_channelCallbacks.TryGetValue(channelIndex, out var callback))
            {
                callback?.Invoke(deltaTime);
            }
        }

        #endregion

        #region Updates

        protected void UpdateValidChannels(EUpdatePass pass, float deltaTime, float realDeltaTime)
        {
            if (m_channels.TryGetValue(pass, out var channels))
            {
                foreach (var channel in channels)
                {
                    if (IsChannelValid(channel)
                        && channel.Update(channel.realtime ? realDeltaTime : deltaTime, out var actualDeltaTime))
                    {
                        TriggerChannelCallback(channel.index, actualDeltaTime);
                    }
                }
            }
        }

        #endregion

        #region Setters

        public void SetChannelEnable(int channelIndex, bool enabled)
        {
            if (GetChannelByIndex(channelIndex, out var channel))
            {
                channel.Enabled = enabled;
            }
        }
        public void SetChannelTimescale(int channelIndex, float timescale)
        {
            if (GetChannelByIndex(channelIndex, out var channel))
            {
                channel.Timescale = timescale;
            }
        }

        #endregion

        #region Validity

        protected virtual bool IsChannelValid(UpdateChannel channel)
        {
            return channel.Enabled && IsConditionFulfilled(channel.condition);
        }

        protected virtual bool IsConditionFulfilled(EUpdateCondition condition)
        {
            switch (condition)
            {
                case EUpdateCondition.ALWAYS: return true;
                case EUpdateCondition.GAME_PLAYING: return UnityEngine.Time.timeScale > 0f;
                case EUpdateCondition.GAME_PAUSED: return UnityEngine.Time.timeScale == 0f;
                case EUpdateCondition.GAME_OVER: return false;
            }
            return false;
        }

        #endregion

        #endregion

        #region Timelines

        #region Creation & Deletion

        private readonly Dictionary<ulong, UpdateTimelineInstance> m_updateTimelineInstances = new();

        public bool CreateUpdateTimelineInstance(IUpdateTimeline updateTimeline, ulong key, out UpdateTimelineInstanceHandle handle)
        {
            if (updateTimeline == null || m_updateTimelineInstances.ContainsKey(key))
            {
                handle = UpdateTimelineInstanceHandle.Empty;
                return false;
            }

            if (updateTimeline.UpdateKey > -1 && updateTimeline.Duration > 0f)
            {
                var state = new UpdateTimelineInstance(updateTimeline);
                m_updateTimelineInstances[key] = state;
                RegisterChannelCallback(updateTimeline.UpdateKey, state.OnUpdate);
                handle = new UpdateTimelineInstanceHandle(key);
                return true;
            }
            else
            {
                Debug.LogError("You tried to register an UpdateTimeline that has no valid update or a duration equal to 0");
                handle = UpdateTimelineInstanceHandle.Empty;
                return false;
            }
        }
        public void DestroyUpdateTimelineInstance(ulong key)
        {
            if (m_updateTimelineInstances.TryGetValue(key, out UpdateTimelineInstance state))
            {
                UnregisterChannelCallback(state.updateCategory, state.OnUpdate);
                m_updateTimelineInstances.Remove(key);
            }
        }

        private void ClearUpdateTimelineInstances()
        {
            foreach (var key in m_updateTimelineInstances.Keys)
            {
                DestroyUpdateTimelineInstance(key);
            }
            m_updateTimelineInstances.Clear();
        }

        #endregion

        #region Acessors

        public bool TimelineInstanceExist(ulong key) => m_updateTimelineInstances.ContainsKey(key);
        public bool TryGetUpdateTimelineInstance(ulong key, out UpdateTimelineInstance state) => m_updateTimelineInstances.TryGetValue(key, out state);
        
        public bool TryGetUpdateTimelineInstanceHandle(int timelineUID, out UpdateTimelineInstanceHandle handle)
        {
            foreach (var (key, instance) in m_updateTimelineInstances)
            {
                if (instance.timelineUID == timelineUID)
                {
                    handle = new(key);
                    return true;
                }
            }

            handle = UpdateTimelineInstanceHandle.Empty;
            return false;
        }

        #endregion

        #endregion

        #region Delayed Calls

        #region CLASSES

        private abstract class DelayedCall
        {
            #region Members

            public readonly EUpdatePass pass;
            public readonly EUpdateCondition condition;
            protected Action m_callback;

            #endregion

            #region Constructor

            public DelayedCall(EUpdatePass pass, EUpdateCondition condition, Action callback)
            {
                this.pass = pass;
                this.condition = condition;
                m_callback = callback;
            }

            #endregion

            #region Update

            public abstract bool Update(float deltaTime);

            #endregion
        }
        private class TimedDelayedCall : DelayedCall
        {
            #region Members

            private float m_remainingTime;

            #endregion

            #region Constructor

            public TimedDelayedCall(float delay, EUpdatePass pass, EUpdateCondition condition, Action callback) : base(pass, condition, callback)
            {
                m_remainingTime = delay;
            }

            #endregion

            #region Update

            public override bool Update(float deltaTime)
            {
                m_remainingTime -= deltaTime;
                if (m_remainingTime <= 0f)
                {
                    m_callback?.Invoke();
                    return true;
                }
                return false;
            }

            #endregion
        }
        private class FrameDelayedCall : DelayedCall
        {
            #region Members

            private int m_remainingFrames;

            #endregion

            #region Constructor

            public FrameDelayedCall(int framesToWait, EUpdatePass pass, EUpdateCondition condition, Action callback) : base(pass, condition, callback)
            {
                m_remainingFrames = framesToWait;
            }

            #endregion

            #region Update

            public override bool Update(float deltaTime)
            {
                m_remainingFrames -= 1;
                if (m_remainingFrames == 0)
                {
                    m_callback?.Invoke();
                    return true;
                }
                return false;
            }

            #endregion
        }

        #endregion

        #region Registration

        private readonly Dictionary<ulong, DelayedCall> m_delayedCalls = new();
        private readonly List<EUpdatePass> m_delayedCallPasses = new();

        public void RegisterTimedDelayedCall(ulong key, float delay, EUpdatePass pass, EUpdateCondition condition, Action callback)
        {
            m_delayedCalls.Add(key, new TimedDelayedCall(delay, pass, condition, callback));
        }
        public void RegisterFrameDelayedCall(ulong key, int framesToWait, EUpdatePass pass, EUpdateCondition condition, Action callback)
        {
            // If the pass has not been triggered yet,
            // it will be triggered shortly after this registration
            // thus we need to increase the frames to wait to make sure the current frame is not deducted
            if (!m_delayedCallPasses.Contains(pass))
            {
                framesToWait++;
            }
            // If the pass has already been triggered and framesToWait = 0
            // we need to trigger the callback and don't register the delayed call
            else if (framesToWait == 0)
            {
                callback?.Invoke();
                return;
            }

            m_delayedCalls.Add(key, new FrameDelayedCall(framesToWait, pass, condition, callback));
        }

        public void UnregisterDelayedCall(ulong key)
        {
            m_delayedCalls.Remove(key);
        }

        #endregion

        #region Update

        private void UpdateDelayedCalls(EUpdatePass pass, float deltaTime)
        {
            // At this moment, newly registrated delayed calls won't be updated this frame
            m_delayedCallPasses.Add(pass);

            List<ulong> toDestroy = new();
            foreach (var (key, delayedCall) in m_delayedCalls)
            {
                if (IsConditionFulfilled(delayedCall.condition)
                    && delayedCall.pass == pass
                    && delayedCall.Update(deltaTime))
                {
                    toDestroy.Add(key);
                }
            }

            foreach (var key in toDestroy)
            {
                UnregisterDelayedCall(key);
            }
        }

        #endregion

        #region Utility

        protected void ClearDelayedCalls()
        {
            m_delayedCalls.Clear();
            m_delayedCallPasses.Clear();
        }

        #endregion

        #endregion

        #region Default Events

        private void InvokeDefaultEvents(EUpdatePass pass, float deltaTime)
        {
            switch (pass)
            {
                case EUpdatePass.EARLY: OnEarlyUpdate?.Invoke(deltaTime); break;
                case EUpdatePass.CLASSIC: OnUpdate?.Invoke(deltaTime); break;
                case EUpdatePass.LATE: OnLateUpdate?.Invoke(deltaTime); break;
                case EUpdatePass.FIXED: OnFixedUpdate?.Invoke(deltaTime); break;
            }
        }

        #endregion

        
        #region Utility

        public void Clear()
        {
            // PASSES
            m_currentFramePasses.Clear();

            // CHANNELS
            ClearChannels();

            // DELAYED CALLS
            ClearDelayedCalls();

            // TIMELINES
            ClearUpdateTimelineInstances();

            // DEFAULT EVENTS
            OnEarlyUpdate = null;
            OnUpdate = null;
            OnLateUpdate = null;
            OnFixedUpdate = null;
        }

        #endregion
    }
}
