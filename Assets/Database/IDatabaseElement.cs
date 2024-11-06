using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDatabaseElement
{
    bool HasDatabaseElementTexture(out Texture2D texture);
    bool HasDatabaseElementName(out string name);
}
