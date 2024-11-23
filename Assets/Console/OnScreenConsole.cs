using Dhs5.Utility.Editors;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Dhs5.Utility.Console
{
    public class OnScreenConsole : MonoBehaviour
    {
        #region STRUCT : ValidCommand

        public struct ValidCommand
        {
            public ValidCommand(string command)
            {
                rawCommand = command;
                parameters = null;
            }

            public readonly string rawCommand;
            public readonly object[] parameters;
        }

        #endregion

        #region INSTANCE

        #region Members

        private GUIStyle m_inputStyle;

        private string m_inputControlName = "Command Input";
        private bool m_justOpenedConsole;
        private string m_currentInputString;

        #endregion

        #region Properties

        public bool IsActive { get; private set; }

        #endregion

        #region Core Behaviour

        private void Start()
        {
            m_inputStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                richText = false,
                wordWrap = false,
                fontSize = 40,
                contentOffset = new Vector2(15, 0),
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                }
            };
        }

        #endregion

        #region Activation

        public void OpenConsole()
        {
            IsActive = true;
            m_currentInputString = string.Empty;
            m_justOpenedConsole = true;
        }
        private void CloseConsole()
        {
            IsActive = false;
        }

        #endregion


        #region GUI

        private void OnGUI()
        {
            if (IsActive)
            {
                float inputRectHeight = 50f;
                var inputRect = new Rect(0f, Screen.height - inputRectHeight, Screen.width * 0.8f, inputRectHeight);
                bool hasFocus = GUI.GetNameOfFocusedControl() == m_inputControlName;

                if (Event.current.type == EventType.KeyDown)
                {
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.Return:
                            if (hasFocus)
                            {
                                Event.current.Use();
                                Debug.Log("VALIDATE");
                                CloseConsole();
                            }
                            return;
                        case KeyCode.Escape:
                            Event.current.Use();
                            CloseConsole();
                            return;
                    }
                }

                EditorGUI.DrawRect(inputRect, hasFocus ? EditorGUIHelper.transparentBlack07 : EditorGUIHelper.transparentBlack03);

                GUI.SetNextControlName(m_inputControlName);

                EditorGUI.BeginChangeCheck();
                m_currentInputString = GUI.TextField(inputRect, m_currentInputString, m_inputStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    Debug.Log(m_currentInputString);
                }

                if (m_justOpenedConsole)
                {
                    m_justOpenedConsole = false;
                    EditorGUI.FocusTextInControl(m_inputControlName);
                }
            }
        }

        #endregion

        #endregion

        // ---------- ---------- ---------- 

        #region STATIC

        #region Instance Creation

        private static OnScreenConsole Instance { get; set; }

        private static void CreateInstance()
        {
            if (Instance != null) return;

            var obj = new GameObject("OnScreen Console");
            DontDestroyOnLoad(obj);

            Instance = obj.AddComponent<OnScreenConsole>();
        }

        private static OnScreenConsole GetInstance()
        {
            if (Instance == null)
            {
                CreateInstance();
            }
            return Instance;
        }

        #endregion

        #region Events

        public static event Action<ValidCommand> OnValidCommand;
        public static event Action<string> OnInvalidCommand;

        #endregion

        #region Activation

        public static void Open()
        {
            GetInstance().OpenConsole();
        }

        #endregion

        #region Command Registration

        #endregion

        #endregion
    }
}
