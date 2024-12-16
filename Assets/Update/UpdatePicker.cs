using Dhs5.Utility.Databases;
using System;

namespace Dhs5.Utility.Updates
{
    [Serializable]
    public class UpdatePicker : DataPicker<UpdaterDatabase>
    {
        #region Methods

        public bool TryGetUpdaterElement(out UpdaterDatabaseElement element)
        {
            return TryGetObject(out element);
        }
        public bool TryGetUpdateKey(out int updateKey)
        {
            if (TryGetUpdaterElement(out var element))
            {
                updateKey = element.EnumIndex;
                return true;
            }
            updateKey = -1;
            return false;
        }
        
        #endregion
    }
}
