using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public class BaseSaveSubObject : ScriptableObject
    {
        #region Members

        [SerializeField] private ESaveCategory m_category;

        #endregion

        #region Properties

        public virtual ESaveCategory Category { get => m_category; set => m_category = value; }

        #endregion
    }
}
