using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public class UpdaterInstance : MonoBehaviour
    {
        #region Initialization

        internal void Init(Action<bool> enableCallback, Action updateCallback, Action lateUpdateCallback, Action fixedUpdateCallback)
        {
            m_enableCallback = enableCallback;
            m_updateCallback = updateCallback;
            m_lateUpdateCallback = lateUpdateCallback;
            m_fixedUpdateCallback = fixedUpdateCallback;
        }

        #endregion

        #region Events

        private Action<bool> m_enableCallback;
        private Action m_updateCallback;
        private Action m_lateUpdateCallback;
        private Action m_fixedUpdateCallback;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            m_enableCallback?.Invoke(true);
        }
        private void OnDisable()
        {
            m_enableCallback.Invoke(false);
        }

        #endregion

        #region Update Methods

        private void Update()
        {
            m_updateCallback();
        }
        private void LateUpdate()
        {
            m_lateUpdateCallback();
        }
        private void FixedUpdate()
        {
            m_fixedUpdateCallback();
        }

        #endregion
    }
}
