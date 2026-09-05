using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.NewDatabase
{
    public interface IContainerElement
    {
        #region Properties

        public int UID { get; }
        public string Alias { get; }

        #endregion

        #region Editor Setters

#if UNITY_EDITOR

        public void Editor_SetUID(int UID);

#endif

        #endregion


        #region STATIC UID Computation

#if UNITY_EDITOR

        public static int ComputeContainerElementUID(UnityEngine.Object obj, int offset = 0)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out _))
            {
                return unchecked(guid.GetHashCode() + offset);
            }

            Debug.LogError("Could not compute UID for IContainerElement : " + obj, obj);
            return 0;
        }

#endif

        #endregion
    }
}
