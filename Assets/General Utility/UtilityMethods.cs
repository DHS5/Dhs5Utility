using UnityEngine;

namespace Dhs5.Utility
{
    public static class UtilityMethods
    {
        #region Vectors

        public static float FlatDistance(Vector3 v1, Vector3 v2)
        {
            float num = v1.x - v2.x;
            float num2 = v1.z - v2.z;
            return (float)Mathf.Sqrt(num * num + num2 * num2);
        }

        #endregion
    }
}
