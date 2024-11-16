using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.Editors;
using static UnityEditor.Rendering.FilterWindow;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Debugger
{
    [Database("Debug/Debugger", typeof(DebuggerDatabaseElement))]
    public class DebuggerDatabase : EnumDatabase<DebuggerDatabase>
    {
        
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(DebuggerDatabase), editorForChildClasses:true)]
    public class DebuggerDatabaseEditor : EnumDatabaseEditor
    {
        #region Database Content List

        protected override Rect GetButtonRectForDatabaseContentListElement(Rect rect, int index, Object element, bool contextButton)
        {
            var resultRect = base.GetButtonRectForDatabaseContentListElement(rect, index, element, contextButton);
            resultRect.width -= 125f;
            return resultRect;
        }

        protected override void OnDatabaseContentListElementWithNameAndTextureGUI(Rect rect, int index, bool selected, Object obj, string name, Texture2D texture)
        {
            if (obj is DebuggerDatabaseElement elem)
            {
                // Light rim
                var lightRimRect = new Rect(rect.x, rect.y, rect.height + 4f, rect.height);
                EditorGUI.LabelField(lightRimRect, EditorGUIHelper.LightRimIcon);
                EditorGUI.LabelField(lightRimRect, elem.Level > -1 ? EditorGUIHelper.GreenLightIcon : EditorGUIHelper.RedLightIcon);

                // Color
                float colorRectWidth = 125f;
                var colorRect = new Rect(rect.x + rect.width - colorRectWidth + DatabaseContentListElementContextButtonWidth, rect.y, colorRectWidth, rect.height);
                EditorGUI.DrawRect(colorRect, elem.Color);

                // Level
                var levelSliderRect = new Rect(colorRect.x + 5f, rect.y, colorRectWidth - DatabaseContentListElementContextButtonWidth - 10f, rect.height);
                elem.Level = (int)GUI.HorizontalSlider(levelSliderRect, elem.Level, -1, 2);
                var levelLabelRect = new Rect(colorRect.x - 25f, rect.y, 20f, rect.height);
                EditorGUI.LabelField(levelLabelRect, elem.Level.ToString(), EditorGUIHelper.centeredLabel);

                // Label
                float labelRectX = rect.x + lightRimRect.width + 5f;
                var labelRect = new Rect(labelRectX, rect.y, levelLabelRect.x - labelRectX - 5f, rect.height);
                OnDatabaseContentListElementNameGUI(labelRect, index, selected, obj, name);
            }
            else
            {
                base.OnDatabaseContentListElementWithNameAndTextureGUI(rect, index, selected, obj, name, texture);
            }
        }

        #endregion
    }

#endif

    #endregion
}
