using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public class Updater : MonoBehaviour
    {
        public delegate void UpdateCallback(float deltaTime);

        #region ENUM : Pass

        public enum Pass
        {
            CLASSIC = 0,
            EARLY,
            LATE,

            FIXED,

            PRE_INPUT,
            POST_INPUT,
        }

        #endregion

        #region INSTANCE

        #region Update Methods

        // UNITY
        private void Update()
        {
            Time = UnityEngine.Time.time;
            DeltaTime = UnityEngine.Time.deltaTime;
            
            RealTime = UnityEngine.Time.realtimeSinceStartup;
            RealDeltaTime = UnityEngine.Time.unscaledDeltaTime;

            GamePaused = DeltaTime != 0f;

            EarlyUpdate();
            ClassicUpdate();
        }
        private void LateUpdate()
        {
            OnLateUpdate?.Invoke(DeltaTime);
        }
        private void FixedUpdate()
        {
            OnFixedUpdate?.Invoke(UnityEngine.Time.fixedDeltaTime);
        }

        // INPUT
        private void PreInputUpdate()
        {
            OnBeforeInputUpdate?.Invoke(DeltaTime);
        }
        private void PostInputUpdate()
        {
            OnAfterInputUpdate?.Invoke(DeltaTime);
        }

        // CUSTOM
        private void EarlyUpdate()
        {
            OnEarlyUpdate?.Invoke(DeltaTime);
        }
        private void ClassicUpdate()
        {
            OnUpdate?.Invoke(DeltaTime);
        }

        #endregion


        #region Default Updates Registration

        #endregion

        #region Next Updates

        #endregion

        #endregion

        // ---------- ---------- ---------- 

        #region STATIC

        #region Instance Creation

        private static Updater Instance { get; set; }

        private static void CreateInstance()
        {
            if (Instance != null) return;

            var obj = new GameObject("Updater");
            DontDestroyOnLoad(obj);

            Instance = obj.AddComponent<Updater>();
        }

        private static Updater GetInstance()
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

        #endregion

        #endregion
    }
}
