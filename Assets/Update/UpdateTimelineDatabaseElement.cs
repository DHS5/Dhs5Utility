using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public class UpdateTimelineDatabaseElement : BaseDataContainerScriptableElement
    {
        #region Members

        [SerializeField] private DataPicker<UpdaterDatabase> m_update;
        [SerializeField] private float m_duration;
        [SerializeField] private bool m_loop;

        #endregion

        #region Properties

        public float Duration => m_duration;
        public bool Loop => m_loop;

        #endregion

        #region Accessors

        public bool HasValidUpdate(out int updateKey)
        {
            if (m_update.TryGetObject<UpdaterDatabaseElement>(out var elem))
            {
                updateKey = elem.EnumIndex;
                return true;
            }
            updateKey = -1;
            return false;
        }

        #endregion
    }
}
