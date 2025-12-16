using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Updates
{
    [Database("Update/Updater", typeof(UpdateChannelObject))]
    public class UpdaterDatabase : EnumDatabase
    {
        #region Editor Utility

#if UNITY_EDITOR

        protected override bool TryGetEnumScriptPath(out string path)
        {
            var so = new SerializedObject(this);
            var scriptObj = so.FindProperty("m_Script").objectReferenceValue;
            if (scriptObj != null)
            {
                so.Dispose();
                path = ProjectWindowUtil.GetContainingFolder(AssetDatabase.GetAssetPath(scriptObj)) + "/EUpdateChannel.cs";
                return true;
            }

            so.Dispose();
            path = null;
            return false;
        }
        protected override string GetEnumScriptContentFor(string enumName, string enumNamespace, string usings, string[] enumContent, System.Type dataType, System.Type databaseType)
        {
            return UpdaterDatabaseEditor.GenerateUpdateChannelEnumScriptContent(enumContent, dataType);
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(UpdaterDatabase), editorForChildClasses: true)]
    public class UpdaterDatabaseEditor : EnumDatabaseEditor
    {
        #region Members

        private GUIStyle m_smallInfosStyle;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            ShowExtraUsings = false;

            m_smallInfosStyle = new GUIStyle()
            {
                alignment = TextAnchor.LowerRight,
                fontSize = 10,
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                }
            };
        }

        #endregion

        #region Data Container Informations

        protected override void OnContainerInformationsContentGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(p_textAsset);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5f);

            var guiColor = GUI.color;
            if (HasScriptChanges) GUI.color = Color.cyan;
            if (GUILayout.Button("Update Script"))
            {
                m_enumDatabase.Editor_UpdateEnumScript();
                HasScriptChanges = false;
            }
            GUI.color = guiColor;

            DrawDefault();
        }

        #endregion

        #region Database Content List

        protected override void OnContentListElementWithNameAndTextureGUI(Rect rect, int index, bool selected, UnityEngine.Object obj, string name, Texture2D texture)
        {
            if (obj is UpdateChannelObject elem)
            {
                float passRectWidth = 70f;
                var passRect = new Rect(rect.x + rect.width - passRectWidth, rect.y, passRectWidth, rect.height);
                EditorGUI.LabelField(passRect, elem.Pass.ToString(), m_smallInfosStyle);

                float customFreqRectWidth = 40f;
                var freq = elem.Frequency;
                if (freq > 0f)
                {
                    var customFreqRect = new Rect(rect.x + rect.width - customFreqRectWidth - passRectWidth, rect.y, customFreqRectWidth, rect.height);
                    EditorGUI.LabelField(customFreqRect, "f=" + freq, m_smallInfosStyle); 
                }

                var labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 5f - passRectWidth - customFreqRectWidth, rect.height);
                OnContentListElementNameGUI(labelRect, index, selected, obj, name);
            }
            else
            {
                base.OnContentListElementWithNameAndTextureGUI(rect, index, selected, obj, name, texture);
            }
        }

        #endregion


        #region STATIC Enum Script Content

        internal static string GenerateUpdateChannelEnumScriptContent(string[] enumContent, Type dataType)
        {
            StringBuilder sb = new StringBuilder();

            // Parameters
            string usings = "using UnityEngine;\nusing System;\nusing Dhs5.Utility.Databases;\n";

            // Utility Functions
            string prefix = "";
            void Increment()
            {
                prefix += "    ";
            }
            void Decrement()
            {
                prefix = prefix.Remove(0, 4);
            }
            void AppendPrefix()
            {
                sb.Append(prefix);
            }
            void OpenBracket()
            {
                AppendPrefix();
                sb.AppendLine("{");
            }
            void CloseBracket()
            {
                AppendPrefix();
                sb.AppendLine("}");
            }

            // USINGS
            sb.Append(usings);
            sb.AppendLine();

            // NAMESPACE
            sb.AppendLine("namespace Dhs5.Utility.Updates");
            sb.AppendLine("{");
            Increment();

            // ENUM
            AppendPrefix();
            sb.AppendLine("public enum EUpdateChannel");
            OpenBracket();

            string value;
            Increment();
            int index = 0;
            for (int i = 0; i < enumContent.Length; i++)
            {
                value = enumContent[i];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    AppendPrefix();
                    sb.Append(value);
                    sb.Append(" = ");
                    sb.Append(index.ToString());
                    sb.AppendLine(",");
                    index++;
                }
            }
            Decrement();
            CloseBracket();

            // FLAGS
            sb.AppendLine();
            AppendPrefix();
            sb.AppendLine("[Flags]");
            AppendPrefix();
            sb.AppendLine("public enum EUpdateChannelFlags");

            OpenBracket();
            Increment();
            index = 0;
            for (int i = 0; i < enumContent.Length; i++)
            {
                value = enumContent[i];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    AppendPrefix();
                    sb.Append(value);
                    sb.Append(" = 1 << ");
                    sb.Append(index.ToString());
                    sb.AppendLine(",");
                    index++;
                }
            }
            Decrement();
            CloseBracket();

            // CHANNEL STRUCTS
            foreach (var item in enumContent)
            {
                AppendPrefix();
                sb.Append("public struct ");
                sb.Append(item);
                sb.AppendLine("_UpdateChannel { }");
            }

            // EXTENSION
            AppendPrefix();
            sb.AppendLine();

            AppendPrefix();
            sb.AppendLine("public static class UpdateChannelExtensions");

            OpenBracket();
            Increment();

            // Get Value
            AppendPrefix();
            sb.AppendLine("public static IUpdateChannel GetValue(this EUpdateChannel e)");

            OpenBracket();
            Increment();
            AppendPrefix();
            sb.Append("return Database.Get<UpdaterDatabase>().GetDataAtIndex<");
            sb.Append(dataType.Name);
            sb.AppendLine(">((int)e);");

            Decrement();
            CloseBracket();

            // Get Channel Type
            AppendPrefix();
            sb.AppendLine("public static Type GetChannelType(this EUpdateChannel e)");

            {
                OpenBracket();
                Increment();
                AppendPrefix();
                sb.AppendLine("switch (e)");

                {
                    OpenBracket();
                    Increment();

                    foreach (var item in enumContent)
                    {
                        AppendPrefix();
                        sb.Append("case EUpdateChannel.");
                        sb.Append(item);
                        sb.Append(": return typeof(");
                        sb.Append(item);
                        sb.AppendLine("_UpdateChannel);");
                    }

                    AppendPrefix();
                    sb.AppendLine("default: return typeof(Updater.DefaultUpdateChannel);");

                    Decrement();
                    CloseBracket();
                }

                Decrement();
                CloseBracket();
            }

            // Flags Contains
            // Contains 1
            sb.AppendLine();

            AppendPrefix();
            sb.AppendLine("public static bool Contains(this EUpdateChannelFlags flag, EUpdateChannel e)");

            OpenBracket();
            Increment();
            AppendPrefix();
            sb.AppendLine("return (flag & ((EUpdateChannelFlags)(1 << (int)e))) != 0;");

            Decrement();
            CloseBracket();

            // Contains 2
            sb.AppendLine();

            AppendPrefix();
            sb.AppendLine("public static bool Contains(this EUpdateChannelFlags flag, EUpdateChannelFlags other)");

            OpenBracket();
            Increment();
            AppendPrefix();
            sb.AppendLine("return (flag & other) != 0;");
            Decrement();
            CloseBracket();

            Decrement();
            CloseBracket();

            // END NAMESPACE
            Decrement();
            CloseBracket();

            // Result
            return sb.ToString();
        }

        #endregion
    }

#endif

    #endregion
}
