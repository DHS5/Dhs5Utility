using UnityEngine;

namespace Dhs5.Utility.Debugger
{
    public interface IRuntimeDebugExtension
    {
#if UNITY_EDITOR

        void DrawRuntimeDebugExtensionGUI()
        {

        }

#endif
    }
}
