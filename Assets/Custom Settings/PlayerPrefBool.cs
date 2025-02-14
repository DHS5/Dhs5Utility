using System;
using UnityEngine;

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public class PlayerPrefBool : PlayerPrefMember<bool>
    {
        public override void Load()
        {
            m_current = PlayerPrefs.GetInt(Key, Default ? 1 : 0) == 1;
        }

        public override void Save(bool value)
        {
            PlayerPrefs.SetInt(Key, value ? 1 : 0);
        }
    }
}
