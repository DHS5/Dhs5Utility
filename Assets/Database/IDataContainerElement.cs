using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDataContainerElement
{
    int UID { get; }

#if UNITY_EDITOR
    void Editor_SetUID(int uid);
    bool Editor_HasDataContainerElementTexture(out Texture2D texture);
    bool Editor_HasDataContainerElementName(out string name);
#endif
}
