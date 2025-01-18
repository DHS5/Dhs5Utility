using Dhs5.Utility.Databases;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public class UpdaterInstance : MonoBehaviour
    {
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

        protected virtual void OnEnable()
        {
            FetchUpdaterElements();
        }
        protected virtual void OnDisable()
        {

        }

        #endregion


        #region Updates Registrations

        private Dictionary<int, UpdateCallback> m_registeredCallbacks = new();

        public void RegisterCallback(int category, UpdateCallback callback)
        {
            if (m_registeredCallbacks.ContainsKey(category))
            {
                m_registeredCallbacks[category] += callback;
            }
            else
            {
                m_registeredCallbacks.Add(category, callback);
                SetFirstUpdateTime(category);
            }
        }
        public void UnregisterCallback(int category, UpdateCallback callback)
        {
            if (m_registeredCallbacks.ContainsKey(category))
            {
                m_registeredCallbacks[category] -= callback;
            }
        }

        public bool IsCategoryRegistered(int category)
        {
            return m_registeredCallbacks.ContainsKey(category);
        }

        #endregion

        #region Timeline Instance Creations

        private Dictionary<ulong, UpdateTimelineInstance> m_updateTimelineInstances = new();

        public bool CreateUpdateTimelineInstance(IUpdateTimeline updateTimeline, ulong key, out UpdateTimelineInstanceHandle handle)
        {
            if (updateTimeline == null || m_updateTimelineInstances.ContainsKey(key))
            {
                handle = UpdateTimelineInstanceHandle.Empty;
                return false;
            }

            if (updateTimeline.UpdateKey > 0 && updateTimeline.Duration > 0f)
            {
                var state = new UpdateTimelineInstance(updateTimeline);
                m_updateTimelineInstances[key] = state;
                RegisterCallback(updateTimeline.UpdateKey, state.OnUpdate);
                handle = new UpdateTimelineInstanceHandle(this, key);
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
                UnregisterCallback(state.updateCategory, state.OnUpdate);
                m_updateTimelineInstances.Remove(key);
            }
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
            foreach (var categoryElement in GetPassValidCategories(pass))
            {
                bool timescaleIndependant = categoryElement.TimescaleIndependent;
                InvokeCategoryEvents(categoryElement, timescaleIndependant ? realDeltaTime : deltaTime, timescaleIndependant ? RealTime : Time);
            }

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

            public UpdateChannel(EUpdatePass pass, ushort order, EUpdateCondition condition, float frequency, float timescale, bool realtime)
            {
                this.pass = pass;
                this.order = order;
                this.condition = condition;
                this.customFrequency = frequency > 0f;
                this.frequency = frequency;
                this.timescale = timescale;
                this.realtime = realtime;

                TimeSinceLastUpdate = 0f;
            }

            #endregion

            #region Members

            public readonly EUpdatePass pass;
            public readonly ushort order;
            public readonly EUpdateCondition condition;
            public readonly bool customFrequency;
            public readonly float frequency;
            public readonly float timescale;
            public readonly bool realtime;

            public event UpdateCallback Callback;

            #endregion

            #region Properties

            public float TimeSinceLastUpdate { get; private set; }

            #endregion

            #region Behaviour

            public void Update(float deltaTime)
            {
                if (!customFrequency)
                {
                    Callback?.Invoke(deltaTime);
                }
                else
                {
                    TimeSinceLastUpdate += deltaTime * timescale;
                    if (TimeSinceLastUpdate >= frequency)
                    {
                        Callback?.Invoke(deltaTime);
                        TimeSinceLastUpdate -= frequency;
                    }
                }
            }

            #endregion
        }

        #endregion

        #region Channels Registration

        private Dictionary<EUpdatePass, List<UpdateChannel>> m_channels = new();

        private UpdateChannel GetOrCreateChannel(int channelIndex)
        {
            return null;
        }

        #endregion

        #region Channels Update

        protected void UpdateValidChannels(EUpdatePass pass, float deltaTime, float realDeltaTime)
        {
            if (m_channels.TryGetValue(pass, out var channels))
            {
                foreach (var channel in channels)
                {
                    if (IsChannelValid(channel))
                    {
                        channel.Update(channel.realtime ? realDeltaTime : deltaTime);
                    }
                }
            }
        }

        #endregion

        #region Channel Validity

        protected virtual bool IsChannelValid(UpdateChannel channel)
        {
            return IsConditionFulfilled(channel.condition);
        }

        #endregion

        #endregion

        #region Category Management

        private void InvokeCategoryEvents(UpdaterDatabaseElement categoryElement, float deltaTime, float time)
        {
            int category = categoryElement.EnumIndex;
            if (m_registeredCallbacks.ContainsKey(category))
            {
                if (categoryElement.HasCustomFrequency(out float frequency))
                {
                    if (TryGetLastUpdateTime(category, out float lastUpdateTime))
                    {
                        deltaTime = time - lastUpdateTime; // Ensure elapsed time since last update accuracy
                        SetLastUpdateTime(category, lastUpdateTime + frequency); // Ensure update frequency without gradual offset
                    }
                    else
                    {
                        deltaTime = 0f;
                        SetLastUpdateTime(category, time);
                    }
                }
                else
                {
                    SetLastUpdateTime(category, time);
                }
                m_registeredCallbacks[category]?.Invoke(deltaTime);
            }
        }

        private List<UpdaterDatabaseElement> GetPassValidCategories(EUpdatePass pass)
        {
            List<UpdaterDatabaseElement> list = new();

            foreach (var (category, element) in m_updaterElements)
            {
                if (IsCategoryRegistered(category) && 
                    element.Pass == pass && 
                    CanUpdate(element))
                {
                    list.Add(element);
                }
            }

            list.Sort((e1, e2) => e1.CompareTo(e2));

            return list;
        }
        private bool CanUpdate(UpdaterDatabaseElement categoryElement)
        {
            if (IsConditionFulfilled(categoryElement.Condition))
            {
                if (categoryElement.HasCustomFrequency(out float frequency)
                    && TryGetLastUpdateTime(categoryElement.EnumIndex, out float lastUpdate))
                {
                    return (categoryElement.TimescaleIndependent ? RealTime : Time) >= lastUpdate + frequency;
                }
                return true;
            }
            return false;
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

        #region Update Times

        private Dictionary<int, float> m_lastUpdateTimes = new();

        private bool TryGetLastUpdateTime(int category, out float lastUpdateTime)
        {
            return m_lastUpdateTimes.TryGetValue(category, out lastUpdateTime);
        }
        private void SetLastUpdateTime(int category, float time)
        {
            m_lastUpdateTimes[category] = time;
        }
        private void SetFirstUpdateTime(int category)
        {
            if (m_updaterElements.TryGetValue(category, out var categoryElement))
            {
                m_lastUpdateTimes[category] = categoryElement.TimescaleIndependent ? RealTime : Time;
            }
        }

        #endregion


        #region Updater Elements

        private Dictionary<int, UpdaterDatabaseElement> m_updaterElements = new();

        private void FetchUpdaterElements()
        {
            m_updaterElements.Clear();

            int i = 0;
            foreach (var updaterElement in Database.Enumerate<UpdaterDatabase, UpdaterDatabaseElement>())
            {
                m_updaterElements.Add(i, updaterElement);
                i++;
            }
        }

        #endregion

        #region Timeline Instances Management

        internal bool TimelineInstanceExist(ulong key) => m_updateTimelineInstances.ContainsKey(key);
        internal bool TryGetUpdateTimelineInstance(ulong key, out UpdateTimelineInstance state) => m_updateTimelineInstances.TryGetValue(key, out state);
        internal bool TryGetUpdateTimelineInstanceHandle(ulong key, out UpdateTimelineInstanceHandle handle)
        {
            if (m_updateTimelineInstances.ContainsKey(key))
            {
                handle = new(this, key);
                return true;
            }

            handle = UpdateTimelineInstanceHandle.Empty;
            return false;
        }
        internal bool TryGetUpdateTimelineInstanceHandle(int timelineUID, out UpdateTimelineInstanceHandle handle)
        {
            foreach (var (key, instance) in m_updateTimelineInstances)
            {
                if (instance.timelineUID == timelineUID)
                {
                    handle = new(this, key);
                    return true;
                }
            }

            handle = UpdateTimelineInstanceHandle.Empty;
            return false;
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

        #region Delayed Calls

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

        private Dictionary<ulong, DelayedCall> m_delayedCalls = new();
        private List<EUpdatePass> m_delayedCallPasses = new();

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

        public void UnregisterDelayedCall(ulong key)
        {
            m_delayedCalls.Remove(key);
        }

        #endregion


        #region Utility

        public void Clear()
        {
            m_updaterElements.Clear();
            m_registeredCallbacks.Clear();
            m_delayedCalls.Clear();
            m_currentFramePasses.Clear();
            m_delayedCallPasses.Clear();
            m_lastUpdateTimes.Clear();

            ClearUpdateTimelineInstances();

            OnEarlyUpdate = null;
            OnUpdate = null;
            OnLateUpdate = null;
            OnFixedUpdate = null;
        }

        #endregion
    }
}
