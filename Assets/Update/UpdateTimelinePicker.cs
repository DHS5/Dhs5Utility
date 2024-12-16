using Dhs5.Utility.Databases;
using System;

namespace Dhs5.Utility.Updates
{
    [Serializable]
    public class UpdateTimelinePicker : DataPicker<UpdateTimelineDatabase>
    {
        #region Methods

        public bool TryGetUpdateTimeline(out UpdateTimelineDatabaseElement element)
        {
            return TryGetObject(out element);
        }
        public bool TryGetUpdateTimelineHandle(out UpdateTimelineHandle handle)
        {
            if (TryGetUpdateTimeline(out var element) &&
                BaseUpdater.TryGetUpdateTimelineHandle(element, out handle))
            {
                return true;
            }
            handle = UpdateTimelineHandle.Empty;
            return false;
        }

        #endregion
    }
}
