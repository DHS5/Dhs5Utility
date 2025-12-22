using System.Collections;
using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public class LoadProcessObject : MonoBehaviour
    {
        #region Members

        private Coroutine m_coroutine;

        #endregion

        #region Core Behaviour

        private void OnDisable()
        {
            StopLoadProcessCoroutine();
        }

        #endregion

        #region Methods

        public void StartLoadProcessCoroutine(IEnumerator enumerator)
        {
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
