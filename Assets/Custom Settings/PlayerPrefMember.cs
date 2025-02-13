using System;
using UnityEditor;
using UnityEngine;

namespace Dhs5.Utility.Settings
{
    [Serializable]
    public abstract class PlayerPrefMember<T>
    {
        [SerializeField] private string m_key;
        [SerializeField] private T m_default;
        [SerializeField] protected T m_current;

        protected virtual string Key => m_key;
        protected T Default => m_default;
        public T Value
        {
            get => m_current;
            set
            {
                m_current = value;
                Save(m_current);
            }
        }

        public abstract void Load();
        public abstract void Save(T value);
    }

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
                Rect keyRect = new(position.x, position.y + 20f, position.width, 18f);
                EditorGUI.PropertyField(keyRect, property.FindPropertyRelative("m_key"), true);
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded ? 40f : 20f;
        }
    }

#endif
}
