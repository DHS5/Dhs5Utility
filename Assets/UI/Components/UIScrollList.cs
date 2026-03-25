using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public class UIScrollList : UISelectable, 
        IScrollHandler, IInitializePotentialDragHandler, IDragHandler
    {
        #region CLASS OptionData

        [Serializable]
        /// <summary>
        /// Class to store the text and/or image of a single option in the scroll list
        /// </summary>
        public class OptionData
        {
            #region Members

            [SerializeField] protected string m_text;
            [SerializeField] protected UIComponentOptionAsset m_asset;

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
            public virtual UIComponentOptionAsset Asset
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

            public OptionData(UIComponentOptionAsset asset)
            {
                this.Asset = asset;
            }

            public OptionData(string text, UIComponentOptionAsset asset)
            {
                this.Text = text;
                this.Asset = asset;
            }

            #endregion
        }

        #endregion

        #region ENUM Direction

        public enum EDirection
        {
            /// <summary>
            /// From the left to the right
            /// </summary>
            LeftToRight,

            /// <summary>
            /// From the right to the left
            /// </summary>
            RightToLeft,

            /// <summary>
            /// From the bottom to the top.
            /// </summary>
            BottomToTop,

            /// <summary>
            /// From the top to the bottom.
            /// </summary>
            TopToBottom,
        }

        #endregion


        #region Members

        [Header("Scroll List")]
        [SerializeField] protected RectTransform m_viewportRect;
        [SerializeField] protected UIScrollListItem m_templateItem;
        [SerializeField] protected Graphic m_placeholder;
        [SerializeField] protected UIButton m_leftButton;
        [SerializeField] protected UIButton m_rightButton;
        [Space]
        [SerializeField] protected int m_value = 0;
        [SerializeField] protected List<OptionData> m_options;
        [Space]
        [SerializeField] protected EDirection m_direction = EDirection.LeftToRight;
        [SerializeField] protected bool m_wrapAround = true;
        [SerializeField, Min(0f)] protected float m_scrollDuration = 0.5f;
        [SerializeField] protected bool m_canDrag = true;
        [SerializeField, Min(0f)] protected float m_itemsSpacing = 20f;

        protected List<UIScrollListItem> m_items = new();
        [SerializeField, ReadOnly] protected int m_mainItemIndex;
        protected float m_visibleLimit;
        protected int m_directionMultiplier;

        // field is never assigned warning
#pragma warning disable 649
        protected DrivenRectTransformTracker m_tracker;
#pragma warning restore 649

        #endregion

        #region Properties

        public virtual RectTransform ViewportRect
        {
            get => m_viewportRect;
            set
            {
                if (SetPropertyUtility.SetClass(ref m_viewportRect, value))
                {
                    RefreshItemsSetup();
                    RefreshShownValues();
                }
            }
        }
        public virtual UIScrollListItem TemplateItem
        {
            get => m_templateItem;
            set
            {
                if (SetPropertyUtility.SetClass(ref m_templateItem, value))
                {
                    RefreshItemsSetup();
                    RefreshShownValues();
                }
            }
        }
        public virtual Graphic Placeholder
        {
            get => m_placeholder;
            set
            {
                if (SetPropertyUtility.SetClass(ref m_placeholder, value))
                {
                    RefreshShownValues();
                }
            }
        }
        public virtual UIButton LeftButton
        {
            get => m_leftButton;
            set
            {
                if (
#if UNITY_EDITOR
                    Application.isPlaying &&
#endif
                    m_leftButton != null)
                    m_leftButton.Clicked -= OnLeftButtonClicked;

                m_leftButton = value;

                if (
#if UNITY_EDITOR
                    Application.isPlaying &&
#endif
                    m_leftButton != null)
                    m_leftButton.Clicked += OnLeftButtonClicked;
            }
        }
        public virtual UIButton RightButton
        {
            get => m_rightButton;
            set
            {
                if (
#if UNITY_EDITOR
                    Application.isPlaying &&
#endif
                    m_rightButton != null)
                    m_rightButton.Clicked -= OnRightButtonClicked;

                m_leftButton = value;

                if (
#if UNITY_EDITOR
                    Application.isPlaying &&
#endif
                    m_rightButton != null)
                    m_rightButton.Clicked += OnRightButtonClicked;
            }
        }

        public virtual int Value
        {
            get => m_value;
            set => Set(value);
        }
        public virtual int OptionsCount => m_options.Count;

        public virtual EDirection Direction
        {
            get => m_direction;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_direction, value))
                {
                    // Update direction
                    //...
                    RefreshItemsSetup();
                    RefreshShownValues();
                }
            }
        }
        public virtual bool WrapAround
        {
            get => m_wrapAround;
            set => m_wrapAround = value;
        }
        public virtual float ScrollDuration
        {
            get => m_scrollDuration;
            set => m_scrollDuration = value;
        }
        public virtual bool CanDrag
        {
            get => m_canDrag;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_canDrag, value))
                {
                    RefreshItemsSetup();
                    RefreshShownValues();
                }
            }
        }
        public virtual float ItemsSpacing
        {
            get => m_itemsSpacing;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_itemsSpacing, value))
                {
                    RefreshItemsSetup();
                    RefreshShownValues();
                }
            }
        }

        #endregion

        #region Events

        public event Action<int> ValueChanged;
        protected void TriggerValueChanged()
        {
            UISystemProfilerApi.AddMarker("ScrollList.value", this);
            EventContext = this;
            ValueChanged?.Invoke(m_value);
        }

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            if (LeftButton != null) LeftButton.Clicked += OnLeftButtonClicked;
            if (RightButton != null) RightButton.Clicked += OnRightButtonClicked;

            RefreshItemsSetup();
            RefreshShownValues();
        }
        protected override void OnDisable()
        {
            base.OnDisable();

            if (LeftButton != null) LeftButton.Clicked -= OnLeftButtonClicked;
            if (RightButton != null) RightButton.Clicked -= OnRightButtonClicked;

            m_tracker.Clear();
        }

        #endregion


        #region Setters

        protected virtual void Set(int value, bool triggerEvent = true)
        {
            value = Mathf.Clamp(value, m_placeholder ? -1 : 0, OptionsCount);
            if (Value == value) return;

            m_value = value;
            RefreshShownValues();

            if (triggerEvent)
            {
                TriggerValueChanged();
            }
        }
        public virtual void SetValueWithoutNotify(int value) => Set(value, triggerEvent:false);

        protected virtual void SetNext()
        {
            Set(GetNextIndex(Value, OptionsCount, WrapAround));
        }
        protected virtual void SetPrevious()
        {
            Set(GetPreviousIndex(Value, OptionsCount, WrapAround));
        }

        #endregion

        #region Visuals Update

        public virtual void RefreshShownValues()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying
                && TemplateItem != null
                && OptionsCount > 0)
            {
                TemplateItem.ApplyData(m_options[GetIndex(Value, OptionsCount, WrapAround)]);
                return;
            }
