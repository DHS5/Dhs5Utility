using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public class PlayerPrefVector2 : PlayerPrefMember<Vector2>
    {
        public override void Load()
        {
            float x = PlayerPrefs.GetFloat(Key + "X", Default.x);
            float y = PlayerPrefs.GetFloat(Key + "Y", Default.y);
            m_current = new Vector2(x, y);
        }

        public override void Save(Vector2 value)
        {
            PlayerPrefs.SetFloat(Key + "X", value.x);
            PlayerPrefs.SetFloat(Key + "Y", value.y);
        }
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(PlayerPrefVector2))]
    public class PlayerPrefVector2Drawer : PlayerPrefMemberDrawer
    {
        protected override void OnChangeKey(string formerKey)
        {
            if (!string.IsNullOrWhiteSpace(formerKey))
            {
                PlayerPrefs.DeleteKey(formerKey + "X");
                PlayerPrefs.DeleteKey(formerKey + "Y");
            }
        }
    }

#endif
}
