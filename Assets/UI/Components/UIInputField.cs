using System.Text.RegularExpressions;
using System.Threading;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
using TMPro.EditorUtilities;
#endif

namespace Dhs5.Utility.UI
{
    public class UIInputField : UISelectable, IUpdateSelectedHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler,
        IPointerClickHandler, ISubmitHandler, ICancelHandler,
        ICanvasElement, ILayoutElement
    {
        #region ENUMS

        // Setting the content type acts as a shortcut for setting a combination of InputType, CharacterValidation, LineType, and TouchScreenKeyboardType
        public enum EContentType
        {
            Standard,
            Autocorrected,
            IntegerNumber,
            DecimalNumber,
            Alphanumeric,
            Name,
            EmailAddress,
            Password,
            Pin,
            Custom
        }

        public enum EInputType
        {
            Standard,
            AutoCorrect,
            Password,
        }

        public enum ECharacterValidation
        {
            None,
            Digit,
            Integer,
            Decimal,
            Alphanumeric,
            Name,
            Regex,
            EmailAddress,
            CustomValidator
        }

        public enum ELineType
        {
            SingleLine,
            MultiLineSubmit,
            MultiLineNewline
        }

        protected enum EEditState
        {
            Continue,
            Finish
        }

        #endregion

        #region Delegates

        public delegate char TextSelectionEvent(string text, int positionIndex, int selectPositionIndex);
        public delegate char ValidateInputEvent(string text, int charIndex, char addedChar);

        #endregion


        #region Static & Consts

        static protected readonly char[] kSeparators = { ' ', '.', ',', '\t', '\r', '\n' };

#if UNITY_ANDROID
        static protected bool s_IsQuestDeviceEvaluated = false;
#endif // if UNITY_ANDROID

        static protected bool s_IsQuestDevice = false;

        // Doesn't include dot and @ on purpose! See usage for details.
        protected const string kEmailSpecialCharacters = "!#$%&'*+-/=?^_`{|}~";
        protected const string kOculusQuestDeviceModel = "Oculus Quest";

        protected const float kHScrollSpeed = 0.05f;
        protected const float kVScrollSpeed = 0.10f;

        #endregion

        #region Members

        protected RectTransform m_rectTransform;

        [SerializeField] protected RectTransform m_textViewport;
        protected RectMask2D m_textViewportRectMask;
        //private Rect m_CachedViewportRect;

        [SerializeField] protected TMP_Text m_textComponent;
        protected RectTransform m_textComponentRectTransform;
        protected RectMask2D m_textComponentRectMask;

        [SerializeField] protected Graphic m_placeholder;
        [SerializeField] protected UIScrollbar m_verticalScrollbar;
        //private bool m_ForceDeactivation;

        protected bool m_isDrivenByLayoutComponents = false;
        protected LayoutGroup m_layoutGroup;

        protected IScrollHandler m_scrollHandlerParent;

        /// <summary>
        /// Used to keep track of scroll position
        /// </summary>
        protected float m_scrollPosition;

        [SerializeField] protected float m_scrollSensitivity = 1.0f;
        //[SerializeField] protected TMP_Text m_PlaceholderTextComponent;

        [SerializeField] protected EContentType m_contentType = EContentType.Standard;
        /// <summary>
        /// Type of data expected by the input field.
        /// </summary>
        [SerializeField] protected EInputType m_inputType = EInputType.Standard;
        /// <summary>
        /// The character used to hide text in password field.
        /// </summary>
        [SerializeField] protected char m_passwordChar = '*';
        /// <summary>
        /// Keyboard type applies to mobile keyboards that get shown.
        /// </summary>
        [SerializeField] protected TouchScreenKeyboardType m_keyboardType = TouchScreenKeyboardType.Default;
        [SerializeField] protected ELineType m_lineType = ELineType.SingleLine;

        /// <summary>
        /// Should hide mobile input field part of the virtual keyboard.
        /// </summary>
        [SerializeField] protected bool m_hideMobileInput = false;
        /// <summary>
        /// Should hide soft / virtual keyboard.
        /// </summary>
        [SerializeField] protected bool m_hideSoftKeyboard = false;
        /// <summary>
        /// What kind of validation to use with the input field's data.
        /// </summary>
        [SerializeField] protected ECharacterValidation m_characterValidation = ECharacterValidation.None;
        /// <summary>
        /// The Regex expression used for validating the text input.
        /// </summary>
        [SerializeField] protected string m_regexValue = string.Empty;
        /// <summary>
        /// The point sized used by the placeholder and input text object.
        /// </summary>
        [SerializeField] protected float m_globalPointSize = 14;
        /// <summary>
        /// Maximum number of characters allowed before input no longer works.
        /// </summary>
        [SerializeField] protected int m_characterLimit = 0;

        [SerializeField] protected Color m_caretColor = new Color(50f / 255f, 50f / 255f, 50f / 255f, 1f);
        [SerializeField] protected bool m_customCaretColor = false;
        [SerializeField] protected Color m_selectionColor = new Color(168f / 255f, 206f / 255f, 255f / 255f, 192f / 255f);

        /// <summary>
        /// Input field's value.
        /// </summary>
        [SerializeField][TextArea(5, 10)] protected string m_text = string.Empty;

        [SerializeField][Range(0f, 4f)] protected float m_caretBlinkRate = 0.85f;
        [SerializeField][Range(1, 5)] protected int m_caretWidth = 1;
        [SerializeField] protected bool m_readOnly = false;
        [SerializeField] protected bool m_richText = true;

        [SerializeField] protected TMP_FontAsset m_globalFontAsset;
        [SerializeField] protected bool m_onFocusSelectAll = true;
        protected bool m_isSelectAll;
        [SerializeField] protected bool m_resetOnDeActivation = true;
        protected bool m_selectionStillActive = false;
        protected bool m_releaseSelection = false;
        protected KeyCode m_lastKeyCode;

        protected GameObject m_previouslySelectedObject;

        [SerializeField] protected bool m_keepTextSelectionVisible;
        [SerializeField] protected bool m_restoreOriginalTextOnEscape = true;
        [SerializeField] protected bool m_isRichTextEditingAllowed = false;

        [SerializeField] protected int m_lineLimit = 0;

        protected bool m_isTouchScreenKeyboardAlert;

        [SerializeField] protected TMP_InputValidator m_inputValidator = null;
        [SerializeField] protected bool m_shouldActivateOnSelect = true;

        protected TouchScreenKeyboard m_softKeyboard;

        protected int m_stringPosition = 0;
        protected int m_stringSelectPosition = 0;
        protected int m_caretPosition = 0;
        protected int m_caretSelectPosition = 0;

        protected RectTransform m_caretRect = null;
        protected UIVertex[] m_cursorVerts = null;
        protected CanvasRenderer m_cachedInputRenderer;
        protected Vector2 m_lastPosition;

        [NonSerialized] protected Mesh m_mesh;
        protected bool m_allowInput = false;
        protected bool m_hadFocusBefore = false;
        //bool m_HasLostFocus = false;
        protected bool m_shouldActivateNextUpdate = false;
        protected bool m_updateDrag = false;
        protected bool m_dragPositionOutOfBounds = false;
        protected bool m_caretVisible;
        protected Coroutine m_blinkCoroutine = null;
        protected float m_blinkStartTime = 0.0f;
        protected Coroutine m_dragCoroutine = null;
        protected string m_originalText = "";
        protected bool m_wasCanceled = false;
        protected bool m_hasDoneFocusTransition = false;
        protected WaitForSecondsRealtime m_waitForSecondsRealtime;
        protected bool m_preventCallback = false;

        protected bool m_touchKeyboardAllowsInPlaceEditing = false;

        protected bool m_isTextComponentUpdateRequired = false;

        protected bool m_hasTextBeenRemoved = false;
        protected float m_pointerDownClickStartTime;
        protected float m_keyDownStartTime;
        protected float m_doubleClickDelay = 0.5f;

        protected bool m_isApplePlatform = false;

        protected bool m_isCompositionActive = false;
        protected bool m_shouldUpdateIMEWindowPosition = false;
        protected int m_previousIMEInsertionLine = 0;


        protected bool m_isSelected;
        protected bool m_isStringPositionDirty;
        protected bool m_isCaretPositionDirty;
        protected bool m_forceRectTransformAdjustment;

        // Primary to track when an user presses on the X to close the keyboard in the HoloLens
        protected bool m_isKeyboardBeingClosedInHoloLens = false;

        /// <summary>
        /// Handle the specified event.
        /// </summary>
        protected Event m_processingEvent = new Event();

        #endregion

        #region Properties

        protected virtual BaseInput InputSystem
        {
            get
            {
                if (EventSystem.current && EventSystem.current.currentInputModule)
                    return EventSystem.current.currentInputModule.input;
                return null;
            }
        }

        protected virtual string CompositionString => InputSystem != null ? InputSystem.compositionString : Input.compositionString;
        
        protected virtual int CompositionLength => m_readOnly ? 0 : CompositionString.Length;

        protected virtual Mesh Mesh
        {
            get
            {
                if (m_mesh == null)
                    m_mesh = new Mesh();
                return m_mesh;
            }
        }

        /// <summary>
        /// Should the inputfield be automatically activated upon selection.
        /// </summary>
        public virtual bool ShouldActivateOnSelect
        {
            get => m_shouldActivateOnSelect && Application.platform != RuntimePlatform.tvOS;
            set => m_shouldActivateOnSelect = value;
        }

        /// <summary>
        /// Should the mobile keyboard input be hidden.
        /// </summary>
        public virtual bool ShouldHideMobileInput
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
#if UNITY_2022_1_OR_NEWER
                    case RuntimePlatform.WebGLPlayer:
#endif
                        return m_hideMobileInput;
                    default:
                        return true;
                }
            }
            set
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
#if UNITY_2022_1_OR_NEWER
                    case RuntimePlatform.WebGLPlayer:
#endif
                        SetPropertyUtility.SetStruct(ref m_hideMobileInput, value);
                        break;
                    default:
                        m_hideMobileInput = true;
                        break;
                }
            }
        }

        public virtual bool ShouldHideSoftKeyboard
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
#if UNITY_XR_VISIONOS_SUPPORTED
                    case RuntimePlatform.VisionOS:
#endif
                    case RuntimePlatform.WSAPlayerX86:
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerARM:
#if UNITY_2020_2_OR_NEWER
                    case RuntimePlatform.PS4:
#if !(UNITY_2020_2_1 || UNITY_2020_2_2)
                    case RuntimePlatform.PS5:
#endif
#endif
#if UNITY_2019_4_OR_NEWER
                    case RuntimePlatform.GameCoreXboxOne:
                    case RuntimePlatform.GameCoreXboxSeries:
#endif
                    case RuntimePlatform.Switch:
                    case RuntimePlatform.Switch2:
#if UNITY_2022_1_OR_NEWER
                    case RuntimePlatform.WebGLPlayer:
#endif
                        return m_hideSoftKeyboard;
                    default:
                        return true;
                }
            }

            set
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.tvOS:
#if UNITY_XR_VISIONOS_SUPPORTED
                    case RuntimePlatform.VisionOS:
#endif
                    case RuntimePlatform.WSAPlayerX86:
                    case RuntimePlatform.WSAPlayerX64:
                    case RuntimePlatform.WSAPlayerARM:
#if UNITY_2020_2_OR_NEWER
                    case RuntimePlatform.PS4:
#if !(UNITY_2020_2_1 || UNITY_2020_2_2)
                    case RuntimePlatform.PS5:
#endif
#endif
#if UNITY_2019_4_OR_NEWER
                    case RuntimePlatform.GameCoreXboxOne:
                    case RuntimePlatform.GameCoreXboxSeries:
#endif
                    case RuntimePlatform.Switch:
                    case RuntimePlatform.Switch2:
#if UNITY_2022_1_OR_NEWER
                    case RuntimePlatform.WebGLPlayer:
#endif
                        SetPropertyUtility.SetStruct(ref m_hideSoftKeyboard, value);
                        break;
                    default:
                        m_hideSoftKeyboard = true;
                        break;
                }

                if (m_hideSoftKeyboard == true && m_softKeyboard != null && TouchScreenKeyboard.isSupported && m_softKeyboard.active)
                {
                    m_softKeyboard.active = false;
                    m_softKeyboard = null;
                }
            }
        }

        /// <summary>
        /// Input field's current text value. This is not necessarily the same as what is visible on screen.
        /// </summary>
        /// <remarks>
        /// Note that null is invalid value  for InputField.text.
        /// </remarks>
        public virtual string Text
        {
            get => m_text;
            set => SetText(value);
        }

        public virtual bool IsFocused => m_allowInput;

        public virtual float CaretBlinkRate
        {
            get => m_caretBlinkRate;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_caretBlinkRate, value))
                {
                    if (m_allowInput)
                        SetCaretActive();
                }
            }
        }

        public virtual int CaretWidth 
        { 
            get => m_caretWidth; 
            set 
            { 
                if (SetPropertyUtility.SetStruct(ref m_caretWidth, value)) 
                    MarkGeometryAsDirty();
            } 
        }

        public virtual RectTransform TextViewport 
        { 
            get => m_textViewport; 
            set 
            { 
                SetPropertyUtility.SetClass(ref m_textViewport, value); 
            } 
        }

        public virtual TMP_Text TextComponent
        {
            get => m_textComponent;
            set
            {
                if (SetPropertyUtility.SetClass(ref m_textComponent, value))
                {
                    SetTextComponentWrapMode();
                }
            }
        }

        //public TMP_Text placeholderTextComponent { get { return m_PlaceholderTextComponent; } set { SetPropertyUtility.SetClass(ref m_PlaceholderTextComponent, value); } }

        public virtual Graphic Placeholder 
        { 
            get => m_placeholder; 
            set 
            { 
                SetPropertyUtility.SetClass(ref m_placeholder, value); 
            } 
        }

        public virtual UIScrollbar VerticalScrollbar
        {
            get => m_verticalScrollbar;
            set
            {
                if (m_verticalScrollbar != null)
                    m_verticalScrollbar.ValueChanged -= OnScrollbarValueChange;

                SetPropertyUtility.SetClass(ref m_verticalScrollbar, value);

                if (m_verticalScrollbar)
                {
                    m_verticalScrollbar.ValueChanged += OnScrollbarValueChange;
                }
            }
        }

        public virtual float ScrollSensitivity 
        { 
            get => m_scrollSensitivity;
            set 
            { 
                if (SetPropertyUtility.SetStruct(ref m_scrollSensitivity, value)) 
                    MarkGeometryAsDirty(); 
            }
        }

        public virtual Color CaretColor 
        { 
            get => CustomCaretColor ? m_caretColor : TextComponent.color;
            set 
            { 
                if (SetPropertyUtility.SetColor(ref m_caretColor, value)) 
                    MarkGeometryAsDirty(); 
            } 
        }

        public virtual bool CustomCaretColor 
        { 
            get => m_customCaretColor; 
            set 
            { 
                if (m_customCaretColor != value) 
                { 
                    m_customCaretColor = value; 
                    MarkGeometryAsDirty();
                } 
            }
        }

        public virtual Color SelectionColor 
        { 
            get => m_selectionColor; 
            set 
            { 
                if (SetPropertyUtility.SetColor(ref m_selectionColor, value)) 
                    MarkGeometryAsDirty();
            } 
        }

        public virtual int CharacterLimit
        {
            get => m_characterLimit;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_characterLimit, Math.Max(0, value)))
                {
                    UpdateLabel();
                    if (m_softKeyboard != null)
                        m_softKeyboard.characterLimit = value;
                }
            }
        }

        //public bool isInteractableControl { set { if ( } }

        /// <summary>
        /// Set the point size on both Placeholder and Input text object.
        /// </summary>
        public virtual float PointSize
        {
            get => m_globalPointSize;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_globalPointSize, Math.Max(0, value)))
                {
                    SetGlobalPointSize(m_globalPointSize);
                    UpdateLabel();
                }
            }
        }

        /// <summary>
        /// Sets the Font Asset on both Placeholder and Input child objects.
        /// </summary>
        public virtual TMP_FontAsset FontAsset
        {
            get => m_globalFontAsset;
            set
            {
                if (SetPropertyUtility.SetClass(ref m_globalFontAsset, value))
                {
                    SetGlobalFontAsset(m_globalFontAsset);
                    UpdateLabel();
                }
            }
        }

        /// <summary>
        /// Determines if the whole text will be selected when focused.
        /// </summary>
        public virtual bool OnFocusSelectAll
        {
            get => m_onFocusSelectAll;
            set => m_onFocusSelectAll = value;
        }
        

        /// <summary>
        /// Determines if the text and caret position as well as selection will be reset when the input field is deactivated.
        /// </summary>
        public virtual bool ResetOnDeActivation
        {
            get => m_resetOnDeActivation; 
            set => m_resetOnDeActivation = value; 
        }
        
        /// <summary>
        /// Determines if the text selection will remain visible when the input field looses focus and is deactivated.
        /// </summary>
        public virtual bool KeepTextSelectionVisible
        {
            get => m_keepTextSelectionVisible;
            set => m_keepTextSelectionVisible = value;
        }

        /// <summary>
        /// Controls whether the original text is restored when pressing "ESC".
        /// </summary>
        public virtual bool RestoreOriginalTextOnEscape
        {
            get => m_restoreOriginalTextOnEscape;
            set => m_restoreOriginalTextOnEscape = value;       
        }

        /// <summary>
        /// Is Rich Text editing allowed?
        /// </summary>
        public virtual bool IsRichTextEditingAllowed
        {
            get => m_isRichTextEditingAllowed;
            set => m_isRichTextEditingAllowed = value;
        }

        // Content Type related
        public virtual EContentType ContentType 
        { 
            get => m_contentType;
            set 
            { 
                if (SetPropertyUtility.SetStruct(ref m_contentType, value)) 
                    EnforceContentType(); 
            } 
        }

        public virtual ELineType LineType
        {
            get => m_lineType;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_lineType, value))
                {
                    SetToCustomIfContentTypeIsNot(EContentType.Standard, EContentType.Autocorrected);
                    SetTextComponentWrapMode();
                }
            }
        }

        /// <summary>
        /// Limits the number of lines of text in the Input Field.
        /// </summary>
        public virtual int LineLimit
        {
            get => m_lineLimit;
            set
            {
                if (m_lineType == ELineType.SingleLine)
                    m_lineLimit = 1;
                else
                    SetPropertyUtility.SetStruct(ref m_lineLimit, value);

            }
        }

        /// <summary>
        /// The type of input expected. See InputField.InputType.
        /// </summary>
        public virtual EInputType InputType 
        { 
            get => m_inputType; 
            set 
            { 
                if (SetPropertyUtility.SetStruct(ref m_inputType, value)) 
                    SetToCustom();
            }
        }

        /// <summary>
        /// The TouchScreenKeyboard being used to edit the Input Field.
        /// </summary>
        public virtual TouchScreenKeyboard TouchScreenKeyboard => m_softKeyboard;

        /// <summary>
        /// They type of mobile keyboard that will be used.
        /// </summary>
        public virtual TouchScreenKeyboardType KeyboardType
        {
            get => m_keyboardType;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_keyboardType, value))
                    SetToCustom();
            }
        }

        /// <summary>
        /// The type of validation to perform on a character
        /// </summary>
        public virtual ECharacterValidation CharacterValidation 
        { 
            get => m_characterValidation;
            set 
            { 
                if (SetPropertyUtility.SetStruct(ref m_characterValidation, value)) 
                    SetToCustom(); 
            } 
        }

        /// <summary>
        /// Sets the Input Validation to use a Custom Input Validation script.
        /// </summary>
        public virtual TMP_InputValidator InputValidator
        {
            get => m_inputValidator;
            set 
            { 
                if (SetPropertyUtility.SetClass(ref m_inputValidator, value)) 
                    SetToCustom(ECharacterValidation.CustomValidator); 
            }
        }

        /// <summary>
        /// Determines if the keyboard is opened in alert mode.
        /// </summary>
        public virtual bool IsTouchScreenKeyboardAlert
        {
            get => m_isTouchScreenKeyboardAlert;
            set => m_isTouchScreenKeyboardAlert = value;
        }

        public virtual bool ReadOnly 
        { 
            get => m_readOnly;
            set => m_readOnly = value; 
        }

        public virtual bool RichText 
        { 
            get => m_richText; 
            set 
            { 
                m_richText = value; 
                SetTextComponentRichTextMode(); 
            } 
        }

        // Derived property
        public virtual bool MultiLine => m_lineType == ELineType.MultiLineNewline || LineType == ELineType.MultiLineSubmit;
        // Not shown in Inspector.
        public virtual char PasswordChar 
        { 
            get => m_passwordChar;
            set 
            { 
                if (SetPropertyUtility.SetStruct(ref m_passwordChar, value)) 
                    UpdateLabel(); 
            } 
        }
        public virtual bool WasCanceled => m_wasCanceled;

        /// <summary>
        /// Current position of the cursor.
        /// Getters are public Setters are protected
        /// </summary>

        protected virtual int CaretPositionInternal 
        { 
            get => m_caretPosition + CompositionLength;
            set 
            { 
                m_caretPosition = value; 
                ClampCaretPos(ref m_caretPosition);
            } 
        }
        protected virtual int StringPositionInternal 
        { 
            get => m_stringPosition + CompositionLength;
            set 
            { 
                m_stringPosition = value; 
                ClampStringPos(ref m_stringPosition); 
            } 
        }
        protected virtual int CaretSelectPositionInternal 
        { 
            get => m_caretSelectPosition + CompositionLength; 
            set 
            { 
                m_caretSelectPosition = value;
                ClampCaretPos(ref m_caretSelectPosition);
            }
        }
        protected virtual int StringSelectPositionInternal
        {
            get => m_stringSelectPosition + CompositionLength; 
            set 
            {
                m_stringSelectPosition = value;
                ClampStringPos(ref m_stringSelectPosition); 
            } 
        }

        protected virtual bool IsSelecting => StringPositionInternal != StringSelectPositionInternal;

        /// <summary>
        /// Get: Returns the focus position as thats the position that moves around even during selection.
        /// Set: Set both the anchor and focus position such that a selection doesn't happen
        /// </summary>
        public virtual int CaretPosition
        {
            get => CaretSelectPositionInternal;
            set 
            { 
                SelectionAnchorPosition = value; 
                SelectionFocusPosition = value; UpdateStringIndexFromCaretPosition(); 
            }
        }

        /// <summary>
        /// Get: Returns the fixed position of selection
        /// Set: If compositionString is 0 set the fixed position
        /// </summary>
        public virtual int SelectionAnchorPosition
        {
            get
            {
                return CaretPositionInternal;
            }

            set
            {
                if (CompositionLength != 0)
                    return;

                CaretPositionInternal = value;
                m_isStringPositionDirty = true;
            }
        }

        /// <summary>
        /// Get: Returns the variable position of selection
        /// Set: If compositionString is 0 set the variable position
        /// </summary>
        public virtual int SelectionFocusPosition
        {
            get
            {
                return CaretSelectPositionInternal;
            }
            set
            {
                if (CompositionLength != 0)
                    return;

                CaretSelectPositionInternal = value;
                m_isStringPositionDirty = true;
            }
        }

        public virtual int StringPosition
        {
            get => StringSelectPositionInternal;
            set 
            { 
                SelectionStringAnchorPosition = value; 
                SelectionStringFocusPosition = value; UpdateCaretPositionFromStringIndex(); 
            }
        }

        /// <summary>
        /// The fixed position of the selection in the raw string which may contains rich text.
        /// </summary>
        public virtual int SelectionStringAnchorPosition
        {
            get
            {
                return StringPositionInternal;
            }

            set
            {
                if (CompositionLength != 0)
                    return;

                StringPositionInternal = value;
                m_isCaretPositionDirty = true;
            }
        }

        /// <summary>
        /// The variable position of the selection in the raw string which may contains rich text.
        /// </summary>
        public virtual int SelectionStringFocusPosition
        {
            get
            {
                return StringSelectPositionInternal;
            }
            set
            {
                if (CompositionLength != 0)
                    return;

                StringSelectPositionInternal = value;
                m_isCaretPositionDirty = true;
            }
        }

        #endregion

        #region Static Properties

        protected static string clipboard
        {
            get
            {
                return GUIUtility.systemCopyBuffer;
            }
            set
            {
                GUIUtility.systemCopyBuffer = value;
            }
        }

        #endregion

        #region Events

        public event Action<string> ValueChanged;
        public event Action<string> EditEnded;
        public event Action<string> Submitted;
        public event Action<string> Focused;
        public event Action<string> Unfocused;

        public event TextSelectionEvent TextSelected;
        public event TextSelectionEvent TextDeselected;

        public event Action<TouchScreenKeyboard.Status> TouchScreenKeyboardStatusChanged;

        public event ValidateInputEvent InputValidation;

        #endregion

        #region Events Trigger

        protected void TriggerValueChangedAndUpdateLabel()
        {
            UpdateLabel();
            TriggerValueChanged();
        }

        protected void TriggerValueChanged()
        {
            EventContext = this;
            ValueChanged?.Invoke(Text);
        }

        protected void TriggerEndEdit()
        {
            EventContext = this;
            EditEnded?.Invoke(m_text);
        }

        protected void TriggerSubmit()
        {
            EventContext = this;
            Submitted?.Invoke(m_text);
        }

        protected void TriggerFocus()
        {
            EventContext = this;
            Focused?.Invoke(m_text);
        }

        protected void TriggerFocusLost()
        {
            EventContext = this;
            Unfocused?.Invoke(m_text);
        }

        protected void TriggerTextSelected()
        {
            m_isSelected = true;

            EventContext = this;
            TextSelected?.Invoke(m_text, StringPositionInternal, StringSelectPositionInternal);
        }

        protected void TriggerTextDeselected()
        {
            if (!m_isSelected) return;

            EventContext = this;
            TextDeselected?.Invoke(m_text, StringPositionInternal, StringSelectPositionInternal);

            m_isSelected = false;
        }

        protected void TriggerTouchScreenKeyboardStatusChanged()
        {
            if (m_softKeyboard != null)
            {
                EventContext = this;
                TouchScreenKeyboardStatusChanged?.Invoke(m_softKeyboard.status);
            }
        }

        protected bool TriggerInputValidation(string text, int charIndex, char addedChar, out char result)
        {
            if (InputValidation != null)
            {
                EventContext = this;
                result = InputValidation.Invoke(text, charIndex, addedChar);
                return true;
            }

            result = default;
            return false;
        }

        #endregion

        #region Core Behaviour

