using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;

namespace Dhs5.Utility.Editors
{
    public static class EditorGUIHelper
    {
        #region GUI Color

        public static Color transparentBlack01 = new Color(0f, 0f, 0f, 0.1f);
        public static Color transparentBlack02 = new Color(0f, 0f, 0f, 0.2f);
        public static Color transparentBlack03 = new Color(0f, 0f, 0f, 0.3f);
        public static Color transparentBlack04 = new Color(0f, 0f, 0f, 0.4f);
        public static Color transparentBlack05 = new Color(0f, 0f, 0f, 0.5f);
        public static Color transparentBlack06 = new Color(0f, 0f, 0f, 0.6f);
        public static Color transparentBlack07 = new Color(0f, 0f, 0f, 0.7f);
        public static Color transparentBlack08 = new Color(0f, 0f, 0f, 0.8f);
        public static Color transparentBlack09 = new Color(0f, 0f, 0f, 0.9f);
        
        public static Color transparentWhite01 = new Color(1f, 1f, 1f, 0.1f);
        public static Color transparentWhite02 = new Color(1f, 1f, 1f, 0.2f);
        public static Color transparentWhite03 = new Color(1f, 1f, 1f, 0.3f);
        public static Color transparentWhite04 = new Color(1f, 1f, 1f, 0.4f);
        public static Color transparentWhite05 = new Color(1f, 1f, 1f, 0.5f);
        public static Color transparentWhite06 = new Color(1f, 1f, 1f, 0.6f);
        public static Color transparentWhite07 = new Color(1f, 1f, 1f, 0.7f);
        public static Color transparentWhite08 = new Color(1f, 1f, 1f, 0.8f);
        public static Color transparentWhite09 = new Color(1f, 1f, 1f, 0.9f);
        
        public static Color grey01 = new Color(0.1f, 0.1f, 0.1f, 1f);
        public static Color grey015 = new Color(0.15f, 0.15f, 0.15f, 1f);
        public static Color grey02 = new Color(0.2f, 0.2f, 0.2f, 1f);
        public static Color grey03 = new Color(0.3f, 0.3f, 0.3f, 1f);
        public static Color grey04 = new Color(0.4f, 0.4f, 0.4f, 1f);
        public static Color grey05 = new Color(0.5f, 0.5f, 0.5f, 1f);
        public static Color grey06 = new Color(0.6f, 0.6f, 0.6f, 1f);
        public static Color grey07 = new Color(0.7f, 0.7f, 0.7f, 1f);
        public static Color grey08 = new Color(0.8f, 0.8f, 0.8f, 1f);
        public static Color grey09 = new Color(0.9f, 0.9f, 0.9f, 1f);

        #endregion

        #region GUI Styles

        public static GUIStyle foldoutStyle = new(EditorStyles.foldout)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            onNormal = new GUIStyleState()
            {
                textColor = Color.white,
            },
        };

        public static GUIStyle blackFolderStyle = new GUIStyle()
        {
            fontStyle = FontStyle.Bold,
            fontSize = 14,
            contentOffset = new Vector2(5f, 0f)
        };

