using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    /// <summary>
    /// Data of one <see cref="ESaveCategory"/> in a save file, serialized with <see cref="JsonUtility"/>.<br></br>
    /// UnityEngine.Object references (assets, GameObjects, components...) CAN'T be saved :
    /// they are serialized as instance IDs that are only valid during the current session.
    /// Save identifiers (UIDs, names, paths...) instead and resolve them when loading.<br></br>
    /// Loaded sub objects are destroyed when the current save is replaced (next save or load) :
    /// copy their data in <see cref="ILoadable.LoadCoroutine"/>, never keep or use them as live data.
    /// </summary>
    public class BaseSaveSubObject : ScriptableObject
    {
        #region Members

        [SerializeField] private ESaveCategory m_category;

        #endregion

        #region Properties

        public ESaveCategory Category { get => m_category; set => m_category = value; }

        #endregion
    }
}
