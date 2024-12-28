using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

[AttributeUsage(AttributeTargets.Field)]
public class CreatorAttribute : PropertyAttribute
{

}

#region Drawer

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(CreatorAttribute))]
public class CreatorAttributeDrawer : PropertyDrawer
{
    #region GUI

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType == SerializedPropertyType.ObjectReference)
        {
            if (TryGetCreatedObject(out var obj))
            {
                property.objectReferenceValue = obj;
            }

            if (fieldInfo != null && fieldInfo.FieldType.IsSubclassOf(typeof(ScriptableObject)))
            {
                Type fieldType = fieldInfo.FieldType;

                float buttonWidth = 70f;

                Rect basePropertyRect = new Rect(position.x, position.y, position.width - buttonWidth, 18f);
                OnBasePropertyGUI(basePropertyRect, property, label);

                Rect buttonRect = new Rect(position.width - buttonWidth, position.y, buttonWidth, 18f);
                if (fieldType.IsAbstract)
                {
                    OnAbstractCreatorButtonGUI(buttonRect, fieldType);
                }
                else
                {
                    OnCreatorButtonGUI(buttonRect, fieldType);
                }
                EditorGUI.EndProperty();
                return;
            }
        }
        EditorGUI.PropertyField(position, property, label, true);

        EditorGUI.EndProperty();
    }

    private void OnBasePropertyGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(rect, property, label, true);
    }


    private void OnAbstractCreatorButtonGUI(Rect rect, Type type)
    {
        GUIContent content = EditorGUIUtility.IconContent("d_icon dropdown");
        content.text = "Create";
        if (GUI.Button(rect, content))
        {
            var childTypes = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .Where(t => t.IsSubclassOf(type) && !t.IsAbstract)
                            .ToArray();
            if (childTypes.Length > 0)
            {
                var menu = new GenericMenu();

                for (int i = 0; i < childTypes.Length; i++)
                {
                    menu.AddItem(new GUIContent(childTypes[i].Name), false, CreateNewInstance, childTypes[i]);
                }

                menu.ShowAsContext();
            }
        }
    }
    private void OnCreatorButtonGUI(Rect rect, Type type)
    {
        if (GUI.Button(rect, "Create"))
        {
            CreateNewInstanceOfType(type);
        }
    }

    #endregion

    #region Utility

    private void CreateNewInstance(object obj)
    {
        CreateNewInstanceOfType(obj as Type);
    }
    private void CreateNewInstanceOfType(Type type)
    {
        ScriptableObject obj = ScriptableObject.CreateInstance(type);

        EditorApplication.delayCall += () =>
        {
            string path = EditorUtility.SaveFilePanelInProject("New asset location", "New", "asset", "Choose location to create the instance");
            if (!string.IsNullOrEmpty(path))
            {
                _createdObject = obj;

                AssetDatabase.CreateAsset(obj, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(obj);
            }
        };
    }

    #endregion

    #region Static Helpers

    private static ScriptableObject _createdObject;

    private static bool TryGetCreatedObject(out ScriptableObject so)
    {
        if (_createdObject != null)
        {
            so = _createdObject;
            _createdObject = null;
            return true;
        }
        so = null;
        return false;
    }

    #endregion
}

#endif

#endregion
