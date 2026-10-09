using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    /// <summary>
    /// General info of a save file, serialized with <see cref="JsonUtility"/>.<br></br>
    /// UnityEngine.Object references (assets, GameObjects, components...) CAN'T be saved :
    /// they are serialized as instance IDs that are only valid during the current session.
    /// Save identifiers (UIDs, names, paths...) instead and resolve them when loading.<br></br>
    /// A loaded save info is destroyed when the current save is replaced (next save or load) :
    /// copy its data, never keep or use it as live data.
    /// </summary>
    public abstract class BaseSaveInfo : ScriptableObject
    {

    }
}
