using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public interface IUpdaterOverrider
    {
        /// <param name="condition"></param>
        /// <param name="fulfilled"></param>
        /// <returns>Whether to override the Updater conditions fulfillment</returns>
        public bool OverrideConditionFulfillment(EUpdateCondition condition, out bool fulfilled);
    }
}
