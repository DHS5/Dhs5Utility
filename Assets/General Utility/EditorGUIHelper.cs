using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Dhs5.Utility.GUIs;

#if UNITY_EDITOR
using UnityEditor;

namespace Dhs5.Utility.Editors
{
    public static class EditorGUIHelper
    {
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
        public static GUIContent ScreenIcon => EditorGUIUtility.IconContent("BuildSettings.Standalone On");
        public static GUIContent ScreenInactiveIcon => EditorGUIUtility.IconContent("BuildSettings.LinuxHeadlessSimulation");
        public static GUIContent ConsoleIcon => EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow@2x");

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
            EditorGUI.DrawRect(rect, GUIHelper.transparentBlack03);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * value, rect.height), color);

            EditorGUI.LabelField(rect, text, GUIHelper.centeredBoldLabel);
        }

        #endregion

        #region Toolbar Toogle

        public static bool ToolbarToggle(Rect rect, GUIContent content, bool value)
        {
            bool result = GUI.Toggle(rect, value, GUIContent.none, EditorStyles.toolbarButton);
            EditorGUI.LabelField(rect, content, GUIHelper.centeredLabel);
            return result;
        }

        #endregion

        #region Foldout

        public static bool Foldout(Rect rect, string label, bool value)
        {
            var content = value ? UpIcon : DownIcon;
            content.text = label;

            if (GUI.Button(rect, content, GUIHelper.foldoutStyle))
            {
                return !value;
            }
            return value;
        }

        #endregion

        #endregion
    }
}
#endif