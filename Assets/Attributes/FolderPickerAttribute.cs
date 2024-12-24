using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace Dhs5.Utility.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FolderPickerAttribute : PropertyAttribute
    {
        #region Members

        public readonly bool hasValidRoot;
        public readonly string root;

        #endregion

        #region Properties

        public string DefaultRoot => hasValidRoot ? root : "Assets";

        #endregion

        #region Constructors

        public FolderPickerAttribute()
        {
            this.hasValidRoot = false;
        }
        public FolderPickerAttribute(string root)
        {
            this.hasValidRoot = root.StartsWith("Assets") && Directory.Exists(root);
            if (hasValidRoot)
            {
                this.root = root;
            }
        }

        #endregion
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(FolderPickerAttribute))]
    public class FolderPickerAttributeDrawer : PropertyDrawer
    {
        FolderPickerAttribute folderPickerAttribute;

        #region GUI

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            folderPickerAttribute = attribute as FolderPickerAttribute;
            string defaultRoot = folderPickerAttribute.DefaultRoot;

            EditorGUI.BeginProperty(position, label, property);

            // --- FOLDER PATH ---
            if (property.propertyType == SerializedPropertyType.String)
            {
                // --- RECTS COMPUTATIONS ---
                float buttonsWidth = 32f;

                Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, 20f);
                Rect folderRect = new Rect(labelRect.x + labelRect.width + 2f, position.y, position.width - labelRect.width - buttonsWidth - 2f, 20f);
                Rect folderButtonRect = new Rect(folderRect.x + folderRect.width, position.y, buttonsWidth, 20f);

                // --- CONTEXT MENU ---
                if (Event.current.type == EventType.ContextClick
                    && folderRect.Contains(Event.current.mousePosition))
                {
                    var menu = new GenericMenu();

                    menu.AddItem(new GUIContent("Copy path"), false, () => GUIUtility.systemCopyBuffer = property.stringValue);
                    menu.AddItem(new GUIContent("Ping folder"), false, () => PingFolder(property.stringValue));

                    menu.ShowAsContext();

                    Event.current.Use();
                }

                // --- GUI ---
                // Label
                EditorGUI.LabelField(labelRect, label);

                // Label path
                GUI.Box(folderRect, GUIContent.none, EditorStyles.helpBox);
                EditorGUI.SelectableLabel(new Rect(folderRect.x - EditorGUI.indentLevel * 15f + 4f, folderRect.y, folderRect.width - 6f, folderRect.height), property.stringValue);
                if (!property.stringValue.StartsWith(defaultRoot) || !Directory.Exists(property.stringValue))
                {
                    GUI.changed = property.stringValue != defaultRoot;
                    property.stringValue = defaultRoot;
                }

                // Folder Button
                if (GUI.Button(folderButtonRect, EditorGUIUtility.IconContent("FolderOpened On Icon")))
                {
                    string path = EditorUtility.OpenFolderPanel(label.text, property.stringValue, defaultRoot);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        if (path.Contains("Assets"))
                        {
                            path = path.Substring(path.IndexOf("Assets"));
                        }
                        else
                        {
                            path = defaultRoot;
                        }
                        GUI.changed = property.stringValue != path;
                        property.stringValue = path;
                    }
                
                    GUI.FocusControl(null);
                }
            }

            // --- FOLDER PATH ---
            else if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                FieldInfo fieldInfo = property.serializedObject.targetObject.GetType().GetField(property.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (fieldInfo != null && fieldInfo.FieldType == typeof(UnityEngine.Object))
                {
                    float buttonsWidth = 32f;

                    // Label
                    Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, 18f);
                    EditorGUI.LabelField(labelRect, label);
                    // Object
                    Rect objectRect = new Rect(position.x + EditorGUIUtility.labelWidth + 2f, position.y, position.width - EditorGUIUtility.labelWidth - 2f - buttonsWidth, 18f);

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.PropertyField(objectRect, property, GUIContent.none, true);
                    EditorGUI.EndDisabledGroup();

                    // Folder Button
                    Rect folderButtonRect = new Rect(objectRect.x + objectRect.width, position.y, buttonsWidth, 20f);

                    if (GUI.Button(folderButtonRect, EditorGUIUtility.IconContent("FolderOpened On Icon")))
                    {
                        string currentPath = property.objectReferenceValue != null ? AssetDatabase.GetAssetPath(property.objectReferenceValue) : defaultRoot;
                        string path = EditorUtility.OpenFolderPanel(label.text, currentPath, defaultRoot);
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            if (path.Contains(defaultRoot))
                            {
                                path = path.Substring(path.IndexOf("Assets"));
                            }
                            else
                            {
                                path = defaultRoot;
                            }
                            var folderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                            GUI.changed = property.objectReferenceValue != folderObj;
                            property.objectReferenceValue = folderObj;
                        }
                    }
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
            return 20f;
        }

        #endregion

        #region Utility

        private void PingFolder(string folderPath)
        {
            EditorUtility.FocusProjectWindow();
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
            EditorGUIUtility.PingObject(obj);
        }

        #endregion
    }

#endif
}