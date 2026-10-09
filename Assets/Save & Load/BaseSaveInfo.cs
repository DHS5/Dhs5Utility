using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    /// <summary>
    /// General info of a save file, serialized with <see cref="JsonUtility"/>.<br></br>
    /// UnityEngine.Object references (assets, GameObjects, components...) CAN'T be saved :
    /// they are serialized as instance IDs that are only valid during the current session.
    /// Save identifiers (UIDs, names, paths...) instead and resolve them when loading.
    /// </summary>
    public abstract class BaseSaveInfo : ScriptableObject
    {

    }
}
