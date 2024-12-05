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
        #region Accessors

        protected override int GetInputFontSize()
        {
            return OnScreenConsoleSettings.InputFontSize;
        }
        protected override Color GetInputTextColor()
        {
            return OnScreenConsoleSettings.InputTextColor;
        }
        protected override Color GetValidInputTextColor()
        {
            return OnScreenConsoleSettings.InputValidTextColor;
        }
        protected override int GetOptionFontSize()
        {
            return OnScreenConsoleSettings.OptionFontSize;
        }
        protected override Color GetOptionTextColor()
        {
            return OnScreenConsoleSettings.OptionTextColor;
        }

        protected override float GetInputRectHeight()
        {
            return OnScreenConsoleSettings.InputRectHeight;
        }
        protected override float GetOptionRectHeight()
        {
            return OnScreenConsoleSettings.OptionRectHeight;
        }
        protected override int GetMaxOptionsDisplayed()
        {
            return OnScreenConsoleSettings.MaxOptionsDisplayed;
        }

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            RegisterPredefinedCommands(true);
        }
        protected override void OnDisable()
        {
            base.OnDisable();

            RegisterPredefinedCommands(false);
        }

        #endregion

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


        #region Predefined Commands

        private void RegisterPredefinedCommands(bool register)
        {
            if (register)
            {
                foreach (var cmd in OnScreenConsoleSettings.PredefinedCommands)
                {
                    RegisterCommand(cmd, cmd.Callback);
                }
            }
            else
            {
                foreach (var cmd in OnScreenConsoleSettings.PredefinedCommands)
                {
                    UnregisterCommand(cmd, cmd.Callback);
                }
            }
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
