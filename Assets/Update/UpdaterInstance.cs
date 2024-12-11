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


        #region Registrations

        private Dictionary<int, UpdateCallback> _registeredCallbacks = new();

        internal void RegisterCallback(int key, UpdateCallback callback)
        {
            if (_registeredCallbacks.ContainsKey(key))
            {
                _registeredCallbacks[key] += callback;
            }
            else
            {
                _registeredCallbacks.Add(key, callback);
            }
        }
        internal void UnregisterCallback(int key, UpdateCallback callback)
        {
            if (_registeredCallbacks.ContainsKey(key))
            {
                _registeredCallbacks[key] -= callback;
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
                bool timescaleIndependant = _updaterElements[category].TimescaleIndependent;
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

        private Dictionary<int, float> _lastUpdateTimes = new();

        private void InvokeCategoryEvents(int category, float deltaTime, float time)
        {
            if (_registeredCallbacks.ContainsKey(category))
            {
                _registeredCallbacks[category]?.Invoke(deltaTime);
                _lastUpdateTimes[category] = time;
            }
        }

        private List<int> GetPassValidCategories(UpdatePass pass)
        {
            List<int> list = new();

            foreach (var (category, element) in _updaterElements)
            {
                if (element.Pass == pass && CanUpdate(category, element))
                {
                    list.Add(category);
                }
            }

            list.Sort((c1, c2) => _updaterElements[c1].Order.CompareTo(_updaterElements[c2].Order));

            return list;
        }
        protected virtual bool CanUpdate(int category, UpdaterDatabaseElement updaterElement)
        {
            if (updaterElement.Condition.IsFullfilled())
            {
                if (updaterElement.HasCustomFrequency(out float frequency)
                    && _lastUpdateTimes.TryGetValue(category, out float lastUpdate))
                {
                    return (updaterElement.TimescaleIndependent ? RealTime : Time) >= lastUpdate + frequency;
                }
                return true;
            }
            return false;
        }

        #endregion

        #region Updater Elements

        private Dictionary<int, UpdaterDatabaseElement> _updaterElements = new();

        private void FetchUpdaterElements()
        {
            _updaterElements.Clear();

            for (int i = 0; i < UpdaterDatabase.I.Count; i++)
            {
                _updaterElements.Add(i, UpdaterDatabase.I.GetValueAtIndex<UpdaterDatabaseElement>(i));
            }
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

        private Dictionary<int, UpdateCallback> _preciseFrameCallbacks = new();
        private UpdateCallback _oneShotLateUpdateCallback;

        internal void RegisterCallbackOnFrame(int frame, UpdateCallback callback)
        {
            if (m_lastClassicUpdateFrame == frame)
            {
                callback?.Invoke(DeltaTime);
                return;
            }

            if (_preciseFrameCallbacks.ContainsKey(frame))
            {
                _preciseFrameCallbacks[frame] += callback;
            }
            else
            {
                _preciseFrameCallbacks.Add(frame, callback);
            }
        }
        internal void RegisterOneShotLateUpdateCallback(UpdateCallback callback)
        {
            if (m_lastLateUpdateFrame == Frame)
            {
                callback?.Invoke(DeltaTime);
                return;
            }

            _oneShotLateUpdateCallback += callback;
        }

        private void InvokePreciseFramesUpdates(float deltaTime)
        {
            if (_preciseFrameCallbacks.ContainsKey(Frame))
            {
                _preciseFrameCallbacks[Frame].Invoke(deltaTime);
                _preciseFrameCallbacks.Remove(Frame);
            }
        }
        private void InvokeOneShotLateUpdate(float deltaTime)
        {
            _oneShotLateUpdateCallback?.Invoke(deltaTime);
            _oneShotLateUpdateCallback = null;
        }

        #endregion


        #region Utility

        internal void Clear()
        {
            _updaterElements.Clear();
            _registeredCallbacks.Clear();
            _preciseFrameCallbacks.Clear();
            _lastUpdateTimes.Clear();

            OnEarlyUpdate = null;
            OnUpdate = null;
            OnLateUpdate = null;
            OnFixedUpdate = null;
            OnBeforeInputUpdate = null;
            OnAfterInputUpdate = null;
        }

        #endregion
    }
}
