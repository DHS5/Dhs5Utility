using Dhs5.Utility.Databases;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
        public bool GamePaused { get; private set; }

        #endregion

        #region Events

        public event UpdateCallback OnEarlyUpdate;
        public event UpdateCallback OnUpdate;
        public event UpdateCallback OnLateUpdate;

        public event UpdateCallback OnFixedUpdate;

        public event UpdateCallback OnBeforeInputUpdate;
        public event UpdateCallback OnAfterInputUpdate;

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            FetchUpdaterElements();

            InputSystem.onBeforeUpdate += PreInputUpdate;
            InputSystem.onAfterUpdate += PostInputUpdate;
        }
        protected virtual void OnDisable()
        {
            InputSystem.onBeforeUpdate -= PreInputUpdate;
            InputSystem.onAfterUpdate -= PostInputUpdate;
        }

        #endregion


        #region Updates Registrations

        private Dictionary<int, UpdateCallback> m_registeredCallbacks = new();

        internal void RegisterCallback(int category, UpdateCallback callback)
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
        internal void UnregisterCallback(int category, UpdateCallback callback)
        {
            if (m_registeredCallbacks.ContainsKey(category))
            {
                m_registeredCallbacks[category] -= callback;
            }
        }

        private bool IsCategoryRegistered(int category)
        {
            return m_registeredCallbacks.ContainsKey(category);
        }

        #endregion

        #region Timeline Instance Creations

        private Dictionary<ulong, UpdateTimelineInstance> m_updateTimelineInstances = new();

        internal bool CreateUpdateTimelineInstance(UpdateTimeline updateTimeline, ulong key, out UpdateTimelineInstanceHandle handle)
        {
            if (updateTimeline == null || m_updateTimelineInstances.ContainsKey(key))
            {
                handle = UpdateTimelineInstanceHandle.Empty;
                return false;
            }

            if (updateTimeline.HasValidUpdate(out int updateCategory))
            {
                var state = new UpdateTimelineInstance(updateTimeline, updateCategory);
                m_updateTimelineInstances[key] = state;
                RegisterCallback(updateCategory, state.OnUpdate);
                handle = new UpdateTimelineInstanceHandle(this, key);
                return true;
            }
            else
            {
                Debug.LogError("You tried to register an UpdateTimeline that has no valid update");
                handle = UpdateTimelineInstanceHandle.Empty;
                return false;
            }
        }
        internal void DestroyUpdateTimelineInstance(ulong key)
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

            GamePaused = DeltaTime != 0f;
        }

        #endregion

        #region Update Methods

        // UNITY
        protected virtual void Update()
        {
            ComputeTimeState();

            EarlyUpdate();
            ClassicUpdate();
        }
        protected virtual void LateUpdate()
        {
            InvokePassEvents(UpdatePass.LATE, DeltaTime, RealDeltaTime);
        }
        protected virtual void FixedUpdate()
        {
            InvokePassEvents(UpdatePass.FIXED, UnityEngine.Time.fixedDeltaTime, UnityEngine.Time.fixedUnscaledDeltaTime);
        }

        // INPUT
        protected virtual void PreInputUpdate()
        {
            InvokePassEvents(UpdatePass.PRE_INPUT, DeltaTime, RealDeltaTime);
        }
        protected virtual void PostInputUpdate()
        {
            InvokePassEvents(UpdatePass.POST_INPUT, DeltaTime, RealDeltaTime);
        }

        // CUSTOM
        protected virtual void EarlyUpdate()
        {
            InvokePassEvents(UpdatePass.EARLY, DeltaTime, RealDeltaTime);
        }
        protected virtual void ClassicUpdate()
        {
            InvokePassEvents(UpdatePass.CLASSIC, DeltaTime, RealDeltaTime);
        }

        #endregion


        #region Pass Management

        private void InvokePassEvents(UpdatePass pass, float deltaTime, float realDeltaTime)
        {
            foreach (var categoryElement in GetPassValidCategories(pass))
            {
                bool timescaleIndependant = categoryElement.TimescaleIndependent;
                InvokeCategoryEvents(categoryElement, timescaleIndependant ? realDeltaTime : deltaTime, timescaleIndependant ? RealTime : Time);
            }

            InvokeDefaultEvents(pass, deltaTime);

            if (pass == UpdatePass.CLASSIC)
            {
                InvokePreciseFramesUpdates(deltaTime);
            }
            else if (pass == UpdatePass.LATE)
            {
                InvokeOneShotLateUpdate(deltaTime);
            }
        }        

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

        private List<UpdaterDatabaseElement> GetPassValidCategories(UpdatePass pass)
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
        protected virtual bool CanUpdate(UpdaterDatabaseElement categoryElement)
        {
            if (categoryElement.Condition.IsFullfilled())
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
            foreach (var updaterElement in Database.Get<UpdaterDatabase>())
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
        internal bool TryGetUpdateTimelineInstanceHandle(UpdateTimeline timeline, out UpdateTimelineInstanceHandle handle)
        {
            foreach (var (key, instance) in m_updateTimelineInstances)
            {
                if (instance.timelineUID == timeline.UID)
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

        private int m_lastClassicUpdateFrame;
        private int m_lastLateUpdateFrame;

        private void InvokeDefaultEvents(UpdatePass pass, float deltaTime)
        {
            switch (pass)
            {
                case UpdatePass.EARLY: OnEarlyUpdate?.Invoke(deltaTime); break;
                case UpdatePass.CLASSIC: OnUpdate?.Invoke(deltaTime); m_lastClassicUpdateFrame = Frame; break;
                case UpdatePass.LATE: OnLateUpdate?.Invoke(deltaTime); m_lastLateUpdateFrame = Frame; break;
                case UpdatePass.FIXED: OnFixedUpdate?.Invoke(deltaTime); break;
                case UpdatePass.PRE_INPUT: OnBeforeInputUpdate?.Invoke(deltaTime); break;
                case UpdatePass.POST_INPUT: OnAfterInputUpdate?.Invoke(deltaTime); break;
            }
        }

        #endregion

        #region Precise Updates

        private Dictionary<int, UpdateCallback> m_preciseFrameCallbacks = new();
        private UpdateCallback m_oneShotLateUpdateCallback;

        internal void RegisterCallbackOnFrame(int frame, UpdateCallback callback)
        {
            if (m_lastClassicUpdateFrame == frame)
            {
                callback?.Invoke(DeltaTime);
                return;
            }

            if (m_preciseFrameCallbacks.ContainsKey(frame))
            {
                m_preciseFrameCallbacks[frame] += callback;
            }
            else
            {
                m_preciseFrameCallbacks.Add(frame, callback);
            }
        }
        internal void RegisterOneShotLateUpdateCallback(UpdateCallback callback)
        {
            if (m_lastLateUpdateFrame == Frame)
            {
                callback?.Invoke(DeltaTime);
                return;
            }

            m_oneShotLateUpdateCallback += callback;
        }

        private void InvokePreciseFramesUpdates(float deltaTime)
        {
            if (m_preciseFrameCallbacks.ContainsKey(Frame))
            {
                m_preciseFrameCallbacks[Frame].Invoke(deltaTime);
                m_preciseFrameCallbacks.Remove(Frame);
            }
        }
        private void InvokeOneShotLateUpdate(float deltaTime)
        {
            m_oneShotLateUpdateCallback?.Invoke(deltaTime);
            m_oneShotLateUpdateCallback = null;
        }

        #endregion


        #region Utility

        internal void Clear()
        {
            m_updaterElements.Clear();
            m_registeredCallbacks.Clear();
            m_preciseFrameCallbacks.Clear();
            m_lastUpdateTimes.Clear();

            ClearUpdateTimelineInstances();

            m_oneShotLateUpdateCallback = null;

            OnEarlyUpdate = null;
            OnUpdate = null;
            OnLateUpdate = null;
            OnFixedUpdate = null;
            OnBeforeInputUpdate = null;
            OnAfterInputUpdate = null;

            m_lastClassicUpdateFrame = -1;
            m_lastLateUpdateFrame = -1;
        }

        #endregion
    }
}
