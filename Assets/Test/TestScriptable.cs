using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScriptable : ScriptableObject, IDatabaseElement
{
    public bool HasDatabaseElementName(out string name)
    {
        name = this.name;
        return true;
    }

    public bool HasDatabaseElementTexture(out Texture2D texture)
    {
        texture = null;
        return false;
    }
}
