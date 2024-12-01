using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
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
            return base.GetButtonRectForDatabaseContentListElement(rect, index, element, contextButton);
        }

        protected override void OnDatabaseContentListElementWithNameAndTextureGUI(Rect rect, int index, bool selected, Object obj, string name, Texture2D texture)
        {
            if (obj is UpdaterDatabaseElement elem)
            {
                OnDatabaseContentListElementNameGUI(rect, index, selected, obj, name);
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
