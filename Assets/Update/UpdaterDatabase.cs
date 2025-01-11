using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Updates
{
    [Database("Update/Updater", typeof(UpdaterDatabaseElement))]
    public class UpdaterDatabase : EnumDatabase
    {
        #region Editor Utility

#if UNITY_EDITOR

        protected override string GetEnumScriptContentFor(string enumName, string enumNamespace, string usings, string[] enumContent, System.Type dataType, System.Type databaseType)
        {
            return EnumDatabaseEditor.GenerateEnumScriptContent
                (enumName, enumContent, enumNamespace, "using Dhs5.Utility.Updates;\n" + usings, dataType, databaseType);
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

        protected override void OnContentListElementWithNameAndTextureGUI(Rect rect, int index, bool selected, UnityEngine.Object obj, string name, Texture2D texture)
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
