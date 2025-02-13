using System;
using UnityEngine;

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public class PlayerPrefInt : PlayerPrefMember<int>
    {
        public override void Load()
        {
            m_current = PlayerPrefs.GetInt(Key, Default);
        }
        public override void Save(int value)
        {
            PlayerPrefs.SetInt(Key, value);
        }
    }
}
