using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FoldoutContentAttribute : PropertyAttribute
    {
        
    }

    #region Drawer

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(FoldoutContentAttribute))]
    public class FoldoutContentAttributeDrawer : PropertyDrawer
    {
        #region Members

        private Editor m_editor;

        #endregion

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
                        Event.current.Use();
                    }
                }
                else
                {
                    property.isExpanded = false;
                }

                label.image = EditorGUIUtility.IconContent(property.isExpanded ? "d_icon dropdown open" : "d_icon dropdown").image;
                EditorGUI.LabelField(labelRect, label);

                // Base Property
                Rect basePropertyRect = new Rect(position.x + EditorGUIUtility.labelWidth + 2f, position.y, position.width - EditorGUIUtility.labelWidth - 2f, 18f);
                EditorGUI.PropertyField(basePropertyRect, property, GUIContent.none, true);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        #endregion
    }

#endif

    #endregion
}
