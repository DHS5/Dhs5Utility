using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Tags
{
    [Database("Gameplay Tags", dataType:typeof(GameplayTag))]
    public class GameplayTagsDatabase : ScriptableDataContainer
    {
        #region Editor Utility

#if UNITY_EDITOR

        public string Editor_GetTagNameByUID(int uid)
        {
            foreach (var elem in Editor_GetContainerElements<IDataContainerElement>())
            {
                if (elem.UID == uid)
                {
                    return elem.name;
                }
            }
            return null;
        }

#endif

        #endregion
    }
}
