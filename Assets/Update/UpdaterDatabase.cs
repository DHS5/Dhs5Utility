using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Updates
{
    [Database("Updater", typeof(UpdaterDatabaseElement))]
    public class UpdaterDatabase : EnumDatabase<UpdaterDatabase>
    {
        #region Editor Utility

#if UNITY_EDITOR

        protected string[] GetEnumScriptContentUpdaterExtensions(string enumName)
        {
            List<string> extensions = new();

            extensions.Add(" ");

            extensions.Add("public static void Register(this " + enumName + " category, UpdateCallback callback, ref ulong key)");
            extensions.Add("{");
            extensions.Add("    Updater<" + enumName + ">.Register(true, category, callback, ref key);");
            extensions.Add("}");
            
            extensions.Add("public static void Unregister(this " + enumName + " category, UpdateCallback callback, ref ulong key)");
            extensions.Add("{");
            extensions.Add("    Updater<" + enumName + ">.Register(false, category, callback, ref key);");
            extensions.Add("}");

            return extensions.ToArray();
        }

        protected override string GetEnumScriptContentFor(string enumName, string enumNamespace, string usings, string[] enumContent, System.Type dataType, System.Type databaseType)
        {
            return EnumDatabaseEditor.GenerateEnumScriptContent
                (enumName, enumContent, enumNamespace, "using Dhs5.Utility.Updates;\n" + usings, dataType, databaseType, GetEnumScriptContentUpdaterExtensions(enumName));
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

        #region Database Content List

        protected override Rect GetButtonRectForDatabaseContentListElement(Rect rect, int index, Object element, bool contextButton)
        {
            return base.GetButtonRectForDatabaseContentListElement(rect, index, element, contextButton);
        }

        protected override void OnDatabaseContentListElementWithNameAndTextureGUI(Rect rect, int index, bool selected, Object obj, string name, Texture2D texture)
        {
            if (obj is UpdaterDatabaseElement elem)
            {
                float passRectWidth = 70f;
                var passRect = new Rect(rect.x + rect.width - passRectWidth, rect.y, passRectWidth, rect.height);
                EditorGUI.LabelField(passRect, elem.Pass.ToString(), m_smallInfosStyle);

                float customFreqRectWidth = 40f;
                if (elem.HasCustomFrequency(out float f))
                {
                    var customFreqRect = new Rect(rect.x + rect.width - customFreqRectWidth - passRectWidth, rect.y, customFreqRectWidth, rect.height);
                    EditorGUI.LabelField(customFreqRect, "f=" + f, m_smallInfosStyle); 
                }

                var labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 5f - passRectWidth - customFreqRectWidth, rect.height);
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
