using System;
using UnityEngine;

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public class PlayerPrefString : PlayerPrefMember<string>
    {
        public override void Load()
        {
            m_current = PlayerPrefs.GetString(Key, Default);
        }

        public override void Save(string value)
        {
            PlayerPrefs.SetString(Key, value);
        }
    }
}
