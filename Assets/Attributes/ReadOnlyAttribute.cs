using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AttributeUsage(AttributeTargets.Field)]
public class ReadOnlyAttribute : PropertyAttribute
{
    #region Members

    public readonly string param = null;
    public readonly bool inverse = false;

    public readonly bool canWriteInEditor = false;
    public readonly bool canWriteAtRuntime = false;

    #endregion

    #region Constructor

    public ReadOnlyAttribute(bool canWriteInEditor = false, bool canWriteAtRuntime = false)
    {
        this.canWriteInEditor = canWriteInEditor;
        this.canWriteAtRuntime = canWriteAtRuntime;
    }
    public ReadOnlyAttribute(string param, bool inverse = false)
    {
        this.param = param;
        this.inverse = inverse;
    }

    #endregion
}

#region Drawer

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyAttributeDrawer : PropertyDrawer
{
    #region Members

    ReadOnlyAttribute m_readOnlyAttribute;

    #endregion

    #region GUI

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        EditorGUI.BeginDisabledGroup(Disable(property));
        EditorGUI.PropertyField(position, property, label, true);
        EditorGUI.EndDisabledGroup();

        EditorGUI.EndProperty();
    }

    #endregion

    #region GUI Height

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    #endregion

    #region Utility

    private bool Disable(SerializedProperty property)
    {
        m_readOnlyAttribute = attribute as ReadOnlyAttribute;
        if (!string.IsNullOrWhiteSpace(m_readOnlyAttribute.param)) // Has param
        {
            var p_param = property.serializedObject.FindProperty(m_readOnlyAttribute.param);
            if (p_param != null && p_param.propertyType == SerializedPropertyType.Boolean)
            {
                return m_readOnlyAttribute.inverse ? !p_param.boolValue : p_param.boolValue;
            }
        }

        return Application.isPlaying ? !m_readOnlyAttribute.canWriteAtRuntime : !m_readOnlyAttribute.canWriteInEditor;
    }

    #endregion
}

#endif

#endregion