#if UNITY_ANDROID

        protected override void Awake()
        {
            base.Awake();

            if (s_IsQuestDeviceEvaluated)
                return;

            // Used for Oculus Quest 1 and 2 software keyboard regression.
            // TouchScreenKeyboard.isInPlaceEditingAllowed is always returning true in these devices and would prevent the software keyboard from showing up if that value was used.
            s_IsQuestDevice = SystemInfo.deviceModel == kOculusQuestDeviceModel;
            s_IsQuestDeviceEvaluated = true;
        }

#endif


        protected override void OnEnable()
        {
            //Debug.Log("*** OnEnable() *** - " + this.name);

            base.OnEnable();

            if (m_text == null)
                m_text = string.Empty;

            m_isApplePlatform = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX || SystemInfo.operatingSystem.Contains("iOS") || SystemInfo.operatingSystem.Contains("tvOS");

            // Check if Input Field is driven by any layout components
            ILayoutController layoutController = GetComponent<ILayoutController>();

            if (layoutController != null)
            {
                m_isDrivenByLayoutComponents = true;
                m_layoutGroup = GetComponent<LayoutGroup>();
            }
            else
                m_isDrivenByLayoutComponents = false;

            if (Application.isPlaying)
            {
                if (m_cachedInputRenderer == null && m_textComponent != null)
                {
                    GameObject go = new GameObject("Caret", typeof(TMP_SelectionCaret));

                    go.hideFlags = HideFlags.DontSave;
                    go.transform.SetParent(m_textComponent.transform.parent);
                    go.transform.SetAsFirstSibling();
                    go.layer = gameObject.layer;

                    m_caretRect = go.GetComponent<RectTransform>();
                    m_cachedInputRenderer = go.GetComponent<CanvasRenderer>();
                    m_cachedInputRenderer.SetMaterial(Graphic.defaultGraphicMaterial, Texture2D.whiteTexture);

                    // Needed as if any layout is present we want the caret to always be the same as the text area.
                    go.AddComponent<LayoutElement>().ignoreLayout = true;

                    AssignCaretPositioningIfNeeded();
                }
            }

            m_rectTransform = GetComponent<RectTransform>();

            // Check if parent component has IScrollHandler
            IScrollHandler[] scrollHandlers = GetComponentsInParent<IScrollHandler>();
            if (scrollHandlers.Length > 1)
            {
                if (scrollHandlers[1] is ScrollRect s1) m_scrollHandlerParent = s1;
                else if (scrollHandlers[1] is UIScrollRect s2) m_scrollHandlerParent = s2;
                else m_scrollHandlerParent = null;
            }

            // Get a reference to the RectMask 2D on the Viewport Text Area object.
            if (m_textViewport != null)
            {
                m_textViewportRectMask = m_textViewport.GetComponent<RectMask2D>();

                UpdateMaskRegions();
            }

            // If we have a cached renderer then we had OnDisable called so just restore the material.
            if (m_cachedInputRenderer != null)
                m_cachedInputRenderer.SetMaterial(Graphic.defaultGraphicMaterial, Texture2D.whiteTexture);

            if (m_textComponent != null)
            {
                m_textComponent.RegisterDirtyVerticesCallback(MarkGeometryAsDirty);
                m_textComponent.RegisterDirtyVerticesCallback(UpdateLabel);

                // Cache reference to Vertical Scrollbar RectTransform and add listener.
                if (m_verticalScrollbar != null)
                {
                    m_verticalScrollbar.ValueChanged += OnScrollbarValueChange;
                }

                UpdateLabel();
            }

#if UNITY_2019_1_OR_NEWER
            m_touchKeyboardAllowsInPlaceEditing = TouchScreenKeyboard.isInPlaceEditingAllowed;
#endif

            // Subscribe to event fired when text object has been regenerated.
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
        }

        protected override void OnDisable()
        {
            // the coroutine will be terminated, so this will ensure it restarts when we are next activated
            m_blinkCoroutine = null;

            DeactivateInputField();
            if (m_textComponent != null)
            {
                m_textComponent.UnregisterDirtyVerticesCallback(MarkGeometryAsDirty);
                m_textComponent.UnregisterDirtyVerticesCallback(UpdateLabel);

                if (m_verticalScrollbar != null)
                    m_verticalScrollbar.ValueChanged -= OnScrollbarValueChange;

            }
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            // Clear needs to be called otherwise sync never happens as the object is disabled.
            if (m_cachedInputRenderer != null)
                m_cachedInputRenderer.Clear();

            if (m_mesh != null)
                DestroyImmediate(m_mesh);

            m_mesh = null;

            // Unsubscribe to event triggered when text object has been regenerated
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ON_TEXT_CHANGED);

            base.OnDisable();
        }

#endregion


        #region Main Setters

        /// <summary>
        /// Set Input field's current text value without invoke onValueChanged. This is not necessarily the same as what is visible on screen.
        /// </summary>
        public virtual void SetTextWithoutNotify(string input)
        {
            SetText(input, false);
        }

        protected virtual void SetText(string value, bool sendCallback = true)
        {
            if (this.Text == value)
                return;

            if (value == null)
                value = "";

            value = value.Replace("\0", string.Empty); // remove embedded nulls

            m_text = value;

            /*
            if (m_LineType == LineType.SingleLine)
                value = value.Replace("\n", "").Replace("\t", "");

            // If we have an input validator, validate the input and apply the character limit at the same time.
            if (onValidateInput != null || characterValidation != CharacterValidation.None)
            {
                m_Text = "";
                OnValidateInput validatorMethod = onValidateInput ?? Validate;
                m_CaretPosition = m_CaretSelectPosition = value.Length;
                int charactersToCheck = characterLimit > 0 ? Math.Min(characterLimit, value.Length) : value.Length;
                for (int i = 0; i < charactersToCheck; ++i)
                {
                    char c = validatorMethod(m_Text, m_Text.Length, value[i]);
                    if (c != 0)
                        m_Text += c;
                }
            }
            else
            {
                m_Text = characterLimit > 0 && value.Length > characterLimit ? value.Substring(0, characterLimit) : value;
            }
            */

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                TriggerValueChangedAndUpdateLabel();
                return;
            }
#endif

            if (m_softKeyboard != null)
                m_softKeyboard.text = m_text;

            if (m_stringPosition > m_text.Length)
                m_stringPosition = m_stringSelectPosition = m_text.Length;
            else if (m_stringSelectPosition > m_text.Length)
                m_stringSelectPosition = m_text.Length;

            m_forceRectTransformAdjustment = true;

            m_isTextComponentUpdateRequired = true;
            UpdateLabel();

            if (sendCallback)
                TriggerValueChanged();
        }

        #endregion


        #region Utility

        protected virtual bool IsKeyboardUsingEvents()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return InPlaceEditing() && m_hideSoftKeyboard;
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.tvOS:
#if UNITY_XR_VISIONOS_SUPPORTED
                case RuntimePlatform.VisionOS:
#endif
                    return m_hideSoftKeyboard;
#if UNITY_2020_2_OR_NEWER
                case RuntimePlatform.PS4:
#if !(UNITY_2020_2_1 || UNITY_2020_2_2)
                case RuntimePlatform.PS5:
#endif
#endif
#if UNITY_2019_4_OR_NEWER
                case RuntimePlatform.GameCoreXboxOne:
                case RuntimePlatform.GameCoreXboxSeries:
#endif
                case RuntimePlatform.Switch:
                case RuntimePlatform.Switch2:
                    return false;
#if UNITY_2022_1_OR_NEWER
                case RuntimePlatform.WebGLPlayer:
                    return m_softKeyboard == null || !m_softKeyboard.active;
