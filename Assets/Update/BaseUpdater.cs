using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public class BaseUpdater
    {
        #region Instance Creation

        protected static UpdaterInstance Instance { get; set; }

        public static void Init<UpdaterInstanceType>() where UpdaterInstanceType : UpdaterInstance
        {
            CreateInstance<UpdaterInstanceType>();
        }
        protected static void CreateInstance<UpdaterInstanceType>() where UpdaterInstanceType : UpdaterInstance
        {
            if (Instance != null) return;

            var obj = new GameObject("Updater");
            GameObject.DontDestroyOnLoad(obj);

            Instance = obj.AddComponent<UpdaterInstanceType>();
        }


        protected static UpdaterInstance GetInstance()
        {
            if (!IsInstanceValid())
            {
                CreateInstance<UpdaterInstance>();
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

        public static event UpdateCallback OnEarlyUpdate { add { GetInstance().OnEarlyUpdate += value; } remove { if (IsInstanceValid()) GetInstance().OnEarlyUpdate -= value; } }
        public static event UpdateCallback OnUpdate { add { GetInstance().OnUpdate += value; } remove { if (IsInstanceValid()) GetInstance().OnUpdate -= value; } }
        public static event UpdateCallback OnLateUpdate { add { GetInstance().OnLateUpdate += value; } remove { if (IsInstanceValid()) GetInstance().OnLateUpdate -= value; } }

        public static event UpdateCallback OnFixedUpdate { add { GetInstance().OnFixedUpdate += value; } remove { if (IsInstanceValid()) GetInstance().OnFixedUpdate -= value; } }

        #endregion

        #region Callbacks Registration

        private static ulong _registrationCount = 0;
        protected static void ResetRegistrationCount() => _registrationCount = 0;

        protected static ulong GetUniqueRegistrationKey()
        {
            _registrationCount++;
            return _registrationCount;
        }

        protected static void RegisterCallback(int category, UpdateCallback callback)
        {
            GetInstance().RegisterCallback(category, callback);
        }
        protected static void UnregisterCallback(int category, UpdateCallback callback)
        {
            GetInstance().UnregisterCallback(category, callback);
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
        public static bool CreateTimelineInstance(int updateKey, float duration, out UpdateTimelineInstanceHandle handle, bool loop = false, float timescale = 1f, List<IUpdateTimeline.Event> events = null, int uid = 0)
        {
            return GetInstance().CreateUpdateTimelineInstance(new ScriptedUpdateTimeline(updateKey, duration, loop, timescale, events, uid), GetUniqueRegistrationKey(), out handle);
        }

        /// <summary>
        /// Destroys the <see cref="UpdateTimelineInstance"/> with <paramref name="handle"/>.key
        /// </summary>
        public static void DestroyTimelineInstance(UpdateTimelineInstanceHandle handle) => DestroyTimelineInstance(handle.key);
        /// <summary>
        /// Destroys the <see cref="UpdateTimelineInstance"/> with <paramref name="key"/>
        /// </summary>
        public static void DestroyTimelineInstance(ulong key)
        {
            if (IsInstanceValid())
            {
                Instance.DestroyUpdateTimelineInstance(key);
            }
        }

        /// <summary>
        /// Attempts to get a handle for the <see cref="UpdateTimelineInstance"/> with <paramref name="key"/>
        /// </summary>
        public static bool TryGetTimelineInstanceHandle(ulong key, out UpdateTimelineInstanceHandle handle)
        {
            return GetInstance().TryGetUpdateTimelineInstanceHandle(key, out handle);
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
            GetInstance().RegisterCallbackInXFrames(nextFrame, callback);
        }

        #endregion

        #region Delayed Calls

        public static void CallInXSeconds(float seconds, System.Action callback)
        {
            if (callback == null) return;

            GetInstance().RegisterDelayedCall(seconds, callback);
        }

        #endregion

        #region Update Conditions

        public static bool IsConditionFullfilled(EUpdateCondition condition)
        {
            switch (condition)
            {
                case EUpdateCondition.ALWAYS: return true;
                case EUpdateCondition.GAME_PLAYING: return UnityEngine.Time.timeScale > 0f;
                case EUpdateCondition.GAME_PAUSED: return UnityEngine.Time.timeScale == 0f;
            }
            return false;
        }

        #endregion


        #region Utility

        public static void Pause(bool pause)
        {
            UnityEngine.Time.timeScale = pause ? 0f : 1f;
        }

        #endregion
    }
}
