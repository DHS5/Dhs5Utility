using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public abstract class UpdateConditionObject : ScriptableObject
    {
        public const string MenuPath = "Dhs5 Utility/Updater/Update Condition Object/";

        public abstract bool CanUpdate(EUpdateChannel channel);
    }
}