#endif
                default:
                    return true;
            }
        }

        protected virtual bool IsUWP()
        {
            return Application.platform == RuntimePlatform.WSAPlayerX86 || Application.platform == RuntimePlatform.WSAPlayerX64 || Application.platform == RuntimePlatform.WSAPlayerARM;
        }

        protected virtual void ClampStringPos(ref int pos)
        {
            if (pos <= 0)
                pos = 0;
            else if (pos > Text.Length)
                pos = Text.Length;
        }

        protected virtual void ClampCaretPos(ref int pos)
        {
            if (pos > m_textComponent.textInfo.characterCount - 1)
                pos = m_textComponent.textInfo.characterCount - 1;

            if (pos <= 0)
                pos = 0;
        }

        protected virtual int ClampArrayIndex(int index)
        {
            if (index < 0)
                return 0;

            return index;
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Method used to update the tracking of the caret position when the text object has been regenerated.
        /// </summary>
        /// <param name="obj"></param>
        protected virtual void ON_TEXT_CHANGED(UnityEngine.Object obj)
        {
            bool isThisObject = obj == m_textComponent;

            if (isThisObject && !m_isStringPositionDirty)
            {
                if (Application.isPlaying && CompositionLength == 0)
                {
                    UpdateCaretPositionFromStringIndex();

#if TMP_DEBUG_MODE
                    Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif
                }

                if (m_verticalScrollbar)
                    UpdateScrollbar();
            }
        }

        protected virtual void OnFocus()
        {
            if (m_onFocusSelectAll)
                SelectAll();
        }

        #endregion

        #region Caret Setters

        protected virtual IEnumerator CaretBlink()
        {
            // Always ensure caret is initially visible since it can otherwise be confusing for a moment.
            m_caretVisible = true;
            yield return null;

            while ((IsFocused || m_selectionStillActive) && m_caretBlinkRate > 0)
            {
                // the blink rate is expressed as a frequency
                float blinkPeriod = 1f / m_caretBlinkRate;

                // the caret should be ON if we are in the first half of the blink period
                bool blinkState = (Time.unscaledTime - m_blinkStartTime) % blinkPeriod < blinkPeriod / 2;
                if (m_caretVisible != blinkState)
                {
                    m_caretVisible = blinkState;
                    if (!IsSelecting)
                        MarkGeometryAsDirty();
                }

                // Then wait again.
                yield return null;
            }
            m_blinkCoroutine = null;
        }

        protected virtual void SetCaretVisible()
        {
            if (!m_allowInput)
                return;

            m_caretVisible = true;
            m_blinkStartTime = Time.unscaledTime;
            SetCaretActive();
        }

        // SetCaretActive will not set the caret immediately visible - it will wait for the next time to blink.
        // However, it will handle things correctly if the blink speed changed from zero to non-zero or non-zero to zero.
        protected virtual void SetCaretActive()
        {
            if (!m_allowInput)
                return;

            if (m_caretBlinkRate > 0.0f)
            {
                if (m_blinkCoroutine == null)
                    m_blinkCoroutine = StartCoroutine(CaretBlink());
            }
            else
            {
                m_caretVisible = true;
            }
        }

        #endregion

        #region Selection Utility

        protected virtual void SelectAll()
        {
            m_isSelectAll = true;
            StringPositionInternal = Text.Length;
            StringSelectPositionInternal = 0;
        }

        protected virtual string GetSelectedString()
        {
            if (!IsSelecting)
                return "";

            int startPos = StringPositionInternal;
            int endPos = StringSelectPositionInternal;

            // Ensure pos is always less then selPos to make the code simpler
            if (startPos > endPos)
            {
                int temp = startPos;
                startPos = endPos;
                endPos = temp;
            }

            //for (int i = m_CaretPosition; i < m_CaretSelectPosition; i++)
            //{
            //    Debug.Log("Character [" + m_TextComponent.textInfo.characterInfo[i].character + "] using Style [" + m_TextComponent.textInfo.characterInfo[i].style + "] has been selected.");
            //}


            return Text.Substring(startPos, endPos - startPos);
        }

        #endregion

        #region Move Actions

        /// <summary>
        /// Move to the end of the text.
        /// </summary>
        /// <param name="shift"></param>
        public virtual void MoveTextEnd(bool shift)
        {
            if (m_isRichTextEditingAllowed)
            {
                int position = Text.Length;

                if (shift)
                {
                    StringSelectPositionInternal = position;
                }
                else
                {
                    StringPositionInternal = position;
                    StringSelectPositionInternal = StringPositionInternal;
                }
            }
            else
            {
                int position = m_textComponent.textInfo.characterCount - 1;

                if (shift)
                {
                    CaretSelectPositionInternal = position;
                    StringSelectPositionInternal = GetStringIndexFromCaretPosition(position);
                }
                else
                {
                    CaretPositionInternal = CaretSelectPositionInternal = position;
                    StringSelectPositionInternal = StringPositionInternal = GetStringIndexFromCaretPosition(position);
                }
            }

            UpdateLabel();
        }

        /// <summary>
        /// Move to the start of the text.
        /// </summary>
        /// <param name="shift"></param>
        public virtual void MoveTextStart(bool shift)
        {
            if (m_isRichTextEditingAllowed)
            {
                int position = 0;

                if (shift)
                {
                    StringSelectPositionInternal = position;
                }
                else
                {
                    StringPositionInternal = position;
                    StringSelectPositionInternal = StringPositionInternal;
                }
            }
            else
            {
                int position = 0;

                if (shift)
                {
                    CaretSelectPositionInternal = position;
                    StringSelectPositionInternal = GetStringIndexFromCaretPosition(position);
                }
                else
                {
                    CaretPositionInternal = CaretSelectPositionInternal = position;
                    StringSelectPositionInternal = StringPositionInternal = GetStringIndexFromCaretPosition(position);
                }
            }

            UpdateLabel();
        }

        /// <summary>
        /// Move to the end of the current line of text.
        /// </summary>
        /// <param name="shift"></param>
        public virtual void MoveToEndOfLine(bool shift, bool ctrl)
        {
            // Get the line the caret is currently located on.
            int currentLine = m_textComponent.textInfo.characterInfo[CaretPositionInternal].lineNumber;

            // Get the last character of the given line.
            int characterIndex = ctrl == true ? m_textComponent.textInfo.characterCount - 1 : m_textComponent.textInfo.lineInfo[currentLine].lastCharacterIndex;

            int position = m_textComponent.textInfo.characterInfo[characterIndex].index;

            if (shift)
            {
                StringSelectPositionInternal = position;

                CaretSelectPositionInternal = characterIndex;
            }
            else
            {
                StringPositionInternal = position;
                StringSelectPositionInternal = StringPositionInternal;

                CaretSelectPositionInternal = CaretPositionInternal = characterIndex;
            }

            UpdateLabel();
        }

        /// <summary>
        /// Move to the start of the current line of text.
        /// </summary>
        /// <param name="shift"></param>
        public virtual void MoveToStartOfLine(bool shift, bool ctrl)
        {
            // Get the line the caret is currently located on.
            int currentLine = m_textComponent.textInfo.characterInfo[CaretPositionInternal].lineNumber;

            // Get the first character of the given line.
            int characterIndex = ctrl == true ? 0 : m_textComponent.textInfo.lineInfo[currentLine].firstCharacterIndex;

            int position = 0;
            if (characterIndex > 0)
                position = m_textComponent.textInfo.characterInfo[characterIndex - 1].index + m_textComponent.textInfo.characterInfo[characterIndex - 1].stringLength;

            if (shift)
            {
                StringSelectPositionInternal = position;

                CaretSelectPositionInternal = characterIndex;
            }
            else
            {
                StringPositionInternal = position;
                StringSelectPositionInternal = StringPositionInternal;

                CaretSelectPositionInternal = CaretPositionInternal = characterIndex;
            }

            UpdateLabel();
        }

        protected virtual int FindNextWordBegin()
        {
            if (StringSelectPositionInternal + 1 >= Text.Length)
                return Text.Length;

            int spaceLoc = Text.IndexOfAny(kSeparators, StringSelectPositionInternal + 1);

            if (spaceLoc == -1)
                spaceLoc = Text.Length;
            else
                spaceLoc++;

            return spaceLoc;
        }

        protected virtual void MoveRight(bool shift, bool ctrl)
        {
            if (IsSelecting && !shift)
            {
                // By convention, if we have a selection and move right without holding shift,
                // we just place the cursor at the end.
                StringPositionInternal = StringSelectPositionInternal = Mathf.Max(StringPositionInternal, StringSelectPositionInternal);
                CaretPositionInternal = CaretSelectPositionInternal = GetCaretPositionFromStringIndex(StringSelectPositionInternal);

#if TMP_DEBUG_MODE
                    Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif
                return;
            }

            int position;
            if (ctrl)
                position = FindNextWordBegin();
            else
            {
                if (m_isRichTextEditingAllowed)
                {
                    // Special handling for Surrogate pairs and Diacritical marks.
                    if (StringSelectPositionInternal < Text.Length && char.IsHighSurrogate(Text[StringSelectPositionInternal]))
                        position = StringSelectPositionInternal + 2;
                    else
                        position = StringSelectPositionInternal + 1;
                }
                else
                {
                    // Special handling for <CR><LF>
                    if (m_textComponent.textInfo.characterInfo[CaretSelectPositionInternal].character == '\r' && m_textComponent.textInfo.characterInfo[CaretSelectPositionInternal + 1].character == '\n')
                        position = m_textComponent.textInfo.characterInfo[CaretSelectPositionInternal + 1].index + m_textComponent.textInfo.characterInfo[CaretSelectPositionInternal + 1].stringLength;
                    else
                        position = m_textComponent.textInfo.characterInfo[CaretSelectPositionInternal].index + m_textComponent.textInfo.characterInfo[CaretSelectPositionInternal].stringLength;
                }

            }

            if (shift)
            {
                StringSelectPositionInternal = position;
                CaretSelectPositionInternal = GetCaretPositionFromStringIndex(StringSelectPositionInternal);
            }
            else
            {
                StringSelectPositionInternal = StringPositionInternal = position;

                // Only increase caret position as we cross character boundary.
                if (StringPositionInternal >= m_textComponent.textInfo.characterInfo[CaretPositionInternal].index + m_textComponent.textInfo.characterInfo[CaretPositionInternal].stringLength)
                    CaretSelectPositionInternal = CaretPositionInternal = GetCaretPositionFromStringIndex(StringSelectPositionInternal);
            }

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + "  Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + "  String Select Position: " + stringSelectPositionInternal);
#endif
        }

        protected virtual int FindPrevWordBegin()
        {
            if (StringSelectPositionInternal - 2 < 0)
                return 0;

            int spaceLoc = Text.LastIndexOfAny(kSeparators, StringSelectPositionInternal - 2);

            if (spaceLoc == -1)
                spaceLoc = 0;
            else
                spaceLoc++;

            return spaceLoc;
        }

        protected virtual void MoveLeft(bool shift, bool ctrl)
        {
            if (IsSelecting && !shift)
            {
                // By convention, if we have a selection and move left without holding shift,
                // we just place the cursor at the start.
                StringPositionInternal = StringSelectPositionInternal = Mathf.Min(StringPositionInternal, StringSelectPositionInternal);
                CaretPositionInternal = CaretSelectPositionInternal = GetCaretPositionFromStringIndex(StringSelectPositionInternal);

#if TMP_DEBUG_MODE
                    Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif
                return;
            }

            int position;
            if (ctrl)
                position = FindPrevWordBegin();
            else
            {
                if (m_isRichTextEditingAllowed)
                {
                    // Special handling for Surrogate pairs and Diacritical marks.
                    if (StringSelectPositionInternal > 0 && char.IsLowSurrogate(Text[StringSelectPositionInternal - 1]))
                        position = StringSelectPositionInternal - 2;
                    else
                        position = StringSelectPositionInternal - 1;
                }
                else
                {
                    position = CaretSelectPositionInternal < 1
                        ? m_textComponent.textInfo.characterInfo[0].index
                        : m_textComponent.textInfo.characterInfo[CaretSelectPositionInternal - 1].index;

                    // Special handling for <CR><LF>
                    if (position > 0 && m_textComponent.textInfo.characterInfo[CaretSelectPositionInternal - 1].character == '\n' && m_textComponent.textInfo.characterInfo[CaretSelectPositionInternal - 2].character == '\r')
                        position = m_textComponent.textInfo.characterInfo[CaretSelectPositionInternal - 2].index;
                }
            }

            if (shift)
            {
                StringSelectPositionInternal = position;
                CaretSelectPositionInternal = GetCaretPositionFromStringIndex(StringSelectPositionInternal);
            }
            else
            {
                StringSelectPositionInternal = StringPositionInternal = position;

                // Only decrease caret position as we cross character boundary.
                if (CaretPositionInternal > 0 && StringPositionInternal <= m_textComponent.textInfo.characterInfo[CaretPositionInternal - 1].index)
                    CaretSelectPositionInternal = CaretPositionInternal = GetCaretPositionFromStringIndex(StringSelectPositionInternal);
            }

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + "  Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + "  String Select Position: " + stringSelectPositionInternal);
#endif
        }

        protected virtual int LineUpCharacterPosition(int originalPos, bool goToFirstChar)
        {
            if (originalPos >= m_textComponent.textInfo.characterCount)
                originalPos -= 1;

            TMP_CharacterInfo originChar = m_textComponent.textInfo.characterInfo[originalPos];
            int originLine = originChar.lineNumber;

            // We are on the first line return first character
            if (originLine - 1 < 0)
                return goToFirstChar ? 0 : originalPos;

            int endCharIdx = m_textComponent.textInfo.lineInfo[originLine].firstCharacterIndex - 1;

            int closest = -1;
            float distance = TMP_Math.FLOAT_MAX;
            float range = 0;

            for (int i = m_textComponent.textInfo.lineInfo[originLine - 1].firstCharacterIndex; i < endCharIdx; ++i)
            {
                TMP_CharacterInfo currentChar = m_textComponent.textInfo.characterInfo[i];

                float d = originChar.origin - currentChar.origin;
                float r = d / (currentChar.xAdvance - currentChar.origin);

                if (r >= 0 && r <= 1)
                {
                    if (r < 0.5f)
                        return i;
                    else
                        return i + 1;
                }

                d = Mathf.Abs(d);

                if (d < distance)
                {
                    closest = i;
                    distance = d;
                    range = r;
                }
            }

            if (closest == -1) return endCharIdx;

            //Debug.Log("Returning nearest character with Range = " + range);

            if (range < 0.5f)
                return closest;
            else
                return closest + 1;
        }

        protected virtual int LineDownCharacterPosition(int originalPos, bool goToLastChar)
        {
            if (originalPos >= m_textComponent.textInfo.characterCount)
                return m_textComponent.textInfo.characterCount - 1; // text.Length;

            TMP_CharacterInfo originChar = m_textComponent.textInfo.characterInfo[originalPos];
            int originLine = originChar.lineNumber;

            //// We are on the last line return last character
            if (originLine + 1 >= m_textComponent.textInfo.lineCount)
                return goToLastChar ? m_textComponent.textInfo.characterCount - 1 : originalPos;

            // Need to determine end line for next line.
            int endCharIdx = m_textComponent.textInfo.lineInfo[originLine + 1].lastCharacterIndex;

            int closest = -1;
            float distance = TMP_Math.FLOAT_MAX;
            float range = 0;

            for (int i = m_textComponent.textInfo.lineInfo[originLine + 1].firstCharacterIndex; i < endCharIdx; ++i)
            {
                TMP_CharacterInfo currentChar = m_textComponent.textInfo.characterInfo[i];

                float d = originChar.origin - currentChar.origin;
                float r = d / (currentChar.xAdvance - currentChar.origin);

                if (r >= 0 && r <= 1)
                {
                    if (r < 0.5f)
                        return i;
                    else
                        return i + 1;
                }

                d = Mathf.Abs(d);

                if (d < distance)
                {
                    closest = i;
                    distance = d;
                    range = r;
                }
            }

            if (closest == -1) return endCharIdx;

            //Debug.Log("Returning nearest character with Range = " + range);

            if (range < 0.5f)
                return closest;
            else
                return closest + 1;
        }

        protected virtual int PageUpCharacterPosition(int originalPos, bool goToFirstChar)
        {
            if (originalPos >= m_textComponent.textInfo.characterCount)
                originalPos -= 1;

            TMP_CharacterInfo originChar = m_textComponent.textInfo.characterInfo[originalPos];
            int originLine = originChar.lineNumber;

            // We are on the first line return first character
            if (originLine - 1 < 0)
                return goToFirstChar ? 0 : originalPos;

            float viewportHeight = m_textViewport.rect.height;

            int newLine = originLine - 1;
            // Iterate through each subsequent line to find the first baseline that is not visible in the viewport.
            for (; newLine > 0; newLine--)
            {
                if (m_textComponent.textInfo.lineInfo[newLine].baseline > m_textComponent.textInfo.lineInfo[originLine].baseline + viewportHeight)
                    break;
            }

            int endCharIdx = m_textComponent.textInfo.lineInfo[newLine].lastCharacterIndex;

            int closest = -1;
            float distance = TMP_Math.FLOAT_MAX;
            float range = 0;

            for (int i = m_textComponent.textInfo.lineInfo[newLine].firstCharacterIndex; i < endCharIdx; ++i)
            {
                TMP_CharacterInfo currentChar = m_textComponent.textInfo.characterInfo[i];

                float d = originChar.origin - currentChar.origin;
                float r = d / (currentChar.xAdvance - currentChar.origin);

                if (r >= 0 && r <= 1)
                {
                    if (r < 0.5f)
                        return i;
                    else
                        return i + 1;
                }

                d = Mathf.Abs(d);

                if (d < distance)
                {
                    closest = i;
                    distance = d;
                    range = r;
                }
            }

            if (closest == -1) return endCharIdx;

            //Debug.Log("Returning nearest character with Range = " + range);

            if (range < 0.5f)
                return closest;
            else
                return closest + 1;
        }

        protected virtual int PageDownCharacterPosition(int originalPos, bool goToLastChar)
        {
            if (originalPos >= m_textComponent.textInfo.characterCount)
                return m_textComponent.textInfo.characterCount - 1;

            TMP_CharacterInfo originChar = m_textComponent.textInfo.characterInfo[originalPos];
            int originLine = originChar.lineNumber;

            // We are on the last line return last character
            if (originLine + 1 >= m_textComponent.textInfo.lineCount)
                return goToLastChar ? m_textComponent.textInfo.characterCount - 1 : originalPos;

            float viewportHeight = m_textViewport.rect.height;

            int newLine = originLine + 1;
            // Iterate through each subsequent line to find the first baseline that is not visible in the viewport.
            for (; newLine < m_textComponent.textInfo.lineCount - 1; newLine++)
            {
                if (m_textComponent.textInfo.lineInfo[newLine].baseline < m_textComponent.textInfo.lineInfo[originLine].baseline - viewportHeight)
                    break;
            }

            // Need to determine end line for next line.
            int endCharIdx = m_textComponent.textInfo.lineInfo[newLine].lastCharacterIndex;

            int closest = -1;
            float distance = TMP_Math.FLOAT_MAX;
            float range = 0;

            for (int i = m_textComponent.textInfo.lineInfo[newLine].firstCharacterIndex; i < endCharIdx; ++i)
            {
                TMP_CharacterInfo currentChar = m_textComponent.textInfo.characterInfo[i];

                float d = originChar.origin - currentChar.origin;
                float r = d / (currentChar.xAdvance - currentChar.origin);

                if (r >= 0 && r <= 1)
                {
                    if (r < 0.5f)
                        return i;
                    else
                        return i + 1;
                }

                d = Mathf.Abs(d);

                if (d < distance)
                {
                    closest = i;
                    distance = d;
                    range = r;
                }
            }

            if (closest == -1) return endCharIdx;

            if (range < 0.5f)
                return closest;
            else
                return closest + 1;
        }

        protected virtual void MoveDown(bool shift)
        {
            MoveDown(shift, true);
        }

        protected virtual void MoveDown(bool shift, bool goToLastChar)
        {
            if (IsSelecting && !shift)
            {
                // If we have a selection and press down without shift,
                // set caret to end of selection before we move it down.
                CaretPositionInternal = CaretSelectPositionInternal = Mathf.Max(CaretPositionInternal, CaretSelectPositionInternal);
            }

            int position = MultiLine ? LineDownCharacterPosition(CaretSelectPositionInternal, goToLastChar) : m_textComponent.textInfo.characterCount - 1; // text.Length;

            if (shift)
            {
                CaretSelectPositionInternal = position;
                StringSelectPositionInternal = GetStringIndexFromCaretPosition(CaretSelectPositionInternal);
            }
            else
            {
                CaretSelectPositionInternal = CaretPositionInternal = position;
                StringSelectPositionInternal = StringPositionInternal = GetStringIndexFromCaretPosition(CaretSelectPositionInternal);
            }

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif
        }

        protected virtual void MoveUp(bool shift)
        {
            MoveUp(shift, true);
        }

        protected virtual void MoveUp(bool shift, bool goToFirstChar)
        {
            if (IsSelecting && !shift)
            {
                // If we have a selection and press up without shift,
                // set caret position to start of selection before we move it up.
                CaretPositionInternal = CaretSelectPositionInternal = Mathf.Min(CaretPositionInternal, CaretSelectPositionInternal);
            }

            int position = MultiLine ? LineUpCharacterPosition(CaretSelectPositionInternal, goToFirstChar) : 0;

            if (shift)
            {
                CaretSelectPositionInternal = position;
                StringSelectPositionInternal = GetStringIndexFromCaretPosition(CaretSelectPositionInternal);
            }
            else
            {
                CaretSelectPositionInternal = CaretPositionInternal = position;
                StringSelectPositionInternal = StringPositionInternal = GetStringIndexFromCaretPosition(CaretSelectPositionInternal);
            }

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif
        }

        protected virtual void MovePageUp(bool shift)
        {
            MovePageUp(shift, true);
        }

        protected virtual void MovePageUp(bool shift, bool goToFirstChar)
        {
            if (IsSelecting && !shift)
            {
                // If we have a selection and press up without shift,
                // set caret position to start of selection before we move it up.
                CaretPositionInternal = CaretSelectPositionInternal = Mathf.Min(CaretPositionInternal, CaretSelectPositionInternal);
            }

            int position = MultiLine ? PageUpCharacterPosition(CaretSelectPositionInternal, goToFirstChar) : 0;

            if (shift)
            {
                CaretSelectPositionInternal = position;
                StringSelectPositionInternal = GetStringIndexFromCaretPosition(CaretSelectPositionInternal);
            }
            else
            {
                CaretSelectPositionInternal = CaretPositionInternal = position;
                StringSelectPositionInternal = StringPositionInternal = GetStringIndexFromCaretPosition(CaretSelectPositionInternal);
            }


            // Scroll to top of viewport
            //int currentLine = m_TextComponent.textInfo.characterInfo[position].lineNumber;
            //float lineAscender = m_TextComponent.textInfo.lineInfo[currentLine].ascender;

            // Adjust text area up or down if not in single line mode.
            if (m_lineType != ELineType.SingleLine)
            {
                float offset = m_textViewport.rect.height; // m_TextViewport.rect.yMax - (m_TextComponent.rectTransform.anchoredPosition.y + lineAscender);

                float topTextBounds = m_textComponent.rectTransform.position.y + m_textComponent.textBounds.max.y;
                float topViewportBounds = m_textViewport.position.y + m_textViewport.rect.yMax;

                offset = topViewportBounds > topTextBounds + offset ? offset : topViewportBounds - topTextBounds;

                m_textComponent.rectTransform.anchoredPosition += new Vector2(0, offset);
                AssignCaretPositioningIfNeeded();
            }

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif

        }

        protected virtual void MovePageDown(bool shift)
        {
            MovePageDown(shift, true);
        }

        protected virtual void MovePageDown(bool shift, bool goToLastChar)
        {
            if (IsSelecting && !shift)
            {
                // If we have a selection and press down without shift,
                // set caret to end of selection before we move it down.
                CaretPositionInternal = CaretSelectPositionInternal = Mathf.Max(CaretPositionInternal, CaretSelectPositionInternal);
            }

            int position = MultiLine ? PageDownCharacterPosition(CaretSelectPositionInternal, goToLastChar) : m_textComponent.textInfo.characterCount - 1;

            if (shift)
            {
                CaretSelectPositionInternal = position;
                StringSelectPositionInternal = GetStringIndexFromCaretPosition(CaretSelectPositionInternal);
            }
            else
            {
                CaretSelectPositionInternal = CaretPositionInternal = position;
                StringSelectPositionInternal = StringPositionInternal = GetStringIndexFromCaretPosition(CaretSelectPositionInternal);
            }

            // Scroll to top of viewport
            //int currentLine = m_TextComponent.textInfo.characterInfo[position].lineNumber;
            //float lineAscender = m_TextComponent.textInfo.lineInfo[currentLine].ascender;

            // Adjust text area up or down if not in single line mode.
            if (m_lineType != ELineType.SingleLine)
            {
                float offset = m_textViewport.rect.height; // m_TextViewport.rect.yMax - (m_TextComponent.rectTransform.anchoredPosition.y + lineAscender);

                float bottomTextBounds = m_textComponent.rectTransform.position.y + m_textComponent.textBounds.min.y;
                float bottomViewportBounds = m_textViewport.position.y + m_textViewport.rect.yMin;

                offset = bottomViewportBounds > bottomTextBounds + offset ? offset : bottomViewportBounds - bottomTextBounds;

                m_textComponent.rectTransform.anchoredPosition += new Vector2(0, offset);
                AssignCaretPositioningIfNeeded();
            }

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif

        }

        #endregion

        #region Edition Utility

        protected virtual bool InPlaceEditing()
        {
            if (m_touchKeyboardAllowsInPlaceEditing)
                return true;

            if (IsUWP())
                return !TouchScreenKeyboard.isSupported;

            if (TouchScreenKeyboard.isSupported && ShouldHideSoftKeyboard)
                return true;

            if (TouchScreenKeyboard.isSupported && ShouldHideSoftKeyboard == false && ShouldHideMobileInput == false)
                return false;

            return true;
        }

        // In-place editing can change state if a hardware keyboard becomes available or is hidden while the input field is activated.
        protected virtual bool InPlaceEditingChanged()
        {
            return !s_IsQuestDevice && m_touchKeyboardAllowsInPlaceEditing != TouchScreenKeyboard.isInPlaceEditingAllowed;
        }

        #endregion

        #region Touch Screen Keyboard Utility

        // Returns true if the TouchScreenKeyboard should be used. On Android and Chrome OS, we only want to use the
        // TouchScreenKeyboard if in-place editing is not allowed (i.e. when we do not have a hardware keyboard available).
        protected virtual bool TouchScreenKeyboardShouldBeUsed()
        {
            RuntimePlatform platform = Application.platform;
            switch (platform)
            {
                case RuntimePlatform.Android:
                case RuntimePlatform.WebGLPlayer:
                    if (s_IsQuestDevice)
                        return TouchScreenKeyboard.isSupported;

                    return !TouchScreenKeyboard.isInPlaceEditingAllowed;
                default:
                    return TouchScreenKeyboard.isSupported;
            }
        }

        protected virtual void UpdateKeyboardStringPosition()
        {
            // On iOS/tvOS we only update SoftKeyboard selection when we know that it might have changed by touch/pointer interactions with InputField
            // Setting the TouchScreenKeyboard selection here instead of LateUpdate so that we wouldn't override
            // TouchScreenKeyboard selection when it's changed with cmd+a/ctrl+a/arrow/etc. in the TouchScreenKeyboard
            // This is only applicable for iOS/tvOS as we have instance of TouchScreenKeyboard even when external keyboard is connected
            if (m_hideMobileInput && m_softKeyboard != null && m_softKeyboard.canSetSelection &&
                (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS))
            {
                var selectionStart = Mathf.Min(StringSelectPositionInternal, StringPositionInternal);
                var selectionLength = Mathf.Abs(StringSelectPositionInternal - StringPositionInternal);
                m_softKeyboard.selection = new RangeInt(selectionStart, selectionLength);
            }
        }

        protected virtual void UpdateStringPositionFromKeyboard()
        {
            // TODO: Might want to add null check here.
            var selectionRange = m_softKeyboard.selection;

            //if (selectionRange.start == 0 && selectionRange.length == 0)
            //    return;

            var selectionStart = selectionRange.start;
            var selectionEnd = selectionRange.end;

            var stringPositionChanged = false;

            if (StringPositionInternal != selectionStart)
            {
                stringPositionChanged = true;
                StringPositionInternal = selectionStart;

                CaretPositionInternal = GetCaretPositionFromStringIndex(StringPositionInternal);
            }

            if (StringSelectPositionInternal != selectionEnd)
            {
                StringSelectPositionInternal = selectionEnd;
                stringPositionChanged = true;

                CaretSelectPositionInternal = GetCaretPositionFromStringIndex(StringSelectPositionInternal);
            }

            if (stringPositionChanged)
            {
                m_blinkStartTime = Time.unscaledTime;

                UpdateLabel();
            }
        }

        protected virtual void UpdateTouchKeyboardFromEditChanges()
        {
            // Update the TouchKeyboard's text from edit changes
            // if in-place editing is allowed
            if (m_softKeyboard != null && InPlaceEditing())
            {
                m_softKeyboard.text = m_text;
            }
        }

        #endregion

        #region Late Update

        /// <summary>
        /// Update the text based on input.
        /// </summary>
        // TODO: Make LateUpdate a coroutine instead. Allows us to control the update to only be when the field is active.
        protected virtual void LateUpdate()
        {
            // Only activate if we are not already activated.
            if (m_shouldActivateNextUpdate)
            {
                if (!IsFocused)
                {
                    ActivateInputFieldInternal();
                    m_shouldActivateNextUpdate = false;
                    return;
                }

                // Reset as we are already activated.
                m_shouldActivateNextUpdate = false;
            }

            // If the device's state changed in a way that affects whether we should use a touchscreen keyboard or not,
            // then deactivate the input field.
            if (IsFocused && InPlaceEditingChanged())
                DeactivateInputField();

            // Handle double click to reset / deselect Input Field when ResetOnActivation is false.
            if (!IsFocused && m_selectionStillActive)
            {
                GameObject selectedObject = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

                if (selectedObject == null && m_resetOnDeActivation)
                {
                    ReleaseSelection();
                    return;
                }

                if (selectedObject != null && selectedObject != this.gameObject)
                {
                    if (selectedObject == m_previouslySelectedObject)
                        return;

                    m_previouslySelectedObject = selectedObject;

                    // Special handling for Vertical Scrollbar
                    if (m_verticalScrollbar && selectedObject == m_verticalScrollbar.gameObject)
                    {
                        // Do not release selection
                        return;
                    }

                    // Release selection for all objects when ResetOnDeActivation is true
                    if (m_resetOnDeActivation)
                    {
                        ReleaseSelection();
                        return;
                    }

                    // Release current selection of selected object is another Input Field
                    if (m_keepTextSelectionVisible == false && (selectedObject.GetComponent<TMP_InputField>() != null || selectedObject.GetComponent<UIInputField>() != null))
                        ReleaseSelection();

                    return;
                }

#if ENABLE_INPUT_SYSTEM
                if (m_processingEvent != null && m_processingEvent.rawType == EventType.MouseDown && m_processingEvent.button == 0)
                {
                    // Check for Double Click
                    bool isDoubleClick = false;
                    float timeStamp = Time.unscaledTime;

                    if (m_keyDownStartTime + m_doubleClickDelay > timeStamp)
                        isDoubleClick = true;

                    m_keyDownStartTime = timeStamp;

                    if (isDoubleClick)
                    {
                        //m_StringPosition = m_StringSelectPosition = 0;
                        //m_CaretPosition = m_CaretSelectPosition = 0;
                        //m_TextComponent.rectTransform.localPosition = m_DefaultTransformPosition;

                        //if (caretRectTrans != null)
                        //    caretRectTrans.localPosition = Vector3.zero;

                        ReleaseSelection();

                        return;
                    }
                }
#else
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    // Check for Double Click
                    bool isDoubleClick = false;
                    float timeStamp = Time.unscaledTime;

                    if (m_KeyDownStartTime + m_DoubleClickDelay > timeStamp)
                        isDoubleClick = true;

                    m_KeyDownStartTime = timeStamp;

                    if (isDoubleClick)
                    {
                        //m_StringPosition = m_StringSelectPosition = 0;
                        //m_CaretPosition = m_CaretSelectPosition = 0;
                        //m_TextComponent.rectTransform.localPosition = m_DefaultTransformPosition;

                        //if (caretRectTrans != null)
                        //    caretRectTrans.localPosition = Vector3.zero;

                        ReleaseSelection();

                        return;
                    }
                }
#endif
            }

            UpdateMaskRegions();

            if (InPlaceEditing() && IsKeyboardUsingEvents() || !IsFocused)
            {
                return;
            }

            AssignCaretPositioningIfNeeded();

            if (m_softKeyboard == null || m_softKeyboard.status != TouchScreenKeyboard.Status.Visible)
            {
                if (m_softKeyboard != null)
                {
                    if (!m_readOnly)
                        Text = m_softKeyboard.text;

                    TouchScreenKeyboard.Status status = m_softKeyboard.status;

                    // Special handling for UWP - Hololens which does not support Canceled status
                    if (m_lastKeyCode != KeyCode.Return && status == TouchScreenKeyboard.Status.Done && IsUWP())
                    {
                        status = TouchScreenKeyboard.Status.Canceled;
                        // The HoloLen's X button will not be acting as an ESC Key (TMBP-98)
                        m_isKeyboardBeingClosedInHoloLens = true;
                    }

                    switch (status)
                    {
                        case TouchScreenKeyboard.Status.LostFocus:
                            TriggerTouchScreenKeyboardStatusChanged();
                            break;
                        case TouchScreenKeyboard.Status.Canceled:
                            m_releaseSelection = true;
                            m_wasCanceled = true;
                            TriggerTouchScreenKeyboardStatusChanged();
                            break;
                        case TouchScreenKeyboard.Status.Done:
                            m_releaseSelection = true;
                            TriggerTouchScreenKeyboardStatusChanged();
                            OnSubmit(null);
                            break;
                    }

#if UNITY_ANDROID || UNITY_IOS
                    DeactivateInputField();
#endif
                }

                return;
            }

            string val = m_softKeyboard.text;

            if (m_text != val)
            {
                if (m_readOnly)
                {
                    m_softKeyboard.text = m_text;
                }
                else
                {
                    m_text = "";

                    for (int i = 0; i < val.Length; ++i)
                    {
                        char c = val[i];
                        bool hasValidateUpdatedText = false;

                        if (c == '\r' || c == 3)
                            c = '\n';

                        if (TriggerInputValidation(m_text, m_text.Length, c, out var result))
                            c = result;
                        else if (CharacterValidation != ECharacterValidation.None)
                        {
                            string textBeforeValidate = m_text;
                            c = Validate(m_text, m_text.Length, c);
                            hasValidateUpdatedText = textBeforeValidate != m_text;
                        }

                        if (LineType != ELineType.MultiLineNewline && c == '\n')
                        {
                            UpdateLabel();

                            OnSubmit(null);
                            OnDeselect(null);
                            return;
                        }

                        // In the case of a Custom Validator, the user is expected to modify the m_Text where as such we do not append c.
                        // However we will append c if the user did not modify the m_Text (UUM-42147)
                        if (c != 0 && (CharacterValidation != ECharacterValidation.CustomValidator || !hasValidateUpdatedText))
                            m_text += c;
                    }

                    if (CharacterLimit > 0 && m_text.Length > CharacterLimit)
                        m_text = m_text.Substring(0, CharacterLimit);

                    UpdateStringPositionFromKeyboard();

                    // Set keyboard text before updating label, as we might have changed it with validation
                    // and update label will take the old value from keyboard if we don't change it here
                    if (m_text != val)
                        m_softKeyboard.text = m_text;

                    TriggerValueChangedAndUpdateLabel();
                }
            }
            // On iOS/tvOS we always have TouchScreenKeyboard instance even when using external keyboard
            // so we keep track of the caret position there
            else if (m_hideMobileInput && m_softKeyboard != null && m_softKeyboard.canSetSelection &&
                     Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.tvOS)
            {
                var selectionStart = Mathf.Min(StringSelectPositionInternal, StringPositionInternal);
                var selectionLength = Mathf.Abs(StringSelectPositionInternal - StringPositionInternal);
                m_softKeyboard.selection = new RangeInt(selectionStart, selectionLength);
            }
            else if (m_hideMobileInput && Application.platform == RuntimePlatform.Android ||
                     m_softKeyboard.canSetSelection && (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS))
            {
                UpdateStringPositionFromKeyboard();
            }

            //else if (m_HideMobileInput) // m_Keyboard.canSetSelection
            //{
            //    int length = stringPositionInternal < stringSelectPositionInternal ? stringSelectPositionInternal - stringPositionInternal : stringPositionInternal - stringSelectPositionInternal;
            //    m_SoftKeyboard.selection = new RangeInt(stringPositionInternal < stringSelectPositionInternal ? stringPositionInternal : stringSelectPositionInternal, length);
            //}
            //else if (!m_HideMobileInput) // m_Keyboard.canGetSelection)
            //{
            //    UpdateStringPositionFromKeyboard();
            //}

            if (m_softKeyboard != null && m_softKeyboard.status != TouchScreenKeyboard.Status.Visible)
            {
                if (m_softKeyboard.status == TouchScreenKeyboard.Status.Canceled)
                    m_wasCanceled = true;

                OnDeselect(null);
            }
        }

        #endregion

        #region Drag

        protected virtual bool MayDrag(PointerEventData eventData)
        {
            return IsActive() &&
                   IsInteractable() &&
                   eventData.button == PointerEventData.InputButton.Left &&
                   m_textComponent != null &&
                   (m_softKeyboard == null || ShouldHideSoftKeyboard || ShouldHideMobileInput);
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            m_updateDrag = true;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            CaretPosition insertionSide;

            int insertionIndex = TMP_TextUtilities.GetCursorIndexFromPosition(m_textComponent, eventData.position, eventData.pressEventCamera, out insertionSide);

            if (m_isRichTextEditingAllowed)
            {
                if (insertionSide == TMPro.CaretPosition.Left)
                {
                    StringSelectPositionInternal = m_textComponent.textInfo.characterInfo[insertionIndex].index;
                }
                else if (insertionSide == TMPro.CaretPosition.Right)
                {
                    StringSelectPositionInternal = m_textComponent.textInfo.characterInfo[insertionIndex].index + m_textComponent.textInfo.characterInfo[insertionIndex].stringLength;
                }
            }
            else
            {
                if (insertionSide == TMPro.CaretPosition.Left)
                {
                    StringSelectPositionInternal = insertionIndex == 0
                        ? m_textComponent.textInfo.characterInfo[0].index
                        : m_textComponent.textInfo.characterInfo[insertionIndex - 1].index + m_textComponent.textInfo.characterInfo[insertionIndex - 1].stringLength;
                }
                else if (insertionSide == TMPro.CaretPosition.Right)
                {
                    StringSelectPositionInternal = m_textComponent.textInfo.characterInfo[insertionIndex].index + m_textComponent.textInfo.characterInfo[insertionIndex].stringLength;
                }
            }

            CaretSelectPositionInternal = GetCaretPositionFromStringIndex(StringSelectPositionInternal);

            MarkGeometryAsDirty();

            m_dragPositionOutOfBounds = !RectTransformUtility.RectangleContainsScreenPoint(TextViewport, eventData.position, eventData.pressEventCamera);
            if (m_dragPositionOutOfBounds && m_dragCoroutine == null)
                m_dragCoroutine = StartCoroutine(MouseDragOutsideRect(eventData));

            UpdateKeyboardStringPosition();
            eventData.Use();

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif
        }

        protected virtual IEnumerator MouseDragOutsideRect(PointerEventData eventData)
        {
            while (m_updateDrag && m_dragPositionOutOfBounds)
            {
                Vector2 localMousePos;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(TextViewport, eventData.position, eventData.pressEventCamera, out localMousePos);

                Rect rect = TextViewport.rect;

                if (MultiLine)
                {
                    if (localMousePos.y > rect.yMax)
                        MoveUp(true, false);
                    else if (localMousePos.y < rect.yMin)
                        MoveDown(true, false);
                }
                else
                {
                    if (localMousePos.x < rect.xMin)
                        MoveLeft(true, false);
                    else if (localMousePos.x > rect.xMax)
                        MoveRight(true, false);
                }

                UpdateLabel();

                float delay = MultiLine ? kVScrollSpeed : kHScrollSpeed;

                if (m_waitForSecondsRealtime == null)
                    m_waitForSecondsRealtime = new WaitForSecondsRealtime(delay);
                else
                    m_waitForSecondsRealtime.waitTime = delay;

                yield return m_waitForSecondsRealtime;
            }
            m_dragCoroutine = null;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            m_updateDrag = false;
        }

        #endregion

        #region IPointerEventHandlers

        protected override bool OnBeforePointerDown(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return false;

            var result = base.OnBeforePointerDown(eventData);

            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            m_hadFocusBefore = m_allowInput;

            return result;
        }
        protected override void OnAfterPointerDown(PointerEventData eventData, bool didBaseLogic)
        {
            if (!didBaseLogic) return;

            if (InPlaceEditing() == false)
            {
                if (m_softKeyboard == null || !m_softKeyboard.active)
                {
                    OnSelect(eventData);
                    return;
                }
            }

#if ENABLE_INPUT_SYSTEM
            Event.PopEvent(m_processingEvent);
            bool shift = m_processingEvent != null && (m_processingEvent.modifiers & EventModifiers.Shift) != 0;
#else
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif

            // Check for Double Click
            bool isDoubleClick = false;
            float timeStamp = Time.unscaledTime;

            if (m_pointerDownClickStartTime + m_doubleClickDelay > timeStamp)
                isDoubleClick = true;

            m_pointerDownClickStartTime = timeStamp;

            // Only set caret position if we didn't just get focus now.
            // Otherwise it will overwrite the select all on focus.
            if (m_hadFocusBefore || !m_onFocusSelectAll)
            {
                CaretPosition insertionSide;

                int insertionIndex = TMP_TextUtilities.GetCursorIndexFromPosition(m_textComponent, eventData.position, eventData.pressEventCamera, out insertionSide);

                if (shift)
                {
                    if (m_isRichTextEditingAllowed)
                    {
                        if (insertionSide == TMPro.CaretPosition.Left)
                        {
                            StringSelectPositionInternal = m_textComponent.textInfo.characterInfo[insertionIndex].index;
                        }
                        else if (insertionSide == TMPro.CaretPosition.Right)
                        {
                            StringSelectPositionInternal = m_textComponent.textInfo.characterInfo[insertionIndex].index + m_textComponent.textInfo.characterInfo[insertionIndex].stringLength;
                        }
                    }
                    else
                    {
                        if (insertionSide == TMPro.CaretPosition.Left)
                        {
                            StringSelectPositionInternal = insertionIndex == 0
                                ? m_textComponent.textInfo.characterInfo[0].index
                                : m_textComponent.textInfo.characterInfo[insertionIndex - 1].index + m_textComponent.textInfo.characterInfo[insertionIndex - 1].stringLength;
                        }
                        else if (insertionSide == TMPro.CaretPosition.Right)
                        {
                            StringSelectPositionInternal = m_textComponent.textInfo.characterInfo[insertionIndex].index + m_textComponent.textInfo.characterInfo[insertionIndex].stringLength;
                        }
                    }
                }
                else
                {
                    if (m_isRichTextEditingAllowed)
                    {
                        if (insertionSide == TMPro.CaretPosition.Left)
                        {
                            StringPositionInternal = StringSelectPositionInternal = m_textComponent.textInfo.characterInfo[insertionIndex].index;
                        }
                        else if (insertionSide == TMPro.CaretPosition.Right)
                        {
                            StringPositionInternal = StringSelectPositionInternal = m_textComponent.textInfo.characterInfo[insertionIndex].index + m_textComponent.textInfo.characterInfo[insertionIndex].stringLength;
                        }
                    }
                    else
                    {
                        if (insertionSide == TMPro.CaretPosition.Left)
                        {
                            StringPositionInternal = StringSelectPositionInternal = insertionIndex == 0
                                ? m_textComponent.textInfo.characterInfo[0].index
                                : m_textComponent.textInfo.characterInfo[insertionIndex - 1].index + m_textComponent.textInfo.characterInfo[insertionIndex - 1].stringLength;
                        }
                        else if (insertionSide == TMPro.CaretPosition.Right)
                        {
                            StringPositionInternal = StringSelectPositionInternal = m_textComponent.textInfo.characterInfo[insertionIndex].index + m_textComponent.textInfo.characterInfo[insertionIndex].stringLength;
                        }
                    }
                }


                if (isDoubleClick)
                {
                    int wordIndex = TMP_TextUtilities.FindIntersectingWord(m_textComponent, eventData.position, eventData.pressEventCamera);

                    if (wordIndex != -1)
                    {
                        // TODO: Should behavior be different if rich text editing is enabled or not?

                        // Select current word
                        CaretPositionInternal = m_textComponent.textInfo.wordInfo[wordIndex].firstCharacterIndex;
                        CaretSelectPositionInternal = m_textComponent.textInfo.wordInfo[wordIndex].lastCharacterIndex + 1;

                        StringPositionInternal = m_textComponent.textInfo.characterInfo[CaretPositionInternal].index;
                        StringSelectPositionInternal = m_textComponent.textInfo.characterInfo[CaretSelectPositionInternal - 1].index + m_textComponent.textInfo.characterInfo[CaretSelectPositionInternal - 1].stringLength;
                    }
                    else
                    {
                        // Select current character
                        CaretPositionInternal = insertionIndex;
                        CaretSelectPositionInternal = CaretPositionInternal + 1;

                        StringPositionInternal = m_textComponent.textInfo.characterInfo[insertionIndex].index;
                        StringSelectPositionInternal = StringPositionInternal + m_textComponent.textInfo.characterInfo[insertionIndex].stringLength;
                    }
                }
                else
                {
                    CaretPositionInternal = CaretSelectPositionInternal = GetCaretPositionFromStringIndex(StringPositionInternal);
                }

                m_isSelectAll = false;
            }

            UpdateLabel();
            UpdateKeyboardStringPosition();
            eventData.Use();

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif
        }

        #endregion

        #region Event Handling

        protected virtual EEditState KeyPressed(Event evt)
        {
            var currentEventModifiers = evt.modifiers;
            bool ctrl = m_isApplePlatform ? (currentEventModifiers & EventModifiers.Command) != 0 : (currentEventModifiers & EventModifiers.Control) != 0;
            bool shift = (currentEventModifiers & EventModifiers.Shift) != 0;
            bool alt = (currentEventModifiers & EventModifiers.Alt) != 0;
            bool ctrlOnly = ctrl && !alt && !shift;
            m_lastKeyCode = evt.keyCode;

            switch (evt.keyCode)
            {
                case KeyCode.Backspace:
                    {
                        Backspace();
                        return EEditState.Continue;
                    }

                case KeyCode.Delete:
                    {
                        DeleteKey();
                        return EEditState.Continue;
                    }

                case KeyCode.Home:
                    {
                        MoveToStartOfLine(shift, ctrl);
                        return EEditState.Continue;
                    }

                case KeyCode.End:
                    {
                        MoveToEndOfLine(shift, ctrl);
                        return EEditState.Continue;
                    }

                // Select All
                case KeyCode.A:
                    {
                        if (ctrlOnly)
                        {
                            SelectAll();
                            return EEditState.Continue;
                        }
                        break;
                    }

                // Copy
                case KeyCode.C:
                    {
                        if (ctrlOnly)
                        {
                            if (InputType != EInputType.Password)
                                clipboard = GetSelectedString();
                            else
                                clipboard = "";
                            return EEditState.Continue;
                        }
                        break;
                    }

                // Paste
                case KeyCode.V:
                    {
                        if (ctrlOnly)
                        {
                            Append(clipboard);
                            return EEditState.Continue;
                        }
                        break;
                    }

                // Cut
                case KeyCode.X:
                    {
                        if (ctrlOnly)
                        {
                            if (InputType != EInputType.Password)
                                clipboard = GetSelectedString();
                            else
                                clipboard = "";
                            Delete();
                            UpdateTouchKeyboardFromEditChanges();
                            TriggerValueChangedAndUpdateLabel();
                            return EEditState.Continue;
                        }
                        break;
                    }

                case KeyCode.LeftArrow:
                    {
                        MoveLeft(shift, ctrl);
                        return EEditState.Continue;
                    }

                case KeyCode.RightArrow:
                    {
                        MoveRight(shift, ctrl);
                        return EEditState.Continue;
                    }

                case KeyCode.UpArrow:
                    {
                        MoveUp(shift);
                        return EEditState.Continue;
                    }

                case KeyCode.DownArrow:
                    {
                        MoveDown(shift);
                        return EEditState.Continue;
                    }

                case KeyCode.PageUp:
                    {
                        MovePageUp(shift);
                        return EEditState.Continue;
                    }

                case KeyCode.PageDown:
                    {
                        MovePageDown(shift);
                        return EEditState.Continue;
                    }

                // Submit
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    {
                        if (LineType != ELineType.MultiLineNewline)
                        {
                            m_releaseSelection = true;
                            return EEditState.Finish;
                        }
                        else
                        {
                            TMP_TextInfo textInfo = m_textComponent.textInfo;

                            if (m_lineLimit > 0 && textInfo != null && textInfo.lineCount >= m_lineLimit)
                            {
                                m_releaseSelection = true;
                                return EEditState.Finish;
                            }
                        }
                        break;
                    }

                case KeyCode.Escape:
                    {
                        m_releaseSelection = true;
                        m_wasCanceled = true;
                        return EEditState.Finish;
                    }
            }

            char c = evt.character;

            // Don't allow return chars or tabulator key to be entered into single line fields.
            if (!MultiLine && (c == '\t' || c == '\r' || c == '\n'))
                return EEditState.Continue;

            // Convert carriage return and end-of-text characters to newline.
            if (c == '\r' || c == 3)
                c = '\n';

            // Convert Shift Enter to Vertical tab
            if (shift && c == '\n')
                c = '\v';

            if (IsValidChar(c))
            {
                Append(c);
            }

            if (c == 0)
            {
                if (CompositionLength > 0)
                {
                    UpdateLabel();
                }
            }
            return EEditState.Continue;
        }

        protected virtual bool IsValidChar(char c)
        {
            // Delete key on mac
            if (c == 127)
                return false;

            // Accept newline and tab
            if (c == '\t' || c == '\n')
                return true;

            // Control characters (not printable)
            if (c < 32)
                return false;

            return true;

            // With the addition of Dynamic support, I think this will best be handled by the text component.
            //return m_TextComponent.font.HasCharacter(c, true);
        }

        public virtual void ProcessEvent(Event e)
        {
            KeyPressed(e);
        }

        #endregion

        #region IUpdateSelectedHandler

        /// <summary>
        ///
        /// </summary>
        /// <param name="eventData"></param>
        public virtual void OnUpdateSelected(BaseEventData eventData)
        {
            if (!IsFocused)
                return;

            bool consumedEvent = false;
            EEditState editState = EEditState.Continue;

            while (Event.PopEvent(m_processingEvent))
            {
                //Debug.Log("Event: " + m_ProcessingEvent.ToString() + "  IsCompositionActive= " + m_IsCompositionActive + "  Composition Length: " + compositionLength);

                EventType eventType = m_processingEvent.rawType;

                if (eventType == EventType.KeyUp)
                    continue;

                if (eventType == EventType.KeyDown)
                {
                    consumedEvent = true;

                    // Special handling on OSX which produces more events which need to be suppressed.
                    if (m_isCompositionActive && CompositionLength == 0)
                    {
                        // Suppress other events related to navigation or termination of composition sequence.
                        if (m_processingEvent.character == 0 && m_processingEvent.modifiers == EventModifiers.None)
                            continue;
                    }

                    editState = KeyPressed(m_processingEvent);
                    if (editState == EEditState.Finish)
                    {
                        if (!m_wasCanceled)
                            TriggerSubmit();

                        DeactivateInputField();
                        break;
                    }

                    m_isTextComponentUpdateRequired = true;
                    UpdateLabel();

                    continue;
                }

                switch (eventType)
                {
                    case EventType.ValidateCommand:
                    case EventType.ExecuteCommand:
                        switch (m_processingEvent.commandName)
                        {
                            case "SelectAll":
                                SelectAll();
                                consumedEvent = true;
                                break;
                        }
                        break;
                }
            }

            // We must also consume events when IME is active to prevent them from being passed to the text field. // UUM-100552
            if (consumedEvent || (m_isCompositionActive && CompositionLength > 0))
            {
                UpdateLabel();
                eventData.Use();
            }
        }

        #endregion

        #region Scroll

        /// <summary>
        ///
        /// </summary>
        /// <param name="eventData"></param>
        public virtual void OnScroll(PointerEventData eventData)
        {
            // Return if Single Line
            if (m_lineType == ELineType.SingleLine)
            {
                if (m_scrollHandlerParent != null)
                    m_scrollHandlerParent.OnScroll(eventData);

                return;
            }

            if (m_textComponent.preferredHeight < m_textViewport.rect.height)
                return;

            float scrollDirection = -eventData.scrollDelta.y;

            // Determine the current scroll position of the text within the viewport
            m_scrollPosition = GetScrollPositionRelativeToViewport();

            m_scrollPosition += (1f / m_textComponent.textInfo.lineCount) * scrollDirection * m_scrollSensitivity;

            m_scrollPosition = Mathf.Clamp01(m_scrollPosition);

            AdjustTextPositionRelativeToViewport(m_scrollPosition);

            if (m_verticalScrollbar)
            {
                m_verticalScrollbar.Value = m_scrollPosition;
            }

            //Debug.Log(GetInstanceID() + "- Scroll Position:" + m_ScrollPosition);
        }

        protected virtual float GetScrollPositionRelativeToViewport()
        {
            // Determine the current scroll position of the text within the viewport
            Rect viewportRect = m_textViewport.rect;

            float scrollPosition = (m_textComponent.textInfo.lineInfo[0].ascender + m_textComponent.margin.y + m_textComponent.margin.w - viewportRect.yMax + m_textComponent.rectTransform.anchoredPosition.y) / (m_textComponent.preferredHeight - viewportRect.height);

            scrollPosition = (int)((scrollPosition * 1000) + 0.5f) / 1000.0f;

            return scrollPosition;
        }

        #endregion

        #region Delete Utility

        protected virtual void Delete()
        {
            if (m_readOnly)
                return;

            if (m_stringPosition == m_stringSelectPosition)
                return;

            if (m_isRichTextEditingAllowed || m_isSelectAll)
            {
                // Handling of Delete when Rich Text is allowed.
                if (m_stringPosition < m_stringSelectPosition)
                {
                    m_text = Text.Remove(m_stringPosition, m_stringSelectPosition - m_stringPosition);
                    m_stringSelectPosition = m_stringPosition;
                }
                else
                {
                    m_text = Text.Remove(m_stringSelectPosition, m_stringPosition - m_stringSelectPosition);
                    m_stringPosition = m_stringSelectPosition;
                }

                if (m_isSelectAll)
                {
                    m_caretPosition = m_caretSelectPosition = 0;
                    m_isSelectAll = false;
                }
            }
            else
            {
                if (m_caretPosition < m_caretSelectPosition)
                {
                    int index = ClampArrayIndex(m_caretSelectPosition - 1);
                    m_stringPosition = m_textComponent.textInfo.characterInfo[m_caretPosition].index;
                    m_stringSelectPosition = m_textComponent.textInfo.characterInfo[index].index + m_textComponent.textInfo.characterInfo[index].stringLength;

                    m_text = Text.Remove(m_stringPosition, m_stringSelectPosition - m_stringPosition);

                    m_stringSelectPosition = m_stringPosition;
                    m_caretSelectPosition = m_caretPosition;
                }
                else
                {
                    int index = ClampArrayIndex(m_caretPosition - 1);
                    m_stringPosition = m_textComponent.textInfo.characterInfo[index].index + m_textComponent.textInfo.characterInfo[index].stringLength;
                    m_stringSelectPosition = m_textComponent.textInfo.characterInfo[m_caretSelectPosition].index;

                    m_text = Text.Remove(m_stringSelectPosition, m_stringPosition - m_stringSelectPosition);

                    m_stringPosition = m_stringSelectPosition;
                    m_caretPosition = m_caretSelectPosition;
                }
            }

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif
        }

        /// <summary>
        /// Handling of DEL key
        /// </summary>
        protected virtual void DeleteKey()
        {
            if (m_readOnly)
                return;

            if (IsSelecting)
            {
                m_hasTextBeenRemoved = true;

                Delete();
                UpdateTouchKeyboardFromEditChanges();
                TriggerValueChangedAndUpdateLabel();
            }
            else
            {
                if (m_isRichTextEditingAllowed)
                {
                    if (StringPositionInternal < Text.Length)
                    {
                        // Special handling for Surrogate Pairs
                        if (char.IsHighSurrogate(Text[StringPositionInternal]))
                            m_text = Text.Remove(StringPositionInternal, 2);
                        else
                            m_text = Text.Remove(StringPositionInternal, 1);

                        m_hasTextBeenRemoved = true;

                        UpdateTouchKeyboardFromEditChanges();
                        TriggerValueChangedAndUpdateLabel();
                    }
                }
                else
                {
                    if (CaretPositionInternal < m_textComponent.textInfo.characterCount - 1)
                    {
                        int numberOfCharactersToRemove = m_textComponent.textInfo.characterInfo[CaretPositionInternal].stringLength;

                        // Special handling for <CR><LF>
                        if (m_textComponent.textInfo.characterInfo[CaretPositionInternal].character == '\r' && m_textComponent.textInfo.characterInfo[CaretPositionInternal + 1].character == '\n')
                            numberOfCharactersToRemove += m_textComponent.textInfo.characterInfo[CaretPositionInternal + 1].stringLength;

                        // Adjust string position to skip any potential rich text tags.
                        int nextCharacterStringPosition = m_textComponent.textInfo.characterInfo[CaretPositionInternal].index;

                        m_text = Text.Remove(nextCharacterStringPosition, numberOfCharactersToRemove);

                        m_hasTextBeenRemoved = true;

                        TriggerValueChangedAndUpdateLabel();
                    }
                }
            }

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif
        }

        /// <summary>
        /// Handling of Backspace key
        /// </summary>
        protected virtual void Backspace()
        {
            if (m_readOnly)
                return;

            if (IsSelecting)
            {
                m_hasTextBeenRemoved = true;

                Delete();
                UpdateTouchKeyboardFromEditChanges();
                TriggerValueChangedAndUpdateLabel();
            }
            else
            {
                if (m_isRichTextEditingAllowed)
                {
                    if (StringPositionInternal > 0)
                    {
                        int numberOfCharactersToRemove = 1;

                        // Special handling for Surrogate pairs and Diacritical marks
                        if (char.IsLowSurrogate(Text[StringPositionInternal - 1]))
                            numberOfCharactersToRemove = 2;

                        StringSelectPositionInternal = StringPositionInternal = StringPositionInternal - numberOfCharactersToRemove;

                        m_text = Text.Remove(StringPositionInternal, numberOfCharactersToRemove);

                        CaretSelectPositionInternal = CaretPositionInternal = CaretPositionInternal - 1;

                        m_hasTextBeenRemoved = true;

                        UpdateTouchKeyboardFromEditChanges();
                        TriggerValueChangedAndUpdateLabel();
                    }
                }
                else
                {
                    if (CaretPositionInternal > 0)
                    {
                        int caretPositionIndex = CaretPositionInternal - 1;
                        int numberOfCharactersToRemove = m_textComponent.textInfo.characterInfo[caretPositionIndex].stringLength;

                        // Special handling for <CR><LR>
                        if (caretPositionIndex > 0 && m_textComponent.textInfo.characterInfo[caretPositionIndex].character == '\n' && m_textComponent.textInfo.characterInfo[caretPositionIndex - 1].character == '\r')
                        {
                            numberOfCharactersToRemove += m_textComponent.textInfo.characterInfo[caretPositionIndex - 1].stringLength;
                            caretPositionIndex -= 1;
                        }

                        // Delete the previous character
                        m_text = Text.Remove(m_textComponent.textInfo.characterInfo[caretPositionIndex].index, numberOfCharactersToRemove);

                        // Get new adjusted string position
                        StringSelectPositionInternal = StringPositionInternal = CaretPositionInternal < 1
                            ? m_textComponent.textInfo.characterInfo[0].index
                            : m_textComponent.textInfo.characterInfo[caretPositionIndex].index;

                        CaretSelectPositionInternal = CaretPositionInternal = caretPositionIndex;
                    }

                    m_hasTextBeenRemoved = true;

                    UpdateTouchKeyboardFromEditChanges();
                    TriggerValueChangedAndUpdateLabel();
                }

            }

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif
        }

        #endregion

        #region Append & Insert

        /// <summary>
        /// Append the specified text to the end of the current.
        /// </summary>
        protected virtual void Append(string input)
        {
            if (m_readOnly)
                return;

            if (InPlaceEditing() == false)
                return;

            for (int i = 0, imax = input.Length; i < imax; ++i)
            {
                char c = input[i];

                if (c >= ' ' || c == '\t' || c == '\r' || c == '\n')
                {
                    Append(c);
                }
            }
        }

        protected virtual void Append(char input)
        {
            if (m_readOnly)
                return;

            if (InPlaceEditing() == false)
                return;

            // If we have an input validator, validate the input first
            int insertionPosition = Mathf.Min(StringPositionInternal, StringSelectPositionInternal);

            //Get the text based on selection for validation instead of whole text(case 1253193).
            var validateText = Text;

            if (SelectionFocusPosition != SelectionAnchorPosition)
            {

                m_hasTextBeenRemoved = true;

                if (m_isRichTextEditingAllowed || m_isSelectAll)
                {
                    // Handling of Delete when Rich Text is allowed.
                    if (m_stringPosition < m_stringSelectPosition)
                        validateText = Text.Remove(m_stringPosition, m_stringSelectPosition - m_stringPosition);
                    else
                        validateText = Text.Remove(m_stringSelectPosition, m_stringPosition - m_stringSelectPosition);
                }
                else
                {
                    if (m_caretPosition < m_caretSelectPosition)
                    {
                        m_stringPosition = m_textComponent.textInfo.characterInfo[m_caretPosition].index;
                        m_stringSelectPosition = m_textComponent.textInfo.characterInfo[m_caretSelectPosition - 1].index + m_textComponent.textInfo.characterInfo[m_caretSelectPosition - 1].stringLength;

                        validateText = Text.Remove(m_stringPosition, m_stringSelectPosition - m_stringPosition);
                    }
                    else
                    {
                        m_stringPosition = m_textComponent.textInfo.characterInfo[m_caretPosition - 1].index + m_textComponent.textInfo.characterInfo[m_caretPosition - 1].stringLength;
                        m_stringSelectPosition = m_textComponent.textInfo.characterInfo[m_caretSelectPosition].index;

                        validateText = Text.Remove(m_stringSelectPosition, m_stringPosition - m_stringSelectPosition);
                    }
                }
            }

            if (TriggerInputValidation(validateText, insertionPosition, input, out var result))
            {
                input = result;
            }
            else if (CharacterValidation == ECharacterValidation.CustomValidator)
            {
                input = Validate(validateText, insertionPosition, input);

                if (input == 0) return;

                if (!char.IsHighSurrogate(input))
                    m_caretSelectPosition = m_caretPosition += 1;

                TriggerValueChanged();
                UpdateLabel();

                return;
            }
            else if (CharacterValidation != ECharacterValidation.None)
            {
                input = Validate(validateText, insertionPosition, input);
            }

            // If the input is invalid, skip it
            if (input == 0)
                return;

            // Append the character and update the label
            Insert(input);
        }


        // Insert the character and update the label.
        protected virtual void Insert(char c)
        {
            if (m_readOnly)
                return;

            //Debug.Log("Inserting character " + m_IsCompositionActive);

            string replaceString = c.ToString();
            Delete();

            // Can't go past the character limit
            if (CharacterLimit > 0 && Text.Length >= CharacterLimit)
                return;

            m_text = Text.Insert(m_stringPosition, replaceString);

            if (!char.IsHighSurrogate(c))
                m_caretSelectPosition = m_caretPosition += 1;

            m_stringSelectPosition = m_stringPosition += 1;

            UpdateTouchKeyboardFromEditChanges();
            TriggerValueChanged();

#if TMP_DEBUG_MODE
                Debug.Log("Caret Position: " + caretPositionInternal + " Selection Position: " + caretSelectPositionInternal + "  String Position: " + stringPositionInternal + " String Select Position: " + stringSelectPositionInternal);
#endif
        }

        #endregion

        #region Label Update

        /// <summary>
        /// Update the visual text Text.
        /// </summary>

        protected virtual void UpdateLabel()
        {
            if (m_textComponent != null && m_textComponent.font != null && m_preventCallback == false)
            {
                // Prevent callback from the text component as we assign new text. This is to prevent a recursive call.
                m_preventCallback = true;

                string fullText;
                if (CompositionLength > 0 && m_readOnly == false)
                {
                    //Input.imeCompositionMode = IMECompositionMode.On;

                    // Handle selections
                    Delete();

                    if (m_richText)
                        fullText = Text.Substring(0, m_stringPosition) + "<u>" + CompositionString + "</u>" + Text.Substring(m_stringPosition);
                    else
                        fullText = Text.Substring(0, m_stringPosition) + CompositionString + Text.Substring(m_stringPosition);

                    m_isCompositionActive = true;

                    //Debug.Log("[" + Time.frameCount + "] Handling IME Input");
                }
                else
                {
                    fullText = Text;
                    m_isCompositionActive = false;
                    m_shouldUpdateIMEWindowPosition = true;

                }

                //Debug.Log("Handling IME Input... [" + compositionString + "] of length [" + compositionLength + "] at StringPosition [" + m_StringPosition + "]  IsActive [" + m_IsCompositionActive + "]");

                string processed;
                if (InputType == EInputType.Password)
                    processed = new string(PasswordChar, fullText.Length);
                else
                    processed = fullText;

                bool isEmpty = string.IsNullOrEmpty(fullText);

                if (m_placeholder != null)
                    m_placeholder.enabled = isEmpty;

                if (!isEmpty && m_readOnly == false)
                {
                    SetCaretVisible();
                }

                m_textComponent.text = processed + "\u200B"; // Extra space is added for Caret tracking.

                // Rebuild layout if using Layout components.
                if (m_isDrivenByLayoutComponents)
                    LayoutRebuilder.MarkLayoutForRebuild(m_rectTransform);

                // Special handling to limit the number of lines of text in the Input Field.
                if (m_lineLimit > 0)
                {
                    m_textComponent.ForceMeshUpdate();

                    TMP_TextInfo textInfo = m_textComponent.textInfo;

                    // Check if text exceeds maximum number of lines.
                    if (textInfo != null && textInfo.lineCount > m_lineLimit)
                    {
                        int lastValidCharacterIndex = textInfo.lineInfo[m_lineLimit - 1].lastCharacterIndex;
                        int characterStringIndex = textInfo.characterInfo[lastValidCharacterIndex].index + textInfo.characterInfo[lastValidCharacterIndex].stringLength;
                        Text = processed.Remove(characterStringIndex, processed.Length - characterStringIndex);
                        m_textComponent.text = Text + "\u200B";
                    }
                }

                if (m_isTextComponentUpdateRequired || m_verticalScrollbar && !(m_isCaretPositionDirty && m_isStringPositionDirty))
                {
                    m_isTextComponentUpdateRequired = false;
                    m_textComponent.ForceMeshUpdate();
                }

                MarkGeometryAsDirty();

                m_preventCallback = false;
            }
        }
        public virtual void ForceLabelUpdate()
        {
            UpdateLabel();
        }

        #endregion

        #region Scrollbar

        protected virtual void UpdateScrollbar()
        {
            // Update Scrollbar
            if (m_verticalScrollbar)
            {
                Rect viewportRect = m_textViewport.rect;

                float size = viewportRect.height / m_textComponent.preferredHeight;

                m_verticalScrollbar.Size = size;

                m_verticalScrollbar.Value = GetScrollPositionRelativeToViewport();
            }
        }

        /// <summary>
        /// Function to update the vertical position of the text container when OnValueChanged event is received from the Scrollbar.
        /// </summary>
        /// <param name="value"></param>
        protected virtual void OnScrollbarValueChange(float value)
        {
            //if (m_IsUpdatingScrollbarValues)
            //{
            //    m_IsUpdatingScrollbarValues = false;
            //    return;
            //}

            if (value < 0 || value > 1) return;

            AdjustTextPositionRelativeToViewport(value);

            m_scrollPosition = value;
        }

        #endregion

        #region Mask Region

        protected virtual void UpdateMaskRegions()
        {
            // TODO: Figure out a better way to handle adding an offset to the masking region
            // This region is defined by the RectTransform of the GameObject that contains the RectMask2D component.
            /*
            // Update Masking Region
            if (m_TextViewportRectMask != null)
            {
                Rect viewportRect = m_TextViewportRectMask.canvasRect;

                if (viewportRect != m_CachedViewportRect)
                {
                    m_CachedViewportRect = viewportRect;

                    viewportRect.min -= m_TextViewport.offsetMin * 0.5f;
                    viewportRect.max -= m_TextViewport.offsetMax * 0.5f;

                    if (m_CachedInputRenderer != null)
                        m_CachedInputRenderer.EnableRectClipping(viewportRect);

                    if (m_TextComponent.canvasRenderer != null)
                        m_TextComponent.canvasRenderer.EnableRectClipping(viewportRect);

                    if (m_Placeholder != null && m_Placeholder.enabled)
                        m_Placeholder.canvasRenderer.EnableRectClipping(viewportRect);
                }
            }
            */
        }

        #endregion

        #region Caret Utility

        protected virtual int GetCaretPositionFromStringIndex(int stringIndex)
        {
            int count = m_textComponent.textInfo.characterCount;

            for (int i = 0; i < count; i++)
            {
                if (m_textComponent.textInfo.characterInfo[i].index >= stringIndex)
                    return i;
            }

            return count;
        }

        /// <summary>
        /// Returns / places the caret before the given character at the string index.
        /// </summary>
        /// <param name="stringIndex"></param>
        /// <returns></returns>
        protected virtual int GetMinCaretPositionFromStringIndex(int stringIndex)
        {
            int count = m_textComponent.textInfo.characterCount;

            for (int i = 0; i < count; i++)
            {
                if (stringIndex < m_textComponent.textInfo.characterInfo[i].index + m_textComponent.textInfo.characterInfo[i].stringLength)
                    return i;
            }

            return count;
        }

        /// <summary>
        /// Returns / places the caret after the given character at the string index.
        /// </summary>
        /// <param name="stringIndex"></param>
        /// <returns></returns>
        protected virtual int GetMaxCaretPositionFromStringIndex(int stringIndex)
        {
            int count = m_textComponent.textInfo.characterCount;

            for (int i = 0; i < count; i++)
            {
                if (m_textComponent.textInfo.characterInfo[i].index >= stringIndex)
                    return i;
            }

            return count;
        }

        protected virtual int GetStringIndexFromCaretPosition(int caretPosition)
        {
            // Clamp values between 0 and character count.
            ClampCaretPos(ref caretPosition);

            return m_textComponent.textInfo.characterInfo[caretPosition].index;
        }

        protected virtual void UpdateStringIndexFromCaretPosition()
        {
            StringPositionInternal = GetStringIndexFromCaretPosition(m_caretPosition);
            StringSelectPositionInternal = GetStringIndexFromCaretPosition(m_caretSelectPosition);
            m_isStringPositionDirty = false;
        }

        protected virtual void UpdateCaretPositionFromStringIndex()
        {
            CaretPositionInternal = GetCaretPositionFromStringIndex(StringPositionInternal);
            CaretSelectPositionInternal = GetCaretPositionFromStringIndex(StringSelectPositionInternal);
            m_isCaretPositionDirty = false;
        }

        #endregion

        #region ICanvasElement

        public virtual void Rebuild(CanvasUpdate update)
        {
            switch (update)
            {
                case CanvasUpdate.LatePreRender:
                    UpdateGeometry();
                    break;
            }
        }

        public virtual void LayoutComplete() { }

        public virtual void GraphicUpdateComplete() { }

        #endregion

        #region Geometry

        protected virtual void UpdateGeometry()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif

            // No need to draw a cursor on mobile as its handled by the devices keyboard with the exception of UWP.
            if (InPlaceEditing() == false && IsUWP() == false)
                return;

            if (m_cachedInputRenderer == null)
                return;

            OnFillVBO(Mesh);

            m_cachedInputRenderer.SetMesh(Mesh);
        }

        protected virtual void MarkGeometryAsDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
                return;
