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
    [Database("Debug/Debugger", typeof(DebuggerDatabaseElement))]
    public class DebuggerDatabase : EnumDatabase<DebuggerDatabase>
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
                EditorGUI.LabelField(levelLabelRect, elem.Level.ToString(), GUIHelper.centeredLabel);

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
