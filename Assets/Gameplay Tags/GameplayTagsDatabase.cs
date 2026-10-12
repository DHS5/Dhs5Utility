using Dhs5.Utility.OldDatabase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Tags
{
    [Database("Gameplay Tags", dataType:typeof(GameplayTag))]
    public class GameplayTagsDatabase : ScriptableDataContainer
    {
        #region Instance

        private static GameplayTagsDatabase _instance;
        /// <summary>
        /// The GameplayTagsDatabase asset in a Resources/Databases folder, null if there is none
        /// </summary>
        public static GameplayTagsDatabase I
        {
            get
            {
                if (_instance == null)
                {
                    var list = Resources.LoadAll<GameplayTagsDatabase>("Databases");
                    if (list != null && list.Length > 0)
                    {
                        _instance = list[0];
                    }
                }
                return _instance;
            }
        }

        #endregion

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
