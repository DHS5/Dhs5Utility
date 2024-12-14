using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDatabaseElement
{
    int UID { get; }

#if UNITY_EDITOR
    void Editor_SetUID(int uid);
    bool Editor_HasDatabaseElementTexture(out Texture2D texture);
    bool Editor_HasDatabaseElementName(out string name);
#endif
}
