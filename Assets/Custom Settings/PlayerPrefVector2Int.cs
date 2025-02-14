using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public class PlayerPrefVector2Int : PlayerPrefMember<Vector2Int>
    {
        public override void Load()
        {
            int x = PlayerPrefs.GetInt(Key + "X", Default.x);
            int y = PlayerPrefs.GetInt(Key + "Y", Default.y);
            m_current = new Vector2Int(x, y);
        }

        public override void Save(Vector2Int value)
        {
            PlayerPrefs.SetInt(Key + "X", value.x);
            PlayerPrefs.SetInt(Key + "Y", value.y);
        }
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(PlayerPrefVector2Int))]
    public class PlayerPrefVector2IntDrawer : PlayerPrefMemberDrawer
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
