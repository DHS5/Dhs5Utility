using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dhs5.Utility.Updates
{
    public class Updater<UpdateEnum> where UpdateEnum : Enum
    {
        #region Instance Creation

        private static UpdaterInstance Instance { get; set; }

        private static void CreateInstance()
        {
            if (Instance != null) return;

            var obj = new GameObject("Updater");
            GameObject.DontDestroyOnLoad(obj);

            Instance = obj.AddComponent<UpdaterInstance>();
            Instance.Init(OnEnable, Update, LateUpdate, FixedUpdate);
        }

        private static UpdaterInstance GetInstance()
        {
            if (Instance == null)
            {
                CreateInstance();
            }
            return Instance;
        }

        #endregion

        #region Properties

        // TIME
        public static float Time { get; private set; }
        public static float DeltaTime { get; private set; }
        // REALTIME
        public static float RealTime { get; private set; }
        public static float RealDeltaTime { get; private set; }
        // FRAME
        public static int Frame { get; private set; }

        // GAME STATE
        public static bool GamePaused { get; private set; }

        #endregion

        #region Events

        // UPDATES
        public static event UpdateCallback OnEarlyUpdate;
        public static event UpdateCallback OnUpdate;
        public static event UpdateCallback OnLateUpdate;

        public static event UpdateCallback OnFixedUpdate;

        public static event UpdateCallback OnBeforeInputUpdate;
        public static event UpdateCallback OnAfterInputUpdate;

        // ONE-SHOT UPDATES
        private static event UpdateCallback OnOneShotUpdate;

        #endregion

        #region Initialization

        private static void Init()
        {
            GetUpdaterElements();
        }

        private static void OnEnable(bool enable)
        {
            if (enable)
            {
                InputSystem.onBeforeUpdate += PreInputUpdate;
                InputSystem.onAfterUpdate += PostInputUpdate;
            }
            else
            {
                InputSystem.onBeforeUpdate -= PreInputUpdate;
                InputSystem.onAfterUpdate -= PostInputUpdate;
            }
        }

        #endregion


        #region Registration

        private static ulong _registrationCount = 0;

        private static Dictionary<UpdateEnum, HashSet<ulong>> _registeredKeys = new();
        private static Dictionary<UpdateEnum, UpdateCallback> _registeredCallbacks = new();

        private static ulong GetUniqueRegistrationKey()
        {
            _registrationCount++;
            return _registrationCount;
        }

        public static bool Register(bool register, UpdateEnum category, UpdateCallback callback, ref ulong key)
        {
            if (register) // Wants to register new callback
            {
                if (_registeredKeys.TryGetValue(category, out var keys)) // The callback category already exists
                {
                    if (keys.Contains(key)) // This callback is already registered
                    {
                        return false;
                    }

                    key = GetUniqueRegistrationKey();
                    keys.Add(key);
                    _registeredCallbacks[category] += callback;

                    return true;
                }
                else // The callback category doesn't exists yet
                {
                    key = GetUniqueRegistrationKey();
                    _registeredKeys.Add(category, new HashSet<ulong>() { key });
                    _registeredCallbacks.Add(category, callback);

                    return true;
                }
            }

            else // Wants to unregister callback
            {
                if (_registeredKeys.TryGetValue(category, out var keys) // The callback category exists
                    && keys.Remove(key)) // AND the key was registered and removed successfully
                {
                    _registeredCallbacks[category] -= callback;

                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Pass Management

        private static void InvokePassEvents(UpdatePass pass, float deltaTime, float realDeltaTime)
        {
            foreach (var category in GetPassList(pass))
            {
                InvokeCategoryEvents(category, deltaTime, realDeltaTime);
            }

            InvokeDefaultEvents(pass, deltaTime);
            InvokeOneShotEvents(pass, deltaTime);
        }
        private static void InvokeDefaultEvents(UpdatePass pass, float deltaTime)
        {
            switch (pass)
            {
                case UpdatePass.CLASSIC: OnUpdate?.Invoke(deltaTime); break;
                case UpdatePass.EARLY: OnEarlyUpdate?.Invoke(deltaTime); break;
                case UpdatePass.LATE: OnLateUpdate?.Invoke(deltaTime); break;
                case UpdatePass.FIXED: OnFixedUpdate?.Invoke(deltaTime); break;
                case UpdatePass.PRE_INPUT: OnBeforeInputUpdate?.Invoke(deltaTime); break;
                case UpdatePass.POST_INPUT: OnAfterInputUpdate?.Invoke(deltaTime); break;
            }
        }
        private static void InvokeOneShotEvents(UpdatePass pass, float deltaTime)
        {
            if (pass == UpdatePass.CLASSIC)
            {
                OnOneShotUpdate?.Invoke(deltaTime);
                OnOneShotUpdate = null;
                OnOneShotUpdate = _onNextUpdate;
                _onNextUpdate = null;
            }
        }

        #endregion

        #region Categories Management

        private static Dictionary<UpdateEnum, UpdaterDatabaseElement> _updaterElements = new();
        private static Dictionary<UpdateEnum, float> _lastUpdateTimes = new();

        private static void GetUpdaterElements()
        {
            _updaterElements.Clear();

            for (int i = 0; i < UpdaterDatabase.I.Count; i++)
            {
                _updaterElements.Add((UpdateEnum)Enum.ToObject(typeof(UpdateEnum), i), UpdaterDatabase.I.GetValueAtIndex<UpdaterDatabaseElement>(i));
            }
        }

        private static List<UpdateEnum> GetPassList(UpdatePass pass)
        {
            List<UpdateEnum> list = new List<UpdateEnum>();

            foreach (var (category, updaterElement) in _updaterElements)
            {
                if (updaterElement.Pass == pass && CanUpdate(category, updaterElement))
                {
                    list.Add(category);
                }
            }

            list.Sort((c1, c2) => _updaterElements[c1].Order.CompareTo(_updaterElements[c2].Order));

            return list;
        }
        private static bool CanUpdate(UpdateEnum category, UpdaterDatabaseElement updaterElement)
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

        private static void InvokeCategoryEvents(UpdateEnum category, float deltaTime, float realDeltaTime)
        {
            if (_registeredCallbacks.ContainsKey(category))
            {
                bool timescaleIndependent = _updaterElements[category].TimescaleIndependent;
                _registeredCallbacks[category]?.Invoke(timescaleIndependent ? realDeltaTime : deltaTime);
                _lastUpdateTimes[category] = timescaleIndependent ? RealTime : Time;
            }
        }

        #endregion


        #region Time Management

        private static void ComputeTimeState()
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
        private static void Update()
        {
            ComputeTimeState();

            EarlyUpdate();
            ClassicUpdate();
        }
        private static void LateUpdate()
        {
            InvokePassEvents(UpdatePass.LATE, DeltaTime, RealDeltaTime);
        }
        private static void FixedUpdate()
        {
            InvokePassEvents(UpdatePass.FIXED, UnityEngine.Time.fixedDeltaTime, UnityEngine.Time.fixedUnscaledDeltaTime);
        }

        // INPUT
        private static void PreInputUpdate()
        {
            InvokePassEvents(UpdatePass.PRE_INPUT, DeltaTime, RealDeltaTime);
        }
        private static void PostInputUpdate()
        {
            InvokePassEvents(UpdatePass.POST_INPUT, DeltaTime, RealDeltaTime);
        }

        // CUSTOM
        private static void EarlyUpdate()
        {
            InvokePassEvents(UpdatePass.EARLY, DeltaTime, RealDeltaTime);
        }
        private static void ClassicUpdate()
        {
            InvokePassEvents(UpdatePass.CLASSIC, DeltaTime, RealDeltaTime);
        }

        #endregion


        #region Next Updates

        private static UpdateCallback _onNextUpdate;
        public static void CallOnNextUpdate(UpdateCallback callback)
        {
            if (_onNextUpdate != null) _onNextUpdate += callback;
            else _onNextUpdate = callback;
        }

        #endregion
    }
}
