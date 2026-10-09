using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Settings
{
    /// <summary>
    /// Counts play sessions : PlayerPrefMembers load their value again in each session (needed when domain reload is disabled)
    /// </summary>
    internal static class PlayerPrefMemberSession
    {
        internal static int Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnPlaySessionStart()
        {
            Current++;
        }
    }

    /// <summary>
    /// Value with a default set in the editor and a current value saved in the PlayerPrefs.<br></br>
    /// In play mode, <see cref="Value"/> loads the saved value on first access. Outside of play mode, it's the default value.
    /// </summary>
    [Serializable]
    public abstract class PlayerPrefMember<T>
    {
        #region Members

        [SerializeField] private string m_key;
        [SerializeField] private T m_default;
        /// <summary>
        /// Not serialized : the settings asset must not keep the values of a play session
        /// </summary>
        [NonSerialized] protected T m_current;
        /// <summary>
        /// Play session in which <see cref="m_current"/> was loaded, -1 if never
        /// </summary>
        [NonSerialized] private int m_loadedSession = -1;

        #endregion

        #region Properties

        protected virtual string Key => m_key;
        protected T Default => m_default;
        public T Value
        {
            get
            {
                if (!Application.isPlaying) return m_default;

                if (m_loadedSession != PlayerPrefMemberSession.Current)
                {
                    Load();
                }
                return m_current;
            }
            set
            {
                m_current = value;
                m_loadedSession = PlayerPrefMemberSession.Current;
                Save(m_current);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the saved value (or the default value if none) : called automatically on the first access to <see cref="Value"/> in play mode
        /// </summary>
        public void Load()
        {
            m_current = LoadValue();
            m_loadedSession = PlayerPrefMemberSession.Current;
        }
        /// <returns>The value saved in the PlayerPrefs, or <see cref="Default"/> if none</returns>
        protected abstract T LoadValue();
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
            var p_default = property.FindPropertyRelative("m_default");
            var p_key = property.FindPropertyRelative("m_key");

            // The key is set as soon as the member is drawn, not only when the foldout is opened
            // (never with several objects selected : stringValue only reflects the first one, the others' keys would be overwritten)
            if (!property.serializedObject.isEditingMultipleObjects
                && string.IsNullOrWhiteSpace(p_key.stringValue))
            {
                p_key.stringValue = GetDefaultKey(property);
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new(position.x, position.y, EditorGUIUtility.labelWidth, 18f);
            Rect valueRect = new(position.x + EditorGUIUtility.labelWidth + 2f, position.y, position.width - EditorGUIUtility.labelWidth - 2f, 18f);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            if (Application.isPlaying)
            {
                // The current value isn't serialized : read from the member itself
                EditorGUI.LabelField(valueRect, GetCurrentValueString(property));
            }
            else
            {
                EditorGUI.PropertyField(valueRect, p_default, GUIContent.none, true);
            }

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                Rect keyRect = new(position.x, position.y + GetValueHeight(p_default), position.width, 18f);
                string key = EditorGUI.DelayedTextField(keyRect, "Key", p_key.stringValue);
                if (key != p_key.stringValue)
                {
                    OnChangeKey(p_key.stringValue);
                    p_key.stringValue = string.IsNullOrWhiteSpace(key) ? GetDefaultKey(property) : key;
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
            return GetDefaultKey(property.serializedObject.targetObject, property.propertyPath);
        }
        /// <summary>
        /// Owner type + property path (e.g. TestSettings.m_volume) : unique across settings classes
        /// </summary>
        internal static string GetDefaultKey(UnityEngine.Object owner, string propertyPath)
        {
            return owner.GetType().Name + "." + propertyPath;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetValueHeight(property.FindPropertyRelative("m_default")) + (property.isExpanded ? 20 : 0f);
        }
        private float GetValueHeight(SerializedProperty p_default)
        {
            return Application.isPlaying ? 20f : Mathf.Max(20f, EditorGUI.GetPropertyHeight(p_default, GUIContent.none));
        }

        private static string GetCurrentValueString(SerializedProperty property)
        {
            try
            {
                var member = GetMemberObject(property);
                var value = member?.GetType().GetProperty("Value")?.GetValue(member);
                return value != null ? value.ToString() : "null";
            }
            catch (Exception e)
            {
                return e.GetType().Name;
            }
        }
        /// <summary>
        /// Whether <paramref name="obj"/> is a PlayerPrefMember (of any type)
        /// </summary>
        internal static bool IsPlayerPrefMember(object obj)
        {
            for (var type = obj?.GetType(); type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PlayerPrefMember<>)) return true;
            }
            return false;
        }

        /// <summary>
        /// The object behind <paramref name="property"/>, found by following its path from the target object
        /// </summary>
        internal static object GetMemberObject(SerializedProperty property)
        {
            object current = property.serializedObject.targetObject;
            var path = property.propertyPath.Replace(".Array.data[", "[");
            foreach (var element in path.Split('.'))
            {
                if (current == null) return null;

                var bracket = element.IndexOf('[');
                var fieldName = bracket >= 0 ? element.Substring(0, bracket) : element;
                current = GetFieldValue(current, fieldName);

                if (bracket >= 0 && current is System.Collections.IList list)
                {
                    var index = int.Parse(element.Substring(bracket + 1, element.Length - bracket - 2));
                    current = index < list.Count ? list[index] : null;
                }
            }
            return current;
        }
        private static object GetFieldValue(object source, string fieldName)
        {
            for (var type = source.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (field != null) return field.GetValue(source);
            }
            return null;
        }
    }

#endif

    #endregion
}
