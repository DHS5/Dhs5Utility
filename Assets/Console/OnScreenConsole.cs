using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Dhs5.Utility.GUIs;

namespace Dhs5.Utility.Console
{
    public class OnScreenConsole : MonoBehaviour
    {
        public delegate void ValidCommandCallback(ConsoleCommand.ValidCommand validCmd);

        #region INSTANCE

        #region Members

        private InputAction m_openConsoleAction;
        private InputAction m_closeConsoleAction;

        private GUIStyle m_inputStyle;
        private GUIStyle m_validInputStyle;
        private GUIStyle m_optionStyle;

        private bool m_justOpenedConsole;
        private string m_currentInputString;
        private bool m_isCurrentInputValid;
        private Vector2 m_optionsScrollPos;

        #endregion

        #region Properties

        public bool IsActive { get; private set; }

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            InitInputs();
            RegisterPredefinedCommands(true);
        }
        private void OnDisable()
        {
            RegisterPredefinedCommands(false);
            EnableOpenConsoleInput(false);
            EnableCloseConsoleInput(false);
        }

        #endregion


        #region Styles

        private void InitStyles()
        {
            m_inputStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                richText = false,
                wordWrap = false,
                fontSize = OnScreenConsoleSettings.InputFontSize,
                contentOffset = new Vector2(15, 0),
                normal = new GUIStyleState()
                {
                    textColor = OnScreenConsoleSettings.InputTextColor,
                }
            };
            m_validInputStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                richText = false,
                wordWrap = false,
                fontSize = OnScreenConsoleSettings.InputFontSize,
                contentOffset = new Vector2(15, 0),
                normal = new GUIStyleState()
                {
                    textColor = OnScreenConsoleSettings.InputValidTextColor,
                }
            };
            m_optionStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                richText = false,
                wordWrap = false,
                fontSize = OnScreenConsoleSettings.OptionFontSize,
                contentOffset = new Vector2(20, 0),
                normal = new GUIStyleState()
                {
                    textColor = OnScreenConsoleSettings.OptionTextColor,
                }
            };
        }

        #endregion

        #region Inputs Management

        private void InitInputs()
        {
            if (OnScreenConsoleSettings.HasOpenConsoleInput(out var openAction))
            {
                m_openConsoleAction = openAction;
                if (!IsActive) m_openConsoleAction.performed += OpenConsole;
            }
            if (OnScreenConsoleSettings.HasCloseConsoleInput(out var closeAction))
            {
                m_closeConsoleAction = closeAction;
                if (IsActive) m_closeConsoleAction.performed += CloseConsole;
            }
        }
        private void EnableOpenConsoleInput(bool enable)
        {
            if (m_openConsoleAction != null)
            {
                if (enable) m_openConsoleAction.performed += OpenConsole;
                else m_openConsoleAction.performed -= OpenConsole;
            }
        }
        private void EnableCloseConsoleInput(bool enable)
        {
            if (m_closeConsoleAction != null)
            {
                if (enable) m_closeConsoleAction.performed += CloseConsole;
                else m_closeConsoleAction.performed -= CloseConsole;
            }
        }

        #endregion

        #region Activation

        public void OpenConsole()
        {
            if (IsActive) return;

            EnableOpenConsoleInput(false);
            EnableCloseConsoleInput(true);
            IsActive = true;

            m_currentInputString = string.Empty;
            m_justOpenedConsole = true;
            InitStyles();
        }
        private void OpenConsole(InputAction.CallbackContext callbackContext)
        {
            OpenConsole();
        }
        private void CloseConsole()
        {
            if (!IsActive) return;

            EnableOpenConsoleInput(true);
            EnableCloseConsoleInput(false);
            IsActive = false;
        }
        private void CloseConsole(InputAction.CallbackContext callbackContext)
        {
            CloseConsole();
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

            m_optionsScrollPos = new Vector2(0, OnScreenConsoleSettings.OptionRectHeight * Mathf.Max(0, m_currentInputOptions.Count - OnScreenConsoleSettings.MaxOptionsDisplayed));
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
            AddToPreviousCommands(m_currentInputString);

            m_currentInputString = string.Empty;
        }

        #endregion

        #region Previous Commands

        private List<string> m_previousCommands = new();
        private int m_previousCommandMarker = 0;

        private void AddToPreviousCommands(string cmd)
        {
            if (m_previousCommands.Count > 100) m_previousCommands.RemoveAt(0);
            m_previousCommands.Add(cmd);
            m_previousCommandMarker = m_previousCommands.Count;
        }

        private void OnGetPreviousCommand()
        {
            m_previousCommandMarker = Mathf.Clamp(m_previousCommandMarker - 1, 0, m_previousCommands.Count - 1);
            m_currentInputString = m_previousCommands[m_previousCommandMarker];
        }
        private void OnGetNextCommand()
        {
            m_previousCommandMarker = Mathf.Clamp(m_previousCommandMarker + 1, 0, m_previousCommands.Count);
            m_currentInputString = m_previousCommandMarker == m_previousCommands.Count ? string.Empty : m_previousCommands[m_previousCommandMarker];
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
        private const string InputControlName = "Command Input";

        private void OnGUI()
        {
            if (IsActive)
            {
                var inputRect = new Rect(0f, Screen.height - OnScreenConsoleSettings.InputRectHeight, Screen.width * 0.8f, OnScreenConsoleSettings.InputRectHeight);
                bool hasFocus = GUI.GetNameOfFocusedControl() == InputControlName;

                // EVENTS
                OnHandleEvents(hasFocus);

                // INPUT
                OnInputGUI(inputRect, hasFocus);

                // OPTIONS
                if (hasFocus && !string.IsNullOrWhiteSpace(m_currentInputString))
                {
                    OnOptionsGUI(inputRect.y, inputRect.width);
                }
            }
        }

        private void OnHandleEvents(bool hasFocus)
        {
            if (hasFocus && Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Return:
                        Event.current.Use();
                        Validate();
                        break;

                    case KeyCode.UpArrow:
                        OnGetPreviousCommand();
                        break;
                    
                    case KeyCode.DownArrow:
                        OnGetNextCommand();
                        break;
                }
            }
        }

        private void OnInputGUI(Rect rect, bool hasFocus)
        {
            GUIHelper.DrawRect(rect, hasFocus ? GUIHelper.transparentBlack07 : GUIHelper.transparentBlack03);

            GUI.SetNextControlName(InputControlName);

            GUIHelper.BeginChangeCheck();
            m_currentInputString = GUI.TextField(rect, m_currentInputString, m_isCurrentInputValid ? m_validInputStyle : m_inputStyle);
            if (GUIHelper.EndChangeCheck())
            {
                OnInputChanged();
            }

            if (m_justOpenedConsole)
            {
                m_justOpenedConsole = false;
                GUI.FocusControl(InputControlName);
            }
        }

        private void OnOptionsGUI(float y, float width)
        {
            float optionRectHeight = OnScreenConsoleSettings.OptionRectHeight;

            float scrollViewRectHeight = optionRectHeight * OnScreenConsoleSettings.MaxOptionsDisplayed;
            var scrollViewRect = new Rect(0, y - scrollViewRectHeight, width, scrollViewRectHeight);
            var viewRect = new Rect(0, 0, width - 25f, Mathf.Max(scrollViewRectHeight, optionRectHeight * m_currentInputOptions.Count));

            GUIHelper.DrawRect(scrollViewRect, GUIHelper.transparentBlack01);

            m_optionsScrollPos = GUI.BeginScrollView(scrollViewRect, m_optionsScrollPos, viewRect);

            var optionRect = new Rect(0, viewRect.height, viewRect.width, optionRectHeight);

            for (int i = 0; i < m_currentInputOptions.Count; i++)
            {
                optionRect.y -= optionRectHeight;
                GUIHelper.DrawRect(optionRect, i % 2 == 0 ? GUIHelper.transparentBlack03 : GUIHelper.transparentBlack05);

                if (GUI.Button(optionRect, m_currentInputOptions[i].ToString(), m_optionStyle))
                {
                    m_currentInputString = m_currentInputOptions[i].ToStringWithoutParams();
                    OnInputChanged();
                }
            }

            GUI.EndScrollView();
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

        public static void Init()
        {
            GetInstance();
        }
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