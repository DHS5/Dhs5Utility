using System;
using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public abstract class SaveProcessModifier : ScriptableObject
    {
        public abstract bool TryHandleTypeDeserializingError(string typeName, out Type type);
    }
}
