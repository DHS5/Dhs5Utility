using UnityEngine;

namespace Dhs5.Utility.Updates
{
    [CreateAssetMenu(fileName = "UpdateConditionObject_Always", menuName = UpdateConditionObject.MenuPath + "Always")]
    public class UpdateConditionObject_Always : UpdateConditionObject
    {
        public override bool CanUpdate()
        {
            return true;
        }
    }
}
