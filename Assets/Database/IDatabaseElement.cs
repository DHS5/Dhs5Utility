using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDatabaseElement
{
#if UNITY_EDITOR
    bool Editor_HasDatabaseElementTexture(out Texture2D texture);
    bool Editor_HasDatabaseElementName(out string name);
#endif
}