#endif

            var halfCount = m_items.Count / 2;
            for (int i = -halfCount; i < halfCount + 1; i++)
            {
                var itemIndex = GetIndex(i * m_directionMultiplier + m_mainItemIndex, m_items.Count, true);
                var item = m_items[itemIndex];

                if (item != null)
                {
                    var optionIndex = GetIndex(i + Value, OptionsCount, WrapAround);
                    var option = m_options[optionIndex];
                    item.ApplyData(option);
                }
            }
        }

        #endregion

        #region Items

        protected virtual void RefreshItemsSetup()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            if (TemplateItem == null || ViewportRect == null) return;

            // Clear
            m_tracker.Clear();
            ClearCurrentItems();

            TemplateItem.gameObject.SetActive(true);

            // Get viewport & template infos
            var viewportSize = ViewportRect.rect.size;
            var itemSize = TemplateItem.RectTransform.rect.size;
            float viewportLength;
            float itemsLength;
            Vector2 itemsVOffset;
            m_directionMultiplier = (m_direction is EDirection.LeftToRight or EDirection.BottomToTop) ? 1 : -1;
            switch (Direction)
            {
                case EDirection.LeftToRight:
                case EDirection.RightToLeft:
                    viewportLength = viewportSize.x;
                    itemsLength = itemSize.x;
                    itemsVOffset = new Vector2(itemsLength + ItemsSpacing, 0f);
                    break;
                
                default:
                    viewportLength = viewportSize.y;
                    itemsLength = itemSize.y;
                    itemsVOffset = new Vector2(0f, itemsLength + ItemsSpacing);
                    break;
            }
            var itemsOffset = itemsLength + ItemsSpacing;
            m_visibleLimit = (viewportLength + itemsLength) / 2f;

            // Compute items count
            var itemsCount = (!CanDrag && ScrollDuration <= 0f ? 1 : 3) + Mathf.FloorToInt(viewportLength / (2f * itemsOffset)) * 2;

            // Instantiate items
            InstantiateItems(itemsCount, itemsVOffset);

            TemplateItem.gameObject.SetActive(false);
        }

        protected virtual void InstantiateItems(int count, Vector2 offset)
        {
            var halfCount = count / 2;
            for (int i = 0; i < count; i++)
            {
                var item = InstantiateItem(i);
                m_items.Add(item);

                m_tracker.Add(this, item.RectTransform, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Pivot);
                item.RectTransform.anchorMin = item.RectTransform.anchorMax = item.RectTransform.pivot = new Vector2(0.5f, 0.5f);
                item.RectTransform.anchoredPosition = offset * (i - halfCount);
            }

            m_mainItemIndex = halfCount;
        }
        protected virtual UIScrollListItem InstantiateItem(int index)
        {
            var result = Instantiate(TemplateItem, ViewportRect);
            result.name = "Item " + index;
            return result;
        }

        protected virtual void ClearCurrentItems()
        {
            foreach (var item in m_items)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
            m_items.Clear();
        }

        #endregion

        #region Options

        /// <summary>
        /// Add multiple options to the options of the ScrollList based on a list of OptionData objects
        /// </summary>
        /// <param name="options">The list of OptionData to add</param>
        public virtual void AddOptions(IEnumerable<OptionData> options)
        {
            m_options.AddRange(options);
            RefreshShownValues();
        }

        public virtual void AddOption(OptionData option)
        {
            m_options.Add(option);
            RefreshShownValues();
        }

        /// <summary>
        /// Add multiple text-only options to the options of the ScrollList based on a list of strings
        /// </summary>
        /// <param name="options">The list of text strings to add</param>
        public virtual void AddOptions(List<string> options)
        {
            for (int i = 0; i < options.Count; i++)
                m_options.Add(new OptionData(options[i]));

            RefreshShownValues();
        }

        public virtual void AddOption(string option)
        {
            m_options.Add(new OptionData(option));
            RefreshShownValues();
        }

        /// <summary>
        /// Clear the list of options in the ScrollList
        /// </summary>
        public virtual void ClearOptions()
        {
            m_options.Clear();
            m_value = m_placeholder ? -1 : 0;
            RefreshShownValues();
        }

        #endregion


        #region Callbacks

        protected virtual void OnLeftButtonClicked()
        {
            SetPrevious();
        }
        protected virtual void OnRightButtonClicked()
        {
            SetNext();
        }

        #endregion


        #region Tweening

        #endregion

        #region Drag

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(ViewportRect, eventData.position))
            {
                eventData.pointerDrag = null;
            }
        }
        public virtual void OnDrag(PointerEventData eventData)
        {
            
        }

        #endregion

        #region Scroll

        public virtual void OnScroll(PointerEventData eventData)
        {
            
        }

        #endregion

        #region Move

        #endregion


        #region Interactability

        protected override void OnBecameInteractable()
        {
            base.OnBecameInteractable();

            EnsureInteractibility();
        }
        protected override void OnBecameUninteractable()
        {
            base.OnBecameUninteractable();

            EnsureInteractibility();
        }

        protected virtual void EnsureInteractibility()
        {
            if (LeftButton != null) LeftButton.interactable = interactable;
            if (RightButton != null) RightButton.interactable = interactable;
        }

        #endregion


        #region Editor

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();

            if (!Application.isPlaying)
                RefreshShownValues();
        }

#endif

        #endregion


        // --- STATIC ---

        #region Index Utility

        protected static int GetIndex(int index, int count, bool wrapAround)
        {
            if (count <= 0)
            {
                throw new Exception("Invalid count " + count);
            }

            while (index < 0)
            {
                if (wrapAround)
                {
                    index += count;
                }
                else return 0;
            }
            while (index >= count)
            {
                if (wrapAround)
                {
                    index -= count;
                }
                else return count - 1;
            }
            return index;
        }
        protected static int GetPreviousIndex(int index, int count, bool wrapAround)
        {
            return GetIndex(index - 1, count, wrapAround);
        }
        protected static int GetNextIndex(int index, int count, bool wrapAround)
        {
            return GetIndex(index + 1, count, wrapAround);
        }

        #endregion
    }
}
