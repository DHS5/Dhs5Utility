using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class EnabledValue<T>
{
    [SerializeField] private bool m_enabled;
    [SerializeField] private T m_value;

    public bool IsEnabled(out T value)
    {
        value = m_value;
        return m_enabled;
    }

    public void SetValue(bool enabled, T value)
    {
        m_enabled = enabled;
        m_value = value;
    }
}

#region Drawer

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(EnabledValue<>))]
public class EnabledValueDrawer : PropertyDrawer
{
    SerializedProperty p_enabled;
    SerializedProperty p_value;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        p_enabled = property.FindPropertyRelative("m_enabled");

        Rect rect = new Rect(position.x, position.y, position.width, 18f);

        EditorGUI.BeginProperty(position, label, property);

        p_enabled.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height), label, p_enabled.boolValue);
        property.isExpanded = p_enabled.boolValue;

        if (property.isExpanded)
        {
            p_value = property.FindPropertyRelative("m_value");
            if (p_value != null)
            {
                EditorGUI.PropertyField(rect, p_value, new GUIContent(" "), true);
            }
            else
            {
                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(rect, "Value is invalid");
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return property.isExpanded ?
            EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_value")) :
            18f;
    }
}

#endif

#endregion
