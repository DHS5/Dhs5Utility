using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public class UpdaterAsset : ScriptableObject
    {
        #region Members

        [SerializeField] private bool m_used;

        #endregion

        #region Properties

        public bool Used => m_used;

        #endregion



        // --- EDITOR ---

#if UNITY_EDITOR

        #region Editor Setters

        public void Editor_SetUsed(bool used)
        {
            m_used = used;
        }

        #endregion

#endif
    }
}
