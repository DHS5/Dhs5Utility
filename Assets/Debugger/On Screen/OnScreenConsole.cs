using System;
using Dhs5.Utility.GUIs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dhs5.Utility.Debugger
{
    /// <summary>
    /// On screen console to run <see cref="ConsoleCommandAttribute"/> commands in game.<br></br>
    /// Only compiled in the editor and development builds : in release builds, only the public API remains
    /// (<see cref="IsOpened"/> is always false, <see cref="Opened"/>/<see cref="Closed"/> never fire, <see cref="Open"/> does nothing)
    /// so that game code using it still compiles.
    /// </summary>
    public class OnScreenConsole : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        #region INSTANCE

        #region Consts

        private const string ConsoleCommandTextFieldControl = "ConsoleCommandTextField";

        #endregion

        #region Members

        // GUI COLORS
        protected Color m_transparentBlack01 = new Color(0f, 0f, 0f, 0.1f);
        protected Color m_transparentBlack03 = new Color(0f, 0f, 0f, 0.3f);
        protected Color m_transparentBlack05 = new Color(0f, 0f, 0f, 0.5f);
        protected Color m_transparentBlack07 = new Color(0f, 0f, 0f, 0.7f);

        // PARAMETERS
        private bool m_justOpenedConsole;
        private Vector2 m_optionsScrollPos;
        private int m_lastScrolledOptionIndex = -1;
        private int m_lastScrolledOptionsCount = -1;

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
            Instance = this;
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
            if (DebuggerAsset.TryGetOpenOnScreenConsoleInputRef(out var openInputRef))
            {
                if (register) openInputRef.action.performed += OpenConsoleCallback;
                else openInputRef.action.performed -= OpenConsoleCallback;
            }
            if (DebuggerAsset.TryGetCloseOnScreenConsoleInputRef(out var closeInputRef))
            {
                if (register) closeInputRef.action.performed += CloseConsoleCallback;
                else closeInputRef.action.performed -= CloseConsoleCallback;
            }
        }

        protected void EnableOpenConsoleInput(bool enable)
        {
            if (DebuggerAsset.TryGetOpenOnScreenConsoleInputRef(out var openInputRef))
            {
                if (enable) openInputRef.action.Enable();
                else openInputRef.action.Disable();
            }
        }
        protected void EnableCloseConsoleInput(bool enable)
        {
            if (DebuggerAsset.TryGetCloseOnScreenConsoleInputRef(out var closeInputRef))
            {
                if (enable) closeInputRef.action.Enable();
                else closeInputRef.action.Disable();
            }
        }

        #endregion

        #region Activation

        private int m_lastActivationChangeFrame = -1;
        /// <summary>
        /// Last frame the close console input was performed, or the open input opened the console :
        /// that key press must not be typed in the command line.<br></br>
        /// The open key stays typable once the console is opened.
        /// </summary>
        private int m_lastConsoleInputFrame = -1;

        protected void OpenConsole()
        {
            if (IsActive || m_lastActivationChangeFrame == Time.frameCount) return;

            m_lastActivationChangeFrame = Time.frameCount;

            IsActive = true;

            m_justOpenedConsole = true;

            OnOpenConsole();
            Opened?.Invoke();
        }
        private void OpenConsoleCallback(InputAction.CallbackContext callbackContext)
        {
            var wasActive = IsActive;
            OpenConsole();
            // Only the key press that opens the console is filtered : the open key can be typed afterwards
            if (!wasActive && IsActive)
            {
                m_lastConsoleInputFrame = Time.frameCount;
            }
        }
        protected void CloseConsole()
        {
            if (!IsActive || m_lastActivationChangeFrame == Time.frameCount) return;

            m_lastActivationChangeFrame = Time.frameCount;

            IsActive = false;

            OnCloseConsole();
            Closed?.Invoke();
        }
        private void CloseConsoleCallback(InputAction.CallbackContext callbackContext)
        {
            m_lastConsoleInputFrame = Time.frameCount;
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
                // The key of the open/close console input must not be typed in the command line
                // (its key events can reach the GUI the frame it was performed, or the next one)
                if (Event.current.type == EventType.KeyDown && Time.frameCount - m_lastConsoleInputFrame <= 1)
                {
                    Event.current.Use();
                }

                var sizes = DebuggerAsset.OnScreenGUI;
                var scale = sizes.Scale;
                float inputRectHeight = sizes.consoleInputHeight * scale;
                var inputRect = new Rect(0f, Screen.height - inputRectHeight - 7f * scale, Screen.width, inputRectHeight);
                bool hasFocus = GUI.GetNameOfFocusedControl() == ConsoleCommandTextFieldControl;

                // The command line keeps the focus while the console is opened :
                // after a click elsewhere, the next click or key press gives it back
                if (!hasFocus && Event.current.type is EventType.KeyDown or EventType.MouseDown)
                {
                    GUI.FocusControl(ConsoleCommandTextFieldControl);
                }

                // EVENTS
                OnHandleEvents(hasFocus);

                // INPUT
                OnInputGUI(inputRect, hasFocus, sizes);

                // OPTIONS
                if (hasFocus)
                {
                    OnOptionsGUI(inputRect.y, inputRect.width * sizes.consoleOptionsWidthRatio, sizes);
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
                        ConsoleCommandsRegister.SelectNextOption();
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
                        ConsoleCommandsRegister.SelectPreviousOption();
                    }
                }
            }
        }

        private void OnInputGUI(Rect rect, bool hasFocus, DebuggerAsset.OnScreenGUISettings sizes)
        {
            var prevInputFontSize = GUI.skin.textField.fontSize;
            var prevLabelFontSize = GUI.skin.label.fontSize;
            var prevInputAlignment = GUI.skin.textField.alignment;
            var prevLabelAlignment = GUI.skin.label.alignment;
            GUI.skin.textField.fontSize = sizes.ScaledFont(sizes.consoleFontSize);
            GUI.skin.label.fontSize = sizes.ScaledFont(sizes.consoleFontSize);
            GUI.skin.textField.alignment = TextAnchor.MiddleLeft;
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUI.SetNextControlName(ConsoleCommandTextFieldControl);
            ConsoleCommandsRegister.CommandLineContent = GUI.TextField(rect, ConsoleCommandsRegister.CommandLineContent);
            using (new GUIHelper.GUIContentColorScope(new Color(1f, 1f, 1f, 0.3f)))
            {
                GUI.Label(new Rect(rect.x + 2f, rect.y, rect.width, rect.height), ConsoleCommandsRegister.GetHintString());
            }
            GUI.skin.textField.fontSize = prevInputFontSize;
            GUI.skin.label.fontSize = prevLabelFontSize;
            GUI.skin.textField.alignment = prevInputAlignment;
            GUI.skin.label.alignment = prevLabelAlignment;

            if (m_justOpenedConsole)
            {
                m_justOpenedConsole = false;
                GUI.FocusControl(ConsoleCommandTextFieldControl);
            }
        }

        private void OnOptionsGUI(float y, float width, DebuggerAsset.OnScreenGUISettings sizes)
        {
            var optionsCount = ConsoleCommandsRegister.CurrentOptionsCount;
            float optionRectHeight = sizes.consoleOptionHeight * sizes.Scale;
            float scrollViewRectHeight = sizes.consoleOptionsMaxHeight * sizes.Scale;
            var scrollViewRect = new Rect(0, y - scrollViewRectHeight, width, scrollViewRectHeight);
            var viewRect = new Rect(0, 0, width - 25f, Mathf.Max(scrollViewRectHeight, optionRectHeight * optionsCount));

            GUIHelper.DrawRect(scrollViewRect, m_transparentBlack01);

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
                    ConsoleCommand.EMatchResult.ACCEPTED_MATCH => Color.greenYellow,
                    ConsoleCommand.EMatchResult.PERFECT_MATCH => Color.green,
                    _ => Color.white
                };

                optionRect.y -= optionRectHeight;

                if (selected)
                {
                    GUIHelper.DrawRect(optionRect, Color.gray1);
                    // Only when the selection changes : the list can be scrolled freely otherwise
                    if (index != m_lastScrolledOptionIndex || optionsCount != m_lastScrolledOptionsCount)
                    {
                        m_lastScrolledOptionIndex = index;
                        m_lastScrolledOptionsCount = optionsCount;
                        GUI.ScrollTo(optionRect);
                    }
                }

                var prevFontSize = GUI.skin.label.fontSize;
                GUI.skin.label.fontSize = sizes.ScaledFont(sizes.consoleOptionFontSize);
                using (new GUIHelper.GUIContentColorScope(color))
                {
                    GUI.Label(new Rect(optionRect.x + 2f, optionRect.y, optionRect.width - 4f, optionRect.height), option);
                }
                GUI.skin.label.fontSize = prevFontSize;

                index++;
            }

            GUI.EndScrollView(false);
        }

        #endregion

        #endregion
