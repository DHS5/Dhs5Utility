using System;
using System.Collections;
using System.Collections.Generic;
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


        #region Registration Keys

        private static ulong _registrationCount = 0;
        protected static ulong GetUniqueRegistrationKey()
        {
            _registrationCount++;
            return _registrationCount;
        }

        #endregion

        #region Channels

        #region Registration

        public static void Register(bool register, UpdateEnum channel, UpdateCallback callback)
        {
            if (register)
            {
                GetInstance().RegisterChannelCallback(Convert.ToInt32(channel), callback);
            }
            else if (IsInstanceValid())
            {
                Instance.UnregisterChannelCallback(Convert.ToInt32(channel), callback);
            }
        }

        #endregion

        #region Setters

        public static void SetChannelEnabled(UpdateEnum channel, bool enabled)
        {
            if (IsInstanceValid())
            {
                Instance.SetChannelEnable(Convert.ToInt32(channel), enabled);
            }
        }
        public static void SetChannelTimescale(UpdateEnum channel, float timescale)
        {
            if (IsInstanceValid())
            {
                Instance.SetChannelTimescale(Convert.ToInt32(channel), timescale);
            }
        }

        #endregion

        #endregion

        #region Timelines Management

        /// <summary>
        /// Creates an Instance of <paramref name="timeline"/> and out a handle for it
        /// </summary>
        /// <returns>Whether the instance was successfully registered</returns>
        public static bool CreateTimelineInstance(IUpdateTimeline timeline, out UpdateTimelineInstanceHandle handle)
        {
            var key = GetUniqueRegistrationKey();
            if (GetInstance().CreateUpdateTimelineInstance(timeline, key))
            {
                handle = new(key);
                return true;
            }
            handle = UpdateTimelineInstanceHandle.Empty;
            return false;
        }
        /// <summary>
        /// Creates an <see cref="UpdateTimelineInstance"/> from the parameters and out a handle for it
        /// </summary>
        /// <returns>Whether the instance was successfully registered</returns>
        public static bool CreateTimelineInstance(UpdateEnum category, float duration, out UpdateTimelineInstanceHandle handle, bool loop = false, float timescale = 1f, List<IUpdateTimeline.Event> events = null, int uid = 0)
        {
            return CreateTimelineInstance(new ScriptedUpdateTimeline(Convert.ToInt32(category), duration, loop, timescale, events, uid), out handle);
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
            if (GetInstance().TryGetUpdateTimelineInstanceKey(timeline.UID, out var key))
            {
                handle = new(key);
                return true;
            }
            handle = UpdateTimelineInstanceHandle.Empty;
            return false;
        }

        #endregion

        #region Delayed Calls

        /// <summary>
        /// Register a callback to be called once in <paramref name="framesToWait"/> number of frames
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
        /// <summary>
        /// Register a callback to be called once in <paramref name="time"/> seconds
        /// </summary>
        public static void CallInXSeconds(float time, Action callback, out ulong key, EUpdatePass pass = EUpdatePass.CLASSIC, EUpdateCondition condition = EUpdateCondition.ALWAYS)
        {
            if (callback == null || time < 0f)
            {
                key = 0;
                return;
            }

            key = GetUniqueRegistrationKey();
            GetInstance().RegisterTimedDelayedCall(key, time, pass, condition, callback);
        }
        /// <summary>
        /// Register a callback to be called once <paramref name="predicate"/> becomes true
        /// </summary>
        public static void CallWhenTrue(Func<bool> predicate, Action callback, out ulong key, EUpdatePass pass = EUpdatePass.CLASSIC, EUpdateCondition condition = EUpdateCondition.ALWAYS)
        {
            if (callback == null || predicate == null)
            {
                key = 0;
                return;
            }

            key = GetUniqueRegistrationKey();
            GetInstance().RegisterWaitUntilDelayedCall(key, predicate, pass, condition, callback);
        }
        /// <summary>
        /// Register a callback to be called once <paramref name="predicate"/> becomes false
        /// </summary>
        public static void CallWhenFalse(Func<bool> predicate, Action callback, out ulong key, EUpdatePass pass = EUpdatePass.CLASSIC, EUpdateCondition condition = EUpdateCondition.ALWAYS)
        {
            if (callback == null || predicate == null)
            {
                key = 0;
                return;
            }

            key = GetUniqueRegistrationKey();
            GetInstance().RegisterWaitWhileDelayedCall(key, predicate, pass, condition, callback);
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

        public static void PauseTime(bool pause)
        {
            UnityEngine.Time.timeScale = pause ? 0f : 1f;
        }

        public static void Clear()
        {
            _registrationCount = 0;

            if (IsInstanceValid())
                Instance.Clear();
        }

        #endregion
    }
}
