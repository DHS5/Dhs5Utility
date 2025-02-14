using System;
using UnityEngine;

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public class PlayerPrefFloat : PlayerPrefMember<float>
    {
        public override void Load()
        {
            m_current = PlayerPrefs.GetFloat(Key, Default);
        }

        public override void Save(float value)
        {
            PlayerPrefs.SetFloat(Key, value);
        }
    }
}
