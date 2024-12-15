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

        public static void Init<UpdaterInstanceType>() where UpdaterInstanceType : UpdaterInstance
        {
            CreateInstance<UpdaterInstanceType>();
        }
        private static void CreateInstance<UpdaterInstanceType>() where UpdaterInstanceType : UpdaterInstance
        {
            if (Instance != null) return;

            var obj = new GameObject("Updater");
            GameObject.DontDestroyOnLoad(obj);

            Instance = obj.AddComponent<UpdaterInstanceType>();
        }


        private static UpdaterInstance GetInstance()
        {
            if (!IsInstanceValid())
            {
                CreateInstance<UpdaterInstance>();
            }
            return Instance;
        }
        private static bool IsInstanceValid()
        {
            return Instance != null;
        }

        #endregion

        #region Properties

        // ACTIVATION
        public static bool IsActive 
        { 
            get 
            { 
                if (IsInstanceValid()) return Instance.enabled; 
                return false;
            } 
        }

        // TIME
        public static float Time
        {
            get
            {
                if (IsInstanceValid()) return Instance.Time;
                return UnityEngine.Time.time;
            }
        }
        public static float DeltaTime
        {
            get
            {
                if (IsInstanceValid()) return Instance.DeltaTime;
                return UnityEngine.Time.deltaTime;
            }
        }
        // REALTIME
        public static float RealTime
        {
            get
            {
                if (IsInstanceValid()) return Instance.RealTime;
                return UnityEngine.Time.realtimeSinceStartup;
            }
        }
        public static float RealDeltaTime
        {
            get
            {
                if (IsInstanceValid()) return Instance.RealDeltaTime;
                return UnityEngine.Time.unscaledDeltaTime;
            }
        }
        // FRAME
        public static int Frame
        {
            get
            {
                if (IsInstanceValid()) return Instance.Frame;
                return UnityEngine.Time.frameCount;
            }
        }

        // GAME STATE
        public static bool GamePaused
        {
            get
            {
                if (IsInstanceValid()) return Instance.GamePaused;
                return UnityEngine.Time.timeScale > 0f;
            }
        }

        #endregion

        #region Events

        public static event UpdateCallback OnEarlyUpdate { add { GetInstance().OnEarlyUpdate += value; } remove { if (IsInstanceValid()) GetInstance().OnEarlyUpdate -= value; } }
        public static event UpdateCallback OnUpdate { add { GetInstance().OnUpdate += value; } remove { if (IsInstanceValid()) GetInstance().OnUpdate -= value; } }
        public static event UpdateCallback OnLateUpdate { add { GetInstance().OnLateUpdate += value; } remove { if (IsInstanceValid()) GetInstance().OnLateUpdate -= value; } }

        public static event UpdateCallback OnFixedUpdate { add { GetInstance().OnFixedUpdate += value; } remove { if (IsInstanceValid()) GetInstance().OnFixedUpdate -= value; } }

        public static event UpdateCallback OnBeforeInputUpdate { add { GetInstance().OnBeforeInputUpdate += value; } remove { if (IsInstanceValid()) GetInstance().OnBeforeInputUpdate -= value; } }
        public static event UpdateCallback OnAfterInputUpdate { add { GetInstance().OnAfterInputUpdate += value; } remove { if (IsInstanceValid()) GetInstance().OnAfterInputUpdate -= value; } }

        #endregion


        #region Callbacks Registration

        private static ulong _registrationCount = 0;
        private static Dictionary<UpdateEnum, HashSet<ulong>> _registeredCallbackKeys = new();

        private static ulong GetUniqueRegistrationKey()
        {
            if (!IsActive) GetInstance(); // Activate on first registration

            _registrationCount++;
            return _registrationCount;
        }

        public static bool Register(bool register, UpdateEnum category, UpdateCallback callback, ref ulong key)
        {
            if (register) // Wants to register new callback
            {
                if (_registeredCallbackKeys.TryGetValue(category, out var keys)) // The callback category already exists
                {
                    if (keys.Contains(key)) // This callback is already registered
                    {
                        return false;
                    }

                    key = GetUniqueRegistrationKey();
                    keys.Add(key);
                    GetInstance().RegisterCallback(Convert.ToInt32(category), callback);

                    return true;
                }
                else // The callback category doesn't exists yet
                {
                    key = GetUniqueRegistrationKey();
                    _registeredCallbackKeys.Add(category, new HashSet<ulong>() { key });
                    GetInstance().RegisterCallback(Convert.ToInt32(category), callback);

                    return true;
                }
            }

            else if (IsInstanceValid()) // Wants to unregister callback
            {
                if (_registeredCallbackKeys.TryGetValue(category, out var keys) // The callback category exists
                    && keys.Remove(key)) // AND the key was registered and removed successfully
                {
                    GetInstance().UnregisterCallback(Convert.ToInt32(category), callback);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Timelines Management

        /// <summary>
        /// Attempts to get a handle for the UpdateTimeline
        /// </summary>
        /// <remarks>
        /// If the UpdateTimeline is not registered yet, this method will register it
        /// </remarks>
        public static bool TryGetUpdateTimelineHandle(UpdateTimelineDatabaseElement updateTimeline, out UpdateTimelineHandle handle)
        {
            return GetInstance().TryGetOrCreateUpdateTimelineHandle(updateTimeline, out handle);
        }

        #endregion

        #region Precise Frames Updates

        /// <summary>
        /// Register a callback to be called once on the next classic update (next frame)
        /// </summary>
        public static void CallOnNextUpdate(UpdateCallback callback)
        {
            CallInXFrames(1, callback);
        }
        /// <summary>
        /// Register a callback to be called once on this frame late update
        /// </summary>
        /// <remarks>
        /// If this frame's late update has already been called, call instantaneously
        /// </remarks>
        public static void CallOnLateUpdate(UpdateCallback callback)
        {
            if (callback == null) return;

            GetInstance().RegisterOneShotLateUpdateCallback(callback);
        }

        /// <summary>
        /// Register a callback to be called once in <paramref name="framesToWait"/> number of frames in the classic update
        /// </summary>
        public static void CallInXFrames(int framesToWait, UpdateCallback callback)
        {
            if (callback == null || framesToWait < 0) return;

            int nextFrame = Frame + framesToWait;
            GetInstance().RegisterCallbackOnFrame(nextFrame, callback);
        }

        #endregion


        #region Utility

        public static void Pause(bool pause)
        {
            UnityEngine.Time.timeScale = pause ? 0f : 1f;
        }

        public static void Clear()
        {
            _registrationCount = 0;
            _registeredCallbackKeys.Clear();

            GetInstance().Clear();
        }

        #endregion
    }
}
