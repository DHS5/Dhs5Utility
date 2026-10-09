using System;
using UnityEngine;

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public class PlayerPrefInt : PlayerPrefMember<int>
    {
        protected override int LoadValue()
        {
            return PlayerPrefs.GetInt(Key, Default);
        }
        public override void Save(int value)
        {
            PlayerPrefs.SetInt(Key, value);
        }
    }
}
