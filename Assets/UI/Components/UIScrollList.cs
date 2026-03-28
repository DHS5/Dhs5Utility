using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UIScrollList : UISelectable, 
        IScrollHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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
        [SerializeField] protected UIButton m_leftButton;
        [SerializeField] protected UIButton m_rightButton;
        [Space]
        [SerializeField] protected int m_value = 0;
        [SerializeField] protected List<OptionData> m_options = new();
        [SerializeField] protected EnabledValue<OptionData> m_minusOneOption = new();
        [Space]
        [SerializeField] protected EDirection m_direction = EDirection.LeftToRight;
        [SerializeField] protected bool m_wrapAround = true;
        [SerializeField] protected float m_itemsSpacing = 20f;
        [SerializeField, Min(0f)] protected float m_scrollDuration = 0.25f;
        [SerializeField] protected bool m_animateOnClick = true;
        [SerializeField] protected bool m_canDrag = true;
        [SerializeField] protected bool m_confineDragToViewport = false;

        protected List<UIScrollListItem> m_items = new();
        protected int m_mainItemIndex;
        protected float m_dragValueChangeThreshold;
        protected float m_itemsDistance;
        protected int m_directionMultiplier;
        protected float m_lastDragDelta;

        protected Coroutine m_moveCoroutine;

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

                m_rightButton = value;

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
        public virtual int OptionsCount => m_options?.Count ?? 0;
        public virtual EnabledValue<OptionData> MinusOneOption
        {
            get => m_minusOneOption;
            set
            {
                if (SetPropertyUtility.SetClass(ref m_minusOneOption, value))
                {
                    RefreshItemsSetup();
                    RefreshShownValues();
                }
            }
        }

        public virtual EDirection Direction
        {
            get => m_direction;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_direction, value))
                {
                    RefreshItemsSetup();
                    RefreshShownValues();
                }
            }
        }
        public virtual bool WrapAround
        {
            get => m_wrapAround;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_wrapAround, value))
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
        public virtual float ScrollDuration
        {
            get => m_scrollDuration;
            set
            {
                var needRefresh = (m_scrollDuration <= 0f) != (value <= 0f);
                if (SetPropertyUtility.SetStruct(ref m_scrollDuration, Mathf.Max(0f, value))
                    && needRefresh)
                {
                    RefreshItemsSetup();
                    RefreshShownValues();
                }
            }
        }
        public virtual bool AnimateOnClick
        {
            get => m_animateOnClick;
            set => m_animateOnClick = value;
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
        public virtual bool ConfineDragToViewport
        {
            get => m_confineDragToViewport;
            set => m_confineDragToViewport = value;
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
            Set(Value, triggerEvent: false, refreshShownValues: true, force: true);
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

        protected virtual bool Set(int value, bool triggerEvent = true, bool refreshShownValues = true, bool force = false)
        {
            value = Mathf.Clamp(value, MinusOneOption.IsEnabled(out _) ? -1 : 0, OptionsCount);
            if (!force && Value == value) return false;

            m_value = value;

            if (refreshShownValues)
            {
                RefreshShownValues();
            }

            if (triggerEvent)
            {
                TriggerValueChanged();
            }
            return true;
        }

        protected virtual bool SetNext(bool triggerEvent = true, bool refreshShownValues = true)
        {
            var newValue = GetOffsetedIndex(Value, m_directionMultiplier, OptionsCount, WrapAround, MinusOneOption.IsEnabled(out _));
            if (newValue > -1 || (WrapAround && MinusOneOption.IsEnabled(out _) && newValue == -1))
            {
                return Set(newValue, triggerEvent, refreshShownValues);
            }
            return false;
        }
        protected virtual bool SetPrevious(bool triggerEvent = true, bool refreshShownValues = true)
        {
            var newValue = GetOffsetedIndex(Value, -m_directionMultiplier, OptionsCount, WrapAround, MinusOneOption.IsEnabled(out _));
            if (newValue > -1 || (MinusOneOption.IsEnabled(out _) && newValue == -1))
            {
                return Set(newValue, triggerEvent, refreshShownValues);
            }
            return false;
        }

        // PUBLIC
        public virtual void SetValueWithoutNotify(int value) => Set(value, triggerEvent: false);
        public virtual void SetNext(bool notify) => SetNext(notify, true);
        public virtual void SetPrevious(bool notify) => SetPrevious(notify, true);

        public virtual bool AnimateNext()
        {
            if (SetNext(triggerEvent: true, refreshShownValues: false))
            {
                KillMoveCoroutineInstant(true);
                m_mainItemIndex = GetNextIndex(m_mainItemIndex, m_items.Count, true, false);
                StartMoveCoroutine();
                return true;
            }
            return false;
        }
        public virtual bool AnimatePrevious()
        {
            if (SetPrevious(triggerEvent: true, refreshShownValues: false))
            {
                KillMoveCoroutineInstant(true);
                m_mainItemIndex = GetPreviousIndex(m_mainItemIndex, m_items.Count, true, false);
                StartMoveCoroutine();
                return true;
            }
            return false;
        }

        #endregion

        #region Visual Value Update

        public virtual void RefreshShownValues()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (TemplateItem != null
                    && OptionsCount > 0)
                {
                    var optionIndex = GetIndex(Value, OptionsCount, false, MinusOneOption.IsEnabled(out _));
                    OptionData option = optionIndex > -1 ? m_options[optionIndex] :
                        MinusOneOption.IsEnabled(out var minusOneOption) ? minusOneOption : null;
                    TemplateItem.ApplyData(Value, option);
                }

                return;
            }
#endif

            var halfCount = m_items.Count / 2;
            for (int i = -halfCount; i < halfCount + 1; i++)
            {
                var itemIndex = GetIndex(i + m_mainItemIndex, m_items.Count, true, false);
                var item = m_items[itemIndex];

                if (item != null)
                {
                    OptionData option = null;
                    var allowMinusOne = MinusOneOption.IsEnabled(out var minusOneOption);
                    var optionIndex = GetIndex(i * m_directionMultiplier + Value, OptionsCount, WrapAround, allowMinusOne);
                    if (optionIndex > -1) option = m_options[optionIndex];
                    else if (optionIndex == -1 && allowMinusOne) option = minusOneOption;
                    item.ApplyData(optionIndex, option);
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
            var itemSize = TemplateItem.GetSize();
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
            m_itemsDistance = itemsLength + ItemsSpacing;
            m_dragValueChangeThreshold = ComputeDragValueChangeThreshold(viewportLength, itemsLength);

            // Compute items count
            var itemsCount = (!CanDrag && ScrollDuration <= 0f ? 1 : 3) + Mathf.FloorToInt(viewportLength / (2f * m_itemsDistance)) * 2;

            // Instantiate items
            InstantiateItems(itemsCount, itemsVOffset);

            TemplateItem.gameObject.SetActive(false);
        }
        protected virtual float ComputeDragValueChangeThreshold(float viewportLength, float itemsLength)
        {
            return itemsLength / 2f;
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
                item.Offset(offset * (i - halfCount));
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
        public virtual void AddOptions(IEnumerable<string> options)
        {
            foreach (var option in options)
                m_options.Add(new OptionData(option));

            RefreshShownValues();
        }
        public virtual void AddOptions(params string[] options)
        {
            for (int i = 0; i < options.Length; i++)
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
            m_value = MinusOneOption.IsEnabled(out _) ? -1 : 0;
            RefreshShownValues();
        }

        #endregion


        #region Callbacks

        protected virtual void OnLeftButtonClicked()
        {
            if (!IsInteractable()) return;

            if (AnimateOnClick)
            {
                AnimatePrevious();
            }
            else
            {
                SetPrevious();
            }
        }
        protected virtual void OnRightButtonClicked()
        {
            if (!IsInteractable()) return;

            if (AnimateOnClick)
            {
                AnimateNext();
            }
            else
            {
                SetNext();
            }
        }

        #endregion


        #region Tweening

        protected virtual bool IsMoveCoroutineActive() => m_moveCoroutine != null;
        protected virtual void StartMoveCoroutine(bool kill = false, bool complete = false)
        {
            if (IsMoveCoroutineActive())
            {
                if (!kill) return;
                KillMoveCoroutineInstant(complete);
            }
                
            m_moveCoroutine = StartCoroutine(MoveCoroutine());
        }
        protected virtual IEnumerator MoveCoroutine()
        {
            var startOffset = GetCurrentItemsOffset();

            if (Mathf.Approximately(startOffset, 0f))
                yield break;

            var offsetSign = Mathf.Sign(startOffset);
            var normalizedTime = (1 - (Mathf.Abs(startOffset) / m_itemsDistance));

            while (normalizedTime < 1f)
            {
                normalizedTime += Time.deltaTime / ScrollDuration;
                var offset = Mathf.Lerp(offsetSign * m_itemsDistance, 0f, normalizedTime);
                OffsetItems(offset);

                yield return null;
            }

            OnCompleteMoveCoroutine();
        }
        protected virtual void OnCompleteMoveCoroutine()
        {
            // Anchor items
            OffsetItems(0f);

            m_moveCoroutine = null;
        }
        protected virtual void KillMoveCoroutineInstant(bool complete)
        {
            if (m_moveCoroutine != null)
            {
                StopCoroutine(m_moveCoroutine);

                if (complete) OnCompleteMoveCoroutine();
                m_moveCoroutine = null;
            }
        }

        #endregion

        #region Items Movement

        protected virtual void OffsetItems(float offset)
        {
            var halfCount = m_items.Count / 2;
            for (int i = -halfCount; i < halfCount + 1; i++)
            {
                var itemIndex = GetIndex(i + m_mainItemIndex, m_items.Count, true, false);
                var item = m_items[itemIndex];

                OffsetItem(i, item, offset);
            }
        }
        protected virtual void OffsetItem(int centeredIndex, UIScrollListItem item, float offset)
        {
            var appliedOffset = offset + m_itemsDistance * centeredIndex;
            float currentOffset;

            switch (Direction)
            {
                case EDirection.LeftToRight:
                case EDirection.RightToLeft:
                    currentOffset = item.GetOffset().x;
                    item.Offset(new Vector2(appliedOffset, 0f));
                    break;

                default:
                    currentOffset = item.GetOffset().y;
                    item.Offset(new Vector2(0f, appliedOffset));
                    break;
            }

            // Change of side --> update value
            if (appliedOffset * currentOffset < 0f)
            {
                OptionData option = null;
                var allowMinusOne = MinusOneOption.IsEnabled(out var minusOneOption);
                var optionIndex = GetIndex(centeredIndex * m_directionMultiplier + Value, OptionsCount, WrapAround, allowMinusOne);
                if (optionIndex > -1) option = m_options[optionIndex];
                else if (optionIndex == -1 && allowMinusOne) option = minusOneOption;
                item.ApplyData(optionIndex, option);
            }
        }

        #endregion

        #region Drag

        protected virtual bool MayStartDrag(PointerEventData eventData)
        {
            return IsInteractable() && CanDrag && IsInsideViewport(eventData);
        }
        protected virtual bool MayDrag(PointerEventData eventData)
        {
            return IsInteractable() && CanDrag && (!m_confineDragToViewport || IsInsideViewport(eventData));
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (!MayStartDrag(eventData))
            {
                eventData.pointerDrag = null;
            }
        }
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (IsMoveCoroutineActive())
            {
                KillMoveCoroutineInstant(false);
            }
        }
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
            {
                eventData.pointerDrag = null;
                OnEndDrag(eventData);
                return;
            }

            UpdateDrag(eventData);
        }
        protected virtual void UpdateDrag(PointerEventData eventData)
        {
            var currentOffset = GetCurrentItemsOffset();
            var delta = Direction switch
            {
                EDirection.LeftToRight => eventData.delta.x,
                EDirection.RightToLeft => eventData.delta.x,
                _ => eventData.delta.y,
            };
            var newOffset = currentOffset + delta;

            if (Mathf.Abs(newOffset) > m_dragValueChangeThreshold)
            {
                bool isDragValid;
                int nextItemIndex;
                if (newOffset < 0f)
                {
                    nextItemIndex = GetNextIndex(m_mainItemIndex, m_items.Count, true, false);
                    isDragValid = SetNext(triggerEvent: true, refreshShownValues: false);
                }
                else
                {
                    nextItemIndex = GetPreviousIndex(m_mainItemIndex, m_items.Count, true, false);
                    isDragValid = SetPrevious(triggerEvent: true, refreshShownValues: false);
                }

                if (isDragValid)
                {
                    m_mainItemIndex = nextItemIndex;
                    newOffset = GetCurrentItemsOffset() + delta;
                }
                else
                {
                    newOffset = currentOffset;
                }
            }

            OffsetItems(newOffset);
            m_lastDragDelta = delta;
        }
        public virtual void OnEndDrag(PointerEventData eventData)
        {
            var currentOffset = GetCurrentItemsOffset();
            var newOffset = currentOffset + m_lastDragDelta * 5f;
            if (currentOffset * m_lastDragDelta > 0f 
                && Mathf.Abs(newOffset) > m_dragValueChangeThreshold)
            {
                if (newOffset < 0f)
                {
                    if (AnimateNext()) return;
                }
                else
                {
                    if (AnimatePrevious()) return;
                }
            }

            StartMoveCoroutine();
        }

        #endregion

        #region Scroll

        protected virtual bool MayScroll(PointerEventData eventData)
        {
            return IsInteractable() && !IsMoveCoroutineActive() && IsInsideViewport(eventData);
        }

        public virtual void OnScroll(PointerEventData eventData)
        {
            if (!MayScroll(eventData)) 
                return;

            if (eventData.scrollDelta.y > 0f)
            {
                AnimatePrevious();
            }
            else if (eventData.scrollDelta.y < 0f)
            {
                AnimateNext();
            }
        }

        #endregion

        #region Navigation

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            var horizontal = Direction is EDirection.LeftToRight or EDirection.RightToLeft;
            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (horizontal && FindSelectableOnLeft() == null)
                    {
                        if (AnimateOnClick) AnimatePrevious();
                        else SetPrevious();
                    }
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (horizontal && FindSelectableOnRight() == null)
                    {
                        if (AnimateOnClick) AnimateNext();
                        else SetNext();
                    }
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (!horizontal && FindSelectableOnUp() == null)
                    {
                        if (AnimateOnClick) AnimateNext();
                        else SetNext();
                    }
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (!horizontal && FindSelectableOnDown() == null)
                    {
                        if (AnimateOnClick) AnimatePrevious();
                        else SetPrevious();
                    }
                    else
                        base.OnMove(eventData);
                    break;
            }
        }

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

        #region Utility

        protected float GetCurrentItemsOffset() => GetItemOffset(m_items[GetIndex(m_mainItemIndex, m_items.Count, true, false)]);
        protected float GetItemOffset(UIScrollListItem item)
        {
            switch (Direction)
            {
                case EDirection.LeftToRight:
                case EDirection.RightToLeft:
                    return item.GetOffset().x;
                default:
                    return item.GetOffset().y;
            }
        }

        protected virtual bool IsInsideViewport(PointerEventData eventData)
        {
            return IsInsideViewport(eventData.position);
        }
        protected virtual bool IsInsideViewport(Vector2 position)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(ViewportRect, position);
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

        protected static int GetIndex(int index, int count, bool wrapAround, bool allowMinusOne)
        {
            if (count <= 0)
            {
                throw new Exception("Invalid count " + count);
            }

            while (index < (allowMinusOne ? -1 : 0))
            {
                if (wrapAround)
                {
                    index += count;
                    if (allowMinusOne) index++;
                }
                else return index;
            }
            while (index >= count)
            {
                if (wrapAround)
                {
                    index -= count;
                    if (allowMinusOne) index--;
                }
                else return -2;
            }
            return index;
        }
        protected static int GetOffsetedIndex(int index, int offset, int count, bool wrapAround, bool allowMinusOne)
        {
            return GetIndex(index + offset, count, wrapAround, allowMinusOne);
        }
        protected static int GetPreviousIndex(int index, int count, bool wrapAround, bool allowMinusOne)
        {
            return GetIndex(index - 1, count, wrapAround, allowMinusOne);
        }
        protected static int GetNextIndex(int index, int count, bool wrapAround, bool allowMinusOne)
        {
            return GetIndex(index + 1, count, wrapAround, allowMinusOne);
        }

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(UIScrollList), editorForChildClasses:true)]
    public class UIScrollListEditor : UISelectableEditor
    {
        #region Members

        protected SerializedProperty p_value;
        protected SerializedProperty p_options;
        protected SerializedProperty p_minusOneOption;
        protected SerializedProperty p_direction;
        protected SerializedProperty p_wrapAround;
        protected SerializedProperty p_itemsSpacing;
        protected SerializedProperty p_scrollDuration;
        protected SerializedProperty p_animateOnClick;
        protected SerializedProperty p_canDrag;
        protected SerializedProperty p_confineDragToViewport;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            p_value = serializedObject.FindProperty("m_value");
            p_options = serializedObject.FindProperty("m_options");
            p_minusOneOption = serializedObject.FindProperty("m_minusOneOption");
            p_direction = serializedObject.FindProperty("m_direction");
            p_wrapAround = serializedObject.FindProperty("m_wrapAround");
            p_itemsSpacing = serializedObject.FindProperty("m_itemsSpacing");
            p_scrollDuration = serializedObject.FindProperty("m_scrollDuration");
            p_animateOnClick = serializedObject.FindProperty("m_animateOnClick");
            p_canDrag = serializedObject.FindProperty("m_canDrag");
            p_confineDragToViewport = serializedObject.FindProperty("m_confineDragToViewport");

            m_propertiesToExclude.Add(p_value.propertyPath);
            m_propertiesToExclude.Add(p_options.propertyPath);
            m_propertiesToExclude.Add(p_minusOneOption.propertyPath);
            m_propertiesToExclude.Add(p_direction.propertyPath);
            m_propertiesToExclude.Add(p_wrapAround.propertyPath);
            m_propertiesToExclude.Add(p_itemsSpacing.propertyPath);
            m_propertiesToExclude.Add(p_scrollDuration.propertyPath);
            m_propertiesToExclude.Add(p_animateOnClick.propertyPath);
            m_propertiesToExclude.Add(p_canDrag.propertyPath);
            m_propertiesToExclude.Add(p_confineDragToViewport.propertyPath);
        }

        #endregion

        #region Core GUI

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var optionsCount = p_options.arraySize;
            var allowMinusOne = p_minusOneOption.FindPropertyRelative("m_enabled").boolValue;

            EditorGUILayout.Space();
            EditorGUILayout.IntSlider(p_value, allowMinusOne ? -1 : 0, optionsCount - 1);
            EditorGUILayout.PropertyField(p_options);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(p_minusOneOption);
            if (EditorGUI.EndChangeCheck())
            {
                allowMinusOne = p_minusOneOption.FindPropertyRelative("m_enabled").boolValue;
                if (p_value.intValue == -1)
                {
                    p_value.intValue = 0;
                }
            }

            EditorGUILayout.PropertyField(p_direction);
            EditorGUILayout.PropertyField(p_wrapAround);
            EditorGUILayout.PropertyField(p_itemsSpacing);
            EditorGUILayout.PropertyField(p_scrollDuration);
            if (p_scrollDuration.floatValue > 0f)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(p_animateOnClick);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(p_canDrag);
            if (p_canDrag.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(p_confineDragToViewport);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif

    #endregion
}
