using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Console
{
    public abstract class PredefinedConsoleCommand : ConsoleCommand
    {
        #region Core Behaviour

        private void OnEnable()
        {
            CreateCommand();
        }

        #endregion

        #region Command Pieces Creation

        private void CreateCommand()
        {
            var result = OnCreateCommand();
            if (result.IsValid())
            {
                SetCommandPieces(result);
            }
        }

        protected abstract List<ConsoleCommandPiece> OnCreateCommand();

        #endregion

        #region Validation Callbacks

        public OnScreenConsole.ValidCommandCallback Callback => OnCommandValidated;

        protected abstract void OnCommandValidated(ValidCommand validCommand);

        #endregion

        #region Registration

        public void Register(bool register)
        {
            if (register)
            {
                OnScreenConsole.Register(this, OnCommandValidated);
            }
            else
            {
                OnScreenConsole.Unregister(this, OnCommandValidated);
            }
        }

        #endregion


        #region Editor Methods

#if UNITY_EDITOR

        public void Editor_CreateCommand()
        {
            CreateCommand();
        }

#endif

        #endregion
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(PredefinedConsoleCommand), true)]
    public class PredefinedConsoleCommandEditor : ConsoleCommandEditor
    {
        #region GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());

            EditorGUILayout.Space(15f);

            if (GUILayout.Button("Refresh Command"))
            {
                (m_command as PredefinedConsoleCommand).Editor_CreateCommand();
            }

            EditorGUI.BeginDisabledGroup(true);
            m_piecesList.DoLayoutList();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(15f);

            OnPreviewGUI();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif
}
