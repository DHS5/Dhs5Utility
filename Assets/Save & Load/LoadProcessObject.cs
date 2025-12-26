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

        #endregion

        #region Core Behaviour

        private void OnDisable()
        {
            if (m_coroutine != null)
            {
                StopLoadProcessCoroutine();
                m_onInterrupted?.Invoke();
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
