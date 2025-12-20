using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public class BaseSaveSubObject : ScriptableObject
    {
        #region Members

        [SerializeField, ReadOnly] private ESaveCategory m_category;

        #endregion

        #region Properties

        public ESaveCategory Category { get => m_category; protected set => m_category = value; }

        #endregion
    }
}
