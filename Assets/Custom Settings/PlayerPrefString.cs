using System;
using UnityEngine;

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public class PlayerPrefString : PlayerPrefMember<string>
    {
        protected override string LoadValue()
        {
            return PlayerPrefs.GetString(Key, Default);
        }

        public override void Save(string value)
        {
            PlayerPrefs.SetString(Key, value);
        }
    }
}
