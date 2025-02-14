using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public class PlayerPrefVector3 : PlayerPrefMember<Vector3>
    {
        public override void Load()
        {
            float x = PlayerPrefs.GetFloat(Key + "X", Default.x);
            float y = PlayerPrefs.GetFloat(Key + "Y", Default.y);
            float z = PlayerPrefs.GetFloat(Key + "Z", Default.z);
            m_current = new Vector3(x, y, z);
        }

        public override void Save(Vector3 value)
        {
            PlayerPrefs.SetFloat(Key + "X", value.x);
            PlayerPrefs.SetFloat(Key + "Y", value.y);
            PlayerPrefs.SetFloat(Key + "Z", value.z);
        }
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(PlayerPrefVector3))]
    public class PlayerPrefVector3Drawer : PlayerPrefMemberDrawer
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
