using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.Editors;


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

        protected override void OnDatabaseContentListElementBackgroundGUI(Rect rect, int index, bool selected, Object element)
        {
            if (element is DebuggerDatabaseElement elem)
            {
                EditorGUI.DrawRect(rect, elem.Color);
                if (selected) EditorGUI.DrawRect(rect, EditorGUIHelper.transparentWhite02);
            }
            else
            {
                base.OnDatabaseContentListElementBackgroundGUI(rect, index, selected, element);
            }
        }

        #endregion
    }

#endif

    #endregion
}