#else
        // Release build : the on screen console is not compiled
        public bool IsActive => false;
        protected virtual void OnOpenConsole() { }
        protected virtual void OnCloseConsole() { }
#endif


        #region STATIC

        #region Engine Callbacks

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Opened = null;
            Closed = null;
        }

        #endregion

        #region Events & State

#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
#pragma warning disable CS0414 // Never fired in release builds
#endif
        /// <summary>
        /// Triggered when the on screen console opens, e.g. to disable the game inputs while typing commands
        /// </summary>
        public static event Action Opened;
        /// <summary>
        /// Triggered when the on screen console closes
        /// </summary>
        public static event Action Closed;
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
#pragma warning restore CS0414
#endif

        /// <summary>
        /// Whether the on screen console is currently opened
        /// </summary>
        public static bool IsOpened =>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Instance != null && Instance.IsActive;
#else
            false;
#endif

        #endregion

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        #region Instance Creation

        private static OnScreenConsole Instance { get; set; }

        private static void CreateInstance()
        {
            if (DebuggerAsset.EnableOnScreenConsole)
            {
                var obj = new GameObject("OnScreen Console");
                Instance = obj.AddComponent<OnScreenConsole>();
            }
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

        #region Activation

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Init()
        {
            GetInstance();
        }
        public static void Open()
        {
            var instance = GetInstance();
            if (instance != null)
            {
                instance.OpenConsole();
            }
        }

        #endregion
#else
        // Release build : the on screen console is not compiled
        public static void Init() { }
        public static void Open() { }
#endif

        #endregion
    }
}