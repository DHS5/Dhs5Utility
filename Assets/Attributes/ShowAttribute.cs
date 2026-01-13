using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ShowAttribute : PropertyAttribute
{
    #region Members

    public readonly string param;
    public readonly bool inverse;

    #endregion

    #region Constructor

    public ShowAttribute(string param, bool inverse = false)
    {
        this.param = param;
        this.inverse = inverse;
    }

    #endregion
}

#region Drawer

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(ShowAttribute))]
public class ShowAttributeDrawer : PropertyDrawer
{
    #region GUI

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (Show(property))
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
        }
    }

    #endregion

    #region GUI Height

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return Show(property) ? base.GetPropertyHeight(property, label) : 0f;
    }

    #endregion

    #region Utility

    private bool Show(SerializedProperty property)
    {
        ShowAttribute showAttribute = attribute as ShowAttribute;

        if (!string.IsNullOrWhiteSpace(showAttribute.param))
        {
            var p_param = property.serializedObject.FindProperty(showAttribute.param);
            if (p_param != null && p_param.propertyType == SerializedPropertyType.Boolean)
            {
                return showAttribute.inverse ? !p_param.boolValue : p_param.boolValue;
            }
        }

        return true;
    }

    #endregion
}

#endif

#endregion