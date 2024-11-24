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
        public delegate void ValidCommandCallback(ConsoleCommand.ValidCommand validCmd);

        #region INSTANCE

        #region Members

        private GUIStyle m_inputStyle;
        private GUIStyle m_validInputStyle;
        private GUIStyle m_optionStyle;

        private bool m_justOpenedConsole;
        private string m_currentInputString;
        private bool m_isCurrentInputValid;

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
            m_validInputStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                richText = false,
                wordWrap = false,
                fontSize = 40,
                contentOffset = new Vector2(15, 0),
                normal = new GUIStyleState()
                {
                    textColor = Color.green,
                }
            };
            m_optionStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                richText = false,
                wordWrap = false,
                fontSize = 30,
                contentOffset = new Vector2(20, 0),
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


        #region Registration

        private Dictionary<ConsoleCommand, ValidCommandCallback> m_registeredCommands = new();

        private void RegisterCommand(ConsoleCommand command, ValidCommandCallback callback)
        {
            if (m_registeredCommands.ContainsKey(command))
            {
                m_registeredCommands[command] += callback;
            }
            else
            {
                m_registeredCommands.Add(command, callback);
            }
        }
        private void UnregisterCommand(ConsoleCommand command, ValidCommandCallback callback)
        {
            if (m_registeredCommands.ContainsKey(command))
            {
                m_registeredCommands[command] -= callback;
            }
        }
        private void UnregisterCommand(ConsoleCommand command)
        {
            m_registeredCommands.Remove(command);
        }

        #endregion

        #region Options

        private List<ConsoleCommand.CommandArray> m_currentInputOptions = new();

        private void RecomputeOptions()
        {
            m_currentInputOptions.Clear();

            if (!string.IsNullOrWhiteSpace(m_currentInputString))
            {
                foreach (var command in m_registeredCommands.Keys)
                {
                    foreach (var option in command.GetCommandOptionsStartingWith(m_currentInputString))
                    {
                        m_currentInputOptions.Add(option);
                    }
                }
            }
        }

        #endregion

        #region Validation

        private void RecomputeCurrentInputValidity()
        {
            m_isCurrentInputValid = false;

            foreach (var command in m_registeredCommands.Keys)
            {
                if (command.IsCommandValid(m_currentInputString, out _))
                {
                    m_isCurrentInputValid = true;
                    return;
                }
            }
        }

        private void Validate()
        {
            foreach (var (command, callback) in m_registeredCommands)
            {
                if (command.IsCommandValid(m_currentInputString, out var validCommand))
                {
                    callback?.Invoke(validCommand);
                }
            }

            ValidatedInConsole?.Invoke(m_currentInputString);
        }

        #endregion

        #region Callbacks

        private void OnInputChanged()
        {
            RecomputeOptions();
            RecomputeCurrentInputValidity();
        }

        #endregion


        #region GUI

        // PARAMETERS
        private string m_inputControlName = "Command Input";
        float m_inputRectHeight = 50f;
        float m_optionRectHeight = 30f;
        float m_optionsRectHeight = 300f;

        private void OnGUI()
        {
            if (IsActive)
            {
                var inputRect = new Rect(0f, Screen.height - m_inputRectHeight, Screen.width * 0.8f, m_inputRectHeight);
                bool hasFocus = GUI.GetNameOfFocusedControl() == m_inputControlName;

                // EVENTS
                if (Event.current.type == EventType.KeyDown)
                {
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.Return:
                            if (hasFocus)
                            {
                                Event.current.Use();
                                Validate();
                                CloseConsole();
                            }
                            return;
                        case KeyCode.Escape:
                            Event.current.Use();
                            CloseConsole();
                            return;
                    }
                }

                OnInputGUI(inputRect, hasFocus);

                // OPTIONS
                if (hasFocus && !string.IsNullOrWhiteSpace(m_currentInputString))
                {
                    OnOptionsGUI(new Rect(0f, inputRect.y - m_optionsRectHeight, inputRect.width, m_optionsRectHeight));
                }
            }
        }

        private void OnInputGUI(Rect rect, bool hasFocus)
        {
            EditorGUI.DrawRect(rect, hasFocus ? EditorGUIHelper.transparentBlack07 : EditorGUIHelper.transparentBlack03);

            GUI.SetNextControlName(m_inputControlName);

            EditorGUI.BeginChangeCheck();
            m_currentInputString = GUI.TextField(rect, m_currentInputString, m_isCurrentInputValid ? m_validInputStyle : m_inputStyle);
            if (EditorGUI.EndChangeCheck())
            {
                OnInputChanged();
            }

            if (m_justOpenedConsole)
            {
                m_justOpenedConsole = false;
                EditorGUI.FocusTextInControl(m_inputControlName);
            }
        }

        private void OnOptionsGUI(Rect rect)
        {
            var optionRect = new Rect(rect.x, rect.y + rect.height, rect.width, m_optionRectHeight);

            for (int i = 0; i < m_currentInputOptions.Count; i++)
            {
                optionRect.y -= m_optionRectHeight;
                if (optionRect.y >= rect.y)
                {
                    EditorGUI.DrawRect(optionRect, i % 2 == 0 ? EditorGUIHelper.transparentBlack03 : EditorGUIHelper.transparentBlack05);
                    //EditorGUI.LabelField(optionRect, m_currentInputOptions[i], m_optionStyle);
                    if (GUI.Button(optionRect, m_currentInputOptions[i].ToString(), m_optionStyle))
                    {
                        m_currentInputString = m_currentInputOptions[i].ToStringWithoutParams();
                        OnInputChanged();
                    }
                }
                else
                {
                    break;
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

        public static event Action<string> ValidatedInConsole;

        #endregion

        #region Activation

        public static void Open()
        {
            GetInstance().OpenConsole();
        }

        #endregion

        #region Command Registration

        public static void Register(ConsoleCommand command, ValidCommandCallback callback)
        {
            GetInstance().RegisterCommand(command, callback);
        }
        
        public static void Unregister(ConsoleCommand command, ValidCommandCallback callback)
        {
            GetInstance().UnregisterCommand(command, callback);
        }
        public static void Unregister(ConsoleCommand command)
        {
            GetInstance().UnregisterCommand(command);
        }

        #endregion

        #endregion
    }
}
