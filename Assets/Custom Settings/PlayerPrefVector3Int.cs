using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public class PlayerPrefVector3Int : PlayerPrefMember<Vector3Int>
    {
        public override void Load()
        {
            int x = PlayerPrefs.GetInt(Key + "X", Default.x);
            int y = PlayerPrefs.GetInt(Key + "Y", Default.y);
            int z = PlayerPrefs.GetInt(Key + "Z", Default.z);
            m_current = new Vector3Int(x, y, z);
        }

        public override void Save(Vector3Int value)
        {
            PlayerPrefs.SetInt(Key + "X", value.x);
            PlayerPrefs.SetInt(Key + "Y", value.y);
            PlayerPrefs.SetInt(Key + "Z", value.z);
        }
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(PlayerPrefVector3Int))]
    public class PlayerPrefVector3IntDrawer : PlayerPrefMemberDrawer
    {
        protected override void OnChangeKey(string formerKey)
        {
            if (!string.IsNullOrWhiteSpace(formerKey))
            {
                PlayerPrefs.DeleteKey(formerKey + "X");
                PlayerPrefs.DeleteKey(formerKey + "Y");
                PlayerPrefs.DeleteKey(formerKey + "Z");
            }
        }
    }

#endif
}
