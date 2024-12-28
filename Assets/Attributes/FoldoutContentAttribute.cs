using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AttributeUsage(AttributeTargets.Field)]
public class FoldoutContentAttribute : PropertyAttribute
{

}

#region Drawer

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(FoldoutContentAttribute))]
public class FoldoutContentAttributeDrawer : PropertyDrawer
{
    #region GUI

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType == SerializedPropertyType.ObjectReference &&
            fieldInfo != null && fieldInfo.FieldType.IsSubclassOf(typeof(ScriptableObject)))
        {
            // Label
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, 18f);
            if (property.objectReferenceValue != null)
            {
                if (Event.current.type == EventType.MouseDown
                    && Event.current.button == 0
                    && labelRect.Contains(Event.current.mousePosition))
                {
                    property.isExpanded = !property.isExpanded;
                    if (!property.isExpanded) DisposeSerializedObject();
                    Event.current.Use();
                }
            }
            // Repaint
            if (Event.current.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(labelRect, label, 0, property.isExpanded);
            }
            //label.image = EditorGUIUtility.IconContent(property.isExpanded ? "HoverBar_Down" : "d_tab_next").image;
            //EditorGUI.LabelField(labelRect, label);

            // Base Property
            Rect basePropertyRect = new Rect(position.x + EditorGUIUtility.labelWidth + 2f, position.y, position.width - EditorGUIUtility.labelWidth - 2f, 18f);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(basePropertyRect, property, GUIContent.none, true);
            if (EditorGUI.EndChangeCheck())
            {
                if (property.objectReferenceValue == null)
                {
                    property.isExpanded = false;
                    DisposeSerializedObject();
                }
            }

            // Foldout Content
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                Rect rect = new Rect(position.x, position.y + 20f, position.width, 18f);
                SerializedObject serializedObject = GetSerializedObject(property);

                var objProperty = serializedObject.GetIterator();
                if (objProperty.Next(true))
                {
                    int count = 0; // Fail safe
                    while (objProperty.NextVisible(false) && count < 100)
                    {
                        if (objProperty.propertyPath != "m_Script")
                        {
                            EditorGUI.PropertyField(rect, objProperty, true);
                            rect.y += EditorGUI.GetPropertyHeight(objProperty) + 2f;
                        }
                        count++;
                    }
                }

                EditorGUI.indentLevel--;
                serializedObject.ApplyModifiedProperties();
            }
        }
        else
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        EditorGUI.EndProperty();
    }

    #endregion

    #region GUI Height

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.ObjectReference &&
            fieldInfo != null && fieldInfo.FieldType.IsSubclassOf(typeof(ScriptableObject)) &&
            property.isExpanded)
        {
            float height = 20f;

            SerializedObject serializedObject = GetSerializedObject(property);

            var objProperty = serializedObject.GetIterator();
            if (objProperty.Next(true))
            {
                int count = 0; // Fail safe
                while (objProperty.NextVisible(false) && count < 100)
                {
                    if (objProperty.propertyPath != "m_Script")
                        height += EditorGUI.GetPropertyHeight(objProperty) + 2f;
                    count++;
                }
            }

            return height;
        }
        return base.GetPropertyHeight(property, label);
    }

    #endregion

    #region Utility

    private static SerializedObject _serializedObject;

    private static SerializedObject GetSerializedObject(SerializedProperty property)
    {
        if (_serializedObject != null && _serializedObject.targetObject == property.objectReferenceValue)
        {
            _serializedObject.UpdateIfRequiredOrScript();
            return _serializedObject;
        }

        if (_serializedObject != null)
        {
            _serializedObject.Dispose();
        }

        _serializedObject = new SerializedObject(property.objectReferenceValue);
        return _serializedObject;
    }
    private static void DisposeSerializedObject()
    {
        if (_serializedObject != null)
        {
            _serializedObject.Dispose();
            _serializedObject = null;
        }
    }

    #endregion
}

#endif

#endregion