#endif

            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }

        protected virtual void OnFillVBO(Mesh vbo)
        {
            using (var helper = new VertexHelper())
            {
                if (!IsFocused && !m_selectionStillActive)
                {
                    helper.FillMesh(vbo);
                    return;
                }

                if (m_isStringPositionDirty)
                    UpdateStringIndexFromCaretPosition();

                if (m_isCaretPositionDirty)
                    UpdateCaretPositionFromStringIndex();

                if (!IsSelecting)
                {
                    GenerateCaret(helper, Vector2.zero);
                    TriggerTextDeselected();
                }
                else
                {
                    GenerateHighlight(helper, Vector2.zero);
                    TriggerTextSelected();
                }

                helper.FillMesh(vbo);
            }
        }

        #endregion

        #region Caret

        protected virtual void GenerateCaret(VertexHelper vbo, Vector2 roundingOffset)
        {
            if (m_caretVisible == false || m_textComponent.canvas == null || m_readOnly)
                return;

            if (m_cursorVerts == null)
            {
                CreateCursorVerts();
            }

            // TODO: Optimize to only update the caret position when needed.

            Vector2 startPosition = Vector2.zero;
            float height = 0;
            TMP_CharacterInfo currentCharacter;

            // Make sure caret position does not exceed characterInfo array size or less than zero.
            if (CaretPositionInternal >= m_textComponent.textInfo.characterInfo.Length || CaretPositionInternal < 0)
                return;

            int currentLine = m_textComponent.textInfo.characterInfo[CaretPositionInternal].lineNumber;

            // Caret is positioned at the origin for the first character of each lines and at the advance for subsequent characters.
            if (CaretPositionInternal == m_textComponent.textInfo.lineInfo[currentLine].firstCharacterIndex)
            {
                currentCharacter = m_textComponent.textInfo.characterInfo[CaretPositionInternal];
                height = currentCharacter.ascender - currentCharacter.descender;

                if (m_textComponent.verticalAlignment == VerticalAlignmentOptions.Geometry)
                    startPosition = new Vector2(currentCharacter.origin, 0 - height / 2);
                else
                    startPosition = new Vector2(currentCharacter.origin, currentCharacter.descender);
            }
            else
            {
                currentCharacter = m_textComponent.textInfo.characterInfo[CaretPositionInternal - 1];
                height = currentCharacter.ascender - currentCharacter.descender;

                if (m_textComponent.verticalAlignment == VerticalAlignmentOptions.Geometry)
                    startPosition = new Vector2(currentCharacter.xAdvance, 0 - height / 2);
                else
                    startPosition = new Vector2(currentCharacter.xAdvance, currentCharacter.descender);

            }

            if (m_softKeyboard != null && CompositionLength == 0)
            {
                int selectionStart = m_stringPosition;
                int softKeyboardStringLength = m_softKeyboard.text == null ? 0 : m_softKeyboard.text.Length;

                if (selectionStart < 0)
                    selectionStart = 0;

                if (selectionStart > softKeyboardStringLength)
                    selectionStart = softKeyboardStringLength;

                m_softKeyboard.selection = new RangeInt(selectionStart, 0);
            }

            // Adjust the position of the RectTransform based on the caret position in the viewport (only if we have focus).
            if (IsFocused && startPosition != m_lastPosition || m_forceRectTransformAdjustment || m_hasTextBeenRemoved)
                AdjustRectTransformRelativeToViewport(startPosition, height, currentCharacter.isVisible);

            m_lastPosition = startPosition;

            // Clamp Caret height
            float top = startPosition.y + height;
            float bottom = top - height;

            // Compute the width of the caret which is based on the line height of the primary font asset.
            //float width = m_CaretWidth;
            TMP_FontAsset fontAsset = m_textComponent.font;
            float baseScale = (m_textComponent.fontSize / fontAsset.faceInfo.pointSize * fontAsset.faceInfo.scale);
            float width = m_caretWidth * fontAsset.faceInfo.lineHeight * baseScale * 0.05f;
            width = Mathf.Max(width, 1.0f);

            m_cursorVerts[0].position = new Vector3(startPosition.x, bottom, 0.0f);
            m_cursorVerts[1].position = new Vector3(startPosition.x, top, 0.0f);
            m_cursorVerts[2].position = new Vector3(startPosition.x + width, top, 0.0f);
            m_cursorVerts[3].position = new Vector3(startPosition.x + width, bottom, 0.0f);

            // Set Vertex Color for the caret color.
            m_cursorVerts[0].color = CaretColor;
            m_cursorVerts[1].color = CaretColor;
            m_cursorVerts[2].color = CaretColor;
            m_cursorVerts[3].color = CaretColor;

            vbo.AddUIVertexQuad(m_cursorVerts);

            // Update position of IME window when necessary.
            if (m_shouldUpdateIMEWindowPosition || currentLine != m_previousIMEInsertionLine)
            {
                m_shouldUpdateIMEWindowPosition = false;
                m_previousIMEInsertionLine = currentLine;

                // Calculate position of IME Window in screen space.
                Camera cameraRef;
                if (m_textComponent.canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    cameraRef = null;
                else
                {
                    cameraRef = m_textComponent.canvas.worldCamera;

                    if (cameraRef == null)
                        cameraRef = Camera.current;
                }

                Vector3 cursorPosition = m_cachedInputRenderer.gameObject.transform.TransformPoint(m_cursorVerts[0].position);
                Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(cameraRef, cursorPosition);
                screenPosition.y = Screen.height - screenPosition.y;

                if (InputSystem != null)
                    InputSystem.compositionCursorPos = screenPosition;

                //Debug.Log("[" + Time.frameCount + "] Updating IME Window position  Cursor Pos: (" + cursorPosition + ")  Screen Pos: (" + screenPosition + ") with Composition Length: " + compositionLength);
            }

            //#if TMP_DEBUG_MODE
            //Debug.Log("Caret position updated at frame: " + Time.frameCount);
            //#endif
        }

        protected virtual void CreateCursorVerts()
        {
            m_cursorVerts = new UIVertex[4];

            for (int i = 0; i < m_cursorVerts.Length; i++)
            {
                m_cursorVerts[i] = UIVertex.simpleVert;
                m_cursorVerts[i].uv0 = Vector2.zero;
            }
        }

        /// <summary>
        /// Method to keep the Caret RectTransform properties in sync with the text object's RectTransform
        /// </summary>
        protected virtual void AssignCaretPositioningIfNeeded()
        {
            if (m_textComponent != null && m_caretRect != null &&
                (m_caretRect.localPosition != m_textComponent.rectTransform.localPosition ||
                 m_caretRect.localRotation != m_textComponent.rectTransform.localRotation ||
                 m_caretRect.localScale != m_textComponent.rectTransform.localScale ||
                 m_caretRect.anchorMin != m_textComponent.rectTransform.anchorMin ||
                 m_caretRect.anchorMax != m_textComponent.rectTransform.anchorMax ||
                 m_caretRect.anchoredPosition != m_textComponent.rectTransform.anchoredPosition ||
                 m_caretRect.sizeDelta != m_textComponent.rectTransform.sizeDelta ||
                 m_caretRect.pivot != m_textComponent.rectTransform.pivot))
            {
                m_caretRect.localPosition = m_textComponent.rectTransform.localPosition;
                m_caretRect.localRotation = m_textComponent.rectTransform.localRotation;
                m_caretRect.localScale = m_textComponent.rectTransform.localScale;
                m_caretRect.anchorMin = m_textComponent.rectTransform.anchorMin;
                m_caretRect.anchorMax = m_textComponent.rectTransform.anchorMax;
                m_caretRect.anchoredPosition = m_textComponent.rectTransform.anchoredPosition;
                m_caretRect.sizeDelta = m_textComponent.rectTransform.sizeDelta;
                m_caretRect.pivot = m_textComponent.rectTransform.pivot;
            }
        }

        #endregion

        #region Highlight

        protected virtual void GenerateHighlight(VertexHelper vbo, Vector2 roundingOffset)
        {
            // Update Masking Region
            UpdateMaskRegions();

            // Make sure caret position does not exceed characterInfo array size.
            //if (caretSelectPositionInternal >= m_TextComponent.textInfo.characterInfo.Length)
            //    return;

            TMP_TextInfo textInfo = m_textComponent.textInfo;

            // Return if character count is zero as there is nothing to highlight.
            if (textInfo.characterCount == 0)
                return;

            m_caretPosition = GetCaretPositionFromStringIndex(StringPositionInternal);
            m_caretSelectPosition = GetCaretPositionFromStringIndex(StringSelectPositionInternal);

            if (m_softKeyboard != null && CompositionLength == 0)
            {
                int stringPosition = m_caretPosition < m_caretSelectPosition ? textInfo.characterInfo[m_caretPosition].index : textInfo.characterInfo[m_caretSelectPosition].index;
                int length = m_caretPosition < m_caretSelectPosition ? StringSelectPositionInternal - stringPosition : StringPositionInternal - stringPosition;
                m_softKeyboard.selection = new RangeInt(stringPosition, length);
            }

            // Adjust text RectTranform position to make sure it is visible in viewport.
            Vector2 caretPosition;
            float height = 0;
            if (m_caretSelectPosition < textInfo.characterCount)
            {
                caretPosition = new Vector2(textInfo.characterInfo[m_caretSelectPosition].origin, textInfo.characterInfo[m_caretSelectPosition].descender);
                height = textInfo.characterInfo[m_caretSelectPosition].ascender - textInfo.characterInfo[m_caretSelectPosition].descender;
            }
            else
            {
                caretPosition = new Vector2(textInfo.characterInfo[m_caretSelectPosition - 1].xAdvance, textInfo.characterInfo[m_caretSelectPosition - 1].descender);
                height = textInfo.characterInfo[m_caretSelectPosition - 1].ascender - textInfo.characterInfo[m_caretSelectPosition - 1].descender;
            }

            // TODO: Don't adjust the position of the RectTransform if Reset On Deactivation is disabled
            // and we just selected the Input Field again.
            AdjustRectTransformRelativeToViewport(caretPosition, height, true);

            int startChar = Mathf.Max(0, m_caretPosition);
            int endChar = Mathf.Max(0, m_caretSelectPosition);

            // Ensure pos is always less then selPos to make the code simpler
            if (startChar > endChar)
            {
                int temp = startChar;
                startChar = endChar;
                endChar = temp;
            }

            endChar -= 1;

            //Debug.Log("Updating Highlight... Caret Position: " + startChar + " Caret Select POS: " + endChar);


            int currentLineIndex = textInfo.characterInfo[startChar].lineNumber;
            int nextLineStartIdx = textInfo.lineInfo[currentLineIndex].lastCharacterIndex;

            UIVertex vert = UIVertex.simpleVert;
            vert.uv0 = Vector2.zero;
            vert.color = SelectionColor;

            int currentChar = startChar;
            while (currentChar <= endChar && currentChar < textInfo.characterCount)
            {
                if (currentChar == nextLineStartIdx || currentChar == endChar)
                {
                    TMP_CharacterInfo startCharInfo = textInfo.characterInfo[startChar];
                    TMP_CharacterInfo endCharInfo = textInfo.characterInfo[currentChar];

                    // Extra check to handle Carriage Return
                    if (currentChar > 0 && endCharInfo.character == '\n' && textInfo.characterInfo[currentChar - 1].character == '\r')
                        endCharInfo = textInfo.characterInfo[currentChar - 1];

                    Vector2 startPosition = new Vector2(startCharInfo.origin, textInfo.lineInfo[currentLineIndex].ascender);
                    Vector2 endPosition = new Vector2(endCharInfo.xAdvance, textInfo.lineInfo[currentLineIndex].descender);

                    var startIndex = vbo.currentVertCount;
                    vert.position = new Vector3(startPosition.x, endPosition.y, 0.0f);
                    vbo.AddVert(vert);

                    vert.position = new Vector3(endPosition.x, endPosition.y, 0.0f);
                    vbo.AddVert(vert);

                    vert.position = new Vector3(endPosition.x, startPosition.y, 0.0f);
                    vbo.AddVert(vert);

                    vert.position = new Vector3(startPosition.x, startPosition.y, 0.0f);
                    vbo.AddVert(vert);

                    vbo.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
                    vbo.AddTriangle(startIndex + 2, startIndex + 3, startIndex + 0);

                    startChar = currentChar + 1;
                    currentLineIndex++;

                    if (currentLineIndex < textInfo.lineCount)
                        nextLineStartIdx = textInfo.lineInfo[currentLineIndex].lastCharacterIndex;
                }
                currentChar++;
            }

            //#if TMP_DEBUG_MODE
            //    Debug.Log("Text selection updated at frame: " + Time.frameCount);
            //#endif
        }

        #endregion

        #region Viewport Rect Utility

        /// <summary>
        /// Adjusts the relative position of the body of the text relative to the viewport.
        /// </summary>
        /// <param name="relativePosition"></param>
        protected virtual void AdjustTextPositionRelativeToViewport(float relativePosition)
        {
            if (m_textViewport == null)
                return;

            TMP_TextInfo textInfo = m_textComponent.textInfo;

            // Check to make sure we have valid data and lines to query.
            if (textInfo == null || textInfo.lineInfo == null || textInfo.lineCount == 0 || textInfo.lineCount > textInfo.lineInfo.Length) return;

            float verticalAlignmentOffset = 0;
            float textHeight = m_textComponent.preferredHeight;

            switch (m_textComponent.verticalAlignment)
            {
                case VerticalAlignmentOptions.Top:
                    verticalAlignmentOffset = 0;
                    break;
                case VerticalAlignmentOptions.Middle:
                    verticalAlignmentOffset = 0.5f;
                    break;
                case VerticalAlignmentOptions.Bottom:
                    verticalAlignmentOffset = 1.0f;
                    break;
                case VerticalAlignmentOptions.Baseline:
                    break;
                case VerticalAlignmentOptions.Geometry:
                    verticalAlignmentOffset = 0.5f;
                    textHeight = m_textComponent.bounds.size.y;
                    break;
                case VerticalAlignmentOptions.Capline:
                    verticalAlignmentOffset = 0.5f;
                    break;
            }

            m_textComponent.rectTransform.anchoredPosition = new Vector2(m_textComponent.rectTransform.anchoredPosition.x, (textHeight - m_textViewport.rect.height) * (relativePosition - verticalAlignmentOffset));

            AssignCaretPositioningIfNeeded();

            //Debug.Log("Text height: " + m_TextComponent.preferredHeight + "  Viewport height: " + m_TextViewport.rect.height + "  Adjusted RectTransform anchordedPosition:" + m_TextComponent.rectTransform.anchoredPosition + "  Text Bounds: " + m_TextComponent.bounds.ToString("f3"));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="startPosition"></param>
        /// <param name="height"></param>
        /// <param name="isCharVisible"></param>
        protected virtual void AdjustRectTransformRelativeToViewport(Vector2 startPosition, float height, bool isCharVisible)
        {
            //Debug.Log("Adjusting transform position relative to viewport.");

            if (m_textViewport == null)
                return;

            Vector3 localPosition = transform.localPosition;
            Vector3 textComponentLocalPosition = m_textComponent.rectTransform.localPosition;
            Vector3 textViewportLocalPosition = m_textViewport.localPosition;
            Rect textViewportRect = m_textViewport.rect;

            Vector2 caretPosition = new Vector2(startPosition.x + textComponentLocalPosition.x + textViewportLocalPosition.x + localPosition.x, startPosition.y + textComponentLocalPosition.y + textViewportLocalPosition.y + localPosition.y);
            Rect viewportWSRect = new Rect(localPosition.x + textViewportLocalPosition.x + textViewportRect.x, localPosition.y + textViewportLocalPosition.y + textViewportRect.y, textViewportRect.width, textViewportRect.height);

            // Adjust the position of the RectTransform based on the caret position in the viewport.
            float rightOffset = viewportWSRect.xMax - (caretPosition.x + m_textComponent.margin.z + m_caretWidth);
            if (rightOffset < 0f)
            {
                if (!MultiLine || (MultiLine && isCharVisible))
                {
                    //Debug.Log("Shifting text to the LEFT by " + rightOffset.ToString("f3"));
                    m_textComponent.rectTransform.anchoredPosition += new Vector2(rightOffset, 0);

                    AssignCaretPositioningIfNeeded();
                }
            }

            float leftOffset = (caretPosition.x - m_textComponent.margin.x) - viewportWSRect.xMin;
            if (leftOffset < 0f)
            {
                //Debug.Log("Shifting text to the RIGHT by " + leftOffset.ToString("f3"));
                m_textComponent.rectTransform.anchoredPosition += new Vector2(-leftOffset, 0);
                AssignCaretPositioningIfNeeded();
            }

            // Adjust text area up or down if not in single line mode.
            if (m_lineType != ELineType.SingleLine)
            {
                float topOffset = viewportWSRect.yMax - (caretPosition.y + height);
                if (topOffset < -0.0001f)
                {
                    //Debug.Log("Shifting text to Up " + topOffset.ToString("f3"));
                    m_textComponent.rectTransform.anchoredPosition += new Vector2(0, topOffset);
                    AssignCaretPositioningIfNeeded();
                }

                float bottomOffset = caretPosition.y - viewportWSRect.yMin;
                if (bottomOffset < 0f)
                {
                    //Debug.Log("Shifting text to Down " + bottomOffset.ToString("f3"));
                    m_textComponent.rectTransform.anchoredPosition -= new Vector2(0, bottomOffset);
                    AssignCaretPositioningIfNeeded();
                }
            }

            // Special handling of backspace/text being removed
            if (m_hasTextBeenRemoved)
            {
                float anchoredPositionX = m_textComponent.rectTransform.anchoredPosition.x;

                float firstCharPosition = localPosition.x + textViewportLocalPosition.x + textComponentLocalPosition.x + m_textComponent.textInfo.characterInfo[0].origin - m_textComponent.margin.x;
                int lastCharacterIndex = ClampArrayIndex(m_textComponent.textInfo.characterCount - 1);
                float lastCharPosition = localPosition.x + textViewportLocalPosition.x + textComponentLocalPosition.x + m_textComponent.textInfo.characterInfo[lastCharacterIndex].origin + m_textComponent.margin.z + m_caretWidth;

                if (anchoredPositionX > 0.0001f && firstCharPosition > viewportWSRect.xMin)
                {
                    float offset = viewportWSRect.xMin - firstCharPosition;

                    if (anchoredPositionX < -offset)
                        offset = -anchoredPositionX;

                    m_textComponent.rectTransform.anchoredPosition += new Vector2(offset, 0);
                    AssignCaretPositioningIfNeeded();
                }
                else if (anchoredPositionX < -0.0001f && lastCharPosition < viewportWSRect.xMax)
                {
                    float offset = viewportWSRect.xMax - lastCharPosition;

                    if (-anchoredPositionX < offset)
                        offset = -anchoredPositionX;

                    m_textComponent.rectTransform.anchoredPosition += new Vector2(offset, 0);
                    AssignCaretPositioningIfNeeded();
                }

                m_hasTextBeenRemoved = false;
            }

            m_forceRectTransformAdjustment = false;
        }

        #endregion

        #region Input Validation

        /// <summary>
        /// Validate the specified input.
        /// </summary>
        protected virtual char Validate(string text, int pos, char ch)
        {
            // Validation is disabled
            if (CharacterValidation == ECharacterValidation.None || !enabled)
                return ch;

            if (CharacterValidation == ECharacterValidation.Integer || CharacterValidation == ECharacterValidation.Decimal)
            {
                // Integer and decimal
                bool cursorBeforeDash = (pos == 0 && text.Length > 0 && text[0] == '-');
                bool selectionAtStart = StringPositionInternal == 0 || StringSelectPositionInternal == 0;
                if (!cursorBeforeDash)
                {
                    if (ch >= '0' && ch <= '9') return ch;
                    if (ch == '-' && (pos == 0 || selectionAtStart) && !text.Contains('-')) return ch;

                    var separator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                    if (ch == Convert.ToChar(separator) && CharacterValidation == ECharacterValidation.Decimal && !text.Contains(separator)) return ch;

                    //Some keyboards including Samsung require double tapping a . to get a - this allows these keyboards to input negative integers
                    if (CharacterValidation == ECharacterValidation.Integer && ch == '.' && (pos == 0 || selectionAtStart) && !text.Contains('-')) return '-';
                }

            }
            else if (CharacterValidation == ECharacterValidation.Digit)
            {
                if (ch >= '0' && ch <= '9') return ch;
            }
            else if (CharacterValidation == ECharacterValidation.Alphanumeric)
            {
                // All alphanumeric characters
                if (ch >= 'A' && ch <= 'Z') return ch;
                if (ch >= 'a' && ch <= 'z') return ch;
                if (ch >= '0' && ch <= '9') return ch;
            }
            else if (CharacterValidation == ECharacterValidation.Name)
            {
                char prevChar = (text.Length > 0) ? text[Mathf.Clamp(pos - 1, 0, text.Length - 1)] : ' ';
                char lastChar = (text.Length > 0) ? text[Mathf.Clamp(pos, 0, text.Length - 1)] : ' ';
                char nextChar = (text.Length > 0) ? text[Mathf.Clamp(pos + 1, 0, text.Length - 1)] : '\n';

                if (char.IsLetter(ch))
                {
                    // First letter is always capitalized
                    if (char.IsLower(ch) && pos == 0)
                        return char.ToUpper(ch);

                    // Letter following a space or hyphen is always capitalized
                    if (char.IsLower(ch) && (prevChar == ' ' || prevChar == '-'))
                        return char.ToUpper(ch);

                    // Uppercase letters are only allowed after spaces, apostrophes, hyphens or lowercase letter
                    if (char.IsUpper(ch) && pos > 0 && prevChar != ' ' && prevChar != '\'' && prevChar != '-' && !char.IsLower(prevChar))
                        return char.ToLower(ch);

                    // Do not allow uppercase characters to be inserted before another uppercase character
                    if (char.IsUpper(ch) && char.IsUpper(lastChar))
                        return (char)0;

                    // If character was already in correct case, return it as-is.
                    // Also, letters that are neither upper nor lower case are always allowed.
                    return ch;
                }
                else if (ch == '\'')
                {
                    // Don't allow more than one apostrophe
                    if (lastChar != ' ' && lastChar != '\'' && nextChar != '\'' && !text.Contains("'"))
                        return ch;
                }

                // Allow inserting a hyphen after a character
                if (char.IsLetter(prevChar) && ch == '-' && lastChar != '-')
                {
                    return ch;
                }

                if ((ch == ' ' || ch == '-') && pos != 0)
                {
                    // Don't allow more than one space in a row
                    if (prevChar != ' ' && prevChar != '\'' && prevChar != '-' &&
                        lastChar != ' ' && lastChar != '\'' && lastChar != '-' &&
                        nextChar != ' ' && nextChar != '\'' && nextChar != '-')
                        return ch;
                }
            }
            else if (CharacterValidation == ECharacterValidation.EmailAddress)
            {
                // From StackOverflow about allowed characters in email addresses:
                // Uppercase and lowercase English letters (a-z, A-Z)
                // Digits 0 to 9
                // Characters ! # $ % & ' * + - / = ? ^ _ ` { | } ~
                // Character . (dot, period, full stop) provided that it is not the first or last character,
                // and provided also that it does not appear two or more times consecutively.

                if (ch >= 'A' && ch <= 'Z') return ch;
                if (ch >= 'a' && ch <= 'z') return ch;
                if (ch >= '0' && ch <= '9') return ch;
                if (ch == '@' && text.IndexOf('@') == -1) return ch;
                if (kEmailSpecialCharacters.IndexOf(ch) != -1) return ch;
                if (ch == '.')
                {
                    char lastChar = (text.Length > 0) ? text[Mathf.Clamp(pos, 0, text.Length - 1)] : ' ';
                    char nextChar = (text.Length > 0) ? text[Mathf.Clamp(pos + 1, 0, text.Length - 1)] : '\n';
                    if (lastChar != '.' && nextChar != '.')
                        return ch;
                }
            }
            else if (CharacterValidation == ECharacterValidation.Regex)
            {
                // Regex expression
                if (Regex.IsMatch(ch.ToString(), m_regexValue))
                {
                    return ch;
                }
            }
            else if (CharacterValidation == ECharacterValidation.CustomValidator)
            {
                if (m_inputValidator != null)
                {
                    char c = m_inputValidator.Validate(ref text, ref pos, ch);
                    m_text = text;
                    StringSelectPositionInternal = StringPositionInternal = pos;
                    return c;
                }
            }
            return (char)0;
        }

        #endregion

        #region Activation

        public virtual void ActivateInputField()
        {
            if (m_textComponent == null || m_textComponent.font == null || !IsActive() || !IsInteractable())
                return;

            if (IsFocused)
            {
                if (m_softKeyboard != null && !m_softKeyboard.active)
                {
                    m_softKeyboard.active = true;
                    m_softKeyboard.text = m_text;
                }
            }

            m_shouldActivateNextUpdate = true;
        }

        protected virtual void ActivateInputFieldInternal()
        {
            if (EventSystem.current == null)
                return;

            if (EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);

            // Cache the value of isInPlaceEditingAllowed, because on UWP this involves calling into native code
            // The value only needs to be updated once when the TouchKeyboard is opened.
            m_touchKeyboardAllowsInPlaceEditing = !s_IsQuestDevice && TouchScreenKeyboard.isInPlaceEditingAllowed;

            if (TouchScreenKeyboardShouldBeUsed() && ShouldHideSoftKeyboard == false)
            {
                if (InputSystem != null && InputSystem.touchSupported)
                {
                    TouchScreenKeyboard.hideInput = ShouldHideMobileInput;
                }

                if (ShouldHideSoftKeyboard == false && m_readOnly == false)
                {
                    m_softKeyboard = (InputType == EInputType.Password) ?
                        TouchScreenKeyboard.Open(m_text, KeyboardType, false, MultiLine, true, IsTouchScreenKeyboardAlert, "", CharacterLimit) :
                        TouchScreenKeyboard.Open(m_text, KeyboardType, InputType == EInputType.AutoCorrect, MultiLine, false, IsTouchScreenKeyboardAlert, "", CharacterLimit);

                    OnFocus();

                    // Opening the soft keyboard sets its selection to the end of the text.
                    // As such, we set the selection to match the Input Field's internal selection. // UUM-112457
                    if (m_softKeyboard != null)
                    {
                        int length = StringPositionInternal < StringSelectPositionInternal ? StringSelectPositionInternal - StringPositionInternal : StringPositionInternal - StringSelectPositionInternal;
                        m_softKeyboard.selection = new RangeInt(StringPositionInternal < StringSelectPositionInternal ? StringPositionInternal : StringSelectPositionInternal, length);
                    }
                    //}
                }
            }
            else
            {
                if (!TouchScreenKeyboardShouldBeUsed() && m_readOnly == false && InputSystem != null)
                    InputSystem.imeCompositionMode = IMECompositionMode.On;

                OnFocus();
            }

            m_allowInput = true;
            m_originalText = Text;
            m_wasCanceled = false;
            SetCaretVisible();
            UpdateLabel();
        }

        public virtual void OnControlClick()
        {
            //Debug.Log("Input Field control click...");
        }

        public virtual void ReleaseSelection()
        {
            m_selectionStillActive = false;
            m_releaseSelection = false;
            m_previouslySelectedObject = null;

            MarkGeometryAsDirty();

            TriggerEndEdit();
            TriggerTextDeselected();
        }

        public virtual void DeactivateInputField(bool clearSelection = false)
        {
            //Debug.Log("Deactivate Input Field...");

            // Not activated do nothing.
            if (!m_allowInput)
                return;

            m_hasDoneFocusTransition = false;
            m_allowInput = false;

            if (m_placeholder != null)
                m_placeholder.enabled = string.IsNullOrEmpty(m_text);

            if (m_textComponent != null && IsInteractable())
            {
                if (m_wasCanceled && m_restoreOriginalTextOnEscape && !m_isKeyboardBeingClosedInHoloLens)
                    Text = m_originalText;

                if (m_softKeyboard != null)
                {
                    m_softKeyboard.active = false;
                    m_softKeyboard = null;
                }

                m_selectionStillActive = true;

                if (m_resetOnDeActivation || m_releaseSelection || clearSelection)
                {
                    //m_StringPosition = m_StringSelectPosition = 0;
                    //m_CaretPosition = m_CaretSelectPosition = 0;
                    //m_TextComponent.rectTransform.localPosition = m_DefaultTransformPosition;

                    if (m_verticalScrollbar == null)
                        ReleaseSelection();
                }

                if (InputSystem != null)
                    InputSystem.imeCompositionMode = IMECompositionMode.Auto;

                m_isKeyboardBeingClosedInHoloLens = false;
            }

            MarkGeometryAsDirty();
        }

        #endregion

        #region Event Handlers

        protected override void OnAfterSelect(BaseEventData eventData)
        {
            base.OnAfterSelect(eventData);

            TriggerFocus();

            if (ShouldActivateOnSelect)
                ActivateInputField();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            ActivateInputField();
        }

        protected override void OnBeforeDeselect(BaseEventData eventData)
        {
            base.OnBeforeDeselect(eventData);
            DeactivateInputField();
        }
        protected override void OnAfterDeselect(BaseEventData eventData)
        {
            base.OnAfterDeselect(eventData);
            TriggerFocusLost();
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;

            if (!IsFocused)
                m_shouldActivateNextUpdate = true;

            TriggerSubmit();
#if PLATFORM_TVOS
            // When a keyboard is open in tvOS, the submit button is used for typing.
            // Only actually close the keyboard on tvOS if "Done" was pressed in the soft keyboard.
            if (m_SoftKeyboard != null && m_SoftKeyboard.status == TouchScreenKeyboard.Status.Visible)
                return;
#endif
            DeactivateInputField();
            eventData?.Use();
        }

        public virtual void OnCancel(BaseEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;

            if (!IsFocused)
                m_shouldActivateNextUpdate = true;

            m_wasCanceled = true;
            DeactivateInputField();
            eventData.Use();
        }

        public override void OnMove(AxisEventData eventData)
        {
            // Prevent UI navigation while text is being edited.
            if (!m_allowInput)
                base.OnMove(eventData);
        }

        //public virtual void OnLostFocus(BaseEventData eventData)
        //{
        //    if (!IsActive() || !IsInteractable())
        //        return;
        //}

        #endregion

        #region Content Type Setters

        protected virtual void EnforceContentType()
        {
            switch (ContentType)
            {
                case EContentType.Standard:
                    {
                        // Don't enforce line type for this content type.
                        m_inputType = EInputType.Standard;
                        m_keyboardType = TouchScreenKeyboardType.Default;
                        m_characterValidation = ECharacterValidation.None;
                        break;
                    }
                case EContentType.Autocorrected:
                    {
                        // Don't enforce line type for this content type.
                        m_inputType = EInputType.AutoCorrect;
                        m_keyboardType = TouchScreenKeyboardType.Default;
                        m_characterValidation = ECharacterValidation.None;
                        break;
                    }
                case EContentType.IntegerNumber:
                    {
                        m_lineType = ELineType.SingleLine;
                        m_inputType = EInputType.Standard;
                        m_keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
                        m_characterValidation = ECharacterValidation.Integer;
                        break;
                    }
                case EContentType.DecimalNumber:
                    {
                        m_lineType = ELineType.SingleLine;
                        m_inputType = EInputType.Standard;
                        m_keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
                        m_characterValidation = ECharacterValidation.Decimal;
                        break;
                    }
                case EContentType.Alphanumeric:
                    {
                        m_lineType = ELineType.SingleLine;
                        m_inputType = EInputType.Standard;
                        m_keyboardType = TouchScreenKeyboardType.ASCIICapable;
                        m_characterValidation = ECharacterValidation.Alphanumeric;
                        break;
                    }
                case EContentType.Name:
                    {
                        m_lineType = ELineType.SingleLine;
                        m_inputType = EInputType.Standard;
                        m_keyboardType = TouchScreenKeyboardType.Default;
                        m_characterValidation = ECharacterValidation.Name;
                        break;
                    }
                case EContentType.EmailAddress:
                    {
                        m_lineType = ELineType.SingleLine;
                        m_inputType = EInputType.Standard;
                        m_keyboardType = TouchScreenKeyboardType.EmailAddress;
                        m_characterValidation = ECharacterValidation.EmailAddress;
                        break;
                    }
                case EContentType.Password:
                    {
                        m_lineType = ELineType.SingleLine;
                        m_inputType = EInputType.Password;
                        m_keyboardType = TouchScreenKeyboardType.Default;
                        m_characterValidation = ECharacterValidation.None;
                        break;
                    }
                case EContentType.Pin:
                    {
                        m_lineType = ELineType.SingleLine;
                        m_inputType = EInputType.Password;
                        m_keyboardType = TouchScreenKeyboardType.NumberPad;
                        m_characterValidation = ECharacterValidation.Digit;
                        break;
                    }
                default:
                    {
                        // Includes Custom type. Nothing should be enforced.
                        break;
                    }
            }

            SetTextComponentWrapMode();
        }

        protected virtual void SetTextComponentWrapMode()
        {
            if (m_textComponent == null)
                return;

            if (MultiLine)
                m_textComponent.textWrappingMode = TextWrappingModes.Normal;
            else
                m_textComponent.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
        }

        // Control Rich Text option on the text component.
        protected virtual void SetTextComponentRichTextMode()
        {
            if (m_textComponent == null)
                return;

            m_textComponent.richText = m_richText;
        }

        protected virtual void SetToCustomIfContentTypeIsNot(params EContentType[] allowedContentTypes)
        {
            if (ContentType == EContentType.Custom)
                return;

            for (int i = 0; i < allowedContentTypes.Length; i++)
                if (ContentType == allowedContentTypes[i])
                    return;

            ContentType = EContentType.Custom;
        }

        protected virtual void SetToCustom()
        {
            if (ContentType == EContentType.Custom)
                return;

            ContentType = EContentType.Custom;
        }

        protected virtual void SetToCustom(ECharacterValidation characterValidation)
        {
            if (ContentType == EContentType.Custom)
            {
                characterValidation = ECharacterValidation.CustomValidator;
                return;
            }

            ContentType = EContentType.Custom;
            characterValidation = ECharacterValidation.CustomValidator;
        }

        #endregion

        #region Transitions

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (m_hasDoneFocusTransition)
                state = SelectionState.Selected;
            else if (state == SelectionState.Pressed)
                m_hasDoneFocusTransition = true;

            base.DoStateTransition(state, instant);
        }

        #endregion

        #region ILayoutElement

        /// <summary>
        /// See ILayoutElement.CalculateLayoutInputHorizontal.
        /// </summary>
        public virtual void CalculateLayoutInputHorizontal() { }

        /// <summary>
        /// See ILayoutElement.CalculateLayoutInputVertical.
        /// </summary>
        public virtual void CalculateLayoutInputVertical() { }

        /// <summary>
        /// See ILayoutElement.minWidth.
        /// </summary>
        public virtual float minWidth => 0f;

        /// <summary>
        /// Get the displayed with of all input characters.
        /// </summary>
        public virtual float preferredWidth
        {
            get
            {
                if (TextComponent == null)
                    return 0;

                float horizontalPadding = 0;

                if (m_layoutGroup != null)
                    horizontalPadding = m_layoutGroup.padding.horizontal;

                if (m_textViewport != null)
                    horizontalPadding += m_textViewport.offsetMin.x - m_textViewport.offsetMax.x;

                return m_textComponent.preferredWidth + horizontalPadding; // Should add some extra padding for caret
            }
        }

        /// <summary>
        /// See ILayoutElement.flexibleWidth.
        /// </summary>
        public virtual float flexibleWidth => -1f;

        /// <summary>
        /// See ILayoutElement.minHeight.
        /// </summary>
        public virtual float minHeight => 0f;

        /// <summary>
        /// Get the height of all the text if constrained to the height of the RectTransform.
        /// </summary>
        public virtual float preferredHeight
        {
            get
            {
                if (TextComponent == null)
                    return 0;

                float verticalPadding = 0;

                if (m_layoutGroup != null)
                    verticalPadding = m_layoutGroup.padding.vertical;

                if (m_textViewport != null)
                    verticalPadding += m_textViewport.offsetMin.y - m_textViewport.offsetMax.y;

                return m_textComponent.preferredHeight + verticalPadding;
            }
        }

        /// <summary>
        /// See ILayoutElement.flexibleHeight.
        /// </summary>
        public virtual float flexibleHeight => -1f;

        /// <summary>
        /// See ILayoutElement.layoutPriority.
        /// </summary>
        public virtual int layoutPriority => 1;

        #endregion

        #region Setters

        /// <summary>
        /// Function to conveniently set the point size of both Placeholder and Input Field text object.
        /// </summary>
        /// <param name="pointSize"></param>
        public virtual void SetGlobalPointSize(float pointSize)
        {
            TMP_Text placeholderTextComponent = m_placeholder as TMP_Text;

            if (placeholderTextComponent != null)
                placeholderTextComponent.fontSize = pointSize;

            TextComponent.fontSize = pointSize;
        }

        /// <summary>
        /// Function to conveniently set the Font Asset of both Placeholder and Input Field text object.
        /// </summary>
        /// <param name="fontAsset"></param>
        public virtual void SetGlobalFontAsset(TMP_FontAsset fontAsset)
        {
            TMP_Text placeholderTextComponent = m_placeholder as TMP_Text;

            if (placeholderTextComponent != null)
                placeholderTextComponent.font = fontAsset;

            TextComponent.font = fontAsset;
        }

        #endregion


        #region Editor

#if UNITY_EDITOR

        // Remember: This is NOT related to text validation!
        // This is Unity's own OnValidate method which is invoked when changing values in the Inspector.
        protected override void OnValidate()
        {
            base.OnValidate();
            EnforceContentType();

            m_characterLimit = Math.Max(0, m_characterLimit);

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            SetTextComponentRichTextMode();

            UpdateLabel();

            if (m_allowInput)
                SetCaretActive();
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(UIInputField))]
    public class UIInputFieldEditor : UISelectableEditor
    {
        #region Members

        protected static bool m_fontSettingsOpen = false;
        protected static bool m_extraSettingsOpen = false;

        protected SerializedProperty p_textViewport;
        protected SerializedProperty p_textComponent;
        protected SerializedProperty p_text;
        protected SerializedProperty p_contentType;
        protected SerializedProperty p_lineType;
        protected SerializedProperty p_lineLimit;
        protected SerializedProperty p_inputType;
        protected SerializedProperty p_characterValidation;
        protected SerializedProperty p_inputValidator;
        protected SerializedProperty p_regexValue;
        protected SerializedProperty p_keyboardType;
        protected SerializedProperty p_characterLimit;
        protected SerializedProperty p_passwordChar;
        protected SerializedProperty p_caretBlinkRate;
        protected SerializedProperty p_caretWidth;
        protected SerializedProperty p_caretColor;
        protected SerializedProperty p_customCaretColor;
        protected SerializedProperty p_selectionColor;
        protected SerializedProperty p_hideMobileKeyboard;
        protected SerializedProperty p_hideMobileInput;
        protected SerializedProperty p_placeholder;
        protected SerializedProperty p_verticalScrollbar;
        protected SerializedProperty p_scrollbarScrollSensitivity;
        protected SerializedProperty p_readOnly;
        protected SerializedProperty p_richText;
        protected SerializedProperty p_richTextEditingAllowed;
        protected SerializedProperty p_resetOnDeActivation;
        protected SerializedProperty p_keepTextSelectionVisible;
        protected SerializedProperty p_restoreOriginalTextOnEscape;
        protected SerializedProperty p_shouldActivateOnSelect;
        
        protected SerializedProperty p_onFocusSelectAll;
        protected SerializedProperty p_globalPointSize;
        protected SerializedProperty p_globalFontAsset;

        //TMP_InputValidator m_ValidationScript;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            p_textViewport = serializedObject.FindProperty("m_textViewport");
            p_textComponent = serializedObject.FindProperty("m_textComponent");
            p_text = serializedObject.FindProperty("m_text");
            p_contentType = serializedObject.FindProperty("m_contentType");
            p_lineType = serializedObject.FindProperty("m_lineType");
            p_lineLimit = serializedObject.FindProperty("m_lineLimit");
            p_inputType = serializedObject.FindProperty("m_inputType");
            p_characterValidation = serializedObject.FindProperty("m_characterValidation");
            p_inputValidator = serializedObject.FindProperty("m_inputValidator");
            p_regexValue = serializedObject.FindProperty("m_regexValue");
            p_keyboardType = serializedObject.FindProperty("m_keyboardType");
            p_characterLimit = serializedObject.FindProperty("m_characterLimit");
            p_passwordChar = serializedObject.FindProperty("m_passwordChar");
            p_caretBlinkRate = serializedObject.FindProperty("m_caretBlinkRate");
            p_caretWidth = serializedObject.FindProperty("m_caretWidth");
            p_caretColor = serializedObject.FindProperty("m_caretColor");
            p_customCaretColor = serializedObject.FindProperty("m_customCaretColor");
            p_selectionColor = serializedObject.FindProperty("m_selectionColor");

            p_hideMobileKeyboard = serializedObject.FindProperty("m_hideSoftKeyboard");
            p_hideMobileInput = serializedObject.FindProperty("m_hideMobileInput");

            p_placeholder = serializedObject.FindProperty("m_placeholder");
            p_verticalScrollbar = serializedObject.FindProperty("m_verticalScrollbar");
            p_scrollbarScrollSensitivity = serializedObject.FindProperty("m_scrollSensitivity");

            p_readOnly = serializedObject.FindProperty("m_readOnly");
            p_richText = serializedObject.FindProperty("m_richText");
            p_richTextEditingAllowed = serializedObject.FindProperty("m_isRichTextEditingAllowed");
            p_resetOnDeActivation = serializedObject.FindProperty("m_resetOnDeActivation");
            p_keepTextSelectionVisible = serializedObject.FindProperty("m_keepTextSelectionVisible");
            p_restoreOriginalTextOnEscape = serializedObject.FindProperty("m_restoreOriginalTextOnEscape");

            p_onFocusSelectAll = serializedObject.FindProperty("m_onFocusSelectAll");
            p_shouldActivateOnSelect = serializedObject.FindProperty("m_shouldActivateOnSelect");

            p_globalPointSize = serializedObject.FindProperty("m_globalPointSize");
            p_globalFontAsset = serializedObject.FindProperty("m_globalFontAsset");

            m_propertiesToExclude.Add(p_textViewport.propertyPath);
            m_propertiesToExclude.Add(p_textComponent.propertyPath);
            m_propertiesToExclude.Add(p_text.propertyPath);
            m_propertiesToExclude.Add(p_contentType.propertyPath);
            m_propertiesToExclude.Add(p_lineType.propertyPath);
            m_propertiesToExclude.Add(p_lineLimit.propertyPath);
            m_propertiesToExclude.Add(p_inputType.propertyPath);
            m_propertiesToExclude.Add(p_characterValidation.propertyPath);
            m_propertiesToExclude.Add(p_inputValidator.propertyPath);
            m_propertiesToExclude.Add(p_regexValue.propertyPath);
            m_propertiesToExclude.Add(p_keyboardType.propertyPath);
            m_propertiesToExclude.Add(p_characterLimit.propertyPath);
            m_propertiesToExclude.Add(p_passwordChar.propertyPath);
            m_propertiesToExclude.Add(p_caretBlinkRate.propertyPath);
            m_propertiesToExclude.Add(p_caretWidth.propertyPath);
            m_propertiesToExclude.Add(p_caretColor.propertyPath);
            m_propertiesToExclude.Add(p_customCaretColor.propertyPath);
            m_propertiesToExclude.Add(p_selectionColor.propertyPath);
            m_propertiesToExclude.Add(p_hideMobileKeyboard.propertyPath);
            m_propertiesToExclude.Add(p_hideMobileInput.propertyPath);
            m_propertiesToExclude.Add(p_placeholder.propertyPath);
            m_propertiesToExclude.Add(p_verticalScrollbar.propertyPath);
            m_propertiesToExclude.Add(p_scrollbarScrollSensitivity.propertyPath);
            m_propertiesToExclude.Add(p_readOnly.propertyPath);
            m_propertiesToExclude.Add(p_richText.propertyPath);
            m_propertiesToExclude.Add(p_richTextEditingAllowed.propertyPath);
            m_propertiesToExclude.Add(p_resetOnDeActivation.propertyPath);
            m_propertiesToExclude.Add(p_keepTextSelectionVisible.propertyPath);
            m_propertiesToExclude.Add(p_restoreOriginalTextOnEscape.propertyPath);
            m_propertiesToExclude.Add(p_onFocusSelectAll.propertyPath);
            m_propertiesToExclude.Add(p_shouldActivateOnSelect.propertyPath);
            m_propertiesToExclude.Add(p_globalPointSize.propertyPath);
            m_propertiesToExclude.Add(p_globalFontAsset.propertyPath);
        }

        #endregion

        #region Core GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(p_textViewport);

            EditorGUILayout.PropertyField(p_textComponent);

            TextMeshProUGUI text = null;
            if (p_textComponent != null && p_textComponent.objectReferenceValue != null)
            {
                text = p_textComponent.objectReferenceValue as TextMeshProUGUI;
                //if (text.supportRichText)
                //{
                //    EditorGUILayout.HelpBox("Using Rich Text with input is unsupported.", MessageType.Warning);
                //}
            }

            EditorGUI.BeginDisabledGroup(p_textComponent == null || p_textComponent.objectReferenceValue == null);

            // TEXT INPUT BOX
            EditorGUILayout.PropertyField(p_text);

            // INPUT FIELD SETTINGS
            #region INPUT FIELD SETTINGS

            m_fontSettingsOpen = EditorGUILayout.Foldout(m_fontSettingsOpen, "Input Field Settings", true, TMP_UIStyleManager.boldFoldout);

            if (m_fontSettingsOpen)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(p_globalFontAsset, new GUIContent("Font Asset", "Set the Font Asset for both Placeholder and Input Field text object."));
                if (EditorGUI.EndChangeCheck())
                {
                    UIInputField inputField = target as UIInputField;
                    inputField.SetGlobalFontAsset(p_globalFontAsset.objectReferenceValue as TMP_FontAsset);
                }


                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(p_globalPointSize, new GUIContent("Point Size", "Set the point size of both Placeholder and Input Field text object."));
                if (EditorGUI.EndChangeCheck())
                {
                    UIInputField inputField = target as UIInputField;
                    inputField.SetGlobalPointSize(p_globalPointSize.floatValue);
                }

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(p_characterLimit);

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(p_contentType);
                if (!p_contentType.hasMultipleDifferentValues)
                {
                    EditorGUI.indentLevel++;

                    if (p_contentType.enumValueIndex == (int)UIInputField.EContentType.Standard ||
                        p_contentType.enumValueIndex == (int)UIInputField.EContentType.Autocorrected ||
                        p_contentType.enumValueIndex == (int)UIInputField.EContentType.Custom)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(p_lineType);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (text != null)
                            {
                                if (p_lineType.enumValueIndex == (int)UIInputField.ELineType.SingleLine)
                                    text.textWrappingMode = TextWrappingModes.PreserveWhitespaceNoWrap;
                                else
                                {
                                    text.textWrappingMode = TextWrappingModes.Normal;
                                }
                            }
                        }

                        if (p_lineType.enumValueIndex != (int)UIInputField.ELineType.SingleLine)
                        {
                            EditorGUILayout.PropertyField(p_lineLimit);
                        }
                    }

                    if (p_contentType.enumValueIndex == (int)UIInputField.EContentType.Password
                        || p_contentType.enumValueIndex == (int)UIInputField.EContentType.Pin)
                    {
                        EditorGUILayout.PropertyField(p_passwordChar);
                    }

                    if (p_contentType.enumValueIndex == (int)UIInputField.EContentType.Custom)
                    {
                        EditorGUILayout.PropertyField(p_inputType);
                        EditorGUILayout.PropertyField(p_keyboardType);
                        EditorGUILayout.PropertyField(p_characterValidation);
                        if (p_characterValidation.enumValueIndex == (int)UIInputField.ECharacterValidation.Regex)
                        {
                            EditorGUILayout.PropertyField(p_regexValue);
                        }
                        else if (p_characterValidation.enumValueIndex == (int)UIInputField.ECharacterValidation.CustomValidator)
                        {
                            EditorGUILayout.PropertyField(p_inputValidator);
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(p_placeholder);
                EditorGUILayout.PropertyField(p_verticalScrollbar);

                if (p_verticalScrollbar.objectReferenceValue != null)
                    EditorGUILayout.PropertyField(p_scrollbarScrollSensitivity);

                EditorGUILayout.PropertyField(p_caretBlinkRate);
                EditorGUILayout.PropertyField(p_caretWidth);

                EditorGUILayout.PropertyField(p_customCaretColor);
                if (p_customCaretColor.boolValue)
                {
                    EditorGUILayout.PropertyField(p_caretColor);
                }

                EditorGUILayout.PropertyField(p_selectionColor);

                EditorGUI.indentLevel--;
            }
            #endregion


            // CONTROL SETTINGS
            #region CONTROL SETTINGS

            m_extraSettingsOpen = EditorGUILayout.Foldout(m_extraSettingsOpen, "Control Settings", true, TMP_UIStyleManager.boldFoldout);

            if (m_extraSettingsOpen)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(p_onFocusSelectAll, new GUIContent("OnFocus - Select All", "Should all the text be selected when the Input Field is selected?"));
                EditorGUILayout.PropertyField(p_resetOnDeActivation, new GUIContent("Reset On Deactivation", "Should the Text and Caret position be reset when Input Field loses focus and is Deactivated?"));

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(p_keepTextSelectionVisible, new GUIContent("Keep Text Selection Visible", "Should the text selection remain visible when the input field loses focus and is deactivated?"));
                EditorGUI.indentLevel--;

                EditorGUILayout.PropertyField(p_restoreOriginalTextOnEscape, new GUIContent("Restore On ESC Key", "Should the original text be restored when pressing ESC? (Property not applicable for HoloLens)"));
                EditorGUILayout.PropertyField(p_shouldActivateOnSelect, new GUIContent("Should Activate On Select", "Determines if the Input Field will be activated when selected."));
                EditorGUILayout.PropertyField(p_hideMobileKeyboard, new GUIContent("Hide Soft Keyboard", "Controls the visibility of the mobile virtual keyboard."));

                EditorGUI.BeginDisabledGroup(p_hideMobileKeyboard.boolValue);
                EditorGUILayout.PropertyField(p_hideMobileInput, new GUIContent("Hide Mobile Input", "Controls the visibility of the editable text field above the mobile virtual keyboard."));
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.PropertyField(p_readOnly);
                EditorGUILayout.PropertyField(p_richText);
                EditorGUILayout.PropertyField(p_richTextEditingAllowed, new GUIContent("Allow Rich Text Editing"));

                EditorGUI.indentLevel--;
            }
            #endregion

            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif

    #endregion
}
