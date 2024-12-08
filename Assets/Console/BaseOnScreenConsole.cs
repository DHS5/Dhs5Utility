using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dhs5.Utility.Console
{
    public delegate void ValidCommandCallback(ValidCommand validCmd);

    public abstract class BaseOnScreenConsole<T> : MonoBehaviour where T : BaseOnScreenConsole<T>
    {
        #region INSTANCE

        #region Members

        // INPUTS
        [Header("Inputs")]
        [SerializeField] protected InputAction m_openConsoleAction;
        [SerializeField] protected InputAction m_closeConsoleAction;

        // GUI STYLES
        protected GUIStyle m_inputStyle;
        protected GUIStyle m_validInputStyle;
        protected GUIStyle m_optionStyle;

        // GUI COLORS
        protected Color m_transparentBlack01 = new Color(0f, 0f, 0f, 0.1f);
        protected Color m_transparentBlack03 = new Color(0f, 0f, 0f, 0.3f);
        protected Color m_transparentBlack05 = new Color(0f, 0f, 0f, 0.5f);
        protected Color m_transparentBlack07 = new Color(0f, 0f, 0f, 0.7f);

        // PARAMETERS
        private bool m_justOpenedConsole;
        private string m_currentInputString;
        private bool m_isCurrentInputValid;
        private Vector2 m_optionsScrollPos;

        // TEXTURES
        private Texture2D _whiteTexture;
        private Texture2D WhiteTexture
        {
            get
            {
                if (_whiteTexture == null)
                {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                }
                return _whiteTexture;
            }
        }

        #endregion

        #region Properties

        public bool IsActive { get; private set; }

        #endregion

        #region Core Behaviour

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this as T;
        }

        protected virtual void OnEnable()
        {
            InitInputs();
        }
        protected virtual void OnDisable()
        {
            ClearInputs();
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
                fontSize = GetInputFontSize(),
                contentOffset = new Vector2(15, 0),
                normal = new GUIStyleState()
                {
                    textColor = GetInputTextColor(),
                }
            };
            m_validInputStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                richText = false,
                wordWrap = false,
                fontSize = GetInputFontSize(),
                contentOffset = new Vector2(15, 0),
                normal = new GUIStyleState()
                {
                    textColor = GetValidInputTextColor(),
                }
            };
            m_optionStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleLeft,
                richText = false,
                wordWrap = false,
                fontSize = GetOptionFontSize(),
                contentOffset = new Vector2(20, 0),
                normal = new GUIStyleState()
                {
                    textColor = GetOptionTextColor(),
                }
            };
        }

        protected virtual int GetInputFontSize() => 40;
        protected virtual Color GetInputTextColor() => Color.white;
        protected virtual Color GetValidInputTextColor() => Color.green;
        protected virtual int GetOptionFontSize() => 30;
        protected virtual Color GetOptionTextColor() => Color.white;

        #endregion

        #region Inputs Management

        protected virtual void InitInputs()
        {
            RegisterInputs(true);

            EnableOpenConsoleInput(true);
            EnableCloseConsoleInput(true);
        }
        protected virtual void ClearInputs()
        {
            EnableOpenConsoleInput(false);
            EnableCloseConsoleInput(false);

            RegisterInputs(false);
        }

        private void RegisterInputs(bool register)
        {
            if (m_openConsoleAction != null)
            {
                if (register) m_openConsoleAction.performed += OpenConsoleCallback;
                else m_openConsoleAction.performed -= OpenConsoleCallback;
            }
            if (m_closeConsoleAction != null)
            {
                if (register) m_closeConsoleAction.performed += CloseConsoleCallback;
                else m_closeConsoleAction.performed -= CloseConsoleCallback;
            }
        }

        protected void EnableOpenConsoleInput(bool enable)
        {
            if (m_openConsoleAction != null)
            {
                if (enable) m_openConsoleAction.Enable();
                else m_openConsoleAction.Disable();
            }
        }
        protected void EnableCloseConsoleInput(bool enable)
        {
            if (m_closeConsoleAction != null)
            {
                if (enable) m_closeConsoleAction.Enable();
                else m_closeConsoleAction.Disable();
            }
        }

        #endregion

        #region Activation

        private int m_lastActivationChangeFrame = -1;
        protected void OpenConsole()
        {
            if (IsActive || m_lastActivationChangeFrame == Time.frameCount) return;

            m_lastActivationChangeFrame = Time.frameCount;

            IsActive = true;

            m_currentInputString = string.Empty;
            m_justOpenedConsole = true;
            InitStyles();

            OnOpenConsole();
        }
        private void OpenConsoleCallback(InputAction.CallbackContext callbackContext)
        {
            OpenConsole();
        }
        protected void CloseConsole()
        {
            if (!IsActive || m_lastActivationChangeFrame == Time.frameCount) return;

            m_lastActivationChangeFrame = Time.frameCount;

            IsActive = false;

            OnCloseConsole();
        }
        private void CloseConsoleCallback(InputAction.CallbackContext callbackContext)
        {
            CloseConsole();
        }

        protected virtual void OnOpenConsole() { }
        protected virtual void OnCloseConsole() { }

        #endregion


        #region Registration

        private Dictionary<IConsoleCommand, ValidCommandCallback> m_registeredCommands = new();

        protected void RegisterCommand(IConsoleCommand command, ValidCommandCallback callback)
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
        protected void UnregisterCommand(IConsoleCommand command, ValidCommandCallback callback)
        {
            if (m_registeredCommands.ContainsKey(command))
            {
                m_registeredCommands[command] -= callback;
            }
        }
        protected void UnregisterCommand(IConsoleCommand command)
        {
            m_registeredCommands.Remove(command);
        }

        protected virtual void OnRegisteredCommandsChanged()
        {
            RecomputeOptions();
            RecomputeCurrentInputValidity();
        }

        #endregion

        #region Options

        private List<CommandArray> m_currentInputOptions = new();
        private int m_currentlySelectedOptionIndex = -1;

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

            m_optionsScrollPos = new Vector2(0, GetOptionRectHeight() * Mathf.Max(0, m_currentInputOptions.Count - GetMaxOptionsDisplayed()));
            m_currentlySelectedOptionIndex = -1;
        }

        private void FillWithOptionAtIndex(int index)
        {
            m_currentInputString = m_currentInputOptions[index].ToStringWithoutParams();
            OnInputStringChanged();
        }

        private void OnSelectUpOption()
        {
            if (m_currentInputOptions.IsValid())
            {
                m_currentlySelectedOptionIndex = Mathf.Clamp(m_currentlySelectedOptionIndex + 1, 0, m_currentInputOptions.Count - 1);
            }
        }
        private void OnSelectDownOption()
        {
            if (m_currentInputOptions.IsValid())
            {
                m_currentlySelectedOptionIndex = Mathf.Clamp(m_currentlySelectedOptionIndex - 1, -1, m_currentInputOptions.Count - 1);
            }
        }
        private void FillWithSelectedOption()
        {
            if (m_currentlySelectedOptionIndex != -1)
            {
                FillWithOptionAtIndex(m_currentlySelectedOptionIndex);
            }
            else if (m_currentInputOptions.IsValid())
            {
                FillWithOptionAtIndex(0);
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
            AddToPreviousCommands(m_currentInputString);

            m_currentInputString = string.Empty;
        }

        #endregion

        #region Previous Commands

        private List<string> m_previousCommands = new();
        private int m_previousCommandMarker = 0;
        private string m_currentlyEditedCommand;

        private void AddToPreviousCommands(string cmd)
        {
            if (m_previousCommands.Count > 100) m_previousCommands.RemoveAt(0);
            m_previousCommands.Add(cmd);
            m_previousCommandMarker = m_previousCommands.Count;
        }

        private void OnGetPreviousCommand()
        {
            if (m_previousCommands.IsValid())
            {
                m_previousCommandMarker = Mathf.Clamp(m_previousCommandMarker - 1, 0, m_previousCommands.Count - 1);
                m_currentInputString = m_previousCommands[m_previousCommandMarker];
                OnInputStringChanged();
            }
        }
        private void OnGetNextCommand()
        {
            if (m_previousCommands.IsValid())
            {
                m_previousCommandMarker = Mathf.Clamp(m_previousCommandMarker + 1, 0, m_previousCommands.Count);
                m_currentInputString = m_previousCommandMarker == m_previousCommands.Count ? m_currentlyEditedCommand : m_previousCommands[m_previousCommandMarker];
                OnInputStringChanged();
            }
        }

        #endregion

        #region Callbacks

        private void OnPlayerInput()
        {
            m_currentlyEditedCommand = m_currentInputString;
            m_previousCommandMarker = m_previousCommands.Count;

            OnInputStringChanged();
        }
        private void OnInputStringChanged()
        {
            RecomputeOptions();
            RecomputeCurrentInputValidity();
        }

        #endregion


        #region GUI

        private void OnGUI()
        {
            if (IsActive)
            {
                float inputRectHeight = GetInputRectHeight();
                var inputRect = new Rect(0f, Screen.height - inputRectHeight, Screen.width * 0.8f, inputRectHeight);
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

                    case KeyCode.Tab:
                        FillWithSelectedOption();
                        Event.current.Use();
                        break;

                    case KeyCode.UpArrow:
                        if (Event.current.modifiers.HasFlag(EventModifiers.Control)) OnSelectUpOption();
                        else OnGetPreviousCommand();
                        Event.current.Use();
                        break;
                    
                    case KeyCode.DownArrow:
                        if (Event.current.modifiers.HasFlag(EventModifiers.Control)) OnSelectDownOption();
                        else OnGetNextCommand();
                        Event.current.Use();
                        break;
                }
            }
        }

        private void OnInputGUI(Rect rect, bool hasFocus)
        {
            DrawRect(rect, hasFocus ? m_transparentBlack07 : m_transparentBlack03);

            GUI.SetNextControlName(InputControlName);

            BeginInputChangeCheck();
            m_currentInputString = GUI.TextField(rect, m_currentInputString, m_isCurrentInputValid ? m_validInputStyle : m_inputStyle);
            if (EndInputChangeCheck())
            {
                OnPlayerInput();
            }

            if (m_justOpenedConsole)
            {
                m_justOpenedConsole = false;
                GUI.FocusControl(InputControlName);
            }
        }

        private void OnOptionsGUI(float y, float width)
        {
            float optionRectHeight = GetOptionRectHeight();

            float scrollViewRectHeight = optionRectHeight * GetMaxOptionsDisplayed();
            var scrollViewRect = new Rect(0, y - scrollViewRectHeight, width, scrollViewRectHeight);
            var viewRect = new Rect(0, 0, width - 25f, Mathf.Max(scrollViewRectHeight, optionRectHeight * m_currentInputOptions.Count));

            DrawRect(scrollViewRect, m_transparentBlack01);

            m_optionsScrollPos = GUI.BeginScrollView(scrollViewRect, m_optionsScrollPos, viewRect);

            var optionRect = new Rect(0, viewRect.height, viewRect.width, optionRectHeight);
            bool selected = false;

            for (int i = 0; i < m_currentInputOptions.Count; i++)
            {
                selected = m_currentlySelectedOptionIndex == i;
                optionRect.y -= optionRectHeight;
                DrawRect(optionRect, selected ? Color.black : (i % 2 == 0 ? m_transparentBlack03 : m_transparentBlack05));

                if (GUI.Button(optionRect, m_currentInputOptions[i].ToString(), m_optionStyle))
                {
                    FillWithOptionAtIndex(i);
                }
            }

            GUI.EndScrollView();
        }

        #endregion

        #region GUI Parameters

        // CONSTS
        private const string InputControlName = "Command Input";

        // 
        protected virtual float GetInputRectHeight() => 50f;
        protected virtual float GetOptionRectHeight() => 30f;
        protected virtual int GetMaxOptionsDisplayed() => 10;

        #endregion

        #region GUI Helper

        private void DrawRect(Rect rect, Color color)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Color color2 = GUI.color;
                GUI.color *= color;
                GUI.DrawTexture(rect, WhiteTexture);
                GUI.color = color2;
            }
        }

        private string m_inputStringBeforeChangeCheck;
        public void BeginInputChangeCheck()
        {
            m_inputStringBeforeChangeCheck = m_currentInputString;
        }
        public bool EndInputChangeCheck()
        {
            return m_inputStringBeforeChangeCheck != m_currentInputString;
        }

        #endregion

        #endregion

        // ---------- ---------- ---------- 

        #region STATIC

        #region Instance Creation

        private static T Instance { get; set; }

        private static void CreateInstance()
        {
            var obj = new GameObject("OnScreen Console");
            obj.AddComponent<T>();
        }

        private static T GetInstance()
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

        public static bool Register(IConsoleCommand command, ValidCommandCallback callback)
        {
            if (command.IsValid())
            {
                GetInstance().RegisterCommand(command, callback);
                return true;
            }
            return false;
        }
        
        public static void Unregister(IConsoleCommand command, ValidCommandCallback callback)
        {
            GetInstance().UnregisterCommand(command, callback);
        }
        public static void Unregister(IConsoleCommand command)
        {
            GetInstance().UnregisterCommand(command);
        }

        #endregion

        #endregion
    }
}