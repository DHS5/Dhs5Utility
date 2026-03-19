using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace Dhs5.Utility.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UIDropdown : UISelectable, IPointerClickHandler, ISubmitHandler, ICancelHandler
    {
        #region Option CLASSES

        [Serializable]
        /// <summary>
        /// Class to store the text and/or image of a single option in the dropdown list
        /// </summary>
        public class OptionData
        {
            #region Members

            [SerializeField] protected string m_text;
            [SerializeField] protected OptionAsset m_asset;

            #endregion

            #region Properties

            /// <summary>
            /// The text associated with the option
            /// </summary>
            public virtual string Text 
            { 
                get => m_text; 
                set => m_text = value; 
            }

            /// <summary>
            /// Asset containing the option's extra datas
            /// </summary>
            public virtual OptionAsset Asset
            {
                get => m_asset;
                set => m_asset = value;
            }

            #endregion

            #region Constructors

            public OptionData() { }

            public OptionData(string text)
            {
                this.Text = text;
            }

            public OptionData(OptionAsset asset)
            {
                this.Asset = asset;
            }

            public OptionData(string text, OptionAsset asset)
            {
                this.Text = text;
                this.Asset = asset;
            }

            #endregion
        }

        /// <summary>
        /// Class to store extra data of a single option in the dropdown list
        /// </summary>
        public abstract class OptionAsset : ScriptableObject { }

        #endregion

        #region Fade Tween

        protected class FadeTween : UITransitionTween<float, CanvasGroup>
        {
            public void SetStartAlpha(float startAlpha) => m_startAlpha = startAlpha;

            private float m_startAlpha;

            protected override void OnInit(CanvasGroup graphic, float targetValue) { }

            protected override void Update(CanvasGroup graphic, float normalizedTime, float targetValue)
            {
                graphic.alpha = Mathf.Lerp(m_startAlpha, targetValue, normalizedTime);
            }

            protected override void OnComplete(CanvasGroup graphic, float targetValue)
            {
                graphic.alpha = targetValue;
            }
        }

        #endregion


        #region Members

        protected static readonly OptionData _nothingOption = new OptionData { Text = "Nothing" };
        protected static readonly OptionData _everythingOption = new OptionData { Text = "Everything" };
        protected static readonly OptionData _mixedOption = new OptionData { Text = "Mixed..." };
        protected static readonly OptionData _noOptionData = new OptionData();

        [SerializeField] protected RectTransform m_template;
        [SerializeField] protected TMP_Text m_captionText;
        [SerializeField] protected Graphic m_placeholder;
        [Space]
        [SerializeField] protected int m_value;
        [SerializeField] protected bool m_multiSelect;
        [Space]
        [SerializeField] protected List<OptionData> m_options = new();
        [SerializeField] protected float m_alphaFadeSpeed = 0.15f;

        protected GameObject m_dropdown;
        protected Canvas m_dropdownCanvas;
        protected GameObject m_blocker;
        protected List<UIDropdownItem> m_items = new List<UIDropdownItem>();

        protected FadeTween m_fadeTween = new();
        protected bool m_validTemplate = false;
        protected Coroutine m_coroutine = null;

        #endregion

        #region Properties

        /// <summary>
        /// The Rect Transform of the template for the dropdown list.
        /// </summary>
        public virtual RectTransform Template 
        { 
            get => m_template; 
            set 
            {
                if (m_template != value)
                {
                    m_template = value;
                    RefreshShownValue();
                }
            } 
        }

        /// <summary>
        /// The Text component to hold the text of the currently selected option.
        /// </summary>
        public virtual TMP_Text CaptionText 
        { 
            get => m_captionText;
            set 
            {
                if (m_captionText != value)
                {
                    m_captionText = value;
                    RefreshShownValue();
                }
            } 
        }

        /// <summary>
        /// The placeholder Graphic component. Shown when no option is selected.
        /// </summary>
        public virtual Graphic Placeholder 
        { 
            get => m_placeholder; 
            set 
            {
                if (m_placeholder != value)
                {
                    m_placeholder = value;
                    RefreshShownValue();
                }
            } 
        }

        /// <summary>
        /// The list of possible options. A text string and an image can be specified for each option.
        /// </summary>
        /// <remarks>
        /// This is the list of options within the Dropdown. Each option contains Text and/or image data that you can specify using UI.Dropdown.OptionData before adding to the Dropdown list.
        /// This also unlocks the ability to edit the Dropdown, including the insertion, removal, and finding of options, as well as other useful tools
        /// </remarks>
        public virtual IEnumerable<OptionData> Options => m_options;

        /// <summary>
        /// The time interval at which a drop down will appear and disappear
        /// </summary>
        public virtual float AlphaFadeSpeed 
        { 
            get => m_alphaFadeSpeed;
            set => m_alphaFadeSpeed = value; 
        }

        /// <summary>
        /// The Value is the index number of the current selection in the Dropdown. 0 is the first option in the Dropdown, 1 is the second, and so on.
        /// </summary>
        public virtual int Value
        {
            get => m_value;
            set => SetValue(value);
        }

        public bool MultiSelect 
        { 
            get => m_multiSelect; 
            set => m_multiSelect = value; 
        }

        public bool IsExpanded => m_dropdown != null;

        #endregion

        #region Events

        public event Action<int> ValueChanged;

        protected void TriggerValueChanged()
        {
            UISystemProfilerApi.AddMarker("Dropdown.value", this);
            EventContext = this;
            ValueChanged?.Invoke(m_value);
        }

        #endregion

        #region Core Behaviour

        protected override void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif

            if (m_template)
                m_template.gameObject.SetActive(false);
        }

        protected override void Start()
        {
            base.Start();

            RefreshShownValue();
        }

        protected override void OnDisable()
        {
            //Destroy dropdown and blocker in case user deactivates the dropdown when they click an option (case 935649)
            ImmediateDestroyDropdownList();

            if (m_blocker != null)
                DestroyBlocker(m_blocker);

            m_blocker = null;

            base.OnDisable();
        }

        #endregion


        #region Set Process

        /// <summary>
        /// Set index number of the current selection in the Dropdown without invoking onValueChanged callback.
        /// </summary>
        /// <param name="input">The new index for the current selection.</param>
        public virtual void SetValueWithoutNotify(int input)
        {
            SetValue(input, false);
        }

        protected virtual void SetValue(int value, bool triggerEvent = true)
        {
            if (
#if UNITY_EDITOR
                Application.isPlaying &&
#endif
                (value == m_value || m_options.Count == 0))
                return;

            if (m_multiSelect)
                m_value = value;
            else
                m_value = Mathf.Clamp(value, m_placeholder ? -1 : 0, m_options.Count - 1);

            RefreshShownValue();

            if (triggerEvent)
            {
                // Notify all listeners
                TriggerValueChanged();
            }
        }

        #endregion

        #region Options

        /// <summary>
        /// Add multiple options to the options of the Dropdown based on a list of OptionData objects.
        /// </summary>
        /// <param name="options">The list of OptionData to add.</param>
        /// /// <remarks>
        /// See AddOptions(List<string> options) for code example of usages.
        /// </remarks>
        public virtual void AddOptions(IEnumerable<OptionData> options)
        {
            m_options.AddRange(options);
            RefreshShownValue();
        }

        public virtual void AddOption(OptionData option)
        {
            m_options.Add(option);
            RefreshShownValue();
        }

        /// <summary>
        /// Add multiple text-only options to the options of the Dropdown based on a list of strings.
        /// </summary>
        /// <remarks>
        /// Add a List of string messages to the Dropdown. The Dropdown shows each member of the list as a separate option.
        /// </remarks>
        /// <param name="options">The list of text strings to add.</param>
        public virtual void AddOptions(List<string> options)
        {
            for (int i = 0; i < options.Count; i++)
                m_options.Add(new OptionData(options[i]));

            RefreshShownValue();
        }

        public virtual void AddOption(string option)
        {
            m_options.Add(new OptionData(option));
            RefreshShownValue();
        }

        /// <summary>
        /// Clear the list of options in the Dropdown.
        /// </summary>
        public virtual void ClearOptions()
        {
            m_options.Clear();
            m_value = m_placeholder ? -1 : 0;
            RefreshShownValue();
        }

        #endregion

        #region IEventHandlers

        /// <summary>
        /// Handling for when the dropdown is initially 'clicked'. Typically shows the dropdown
        /// </summary>
        /// <param name="eventData">The associated event data.</param>
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            Show();
        }

        /// <summary>
        /// Handling for when the dropdown is selected and a submit event is processed. Typically shows the dropdown
        /// </summary>
        /// <param name="eventData">The associated event data.</param>
        public virtual void OnSubmit(BaseEventData eventData)
        {
            Show();
        }

        /// <summary>
        /// This will hide the dropdown list.
        /// </summary>
        /// <remarks>
        /// Called by a BaseInputModule when a Cancel event occurs.
        /// </remarks>
        /// <param name="eventData">The associated event data.</param>
        public virtual void OnCancel(BaseEventData eventData)
        {
            Hide();
        }

        #endregion

        #region Actions

        /// <summary>
        /// Show the dropdown.
        ///
        /// Plan for dropdown scrolling to ensure dropdown is contained within screen.
        ///
        /// We assume the Canvas is the screen that the dropdown must be kept inside.
        /// This is always valid for screen space canvas modes.
        /// For world space canvases we don't know how it's used, but it could be e.g. for an in-game monitor.
        /// We consider it a fair constraint that the canvas must be big enough to contain dropdowns.
        /// </summary>
        public virtual void Show()
        {
            if (m_coroutine != null)
            {
                StopCoroutine(m_coroutine);
                ImmediateDestroyDropdownList();
            }

            if (!IsActive() || !IsInteractable() || m_dropdown != null)
                return;

            var rootCanvas = FindRootCanvas();
            if (rootCanvas == null) return;

            var parentCanvas = FindParentCanvas();

            if (!m_validTemplate)
            {
                SetupTemplate(rootCanvas, parentCanvas);
                if (!m_validTemplate)
                    return;
            }

            m_template.gameObject.SetActive(true);

            // Instantiate the drop-down template
            m_dropdown = CreateDropdownList(m_template.gameObject);
            m_dropdown.name = "Dropdown List";
            m_dropdown.SetActive(true);

            // Make drop-down RectTransform have same values as original.
            RectTransform dropdownRectTransform = m_dropdown.transform as RectTransform;
            dropdownRectTransform.SetParent(m_template.transform.parent, false);

            // Instantiate the drop-down list items

            // Find the dropdown item and disable it.
            UIDropdownItem itemTemplate = m_dropdown.GetComponentInChildren<UIDropdownItem>();

            GameObject content = itemTemplate.RectTransform.parent.gameObject;
            RectTransform contentRectTransform = content.transform as RectTransform;
            itemTemplate.RectTransform.gameObject.SetActive(true);

            // Get the rects of the dropdown and item
            Rect dropdownContentRect = contentRectTransform.rect;
            Rect itemTemplateRect = itemTemplate.RectTransform.rect;

            // Calculate the visual offset between the item's edges and the background's edges
            Vector2 offsetMin = itemTemplateRect.min - dropdownContentRect.min + (Vector2)itemTemplate.RectTransform.localPosition;
            Vector2 offsetMax = itemTemplateRect.max - dropdownContentRect.max + (Vector2)itemTemplate.RectTransform.localPosition;
            Vector2 itemSize = itemTemplateRect.size;

            // Create items
            CreateItems(itemTemplate);

            // Setup dropdown position and size
            SetupDropdownRect(rootCanvas, dropdownRectTransform, contentRectTransform, offsetMin, offsetMax, itemSize);

            // Fade in the popup
            AlphaFadeList(m_alphaFadeSpeed, 0f, 1f);

            // Make drop-down template and item template inactive
            m_template.gameObject.SetActive(false);
            itemTemplate.gameObject.SetActive(false);

            m_blocker = CreateBlocker(rootCanvas, parentCanvas);
        }

        /// <summary>
        /// Hide the dropdown list. I.e. close it.
        /// </summary>
        public virtual void Hide()
        {
            if (m_coroutine == null)
            {
                if (m_dropdown != null)
                {
                    AlphaFadeList(m_alphaFadeSpeed, 0f);

                    // User could have disabled the dropdown during the OnValueChanged call.
                    if (IsActive())
                        m_coroutine = StartCoroutine(DelayedDestroyDropdownList(m_alphaFadeSpeed));
                }

                if (m_blocker != null)
                    DestroyBlocker(m_blocker);

                m_blocker = null;
                Select();
            }
        }

        #endregion


        #region Update Visuals

        /// <summary>
        /// Refreshes the text and image (if available) of the currently selected option.
        /// </summary>
        /// <remarks>
        /// If you have modified the list of options, you should call this method afterwards to ensure that the visual state of the dropdown corresponds to the updated options.
        /// </remarks>
        public virtual void RefreshShownValue()
        {
            OptionData data = _noOptionData;

            if (m_options.Count > 0)
            {
                if (m_multiSelect)
                {
                    int firstActiveFlag = FirstActiveFlagIndex(m_value);
                    if (m_value == 0 || firstActiveFlag >= m_options.Count)
                        data = _nothingOption;
                    else if (IsEverythingValue(m_options.Count, m_value))
                        data = _everythingOption;
                    else if (Mathf.IsPowerOfTwo(m_value) && m_value > 0)
                        data = m_options[firstActiveFlag];
                    else
                        data = _mixedOption;
                }
                else if (m_value >= 0)
                {
                    data = m_options[Mathf.Clamp(m_value, 0, m_options.Count - 1)];
                }
            }

            UpdateCaption(data);
            UpdatePlaceholder();
        }
        
        protected virtual void UpdateCaption(OptionData data)
        {
            if (m_captionText)
            {
                if (data != null && data.Text != null)
                    m_captionText.text = data.Text;
                else
                    m_captionText.text = "";
            }
        }
        protected virtual void UpdatePlaceholder()
        {
            if (m_placeholder)
            {
                m_placeholder.enabled = m_options.Count == 0 || m_value == -1;
            }
        }

        #endregion

        #region Template

        protected virtual void SetupTemplate(Canvas rootCanvas, Canvas parentCanvas)
        {
            m_validTemplate = false;

            if (!m_template)
            {
                Debug.LogError("The dropdown template is not assigned. The template needs to be assigned and must have a child GameObject with a Toggle component serving as the item.", this);
                return;
            }

            GameObject templateGo = m_template.gameObject;
            templateGo.SetActive(true);

            if (!m_validTemplate)
            {
                templateGo.SetActive(false);
                return;
            }

            m_dropdownCanvas = SetupTemplateCanvas(templateGo, rootCanvas, parentCanvas);

            GetOrAddComponent<CanvasGroup>(templateGo);
            templateGo.SetActive(false);

            m_validTemplate = true;
        }

        protected virtual Canvas SetupTemplateCanvas(GameObject templateObject, Canvas rootCanvas, Canvas parentCanvas)
        {
            Canvas popupCanvas = GetOrAddComponent<Canvas>(templateObject);
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 30000;
            popupCanvas.sortingLayerID = rootCanvas.sortingLayerID;

            // If we have a parent canvas, apply the same raycasters as the parent for consistency.
            if (parentCanvas != null)
            {
                ApplyCanvasRaycasters(parentCanvas, popupCanvas);
            }
            else
            {
                GetOrAddComponent<GraphicRaycaster>(templateObject);
            }

            return popupCanvas;
        }

        #endregion

        #region Blocker

        /// <summary>
        /// Create a blocker that blocks clicks to other controls while the dropdown list is open.
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to obtain a blocker GameObject.
        /// </remarks>
        /// <param name="rootCanvas">The root canvas the dropdown is under.</param>
        /// <returns>The created blocker object</returns>
        protected virtual GameObject CreateBlocker(Canvas rootCanvas, Canvas parentCanvas)
        {
            // Create blocker GameObject.
            GameObject blocker = new GameObject("Blocker");

            // Set the game object layer to match the Canvas' game object layer, as not doing this can lead to issues
            // especially in XR applications like PolySpatial on VisionOS (UUM-62470).
            blocker.layer = rootCanvas.gameObject.layer;

            // Setup blocker RectTransform to cover entire root canvas area.
            RectTransform blockerRect = blocker.AddComponent<RectTransform>();
            blockerRect.SetParent(rootCanvas.transform, false);
            blockerRect.anchorMin = Vector3.zero;
            blockerRect.anchorMax = Vector3.one;
            blockerRect.sizeDelta = Vector2.zero;

            // Make blocker be in separate canvas in same layer as dropdown and in layer just below it.
            Canvas blockerCanvas = blocker.AddComponent<Canvas>();
            blockerCanvas.overrideSorting = true;
            blockerCanvas.sortingLayerID = m_dropdownCanvas.sortingLayerID;
            blockerCanvas.sortingOrder = m_dropdownCanvas.sortingOrder - 1;

            // If we have a parent canvas, apply the same raycasters as the parent for consistency.
            if (parentCanvas != null)
            {
                ApplyCanvasRaycasters(parentCanvas, blockerCanvas);
            }
            else
            {
                // Add raycaster since it's needed to block.
                GetOrAddComponent<GraphicRaycaster>(blocker);
            }

            SetupBlocker(blocker);

            return blocker;
        }
        protected virtual void SetupBlocker(GameObject blocker)
        {
            // Add image since it's needed to block, but make it clear.
            Image blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = Color.clear;

            // Add button since it's needed to block, and to close the dropdown when blocking area is clicked.
            Button blockerButton = blocker.AddComponent<Button>();
            blockerButton.onClick.AddListener(Hide);

            //add canvas group to ensure clicking outside the dropdown will hide it (UUM-33691)
            CanvasGroup blockerCanvasGroup = blocker.AddComponent<CanvasGroup>();
            blockerCanvasGroup.ignoreParentGroups = true;
        }

        /// <summary>
        /// Convenience method to explicitly destroy the previously generated blocker object
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to dispose of a blocker GameObject that blocks clicks to other controls while the dropdown list is open.
        /// </remarks>
        /// <param name="blocker">The blocker object to destroy.</param>
        protected virtual void DestroyBlocker(GameObject blocker)
        {
            Destroy(blocker);
        }

        #endregion

        #region Dropdown List

        /// <summary>
        /// Create the dropdown list to be shown when the dropdown is clicked. The dropdown list should correspond to the provided template GameObject, equivalent to instantiating a copy of it.
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to obtain a dropdown list GameObject.
        /// </remarks>
        /// <param name="template">The template to create the dropdown list from.</param>
        /// <returns>The created drop down list gameobject.</returns>
        protected virtual GameObject CreateDropdownList(GameObject template)
        {
            return (GameObject)Instantiate(template);
        }

        protected virtual void SetupDropdownRect(Canvas rootCanvas, RectTransform dropdownRectTransform, RectTransform contentRectTransform,
            Vector2 offsetMin, Vector2 offsetMax, Vector2 itemSize)
        {
            // Reposition all items now that all of them have been added
            Vector2 sizeDelta = contentRectTransform.sizeDelta;
            sizeDelta.y = itemSize.y * m_items.Count + offsetMin.y - offsetMax.y;
            contentRectTransform.sizeDelta = sizeDelta;

            float extraSpace = dropdownRectTransform.rect.height - contentRectTransform.rect.height;
            if (extraSpace > 0)
                dropdownRectTransform.sizeDelta = new Vector2(dropdownRectTransform.sizeDelta.x, dropdownRectTransform.sizeDelta.y - extraSpace);

            // Invert anchoring and position if dropdown is partially or fully outside of canvas rect.
            // Typically this will have the effect of placing the dropdown above the button instead of below,
            // but it works as inversion regardless of initial setup.
            Vector3[] corners = new Vector3[4];
            dropdownRectTransform.GetWorldCorners(corners);

            RectTransform rootCanvasRectTransform = rootCanvas.transform as RectTransform;
            Rect rootCanvasRect = rootCanvasRectTransform.rect;
            for (int axis = 0; axis < 2; axis++)
            {
                bool outside = false;
                for (int i = 0; i < 4; i++)
                {
                    Vector3 corner = rootCanvasRectTransform.InverseTransformPoint(corners[i]);
                    if ((corner[axis] < rootCanvasRect.min[axis] && !Mathf.Approximately(corner[axis], rootCanvasRect.min[axis])) ||
                        (corner[axis] > rootCanvasRect.max[axis] && !Mathf.Approximately(corner[axis], rootCanvasRect.max[axis])))
                    {
                        outside = true;
                        break;
                    }
                }
                if (outside)
                    RectTransformUtility.FlipLayoutOnAxis(dropdownRectTransform, axis, false, false);
            }

            for (int i = 0; i < m_items.Count; i++)
            {
                RectTransform itemRect = m_items[i].RectTransform;
                itemRect.anchorMin = new Vector2(itemRect.anchorMin.x, 0);
                itemRect.anchorMax = new Vector2(itemRect.anchorMax.x, 0);
                itemRect.anchoredPosition = new Vector2(itemRect.anchoredPosition.x, offsetMin.y + itemSize.y * (m_items.Count - 1 - i) + itemSize.y * itemRect.pivot.y);
                itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, itemSize.y);
            }
        }

        /// <summary>
        /// Convenience method to explicitly destroy the previously generated dropdown list
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to dispose of a dropdown list GameObject.
        /// </remarks>
        /// <param name="dropdownList">The dropdown list GameObject to destroy</param>
        protected virtual void DestroyDropdownList(GameObject dropdownList)
        {
            Destroy(dropdownList);
        }

        protected virtual IEnumerator DelayedDestroyDropdownList(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            ImmediateDestroyDropdownList();
        }

        protected virtual void ImmediateDestroyDropdownList()
        {
            for (int i = 0; i < m_items.Count; i++)
            {
                if (m_items[i] != null)
                    DestroyItem(m_items[i]);
            }

            m_items.Clear();

            if (m_dropdown != null)
                DestroyDropdownList(m_dropdown);

            if (m_fadeTween != null)
                m_fadeTween.Stop();

            m_dropdown = null;
            m_dropdownCanvas = null;
            m_coroutine = null;
        }

        #endregion

        #region Items

        protected virtual void CreateItems(UIDropdownItem itemTemplate)
        {
            m_items.Clear();

            UIDropdownItem prev = null;
            if (m_multiSelect && m_options.Count > 0)
            {
                UIDropdownItem item = AddItem(0, _nothingOption, Value == 0, itemTemplate, m_items);

                item.IsOn = Value == 0;
                item.Pressed += OnPressedItem;
                prev = item;

                bool isEverythingValue = IsEverythingValue(m_options.Count, Value);
                item = AddItem(1, _everythingOption, isEverythingValue, itemTemplate, m_items);

                item.IsOn = isEverythingValue;
                item.Pressed += OnPressedItem;

                // Automatically set up explicit navigation
                prev.SetupNextNavigation(item);
                item.SetupPrevNavigation(prev);
                prev = item;
            }

            for (int i = m_multiSelect ? 2 : 0; i < m_options.Count; ++i)
            {
                OptionData data = m_options[i];
                UIDropdownItem item = AddItem(i, data, Value == i, itemTemplate, m_items);
                if (item == null)
                    continue;

                // Automatically set up a toggle state change listener
                if (m_multiSelect)
                    item.IsOn = (Value & (1 << i)) != 0;
                else
                    item.IsOn = Value == i;

                item.Pressed += OnPressedItem;

                // Select current option
                if (item.IsOn)
                    item.SelectAsFirst();

                // Automatically set up explicit navigation
                if (prev != null) prev.SetupNextNavigation(item);
                item.SetupPrevNavigation(prev);
                prev = item;
            }
        }

        /// <summary>
        /// Create a dropdown item based upon the item template.
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to obtain an option item.
        /// The option item should correspond to the provided template DropdownItem and its GameObject, equivalent to instantiating a copy of it.
        /// </remarks>
        /// <param name="itemTemplate">e template to create the option item from.</param>
        /// <returns>The created dropdown item component</returns>
        protected virtual UIDropdownItem CreateItem(UIDropdownItem itemTemplate)
        {
            return (UIDropdownItem)Instantiate(itemTemplate);
        }

        /// <summary>
        ///  Convenience method to explicitly destroy the previously generated Items.
        /// </summary>
        /// <remarks>
        /// Override this method to implement a different way to dispose of an option item.
        /// Likely no action needed since destroying the dropdown list destroys all contained items as well.
        /// </remarks>
        /// <param name="item">The Item to destroy.</param>
        protected virtual void DestroyItem(UIDropdownItem item)
        {
            item.Selected -= OnSelectedItem;
            item.Cancelled -= OnCancelledItem;
        }

        // Add a new drop-down list item with the specified values.
        protected virtual UIDropdownItem AddItem(int index, OptionData data, bool selected, UIDropdownItem itemTemplate, List<UIDropdownItem> items)
        {
            // Add a new item to the dropdown.
            UIDropdownItem item = CreateItem(itemTemplate);
            item.RectTransform.SetParent(itemTemplate.RectTransform.parent, false);

            item.gameObject.SetActive(true);
            item.gameObject.name = "Item " + items.Count + (data.Text != null ? ": " + data.Text : "");

            item.ApplyData(index, data);
            item.IsOn = false;

            item.Selected += OnSelectedItem;
            item.Cancelled += OnCancelledItem;

            items.Add(item);
            return item;
        }

        #endregion


        #region Callbacks

        // Change the value and hide the dropdown.
        protected virtual void OnPressedItem(int index)
        {
            if (!m_items.IsIndexValid(index, out var item))
                return;

            if (m_multiSelect)
            {
                switch (index)
                {
                    case 0: // Nothing
                        Value = 0;
                        foreach (var i in m_items)
                        {
                            i.IsOn = i == item;
                        }
                        break;
                    case 1: // Everything
                        Value = EverythingValue(m_options.Count);
                        for (var i = 0; i < m_items.Count; i++)
                        {
                            m_items[i].IsOn = i != 0;
                        }
                        break;
                    default:
                        var flagValue = 1 << index;
                        var wasSelected = (Value & flagValue) != 0;
                        item.IsOn = !wasSelected;

                        if (wasSelected)
                            Value &= ~flagValue;
                        else
                            Value |= flagValue;

                        break;
                }
            }
            else
            {
                item.IsOn = true;
                Value = index;
            }

            Hide();
        }

        protected virtual void OnSelectedItem(int index) { }
        protected virtual void OnCancelledItem(int index) { }

        #endregion


        #region Utility

        protected virtual Canvas FindRootCanvas()
        {
            // Get root Canvas.
            List<Canvas> list = UIListPool<Canvas>.Get();
            gameObject.GetComponentsInParent(false, list);
            if (list.Count == 0)
                return null;

            Canvas rootCanvas = list[list.Count - 1];
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].isRootCanvas)
                {
                    rootCanvas = list[i];
                    break;
                }
            }

            UIListPool<Canvas>.Release(list);

            return rootCanvas;
        }
        protected virtual Canvas FindParentCanvas()
        {
            // Find the Canvas that this dropdown is a part of
            Canvas parentCanvas = null;
            Transform parentTransform = m_template.parent;
            while (parentTransform != null)
            {
                parentCanvas = parentTransform.GetComponent<Canvas>();
                if (parentCanvas != null)
                    break;

                parentTransform = parentTransform.parent;
            }

            return parentCanvas;
        }
        protected virtual void ApplyCanvasRaycasters(Canvas model, Canvas target)
        {
            Component[] components = model.GetComponents<BaseRaycaster>();
            for (int i = 0; i < components.Length; i++)
            {
                Type raycasterType = components[i].GetType();
                if (target.gameObject.GetComponent(raycasterType) == null)
                {
                    target.gameObject.AddComponent(raycasterType);
                }
            }
        }

        #endregion

        #region Fading & Alpha

        protected virtual void AlphaFadeList(float duration, float alpha)
        {
            CanvasGroup group = m_dropdown.GetComponent<CanvasGroup>();
            AlphaFadeList(duration, group.alpha, alpha);
        }

        protected virtual void AlphaFadeList(float duration, float start, float end)
        {
            if (end.Equals(start))
                return;

            m_fadeTween.SetStartAlpha(start);
            m_fadeTween.Start(this, m_dropdown.GetComponent<CanvasGroup>(), duration, end);
        }

        #endregion


        #region Editor

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();

            if (!IsActive())
                return;

            RefreshShownValue();
        }

#endif

        #endregion

        // --- STATIC ---

        #region Utility

        protected static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (!comp)
                comp = go.AddComponent<T>();
            return comp;
        }

        protected static bool IsEverythingValue(int count, int value)
        {
            var result = true;
            for (var i = 0; i < count; i++)
            {
                if ((value & 1 << i) == 0)
                    result = false;
            }

            return result;
        }

        protected static int EverythingValue(int count)
        {
            int result = 0;
            for (var i = 0; i < count; i++)
            {
                result |= 1 << i;
            }

            return result;
        }

        protected static int FirstActiveFlagIndex(int value)
        {
            if (value == 0)
                return 0;

            const int bits = sizeof(int) * 8;
            for (var i = 0; i < bits; i++)
                if ((value & 1 << i) != 0)
                    return i;

            return 0;
        }

        #endregion
    }
}
