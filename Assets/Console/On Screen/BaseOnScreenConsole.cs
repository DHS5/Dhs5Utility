using Dhs5.Utility.GUIs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dhs5.Utility.Console
{
    public abstract class BaseOnScreenConsole<T> : MonoBehaviour where T : BaseOnScreenConsole<T>
    {
        #region INSTANCE

        #region Consts

        private const string ConsoleCommandTextFieldControl = "ConsoleCommandTextField";

        #endregion

        #region Members

        // INPUTS
        [Header("Inputs")]
        [SerializeField] protected InputAction m_openConsoleAction;
        [SerializeField] protected InputAction m_closeConsoleAction;

        // GUI COLORS
        protected Color m_transparentBlack01 = new Color(0f, 0f, 0f, 0.1f);
        protected Color m_transparentBlack03 = new Color(0f, 0f, 0f, 0.3f);
        protected Color m_transparentBlack05 = new Color(0f, 0f, 0f, 0.5f);
        protected Color m_transparentBlack07 = new Color(0f, 0f, 0f, 0.7f);

        // PARAMETERS
        private bool m_justOpenedConsole;
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

            m_justOpenedConsole = true;

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


        #region GUI

        private void OnGUI()
        {
            if (IsActive)
            {
                float inputRectHeight = 50f;//TODO settings
                var inputRect = new Rect(0f, Screen.height - inputRectHeight, Screen.width, inputRectHeight);
                bool hasFocus = GUI.GetNameOfFocusedControl() == ConsoleCommandTextFieldControl;

                // EVENTS
                OnHandleEvents(hasFocus);

                // INPUT
                OnInputGUI(inputRect, hasFocus);

                // OPTIONS
                if (hasFocus)
                {
                    OnOptionsGUI(inputRect.y, inputRect.width);
                }
            }
        }

        private void OnHandleEvents(bool hasFocus)
        {
            var commandLineContentEmpty = string.IsNullOrWhiteSpace(ConsoleCommandsRegister.CommandLineContent);
            if (hasFocus && Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return
                    && !commandLineContentEmpty)
                {
                    Event.current.Use();
                    ConsoleCommandsRegister.ValidateCommand();
                }
                else if (Event.current.keyCode == KeyCode.Tab || Event.current.character == '\t')
                {
                    Event.current.Use();
                    ConsoleCommandsRegister.FillFromOption();
                    ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl))?.MoveLineEnd();
                }
                else if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    Event.current.Use();
                    if (Event.current.modifiers.HasFlag(EventModifiers.Control))
                    {
                        ConsoleCommandsRegister.SelectPreviousCommandInHistory();
                        ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl))?.MoveLineEnd();
                    }
                    else
                    {
                        ConsoleCommandsRegister.SelectPreviousOption();
                    }
                }
                else if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    Event.current.Use();
                    if (Event.current.modifiers.HasFlag(EventModifiers.Control))
                    {
                        ConsoleCommandsRegister.SelectNextCommandInHistory();
                        ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl))?.MoveLineEnd();
                    }
                    else
                    {
                        ConsoleCommandsRegister.SelectNextOption();
                    }
                }
            }
        }

        private void OnInputGUI(Rect rect, bool hasFocus)
        {
            DrawRect(rect, hasFocus ? m_transparentBlack07 : m_transparentBlack03);

            GUI.SetNextControlName(ConsoleCommandTextFieldControl);
            ConsoleCommandsRegister.CommandLineContent = GUI.TextField(rect, ConsoleCommandsRegister.CommandLineContent);
            using (new GUIHelper.GUIContentColorScope(new Color(1f, 1f, 1f, 0.3f)))
            {
                GUI.Label(new Rect(rect.x + 2f, rect.y - 2f, rect.width, rect.height), ConsoleCommandsRegister.GetHintString());
            }

            if (m_justOpenedConsole)
            {
                m_justOpenedConsole = false;
                GUI.FocusControl(ConsoleCommandTextFieldControl);
            }
        }

        private void OnOptionsGUI(float y, float width)
        {
            var optionsCount = ConsoleCommandsRegister.CurrentOptionsCount;
            float optionRectHeight = 30f;//TODO settings
            float scrollViewRectHeight = 300f;//TODO settings
            var scrollViewRect = new Rect(0, y - scrollViewRectHeight, width, scrollViewRectHeight);
            var viewRect = new Rect(0, 0, width - 25f, Mathf.Max(scrollViewRectHeight, optionRectHeight * optionsCount));

            DrawRect(scrollViewRect, m_transparentBlack01);

            m_optionsScrollPos = GUI.BeginScrollView(scrollViewRect, m_optionsScrollPos, viewRect);

            var optionRect = new Rect(0, viewRect.height, viewRect.width, optionRectHeight);

            var index = 0;
            foreach (var (option, matchResult) in ConsoleCommandsRegister.GetCurrentOptions())
            {
                var selected = ConsoleCommandsRegister.SelectedOptionIndex == index;
                var color = matchResult switch
                {
                    ConsoleCommand.EMatchResult.NAME_MATCH => Color.red,
                    ConsoleCommand.EMatchResult.PARTIAL_MATCH => Color.white,
                    ConsoleCommand.EMatchResult.PERFECT_MATCH => Color.green,
                    _ => Color.white
                };

                optionRect.y -= optionRectHeight;

                if (selected)
                {
                    DrawRect(optionRect, Color.gray1);
                    GUI.ScrollTo(optionRect);
                }

                index++;
            }

            GUI.EndScrollView(false);
        }

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

        #endregion
    }
}