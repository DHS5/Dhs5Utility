using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Dhs5.Utility.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FolderPickerAttribute : PropertyAttribute
    {

    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(FolderPickerAttribute))]
    public class FolderPickerAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.String)
            {
                float buttonsWidth = 32f;
                int buttonsCount = 3;

                // Label
                Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, 20f);
                EditorGUI.LabelField(labelRect, label);

                // Label path
                Rect folderRect = new Rect(labelRect.x + labelRect.width, position.y, position.width - labelRect.width - buttonsWidth * buttonsCount, 20f);
                GUI.Box(folderRect, GUIContent.none, EditorStyles.helpBox);
                EditorGUI.SelectableLabel(new Rect(folderRect.x - EditorGUI.indentLevel * 15f + 4f, folderRect.y, folderRect.width, folderRect.height), property.stringValue);
                if (!property.stringValue.StartsWith("Assets") || !Directory.Exists(property.stringValue))
                {
                    GUI.changed = property.stringValue != "Assets";
                    property.stringValue = "Assets";
                }

                // Buttons
                // Folder Button
                Rect folderButtonRect = new Rect(folderRect.x + folderRect.width, position.y, buttonsWidth, 20f);
                if (GUI.Button(folderButtonRect, EditorGUIUtility.IconContent("FolderOpened On Icon")))
                {
                    string path = EditorUtility.OpenFolderPanel(label.text, property.stringValue, "");
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        if (path.Contains("Assets"))
                        {
                            path = path.Substring(path.IndexOf("Assets"));
                        }
                        else
                        {
                            path = "Assets";
                        }
                        GUI.changed = property.stringValue != path;
                        property.stringValue = path;
                    }

                    GUI.FocusControl(null);
                }
                // Clipboard Button
                Rect clipboardButtonRect = new Rect(folderRect.x + folderRect.width + buttonsWidth, position.y, buttonsWidth, 20f);
                if (GUI.Button(clipboardButtonRect, EditorGUIUtility.IconContent("Clipboard")))
                {
                    GUIUtility.systemCopyBuffer = property.stringValue;
                }
                // See Button
                Rect seeButtonRect = new Rect(folderRect.x + folderRect.width + buttonsWidth * 2, position.y, buttonsWidth, 20f);
                if (GUI.Button(seeButtonRect, EditorGUIUtility.IconContent("d_scenevis_visible_hover")))
                {
                    if (Directory.Exists(property.stringValue))
                    {
                        EditorUtility.FocusProjectWindow();
                        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(property.stringValue);
                        
                        AssetDatabase.OpenAsset(obj);
                        EditorApplication.delayCall += () => AssetDatabase.OpenAsset(obj);

                        GUIUtility.ExitGUI();
                    }
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }
    }
#endif
}