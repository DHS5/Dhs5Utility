using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public interface IUpdaterOverrides
    {
        public bool OverrideConditionFulfillment(EUpdateCondition condition);
    }
}
