using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Console
{
    public class OnScreenConsole : BaseOnScreenConsole<OnScreenConsole>
    {
        #region Initialization

        protected override void InitInputs()
        {
            if (OnScreenConsoleSettings.HasOpenConsoleInput(out var openAction))
            {
                m_openConsoleAction = openAction;
            }
            if (OnScreenConsoleSettings.HasCloseConsoleInput(out var closeAction))
            {
                m_closeConsoleAction = closeAction;
            }

            base.InitInputs();
        }

        #endregion
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(OnScreenConsole), editorForChildClasses: true)]
    public class OnScreenConsoleEditor : Editor
    {
        #region GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(OnScreenConsoleSettings.I, typeof(OnScreenConsoleSettings), false);
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif
}