        public static GUIStyle centeredLabel = new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            },
        };
        public static GUIStyle centeredBoldLabel = new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            },
        };
        public static GUIStyle downCenteredBoldLabel = new GUIStyle()
        {
            alignment = TextAnchor.LowerCenter,
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            },
        };
        public static GUIStyle bigTitleLabel = new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 18,
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            },
        };

        public static GUIStyle simpleIconButton = new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            clipping = TextClipping.Clip,
            imagePosition = ImagePosition.ImageOnly
        };

        #endregion

        #region GUI Icons

        public static GUIContent RefreshIcon => EditorGUIUtility.IconContent("d_Refresh");
        public static GUIContent HelpIcon => EditorGUIUtility.IconContent("d__Help");
        public static GUIContent MenuIcon => EditorGUIUtility.IconContent("d__Menu");
        public static GUIContent SettingsIcon => EditorGUIUtility.IconContent("d_Settings");
        public static GUIContent PauseIcon => EditorGUIUtility.IconContent("d_Animation.Pause");
        public static GUIContent PlayIcon => EditorGUIUtility.IconContent("d_Animation.Play");
        public static GUIContent CanSeeIcon => EditorGUIUtility.IconContent("d_scenevis_visible_hover");
        public static GUIContent CantSeeIcon => EditorGUIUtility.IconContent("d_SceneViewVisibility");
        public static GUIContent AddIcon => EditorGUIUtility.IconContent("d_Toolbar Plus");
        public static GUIContent DeleteIcon => EditorGUIUtility.IconContent("d_Toolbar Minus");
        public static GUIContent FavoriteInactiveIcon => EditorGUIUtility.IconContent("d_Favorite");
        public static GUIContent FavoriteActiveIcon => EditorGUIUtility.IconContent("d_Favorite_colored");
        public static GUIContent PresetIcon => EditorGUIUtility.IconContent("d_Preset.Context");
        public static GUIContent NextIcon => EditorGUIUtility.IconContent("d_tab_next");
        public static GUIContent PreviousIcon => EditorGUIUtility.IconContent("d_tab_prev");
        public static GUIContent SaveIcon => EditorGUIUtility.IconContent("d_SaveAs");
        public static GUIContent LockedIcon => EditorGUIUtility.IconContent("LockIcon-on");
        public static GUIContent UnlockedIcon => EditorGUIUtility.IconContent("LockIcon");
        public static GUIContent SearchIcon => EditorGUIUtility.IconContent("d_SearchOverlay");
        public static GUIContent UpIcon => EditorGUIUtility.IconContent("d_icon dropdown open");
        public static GUIContent DownIcon => EditorGUIUtility.IconContent("d_icon dropdown");
        public static GUIContent SceneIcon => EditorGUIUtility.IconContent("d_SceneAsset Icon");
        public static GUIContent SceneSmallIcon => EditorGUIUtility.IconContent("d_Scene");
        public static GUIContent DatabaseIcon => EditorGUIUtility.IconContent("d_PreMatCylinder");
        public static GUIContent DebugIcon => EditorGUIUtility.IconContent("d_Debug");
        public static GUIContent GreenLightIcon => EditorGUIUtility.IconContent("greenLight");
        public static GUIContent OrangeLightIcon => EditorGUIUtility.IconContent("orangeLight");
        public static GUIContent RedLightIcon => EditorGUIUtility.IconContent("redLight");
        public static GUIContent LightRimIcon => EditorGUIUtility.IconContent("lightRim");
        public static GUIContent FolderOpenedIcon => EditorGUIUtility.IconContent("FolderOpened On Icon");
        public static GUIContent ConsoleInfoIcon => EditorGUIUtility.IconContent("console.infoicon.sml");
        public static GUIContent ConsoleInfoInactiveIcon => EditorGUIUtility.IconContent("console.infoicon.inactive.sml");
        public static GUIContent ConsoleWarningIcon => EditorGUIUtility.IconContent("console.warnicon.sml");
        public static GUIContent ConsoleWarningInactiveIcon => EditorGUIUtility.IconContent("console.warnicon.inactive.sml");
        public static GUIContent ConsoleErrorIcon => EditorGUIUtility.IconContent("console.erroricon.sml");
        public static GUIContent ConsoleErrorInactiveIcon => EditorGUIUtility.IconContent("console.erroricon.inactive.sml");

        #endregion


        #region GUI Elements

        #region Lists

        public static bool FoldoutObjectList<T>(bool open, IList<T> list, GUIContent label, bool allowSceneObjects, bool showNull = false) where T : UnityEngine.Object
        {
            open = EditorGUILayout.Foldout(open, label, true);
            if (open)
            {
                EditorGUI.indentLevel++;
                foreach (var item in list)
                {
                    if (showNull || item != null) EditorGUILayout.ObjectField(item, typeof(UnityEngine.Object), allowSceneObjects);
                }
                EditorGUI.indentLevel--;
            }

            return open;
        }
        public static void ObjectList<T>(IList<T> list, GUIContent label, bool allowSceneObjects, bool showNull = false) where T : UnityEngine.Object
        {
            EditorGUILayout.LabelField(label);
            EditorGUI.indentLevel++;
            foreach (var item in list)
            {
                if (showNull || item != null) EditorGUILayout.ObjectField(item, typeof(UnityEngine.Object), allowSceneObjects);
            }
            EditorGUI.indentLevel--;
        }

        #endregion

        #region Folder Picker

        public static string FolderPicker(string value, GUIContent label)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 20f);

            float labelWidth = EditorGUIUtility.labelWidth;
            float buttonWidth = 32f;
            float fieldWidth = rect.width - labelWidth - buttonWidth;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height), label);

            Rect fieldRect = new Rect(rect.x + labelWidth, rect.y, fieldWidth, rect.height);
            GUI.Box(fieldRect, "", EditorStyles.helpBox);
            fieldRect.x += 3f;
            fieldRect.width -= 3f;
            EditorGUI.SelectableLabel(fieldRect, value);
            if (GUI.Button(new Rect(rect.x + rect.width - buttonWidth + 2f, rect.y, buttonWidth - 2f, rect.height), FolderOpenedIcon))
            {
                string absPath = EditorUtility.OpenFolderPanel(label.text, value, "");
                if (!string.IsNullOrEmpty(absPath))
                {
                    int startIndex = absPath.IndexOf("Assets");
                    if (startIndex != -1) absPath = absPath.Substring(startIndex);
                    else absPath = "";
                    return absPath;
                }
            }
            return value;
        }

        public static void FolderPicker(SerializedProperty property, Action onChange = null)
        {
            FolderPicker(property, new GUIContent(property.displayName), onChange);
        }
        public static void FolderPicker(SerializedProperty property, GUIContent label, Action onChange = null)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 20f);

            float labelWidth = EditorGUIUtility.labelWidth;
            float buttonWidth = 32f;
            float fieldWidth = rect.width - labelWidth - buttonWidth;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height), label);

            Rect fieldRect = new Rect(rect.x + labelWidth, rect.y, fieldWidth, rect.height);
            GUI.Box(fieldRect, "", EditorStyles.helpBox);
            fieldRect.x += 3f;
            fieldRect.width -= 3f;
            EditorGUI.SelectableLabel(fieldRect, property.stringValue);
            if (GUI.Button(new Rect(rect.x + rect.width - buttonWidth + 2f, rect.y, buttonWidth - 2f, rect.height), FolderOpenedIcon))
            {
                string absPath = EditorUtility.OpenFolderPanel(label.text, property.stringValue, "");
                if (!string.IsNullOrEmpty(absPath))
                {
                    int startIndex = absPath.IndexOf("Assets");
                    if (startIndex != -1) absPath = absPath.Substring(startIndex);
                    else absPath = "";
                    property.stringValue = absPath;
                    property.serializedObject.ApplyModifiedProperties();
                    onChange?.Invoke();
                }
                GUIUtility.ExitGUI();
            }
        }

        #endregion

        #region Progress Bar

        public static void ProgressBar(float value, string text, Color color)
        {
            Rect rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false, 22f));
            rect.height = 20f;

            ProgressBar(rect, value, text, color);
        }
        public static void ProgressBar(Rect rect, float value, string text, Color color)
        {
            EditorGUI.DrawRect(rect, transparentBlack03);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * value, rect.height), color);

            EditorGUI.LabelField(rect, text, centeredBoldLabel);
        }

        #endregion

        #endregion
    }
}

#endif