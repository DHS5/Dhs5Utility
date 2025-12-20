using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public abstract class BaseSaveObject : ScriptableObject
    {
        #region Members

        [SerializeField] protected BaseSaveSubObject[] m_subObjects;

        protected Dictionary<ESaveCategory, BaseSaveSubObject> m_subObjectDictionary;

        #endregion

        #region Load

        public void Load()
        {
            
        }

        #endregion
    }
}
