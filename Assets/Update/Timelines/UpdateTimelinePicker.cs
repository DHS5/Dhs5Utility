using Dhs5.Utility.Databases;
using System;

namespace Dhs5.Utility.Updates
{
    [Serializable]
    public class UpdateTimelinePicker : DataPicker<UpdateTimelineDatabase>
    {
        #region Methods

        public UpdateTimelineObject Get()
        {
            if (TryGetUpdateTimeline(out var timeline)) return timeline;
            return null;
        }
        public bool TryGetUpdateTimeline(out UpdateTimelineObject element)
        {
            return TryGetData(out element);
        }

        #endregion
    }
}
