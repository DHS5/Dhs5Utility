using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public abstract class PlayerPrefMember<T>
    {
        #region Members

        [SerializeField] private string m_key;
        [SerializeField] private T m_default;
        [SerializeField] protected T m_current;

        #endregion

        #region Properties

        protected virtual string Key => m_key;
        protected T Default => m_default;
        public T Value
        {
            get => Application.isPlaying ? m_current : m_default;
            set
            {
                m_current = value;
                Save(m_current);
            }
        }

        #endregion

        #region Methods

        public abstract void Load();
        public abstract void Save(T value);

        #endregion

        #region Helpers

        public static implicit operator T(PlayerPrefMember<T> member)
        {
            return member.Value;
        }
        public override string ToString()
        {
            return Value.ToString();
        }

        #endregion
    }

    #region Drawer

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(PlayerPrefMember<>), useForChildren:true)]
    public class PlayerPrefMemberDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var p_value = property.FindPropertyRelative(Application.isPlaying ? "m_current" : "m_default");

            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new(position.x, position.y, EditorGUIUtility.labelWidth, 18f);
            Rect valueRect = new(position.x + EditorGUIUtility.labelWidth + 2f, position.y, position.width - EditorGUIUtility.labelWidth - 2f, 18f);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUI.PropertyField(valueRect, p_value, GUIContent.none, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                Rect keyRect = new(position.x, position.y + Mathf.Max(20f, EditorGUI.GetPropertyHeight(p_value, GUIContent.none)), position.width, 18f);
                var p_key = property.FindPropertyRelative("m_key");
                string key = EditorGUI.DelayedTextField(keyRect, "Key", p_key.stringValue);
                if (key != p_key.stringValue)
                {
                    OnChangeKey(p_key.stringValue);
                    p_key.stringValue = key;
                }
                if (string.IsNullOrWhiteSpace(key))
                {
                    p_key.stringValue = GetDefaultKey(property);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndProperty();
        }

        protected virtual void OnChangeKey(string formerKey)
        {
            if (!string.IsNullOrWhiteSpace(formerKey))
            {
                PlayerPrefs.DeleteKey(formerKey);
            }
        }
        protected virtual string GetDefaultKey(SerializedProperty property)
        {
            return property.displayName;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var p_value = property.FindPropertyRelative(Application.isPlaying ? "m_current" : "m_default");
            return Mathf.Max(20f, EditorGUI.GetPropertyHeight(p_value, GUIContent.none)) + (property.isExpanded ? 20 : 0f);
        }
    }

#endif

    #endregion
}
