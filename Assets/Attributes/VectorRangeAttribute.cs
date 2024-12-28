using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AttributeUsage(AttributeTargets.Field)]
public class VectorRangeAttribute : PropertyAttribute
{
    #region Members

    public readonly float min;
    public readonly float max;

    #endregion

    #region Constructor

    public VectorRangeAttribute(float min, float max)
    {
        this.min = min;
        this.max = Mathf.Max(min, max);
    }

    #endregion
}

#region Drawer

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(VectorRangeAttribute))]
public class VectorRangeAttributeDrawer : PropertyDrawer
{
    VectorRangeAttribute vectorRangeAttribute;

    #region GUI

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        vectorRangeAttribute = attribute as VectorRangeAttribute;

        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType == SerializedPropertyType.Vector2)
        {
            OnVector2GUI(position, property, label);
        }
        else if (property.propertyType == SerializedPropertyType.Vector2Int)
        {
            OnVector2IntGUI(position, property, label);
        }
        else
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        EditorGUI.EndProperty();
    }

    private void OnVector2GUI(Rect position, SerializedProperty property, GUIContent label)
    {
        float labelWidth = EditorGUIUtility.labelWidth;
        EditorGUI.LabelField(new Rect(position.x, position.y, labelWidth, 18f), label);

        float x = property.vector2Value.x;
        float y = property.vector2Value.y;

        x = EditorGUI.DelayedFloatField(new Rect(position.x + labelWidth + 2f, position.y, 42f, 18f), x);
        y = EditorGUI.DelayedFloatField(new Rect(position.x + position.width - 42f, position.y, 42f, 18f), y);

        EditorGUI.MinMaxSlider(new Rect(position.x + labelWidth + 49f, position.y, position.width - labelWidth - 98f, 18f),
            ref x, ref y, vectorRangeAttribute.min, vectorRangeAttribute.max);

        if (x != property.vector2Value.x || x < vectorRangeAttribute.min || y > vectorRangeAttribute.max)
        {
            x = Mathf.Clamp(Mathf.Round(x * 100f) / 100f, vectorRangeAttribute.min, vectorRangeAttribute.max);
        }
        if (y != property.vector2Value.y || y < vectorRangeAttribute.min || y > vectorRangeAttribute.max)
        {
            y = Mathf.Min(vectorRangeAttribute.max, Mathf.Round(Mathf.Max(x, y) * 100f) / 100f);
        }

        property.vector2Value = new Vector2(x, y);
    }
    private void OnVector2IntGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        float labelWidth = EditorGUIUtility.labelWidth;
        EditorGUI.LabelField(new Rect(position.x, position.y, labelWidth, 18f), label);

        int x = property.vector2IntValue.x;
        int y = property.vector2IntValue.y;

        x = EditorGUI.DelayedIntField(new Rect(position.x + labelWidth + 2f, position.y, 42f, 18f), x);
        y = EditorGUI.DelayedIntField(new Rect(position.x + position.width - 42f, position.y, 42f, 18f), y);

        float fx = x;
        float fy = y;

        EditorGUI.MinMaxSlider(new Rect(position.x + labelWidth + 49f, position.y, position.width - labelWidth - 98f, 18f),
            ref fx, ref fy, vectorRangeAttribute.min, vectorRangeAttribute.max);

        x = Mathf.CeilToInt(fx);
        y = Mathf.CeilToInt(fy);
        if (x != property.vector2IntValue.x || x < vectorRangeAttribute.min || x > vectorRangeAttribute.max)
        {
            x = (int)Mathf.Clamp(x, vectorRangeAttribute.min, vectorRangeAttribute.max);
        }
        if (y != property.vector2IntValue.y || y < vectorRangeAttribute.min || y > vectorRangeAttribute.max)
        {
            y = (int)Mathf.Min(vectorRangeAttribute.max, Mathf.Max(x, y));
        }

        property.vector2IntValue = new Vector2Int(x, y);
    }

    #endregion
}

#endif

#endregion
