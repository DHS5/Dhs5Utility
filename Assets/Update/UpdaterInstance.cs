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

        internal void RegisterCallback(int key, UpdateCallback callback)
        {
            if (m_registeredCallbacks.ContainsKey(key))
            {
                m_registeredCallbacks[key] += callback;
            }
            else
            {
                m_registeredCallbacks.Add(key, callback);
            }
        }
        internal void UnregisterCallback(int key, UpdateCallback callback)
        {
            if (m_registeredCallbacks.ContainsKey(key))
            {
                m_registeredCallbacks[key] -= callback;
            }
        }

        #endregion

        #region Timeline Registrations

        private Dictionary<int, UpdateTimelineState> m_updateTimelines = new();

        internal bool RegisterUpdateTimeline(UpdateTimelineDatabaseElement updateTimeline)
        {
            if (updateTimeline == null) return false;
            if (m_updateTimelines.ContainsKey(updateTimeline.UID)) return true;

            if (updateTimeline.HasValidUpdate(out int updateKey))
            {
                var state = new UpdateTimelineState(updateTimeline, updateKey);
                m_updateTimelines[updateTimeline.UID] = state;
                RegisterCallback(updateKey, state.OnUpdate);
                return true;
            }
            else
            {
                Debug.LogError("You tried to register an UpdateTimeline that has no valid update");
                return false;
            }
        }
        internal void UnregisterUpdateTimeline(int updateTimelineUID)
        {
            if (m_updateTimelines.TryGetValue(updateTimelineUID, out UpdateTimelineState state))
            {
                UnregisterCallback(state.updateKey, state.OnUpdate);
                m_updateTimelines.Remove(updateTimelineUID);
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
            foreach (var category in GetPassValidCategories(pass))
            {
                bool timescaleIndependant = m_updaterElements[category].TimescaleIndependent;
                InvokeCategoryEvents(category, timescaleIndependant ? realDeltaTime : deltaTime, timescaleIndependant ? RealTime : Time);
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

        private Dictionary<int, float> m_lastUpdateTimes = new();

        private void InvokeCategoryEvents(int category, float deltaTime, float time)
        {
            if (m_registeredCallbacks.ContainsKey(category))
            {
                m_registeredCallbacks[category]?.Invoke(deltaTime);
                m_lastUpdateTimes[category] = time;
            }
        }

        private List<int> GetPassValidCategories(UpdatePass pass)
        {
            List<int> list = new();

            foreach (var (category, element) in m_updaterElements)
            {
                if (element.Pass == pass && CanUpdate(category, element))
                {
                    list.Add(category);
                }
            }

            list.Sort((c1, c2) => m_updaterElements[c1].Order.CompareTo(m_updaterElements[c2].Order));

            return list;
        }
        protected virtual bool CanUpdate(int category, UpdaterDatabaseElement updaterElement)
        {
            if (updaterElement.Condition.IsFullfilled())
            {
                if (updaterElement.HasCustomFrequency(out float frequency)
                    && m_lastUpdateTimes.TryGetValue(category, out float lastUpdate))
                {
                    return (updaterElement.TimescaleIndependent ? RealTime : Time) >= lastUpdate + frequency;
                }
                return true;
            }
            return false;
        }

        #endregion

        #region Updater Elements

        private Dictionary<int, UpdaterDatabaseElement> m_updaterElements = new();

        private void FetchUpdaterElements()
        {
            m_updaterElements.Clear();

            for (int i = 0; i < UpdaterDatabase.I.Count; i++)
            {
                m_updaterElements.Add(i, UpdaterDatabase.I.GetValueAtIndex<UpdaterDatabaseElement>(i));
            }
        }

        #endregion

        #region Update Timeline Management

        internal bool IsUpdateTimelineRegistered(int uid) => m_updateTimelines.ContainsKey(uid);
        internal bool TryGetUpdateTimelineState(int uid, out UpdateTimelineState state) => m_updateTimelines.TryGetValue(uid, out state);
        internal bool TryGetOrCreateUpdateTimelineHandle(UpdateTimelineDatabaseElement updateTimeline, out UpdateTimelineHandle handle)
        {
            if (updateTimeline == null)
            {
                handle = UpdateTimelineHandle.Empty;
                return false;
            }

            if (m_updateTimelines.ContainsKey(updateTimeline.UID))
            {
                handle = new(this, updateTimeline.UID);
                return true;
            }
            else if (RegisterUpdateTimeline(updateTimeline))
            {
                handle = new(this, updateTimeline.UID);
                return true;
            }

            handle = UpdateTimelineHandle.Empty;
            return false;
        }

        private void ClearUpdateTimelines()
        {
            foreach (var updateTimelineUID in m_updateTimelines.Keys)
            {
                UnregisterUpdateTimeline(updateTimelineUID);
            }
            m_updateTimelines.Clear();
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

            ClearUpdateTimelines();

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
