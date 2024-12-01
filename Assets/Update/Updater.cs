using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            Instance.Init(Update, LateUpdate, FixedUpdate);
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

        // GAME STATE
        public static bool GamePaused { get; private set; }

        #endregion

        #region Events

        public static event UpdateCallback OnEarlyUpdate;
        public static event UpdateCallback OnUpdate;
        public static event UpdateCallback OnLateUpdate;

        public static event UpdateCallback OnFixedUpdate;

        public static event UpdateCallback OnBeforeInputUpdate;
        public static event UpdateCallback OnAfterInputUpdate;

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

        private static void InvokePassEvents(UpdatePass pass, float deltaTime)
        {
            //foreach (var category in GetPassList(pass))
            //{
            //    InvokeCategoryEvents(category, deltaTime);
            //}

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

        #endregion

        #region Categories Management

        private static Dictionary<UpdateEnum, float> _lastUpdateTimes = new();

        private static void InvokeCategoryEvents(UpdateEnum category, float deltaTime)
        {
            if (_registeredCallbacks.ContainsKey(category))
            {
                _registeredCallbacks[category]?.Invoke(deltaTime);
                _lastUpdateTimes[category] = Time;
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
            InvokePassEvents(UpdatePass.LATE, DeltaTime);
        }
        private static void FixedUpdate()
        {
            InvokePassEvents(UpdatePass.FIXED, UnityEngine.Time.fixedDeltaTime);
        }

        // INPUT
        private static void PreInputUpdate()
        {
            InvokePassEvents(UpdatePass.PRE_INPUT, DeltaTime);
        }
        private static void PostInputUpdate()
        {
            InvokePassEvents(UpdatePass.POST_INPUT, DeltaTime);
        }

        // CUSTOM
        private static void EarlyUpdate()
        {
            InvokePassEvents(UpdatePass.EARLY, DeltaTime);
        }
        private static void ClassicUpdate()
        {
            InvokePassEvents(UpdatePass.CLASSIC, DeltaTime);
        }

        #endregion


        #region Default Updates Registration

        #endregion

        #region Next Updates

        #endregion



    }
}
