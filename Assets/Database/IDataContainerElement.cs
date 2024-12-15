using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Databases
{
    public interface IDataContainerElement
    {
        int UID { get; }

#if UNITY_EDITOR
        void Editor_SetUID(int uid);
        bool Editor_HasDataContainerElementTexture(out Texture2D texture);
        bool Editor_HasDataContainerElementName(out string name);
#endif
    }
}
