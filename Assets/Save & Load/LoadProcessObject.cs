using System;
using System.Collections;
using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public class LoadProcessObject : MonoBehaviour
    {
        #region Members

        private Coroutine m_coroutine;

        private Action m_onInterrupted;
        private bool m_isQuitting;

        #endregion

        #region Core Behaviour

        private void OnApplicationQuit()
        {
            // Called before OnDisable when quitting (or exiting play mode) : the load is not interrupted by the game
            m_isQuitting = true;
        }

        private void OnDisable()
        {
            if (m_coroutine != null)
            {
                StopLoadProcessCoroutine();
                if (!m_isQuitting)
                {
                    m_onInterrupted?.Invoke();
                }
            }
        }

        #endregion

        #region Methods

        public void StartLoadProcessCoroutine(IEnumerator enumerator, Action onInterrupted)
        {
            m_onInterrupted = onInterrupted;
            m_coroutine = StartCoroutine(enumerator);
        }

        public void StopLoadProcessCoroutine()
        {
            if (m_coroutine != null)
            {
                StopCoroutine(m_coroutine);
                m_coroutine = null;
            }
        }

        #endregion
    }
}
