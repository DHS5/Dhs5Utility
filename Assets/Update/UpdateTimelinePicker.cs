using Dhs5.Utility.Databases;
using System;

namespace Dhs5.Utility.Updates
{
    [Serializable]
    public class UpdateTimelinePicker : DataPicker<UpdateTimelineDatabase>
    {
        #region Methods

        public UpdateTimeline Get()
        {
            if (TryGetUpdateTimeline(out var timeline)) return timeline;
            return null;
        }
        public bool TryGetUpdateTimeline(out UpdateTimeline element)
        {
            return TryGetData(out element);
        }
        public bool TryCreateUpdateTimelineInstance(out UpdateTimelineInstanceHandle handle)
        {
            if (TryGetUpdateTimeline(out var element) &&
                BaseUpdater.CreateTimelineInstance(element, out handle))
            {
                return true;
            }
            handle = UpdateTimelineInstanceHandle.Empty;
            return false;
        }

        #endregion
    }
}
