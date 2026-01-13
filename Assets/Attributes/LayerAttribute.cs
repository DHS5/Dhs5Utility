using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class LayerAttribute : PropertyAttribute { }

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(LayerAttribute))]
public class LayerAttributeDrawer : PropertyDrawer
{
    #region GUI

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType != SerializedPropertyType.Integer)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
        else
        {
            property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        }

        EditorGUI.EndProperty();
    }

    #endregion
}

#endif
