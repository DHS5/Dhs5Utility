using System;
using UnityEngine;

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public class PlayerPrefFloat : PlayerPrefMember<float>
    {
        protected override float LoadValue()
        {
            return PlayerPrefs.GetFloat(Key, Default);
        }

        public override void Save(float value)
        {
            PlayerPrefs.SetFloat(Key, value);
        }
    }
}
