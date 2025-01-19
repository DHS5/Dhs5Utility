using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public class UpdaterLink<UpdateEnum, UpdaterType> where UpdateEnum : Enum where UpdaterType : Updater
    {
        #region Instance Creation

        private static UpdaterType _instance;
        protected static UpdaterType Instance
        {
            get
            {
                if (_instance == null || _instance != Updater.Instance)
                {
                    _instance = Updater.Instance as UpdaterType;
                }
                return _instance;
            }
        }

        public static void Init() 
        {
            CreateInstance();
        }
        protected static void CreateInstance()
        {
            if (Instance != null) return;

            var obj = new GameObject("Updater");
            GameObject.DontDestroyOnLoad(obj);

            var updater = obj.AddComponent<UpdaterType>();
            updater.SetAsInstance();
        }


        protected static UpdaterType GetInstance()
        {
            if (!IsInstanceValid())
            {
                CreateInstance();
            }
            return Instance;
        }
        protected static bool IsInstanceValid()
        {
            return Instance != null;
        }

        #endregion

        #region Properties

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
        public static bool TimePaused
        {
            get
            {
                if (IsInstanceValid()) return Instance.TimePaused;
                return UnityEngine.Time.timeScale > 0f;
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

        #endregion

        #region Events

        public static event UpdateCallback OnEarlyUpdate { add { GetInstance().OnEarlyUpdate += value; } remove { if (IsInstanceValid()) Instance.OnEarlyUpdate -= value; } }
        public static event UpdateCallback OnUpdate { add { GetInstance().OnUpdate += value; } remove { if (IsInstanceValid()) Instance.OnUpdate -= value; } }
        public static event UpdateCallback OnLateUpdate { add { GetInstance().OnLateUpdate += value; } remove { if (IsInstanceValid()) Instance.OnLateUpdate -= value; } }

        public static event UpdateCallback OnFixedUpdate { add { GetInstance().OnFixedUpdate += value; } remove { if (IsInstanceValid()) Instance.OnFixedUpdate -= value; } }

        #endregion

        #region Callbacks Registration

        // --- KEYS ---
        private static ulong _registrationCount = 0;
        protected static ulong GetUniqueRegistrationKey()
        {
            _registrationCount++;
            return _registrationCount;
        }

        // --- REGISTRATION ---
        private static Dictionary<UpdateEnum, HashSet<ulong>> _registeredCallbackKeys = new();

        public static bool Register(bool register, UpdateEnum category, UpdateCallback callback, ref ulong key)
        {
            if (register)
            {
                return Register(category, callback, ref key);
            }
            else
            {
                return Unregister(category, callback, ref key);
            }
        }
        private static bool Register(UpdateEnum category, UpdateCallback callback, ref ulong key)
        {
            if (_registeredCallbackKeys.TryGetValue(category, out var keys)) // The callback category already exists
            {
                if (keys.Contains(key)) // This callback is already registered
                {
                    return false;
                }

                key = GetUniqueRegistrationKey();
                keys.Add(key);
                GetInstance().RegisterChannelCallback(Convert.ToInt32(category), callback);

                return true;
            }
            else // The callback category doesn't exists yet
            {
                key = GetUniqueRegistrationKey();
                _registeredCallbackKeys.Add(category, new HashSet<ulong>() { key });
                GetInstance().RegisterChannelCallback(Convert.ToInt32(category), callback);

                return true;
            }
        }
        private static bool Unregister(UpdateEnum category, UpdateCallback callback, ref ulong key)
        {
            if (IsInstanceValid()) // Wants to unregister callback
            {
                if (_registeredCallbackKeys.TryGetValue(category, out var keys) // The callback category exists
                    && keys.Remove(key)) // AND the key was registered and removed successfully
                {
                    Instance.UnregisterChannelCallback(Convert.ToInt32(category), callback);
                    return true;
                }
            }
            return false;
        }

        #endregion


        #region Timelines Management

        /// <summary>
        /// Creates an Instance of <paramref name="timeline"/> and out a handle for it
        /// </summary>
        /// <returns>Whether the instance was successfully registered</returns>
        public static bool CreateTimelineInstance(IUpdateTimeline timeline, out UpdateTimelineInstanceHandle handle)
        {
            return GetInstance().CreateUpdateTimelineInstance(timeline, GetUniqueRegistrationKey(), out handle);
        }
        /// <summary>
        /// Creates an <see cref="UpdateTimelineInstance"/> from the parameters and out a handle for it
        /// </summary>
        /// <returns>Whether the instance was successfully registered</returns>
        public static bool CreateTimelineInstance(UpdateEnum category, float duration, out UpdateTimelineInstanceHandle handle, bool loop = false, float timescale = 1f, List<IUpdateTimeline.Event> events = null, int uid = 0)
        {
            return GetInstance().CreateUpdateTimelineInstance(new ScriptedUpdateTimeline(Convert.ToInt32(category), duration, loop, timescale, events, uid), GetUniqueRegistrationKey(), out handle);
        }

        /// <summary>
        /// Destroys the <see cref="UpdateTimelineInstance"/> with <paramref name="handle"/>.key
        /// </summary>
        public static void KillTimelineInstance(UpdateTimelineInstanceHandle handle) => KillTimelineInstance(handle.key);
        /// <summary>
        /// Destroys the <see cref="UpdateTimelineInstance"/> with <paramref name="key"/>
        /// </summary>
        public static void KillTimelineInstance(ulong key)
        {
            if (IsInstanceValid())
            {
                Instance.DestroyUpdateTimelineInstance(key);
            }
        }

        /// <summary>
        /// Attempts to get a handle for the first <see cref="UpdateTimelineInstance"/> of type <paramref name="timeline"/>
        /// </summary>
        public static bool GetFirstTimelineInstanceHandleOfType(IUpdateTimeline timeline, out UpdateTimelineInstanceHandle handle)
        {
            return GetInstance().TryGetUpdateTimelineInstanceHandle(timeline.UID, out handle);
        }

        #endregion

        #region Precise Frames Updates

        /// <summary>
        /// Register a callback to be called once on the next classic update (next frame)
        /// </summary>
        public static void CallOnNextUpdate(Action callback)
        {
            CallInXFrames(1, callback, out _);
        }
        /// <summary>
        /// Register a callback to be called once on this frame late update
        /// </summary>
        /// <remarks>
        /// If this frame's late update has already been called, call instantaneously
        /// </remarks>
        public static void CallOnLateUpdate(Action callback)
        {
            if (callback == null) return;

            CallInXFrames(0, callback, out _, EUpdatePass.LATE, EUpdateCondition.ALWAYS);
        }

        /// <summary>
        /// Register a callback to be called once in <paramref name="framesToWait"/> number of frames in the classic update
        /// </summary>
        public static void CallInXFrames(int framesToWait, Action callback, out ulong key, EUpdatePass pass = EUpdatePass.CLASSIC, EUpdateCondition condition = EUpdateCondition.ALWAYS)
        {
            if (callback == null || framesToWait < 0)
            {
                key = 0;
                return;
            }

            key = GetUniqueRegistrationKey();
            GetInstance().RegisterFrameDelayedCall(key, framesToWait, pass, condition, callback);
        }

        #endregion

        #region Delayed Calls

        public static void CallInXSeconds(float seconds, System.Action callback, out ulong key, EUpdatePass pass = EUpdatePass.CLASSIC, EUpdateCondition condition = EUpdateCondition.ALWAYS)
        {
            if (callback == null)
            {
                key = 0;
                return;
            }

            key = GetUniqueRegistrationKey();
            GetInstance().RegisterTimedDelayedCall(key, seconds, pass, condition, callback);
        }

        public static void KillDelayedCall(ulong key)
        {
            if (IsInstanceValid())
            {
                Instance.UnregisterDelayedCall(key);
            }
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

            if (IsInstanceValid())
                Instance.Clear();
        }

        #endregion
    }
}
