using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.GUIs;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Debuggers
{
    [Database("Debugger", typeof(DebuggerDatabaseElement))]
    public class DebuggerDatabase : EnumDatabase
    {
        #region Editor Utility

#if UNITY_EDITOR

        protected string[] GetEnumScriptContentDebuggerExtensions(string enumName)
        {
            List<string> extensions = new();

            extensions.Add("public static void Log(this " + enumName + " category, object message, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)");
            extensions.Add("{");
            extensions.Add("    Debugger<" + enumName + ">.Log(category, message, level, onScreen, context);");
            extensions.Add("}");
            
            extensions.Add("public static void LogWarning(this " + enumName + " category, object message, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, bool onScreen = false, UnityEngine.Object context = null)");
            extensions.Add("{");
            extensions.Add("    Debugger<" + enumName + ">.LogWarning(category, message, level, onScreen, context);");
            extensions.Add("}");
            
            extensions.Add("public static void LogError(this " + enumName + " category, object message, bool onScreen = true, UnityEngine.Object context = null)");
            extensions.Add("{");
            extensions.Add("    Debugger<" + enumName + ">.LogError(category, message, onScreen, context);");
            extensions.Add("}");
            
            extensions.Add("public static void LogAlways(this " + enumName + " category, object message, LogType logType = LogType.Error, bool onScreen = true, UnityEngine.Object context = null)");
            extensions.Add("{");
            extensions.Add("    Debugger<" + enumName + ">.LogAlways(category, message, logType, onScreen, context);");
            extensions.Add("}");

            extensions.Add("public static void LogOnScreen(this " + enumName + " category, object message, LogType logType = LogType.Log, int level = BaseDebugger.MAX_DEBUGGER_LEVEL, float duration = BaseDebugger.DEFAULT_SCREEN_LOG_DURATION)");
            extensions.Add("{");
            extensions.Add("    Debugger<" + enumName + ">.LogOnScreen(category, message, logType, level, duration);");
            extensions.Add("}");

            return extensions.ToArray();
        }

        protected override string GetEnumScriptContentFor(string enumName, string enumNamespace, string usings, string[] enumContent, System.Type dataType, System.Type databaseType)
        {
            return EnumDatabaseEditor.GenerateEnumScriptContent
                (enumName, enumContent, enumNamespace, "using Dhs5.Utility.Debuggers;\n" + usings, dataType, databaseType, GetEnumScriptContentDebuggerExtensions(enumName));
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(DebuggerDatabase), editorForChildClasses:true)]
    public class DebuggerDatabaseEditor : EnumDatabaseEditor
    {
        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            ShowExtraUsings = false;
        }

        #endregion

        #region Database Content List

        float m_extraInfosWidth = 150f;

        protected override Rect GetButtonRectForContentListElement(Rect rect, int index, FolderStructureEntry entry, bool contextButton)
        {
            var resultRect = base.GetButtonRectForContentListElement(rect, index, entry, contextButton);
            resultRect.width -= m_extraInfosWidth;
            return resultRect;
        }

        protected override void OnContentListElementWithNameAndTextureGUI(Rect rect, int index, bool selected, Object obj, string name, Texture2D texture)
        {
            if (obj is DebuggerDatabaseElement elem)
            {
                // Light rim
                var lightRimRect = new Rect(rect.x, rect.y, rect.height + 4f, rect.height);
                EditorGUI.LabelField(lightRimRect, elem.Level > -1 ? EditorGUIHelper.GreenLightIcon : EditorGUIHelper.RedLightIcon);

                // Color
                float colorRectWidth = 40f;
                var colorRect = new Rect(rect.x + rect.width - m_extraInfosWidth, rect.y, colorRectWidth, rect.height);
                elem.Color = EditorGUI.ColorField(colorRect, GUIContent.none, elem.Color, false, false, false);

                // Level
                float levelLabelWidth = 20f;
                float levelSliderWidth = m_extraInfosWidth - colorRectWidth - 5f - levelLabelWidth;
                var levelSliderRect = new Rect(colorRect.x + colorRectWidth + 5f, rect.y, levelSliderWidth, rect.height);
                elem.Level = (int)GUI.HorizontalSlider(levelSliderRect, elem.Level, -1, BaseDebugger.MAX_DEBUGGER_LEVEL);
                //elem.Level = EditorGUI.IntSlider(levelSliderRect, elem.Level, -1, BaseDebugger.MAX_DEBUGGER_LEVEL);
                var levelLabelRect = new Rect(levelSliderRect.x + levelSliderWidth, rect.y, levelLabelWidth, rect.height);
                EditorGUI.LabelField(levelLabelRect, elem.Level.ToString(), GUIHelper.centeredLabel);

                // Label
                float labelRectX = rect.x + lightRimRect.width + 5f;
                var labelRect = new Rect(labelRectX, rect.y, colorRect.x - labelRectX - 5f, rect.height);
                OnContentListElementNameGUI(labelRect, index, selected, obj, name);
            }
            else
            {
                base.OnContentListElementWithNameAndTextureGUI(rect, index, selected, obj, name, texture);
            }
        }

        #endregion
    }

#endif

    #endregion
}
