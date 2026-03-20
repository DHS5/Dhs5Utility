using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor.AnimatedValues;
using UnityEditor;
#endif

namespace Dhs5.Utility.UI
{
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A component for making a child RectTransform scroll.
    /// </summary>
    /// <remarks>
    /// ScrollRect will not do any clipping on its own. Combined with a Mask component, it can be turned into a scroll view.
    /// </remarks>
    public class UIScrollRect : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler, ICanvasElement, ILayoutElement, ILayoutGroup
    {
        #region ENUM MovementType

        public enum EMovementType
        {
            /// <summary>
            /// Unrestricted movement. The content can move forever.
            /// </summary>
            Unrestricted,

            /// <summary>
            /// Elastic movement. The content is allowed to temporarily move beyond the container, but is pulled back elastically.
            /// </summary>
            Elastic,

            /// <summary>
            /// Clamped movement. The content can not be moved beyond its container.
            /// </summary>
            Clamped,
        }

        #endregion

        #region ENUM ScrollbarVisibility

        /// <summary>
        /// Enum for which behavior to use for scrollbar visibility.
        /// </summary>
        public enum EScrollbarVisibility
        {
            /// <summary>
            /// Always show the scrollbar.
            /// </summary>
            Permanent,

            /// <summary>
            /// Automatically hide the scrollbar when no scrolling is needed on this axis. The viewport rect will not be changed.
            /// </summary>
            AutoHide,

            /// <summary>
            /// Automatically hide the scrollbar when no scrolling is needed on this axis, and expand the viewport rect accordingly.
            /// </summary>
            /// <remarks>
            /// When this setting is used, the scrollbar and the viewport rect become driven, meaning that values in the RectTransform are calculated automatically and can't be manually edited.
            /// </remarks>
            AutoHideAndExpandViewport,
        }

        #endregion


        #region Members

        [SerializeField] protected RectTransform m_contentRect;
        [SerializeField] protected bool m_horizontal = true;
        [SerializeField] protected bool m_vertical = true;
        [SerializeField] protected EMovementType m_movementType = EMovementType.Clamped;
        [SerializeField] protected float m_elasticity = 0.1f;
        [SerializeField] protected bool m_inertia = true;
        [SerializeField] protected float m_decelerationRate = 0.135f; // Only used when inertia is enabled
        [SerializeField] protected float m_scrollSensitivity = 20.0f;
        [SerializeField] protected RectTransform m_viewportRect;
        [SerializeField] protected UIScrollbar m_horizontalScrollbar;
        [SerializeField] protected UIScrollbar m_verticalScrollbar;
        [SerializeField] protected EScrollbarVisibility m_horizontalScrollbarVisibility;
        [SerializeField] protected EScrollbarVisibility m_verticalScrollbarVisibility;
        [SerializeField] protected float m_horizontalScrollbarSpacing;
        [SerializeField] protected float m_verticalScrollbarSpacing;


        // The offset from handle position to mouse down position
        protected Vector2 m_pointerStartLocalCursor = Vector2.zero;
        protected Vector2 m_contentStartPosition = Vector2.zero;

        protected RectTransform m_viewRect;

        protected Bounds m_contentBounds;
        protected Bounds m_viewBounds;

        protected Vector2 m_velocity;

        protected bool m_dragging;
        protected bool m_scrolling;

        protected Vector2 m_prevPosition = Vector2.zero;
        protected Bounds m_prevContentBounds;
        protected Bounds m_prevViewBounds;
        protected bool m_hasRebuiltLayout = false;

        protected bool m_hSliderExpand;
        protected bool m_vSliderExpand;
        protected float m_hSliderHeight;
        protected float m_vSliderWidth;

        protected RectTransform m_rect;

        protected RectTransform m_horizontalScrollbarRect;
        protected RectTransform m_verticalScrollbarRect;

        // field is never assigned warning
#pragma warning disable 649
        protected DrivenRectTransformTracker m_tracker;
#pragma warning restore 649

        #endregion

        #region Properties

        public virtual RectTransform ContentRect
        {
            get => m_contentRect;
            set => m_contentRect = value;
        }
        public virtual bool Horizontal 
        { 
            get => m_horizontal;
            set => m_horizontal = value; 
        }
        public virtual bool Vertical 
        { 
            get => m_vertical; 
            set => m_vertical = value; 
        }
        public virtual EMovementType MovementType 
        { 
            get => m_movementType; 
            set => m_movementType = value; 
        }
        public virtual float Elasticity 
        { 
            get => m_elasticity; 
            set => m_elasticity = value; 
        }
        public virtual bool Inertia 
        { 
            get => m_inertia; 
            set => m_inertia = value; 
        }
        public virtual float DecelerationRate 
        { 
            get => m_decelerationRate; 
            set => m_decelerationRate = value; 
        }
        /// <summary>
        /// The sensitivity to scroll wheel and track pad scroll events.
        /// </summary>
        /// <remarks>
        /// Higher values indicate higher sensitivity.
        /// </remarks>
        public virtual float ScrollSensitivity 
        { 
            get => m_scrollSensitivity; 
            set => m_scrollSensitivity = value; 
        }
        /// <summary>
        /// Reference to the viewport RectTransform that is the parent of the content RectTransform.
        /// </summary>
        public virtual RectTransform ViewportRect 
        { 
            get => m_viewportRect; 
            set 
            {
                if (m_viewportRect != value)
                {
                    m_viewportRect = value;
                    SetDirtyCaching();
                }
            } 
        }
        /// <summary>
        /// Optional Scrollbar object linked to the horizontal scrolling of the ScrollRect.
        /// </summary>
        public virtual UIScrollbar HorizontalScrollbar
        {
            get => m_horizontalScrollbar;
            set
            {
                if (m_horizontalScrollbar == value) return;

                var applicationPlaying =
#if UNITY_EDITOR
                    Application.isPlaying;
#else
                    true;
#endif

                if (applicationPlaying && m_horizontalScrollbar != null)
                    m_horizontalScrollbar.ValueChanged -= SetHorizontalNormalizedPosition;

                m_horizontalScrollbar = value;

                if (applicationPlaying && m_horizontal && m_horizontalScrollbar)
                    m_horizontalScrollbar.ValueChanged += SetHorizontalNormalizedPosition;

                SetDirtyCaching();
            }
        }
        /// <summary>
        /// Optional Scrollbar object linked to the vertical scrolling of the ScrollRect.
        /// </summary>
        public virtual UIScrollbar VerticalScrollbar
        {
            get => m_verticalScrollbar;
            set
            {
                if (m_verticalScrollbar == value) return;

                var applicationPlaying =
#if UNITY_EDITOR
                    Application.isPlaying;
#else
                    true;
#endif

                if (applicationPlaying && m_verticalScrollbar)
                    m_verticalScrollbar.ValueChanged -= SetVerticalNormalizedPosition;

                m_verticalScrollbar = value;

                if (applicationPlaying && m_vertical && m_verticalScrollbar)
                    m_verticalScrollbar.ValueChanged += SetVerticalNormalizedPosition;

                SetDirtyCaching();
            }
        }
        /// <summary>
        /// The mode of visibility for the horizontal scrollbar.
        /// </summary>
        public virtual EScrollbarVisibility HorizontalScrollbarVisibility 
        { 
            get => m_horizontalScrollbarVisibility; 
            set 
            {
                if (m_horizontalScrollbarVisibility != value)
                {
                    m_horizontalScrollbarVisibility = value;
                    SetDirtyCaching();
                }
            } 
        }
        /// <summary>
        /// The mode of visibility for the vertical scrollbar.
        /// </summary>
        public virtual EScrollbarVisibility VerticalScrollbarVisibility 
        { 
            get => m_verticalScrollbarVisibility; 
            set 
            {
                if (m_verticalScrollbarVisibility != value)
                {
                    m_verticalScrollbarVisibility = value;
                    SetDirtyCaching();
                }
            } 
        }
        /// <summary>
        /// The space between the scrollbar and the viewport.
        /// </summary>
        public virtual float HorizontalScrollbarSpacing 
        { 
            get => m_horizontalScrollbarSpacing; 
            set 
            {
                if (m_horizontalScrollbarSpacing != value)
                {
                    m_horizontalScrollbarSpacing = value;
                    SetDirty();
                }
            } 
        }
        /// <summary>
        /// The space between the scrollbar and the viewport.
        /// </summary>
        public virtual float VerticalScrollbarSpacing 
        { 
            get => m_verticalScrollbarSpacing; 
            set 
            {
                if (m_verticalScrollbarSpacing != value)
                {
                    m_verticalScrollbarSpacing = value;
                    SetDirty();
                }
            } 
        }
        protected virtual RectTransform ViewRect
        {
            get
            {
                if (m_viewRect == null)
                    m_viewRect = m_viewportRect;
                if (m_viewRect == null)
                    m_viewRect = (RectTransform)transform;
                return m_viewRect;
            }
        }
        /// <summary>
        /// The current velocity of the content.
        /// </summary>
        /// <remarks>
        /// The velocity is defined in units per second.
        /// </remarks>
        public virtual Vector2 Velocity 
        { 
            get => m_velocity;
            set => m_velocity = value; 
        }
        protected virtual RectTransform Rect
        {
            get
            {
                if (m_rect == null)
                    m_rect = GetComponent<RectTransform>();
                return m_rect;
            }
        }
        /// <summary>
        /// The scroll position as a Vector2 between (0,0) and (1,1) with (0,0) being the lower left corner.
        /// </summary>
        public virtual Vector2 NormalizedPosition
        {
            get
            {
                return new Vector2(HorizontalNormalizedPosition, VerticalNormalizedPosition);
            }
            set
            {
                SetNormalizedPosition(value.x, 0);
                SetNormalizedPosition(value.y, 1);
            }
        }

        /// <summary>
        /// The horizontal scroll position as a value between 0 and 1, with 0 being at the left.
        /// </summary>
        public virtual float HorizontalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if ((m_contentBounds.size.x <= m_viewBounds.size.x) || Mathf.Approximately(m_contentBounds.size.x, m_viewBounds.size.x))
                    return (m_viewBounds.min.x > m_contentBounds.min.x) ? 1 : 0;
                return (m_viewBounds.min.x - m_contentBounds.min.x) / (m_contentBounds.size.x - m_viewBounds.size.x);
            }
            set => SetNormalizedPosition(value, 0);
        }

        /// <summary>
        /// The vertical scroll position as a value between 0 and 1, with 0 being at the bottom.
        /// </summary>
        public virtual float VerticalNormalizedPosition
        {
            get
            {
                UpdateBounds();
                if ((m_contentBounds.size.y <= m_viewBounds.size.y) || Mathf.Approximately(m_contentBounds.size.y, m_viewBounds.size.y))
                    return (m_viewBounds.min.y > m_contentBounds.min.y) ? 1 : 0;

                return (m_viewBounds.min.y - m_contentBounds.min.y) / (m_contentBounds.size.y - m_viewBounds.size.y);
            }
            set => SetNormalizedPosition(value, 1);
        }
        protected virtual bool HScrollingNeeded
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) return true;
#endif
                return m_contentBounds.size.x > m_viewBounds.size.x + 0.01f;
            }
        }
        protected virtual bool VScrollingNeeded
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) return true;
#endif
                return m_contentBounds.size.y > m_viewBounds.size.y + 0.01f;
            }
        }

        #endregion

        #region Events

        public event Action<UIScrollRect, Vector2> ValueChanged;

        protected void TriggerValueChanged()
        {
            UISystemProfilerApi.AddMarker("ScrollRect.value", this);
            ValueChanged?.Invoke(this, NormalizedPosition);
        }

        #endregion

        #region Core Behaviour

        public override bool IsActive()
        {
            return base.IsActive() && m_contentRect != null;
        }
        protected override void OnEnable()
        {
            base.OnEnable();

            if (m_horizontal && m_horizontalScrollbar)
                m_horizontalScrollbar.ValueChanged += SetHorizontalNormalizedPosition;
            if (m_vertical && m_verticalScrollbar)
                m_verticalScrollbar.ValueChanged += SetVerticalNormalizedPosition;

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            SetDirty();
        }
        protected override void OnDisable()
        {
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);

            if (m_horizontalScrollbar)
                m_horizontalScrollbar.ValueChanged -= SetHorizontalNormalizedPosition;
            if (m_verticalScrollbar)
                m_verticalScrollbar.ValueChanged -= SetVerticalNormalizedPosition;

            m_dragging = false;
            m_scrolling = false;
            m_hasRebuiltLayout = false;
            m_tracker.Clear();
            m_velocity = Vector2.zero;

            LayoutRebuilder.MarkLayoutForRebuild(Rect);

            base.OnDisable();
        }
        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        #endregion


        #region Process

        protected virtual void UpdateCachedData()
        {
            Transform transform = this.transform;
            m_horizontalScrollbarRect = m_horizontalScrollbar == null ? null : m_horizontalScrollbar.transform as RectTransform;
            m_verticalScrollbarRect = m_verticalScrollbar == null ? null : m_verticalScrollbar.transform as RectTransform;

            // These are true if either the elements are children, or they don't exist at all.
            bool viewIsChild = (ViewRect.parent == transform);
            bool hScrollbarIsChild = (!m_horizontalScrollbarRect || m_horizontalScrollbarRect.parent == transform);
            bool vScrollbarIsChild = (!m_verticalScrollbarRect || m_verticalScrollbarRect.parent == transform);
            bool allAreChildren = (viewIsChild && hScrollbarIsChild && vScrollbarIsChild);

            m_hSliderExpand = allAreChildren && m_horizontalScrollbarRect && HorizontalScrollbarVisibility == EScrollbarVisibility.AutoHideAndExpandViewport;
            m_vSliderExpand = allAreChildren && m_verticalScrollbarRect && VerticalScrollbarVisibility == EScrollbarVisibility.AutoHideAndExpandViewport;
            m_hSliderHeight = (m_horizontalScrollbarRect == null ? 0 : m_horizontalScrollbarRect.rect.height);
            m_vSliderWidth = (m_verticalScrollbarRect == null ? 0 : m_verticalScrollbarRect.rect.width);
        }

        protected virtual void EnsureLayoutHasRebuilt()
        {
            if (!m_hasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
                Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// Helper function to update the previous data fields on a ScrollRect. Call this before you change data in the ScrollRect.
        /// </summary>
        protected virtual void UpdatePrevData()
        {
            if (m_contentRect == null)
                m_prevPosition = Vector2.zero;
            else
                m_prevPosition = m_contentRect.anchoredPosition;
            m_prevViewBounds = m_viewBounds;
            m_prevContentBounds = m_contentBounds;
        }

        #endregion

        #region Setters

        /// <summary>
        /// Sets the anchored position of the content.
        /// </summary>
        protected virtual void SetContentAnchoredPosition(Vector2 position)
        {
            if (!m_horizontal)
                position.x = m_contentRect.anchoredPosition.x;
            if (!m_vertical)
                position.y = m_contentRect.anchoredPosition.y;

            if (position != m_contentRect.anchoredPosition)
            {
                m_contentRect.anchoredPosition = position;
                UpdateBounds();
            }
        }

        protected void SetHorizontalNormalizedPosition(float value) => SetNormalizedPosition(value, 0);
        protected void SetVerticalNormalizedPosition(float value) => SetNormalizedPosition(value, 1);

        /// <summary>
        /// Set the horizontal or vertical scroll position as a value between 0 and 1, with 0 being at the left or at the bottom.
        /// </summary>
        /// <param name="value">The position to set, between 0 and 1.</param>
        /// <param name="axis">The axis to set: 0 for horizontal, 1 for vertical.</param>
        protected virtual void SetNormalizedPosition(float value, int axis)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();
            // How much the content is larger than the view.
            float hiddenLength = m_contentBounds.size[axis] - m_viewBounds.size[axis];
            // Where the position of the lower left corner of the content bounds should be, in the space of the view.
            float contentBoundsMinPosition = m_viewBounds.min[axis] - value * hiddenLength;
            // The new content localPosition, in the space of the view.
            float newAnchoredPosition = m_contentRect.anchoredPosition[axis] + contentBoundsMinPosition - m_contentBounds.min[axis];

            Vector3 anchoredPosition = m_contentRect.anchoredPosition;
            if (Mathf.Abs(anchoredPosition[axis] - newAnchoredPosition) > 0.01f)
            {
                anchoredPosition[axis] = newAnchoredPosition;
                m_contentRect.anchoredPosition = anchoredPosition;
                m_velocity[axis] = 0;
                UpdateBounds();
            }
        }

        #endregion

        #region Actions

        /// <summary>
        /// Usable only on screen space / overlay canvas
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <returns></returns>
        public virtual bool EnsureRectTransformVisible(RectTransform rectTransform)
        {
            if (rectTransform == null 
                || ContentRect == null 
                || ViewportRect == null) return false;

            var parent = rectTransform.parent;
            while (parent != ContentRect)
            {
                parent = parent.parent;
                if (parent == null) return false;
            }

            // Get content screen space rect
            ViewportRect.GetWorldCorners(m_Corners);
            var viewportRect = new Rect(m_Corners[0], m_Corners[2] - m_Corners[0]);

            // Check if rectTransform is inside and get offset if not
            rectTransform.GetWorldCorners(m_Corners);
            var offset = Vector2.zero;
            var hasOffset = false;

            if (m_Corners[0].x < viewportRect.xMin)
            {
                offset.x = m_Corners[0].x - viewportRect.xMin;
                hasOffset = true;
            }
            if (m_Corners[0].y < viewportRect.yMin)
            {
                offset.y = m_Corners[0].y - viewportRect.yMin;
                hasOffset = true;
            }
            if (m_Corners[2].x > viewportRect.xMax)
            {
                offset.x = m_Corners[2].x - viewportRect.xMax;
                hasOffset = true;
            }
            if (m_Corners[2].y > viewportRect.yMax)
            {
                offset.y = m_Corners[2].y - viewportRect.yMax;
                hasOffset = true;
            }

            if (hasOffset)
            {
                SetContentAnchoredPosition(m_contentRect.anchoredPosition - offset);
            }

            return true;
        }

        #endregion

        #region Update

        protected virtual void LateUpdate()
        {
            if (!m_contentRect)
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float deltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);

            // Skip processing if deltaTime is invalid (0 or less) as it will cause inaccurate velocity calculations and a divide by zero error.
            if (deltaTime > 0.0f)
            {
                if (!m_dragging && (offset != Vector2.zero || m_velocity != Vector2.zero))
                {
                    Vector2 position = m_contentRect.anchoredPosition;
                    for (int axis = 0; axis < 2; axis++)
                    {
                        // Apply spring physics if movement is elastic and content has an offset from the view.
                        if (m_movementType == EMovementType.Elastic && offset[axis] != 0)
                        {
                            float speed = m_velocity[axis];
                            float smoothTime = m_elasticity;
                            if (m_scrolling)
                                smoothTime *= 3.0f;
                            position[axis] = Mathf.SmoothDamp(m_contentRect.anchoredPosition[axis], m_contentRect.anchoredPosition[axis] + offset[axis], ref speed, smoothTime, Mathf.Infinity, deltaTime);
                            if (Mathf.Abs(speed) < 1)
                                speed = 0;
                            m_velocity[axis] = speed;
                        }
                        // Else move content according to velocity with deceleration applied.
                        else if (m_inertia)
                        {
                            m_velocity[axis] *= Mathf.Pow(m_decelerationRate, deltaTime);
                            if (Mathf.Abs(m_velocity[axis]) < 1)
                                m_velocity[axis] = 0;
                            position[axis] += m_velocity[axis] * deltaTime;
                        }
                        // If we have neither elaticity or friction, there shouldn't be any velocity.
                        else
                        {
                            m_velocity[axis] = 0;
                        }
                    }

                    if (m_movementType == EMovementType.Clamped)
                    {
                        offset = CalculateOffset(position - m_contentRect.anchoredPosition);
                        position += offset;
                    }

                    SetContentAnchoredPosition(position);
                }

                if (m_dragging && m_inertia)
                {
                    Vector3 newVelocity = (m_contentRect.anchoredPosition - m_prevPosition) / deltaTime;
                    m_velocity = Vector3.Lerp(m_velocity, newVelocity, deltaTime * 10);
                }
            }

            if (m_viewBounds != m_prevViewBounds || m_contentBounds != m_prevContentBounds || m_contentRect.anchoredPosition != m_prevPosition)
            {
                UpdateScrollbars(offset);
                TriggerValueChanged();
                UpdatePrevData();
            }
            UpdateScrollbarVisibility();
            m_scrolling = false;
        }

        #endregion


        #region Drag

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_velocity = Vector2.zero;
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            UpdateBounds();

            m_pointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(ViewRect, eventData.position, eventData.pressEventCamera, out m_pointerStartLocalCursor);
            m_contentStartPosition = m_contentRect.anchoredPosition;
            m_dragging = true;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            m_dragging = false;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!m_dragging)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!IsActive())
                return;

            Vector2 localCursor;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(ViewRect, eventData.position, eventData.pressEventCamera, out localCursor))
                return;

            UpdateBounds();

            var pointerDelta = localCursor - m_pointerStartLocalCursor;
            Vector2 position = m_contentStartPosition + pointerDelta;

            // Offset to get content into place in the view.
            Vector2 offset = CalculateOffset(position - m_contentRect.anchoredPosition);
            position += offset;
            if (m_movementType == EMovementType.Elastic)
            {
                if (offset.x != 0)
                    position.x = position.x - RubberDelta(offset.x, m_viewBounds.size.x);
                if (offset.y != 0)
                    position.y = position.y - RubberDelta(offset.y, m_viewBounds.size.y);
            }

            SetContentAnchoredPosition(position);
        }

        #endregion

        #region Scroll

        public virtual void OnScroll(PointerEventData data)
        {
            if (!IsActive())
                return;

            EnsureLayoutHasRebuilt();
            UpdateBounds();

            Vector2 delta = data.scrollDelta;
            // Down is positive for scroll events, while in UI system up is positive.
            delta.y *= -1;
            if (Vertical && !Horizontal)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    delta.y = delta.x;
                delta.x = 0;
            }
            if (Horizontal && !Vertical)
            {
                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    delta.x = delta.y;
                delta.y = 0;
            }

            if (data.IsScrolling())
                m_scrolling = true;

            Vector2 position = m_contentRect.anchoredPosition;
            position += delta * m_scrollSensitivity;
            if (m_movementType == EMovementType.Clamped)
                position += CalculateOffset(position - m_contentRect.anchoredPosition);

            SetContentAnchoredPosition(position);
            UpdateBounds();
        }

        #endregion

        #region ICanvasElement

        /// <summary>
        /// Rebuilds the scroll rect data after initialization.
        /// </summary>
        /// <param name="executing">The current step in the rendering CanvasUpdate cycle.</param>
        public virtual void Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.Prelayout)
            {
                UpdateCachedData();
            }

            if (executing == CanvasUpdate.PostLayout)
            {
                UpdateBounds();
                UpdateScrollbars(Vector2.zero);
                UpdatePrevData();

                m_hasRebuiltLayout = true;
            }
        }

        public virtual void LayoutComplete()
        { }

        public virtual void GraphicUpdateComplete()
        { }

        #endregion

        #region ILayoutElement

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void CalculateLayoutInputHorizontal() { }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void CalculateLayoutInputVertical() { }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float minWidth { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float preferredWidth { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float flexibleWidth { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float minHeight { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float preferredHeight { get { return -1; } }
        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual float flexibleHeight { get { return -1; } }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual int layoutPriority { get { return -1; } }

        #endregion

        #region ILayoutGroup

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void SetLayoutHorizontal()
        {
            m_tracker.Clear();
            UpdateCachedData();

            if (m_hSliderExpand || m_vSliderExpand)
            {
                m_tracker.Add(this, ViewRect,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.SizeDelta |
                    DrivenTransformProperties.AnchoredPosition);

                // Make view full size to see if content fits.
                ViewRect.anchorMin = Vector2.zero;
                ViewRect.anchorMax = Vector2.one;
                ViewRect.sizeDelta = Vector2.zero;
                ViewRect.anchoredPosition = Vector2.zero;

                // Recalculate content layout with this size to see if it fits when there are no scrollbars.
                LayoutRebuilder.ForceRebuildLayoutImmediate(ContentRect);
                m_viewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
                m_contentBounds = GetBounds();
            }

            // If it doesn't fit vertically, enable vertical scrollbar and shrink view horizontally to make room for it.
            if (m_vSliderExpand && VScrollingNeeded)
            {
                ViewRect.sizeDelta = new Vector2(-(m_vSliderWidth + m_verticalScrollbarSpacing), ViewRect.sizeDelta.y);

                // Recalculate content layout with this size to see if it fits vertically
                // when there is a vertical scrollbar (which may reflowed the content to make it taller).
                LayoutRebuilder.ForceRebuildLayoutImmediate(ContentRect);
                m_viewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
                m_contentBounds = GetBounds();
            }

            // If it doesn't fit horizontally, enable horizontal scrollbar and shrink view vertically to make room for it.
            if (m_hSliderExpand && HScrollingNeeded)
            {
                ViewRect.sizeDelta = new Vector2(ViewRect.sizeDelta.x, -(m_hSliderHeight + m_horizontalScrollbarSpacing));
                m_viewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
                m_contentBounds = GetBounds();
            }

            // If the vertical slider didn't kick in the first time, and the horizontal one did,
            // we need to check again if the vertical slider now needs to kick in.
            // If it doesn't fit vertically, enable vertical scrollbar and shrink view horizontally to make room for it.
            if (m_vSliderExpand && VScrollingNeeded && ViewRect.sizeDelta.x == 0 && ViewRect.sizeDelta.y < 0)
            {
                ViewRect.sizeDelta = new Vector2(-(m_vSliderWidth + m_verticalScrollbarSpacing), ViewRect.sizeDelta.y);
            }
        }

        /// <summary>
        /// Called by the layout system.
        /// </summary>
        public virtual void SetLayoutVertical()
        {
            UpdateScrollbarLayout();
            m_viewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
            m_contentBounds = GetBounds();
        }

        #endregion


        #region Scrollbars

        protected virtual void UpdateScrollbars(Vector2 offset)
        {
            if (m_horizontalScrollbar)
            {
                if (m_contentBounds.size.x > 0)
                    m_horizontalScrollbar.Size = Mathf.Clamp01((m_viewBounds.size.x - Mathf.Abs(offset.x)) / m_contentBounds.size.x);
                else
                    m_horizontalScrollbar.Size = 1;

                m_horizontalScrollbar.Value = HorizontalNormalizedPosition;
            }

            if (m_verticalScrollbar)
            {
                if (m_contentBounds.size.y > 0)
                    m_verticalScrollbar.Size = Mathf.Clamp01((m_viewBounds.size.y - Mathf.Abs(offset.y)) / m_contentBounds.size.y);
                else
                    m_verticalScrollbar.Size = 1;

                m_verticalScrollbar.Value = VerticalNormalizedPosition;
            }
        }

        protected virtual void UpdateScrollbarVisibility()
        {
            UpdateOneScrollbarVisibility(VScrollingNeeded, m_vertical, m_verticalScrollbarVisibility, m_verticalScrollbar);
            UpdateOneScrollbarVisibility(HScrollingNeeded, m_horizontal, m_horizontalScrollbarVisibility, m_horizontalScrollbar);
        }
        protected virtual void UpdateOneScrollbarVisibility(bool xScrollingNeeded, bool xAxisEnabled, EScrollbarVisibility scrollbarVisibility, UIScrollbar scrollbar)
        {
            if (scrollbar)
            {
                if (scrollbarVisibility == EScrollbarVisibility.Permanent)
                {
                    if (scrollbar.gameObject.activeSelf != xAxisEnabled)
                        scrollbar.gameObject.SetActive(xAxisEnabled);
                }
                else
                {
                    if (scrollbar.gameObject.activeSelf != xScrollingNeeded)
                        scrollbar.gameObject.SetActive(xScrollingNeeded);
                }
            }
        }

        protected virtual void UpdateScrollbarLayout()
        {
            if (m_vSliderExpand && m_horizontalScrollbar)
            {
                m_tracker.Add(this, m_horizontalScrollbarRect,
                    DrivenTransformProperties.AnchorMinX |
                    DrivenTransformProperties.AnchorMaxX |
                    DrivenTransformProperties.SizeDeltaX |
                    DrivenTransformProperties.AnchoredPositionX);
                m_horizontalScrollbarRect.anchorMin = new Vector2(0, m_horizontalScrollbarRect.anchorMin.y);
                m_horizontalScrollbarRect.anchorMax = new Vector2(1, m_horizontalScrollbarRect.anchorMax.y);
                m_horizontalScrollbarRect.anchoredPosition = new Vector2(0, m_horizontalScrollbarRect.anchoredPosition.y);
                if (VScrollingNeeded)
                    m_horizontalScrollbarRect.sizeDelta = new Vector2(-(m_vSliderWidth + m_verticalScrollbarSpacing), m_horizontalScrollbarRect.sizeDelta.y);
                else
                    m_horizontalScrollbarRect.sizeDelta = new Vector2(0, m_horizontalScrollbarRect.sizeDelta.y);
            }

            if (m_hSliderExpand && m_verticalScrollbar)
            {
                m_tracker.Add(this, m_verticalScrollbarRect,
                    DrivenTransformProperties.AnchorMinY |
                    DrivenTransformProperties.AnchorMaxY |
                    DrivenTransformProperties.SizeDeltaY |
                    DrivenTransformProperties.AnchoredPositionY);
                m_verticalScrollbarRect.anchorMin = new Vector2(m_verticalScrollbarRect.anchorMin.x, 0);
                m_verticalScrollbarRect.anchorMax = new Vector2(m_verticalScrollbarRect.anchorMax.x, 1);
                m_verticalScrollbarRect.anchoredPosition = new Vector2(m_verticalScrollbarRect.anchoredPosition.x, 0);
                if (HScrollingNeeded)
                    m_verticalScrollbarRect.sizeDelta = new Vector2(m_verticalScrollbarRect.sizeDelta.x, -(m_hSliderHeight + m_horizontalScrollbarSpacing));
                else
                    m_verticalScrollbarRect.sizeDelta = new Vector2(m_verticalScrollbarRect.sizeDelta.x, 0);
            }
        }

        #endregion

        #region Bounds

        protected readonly Vector3[] m_Corners = new Vector3[4];

        /// <summary>
        /// Calculate the bounds the ScrollRect should be using.
        /// </summary>
        protected virtual void UpdateBounds()
        {
            m_viewBounds = new Bounds(ViewRect.rect.center, ViewRect.rect.size);
            m_contentBounds = GetBounds();

            if (m_contentRect == null)
                return;

            Vector3 contentSize = m_contentBounds.size;
            Vector3 contentPos = m_contentBounds.center;
            var contentPivot = m_contentRect.pivot;
            AdjustBounds(ref m_viewBounds, ref contentPivot, ref contentSize, ref contentPos);
            m_contentBounds.size = contentSize;
            m_contentBounds.center = contentPos;

            if (MovementType == EMovementType.Clamped)
            {
                // Adjust content so that content bounds bottom (right side) is never higher (to the left) than the view bounds bottom (right side).
                // top (left side) is never lower (to the right) than the view bounds top (left side).
                // All this can happen if content has shrunk.
                // This works because content size is at least as big as view size (because of the call to InternalUpdateBounds above).
                Vector2 delta = Vector2.zero;
                if (m_viewBounds.max.x > m_contentBounds.max.x)
                {
                    delta.x = Math.Min(m_viewBounds.min.x - m_contentBounds.min.x, m_viewBounds.max.x - m_contentBounds.max.x);
                }
                else if (m_viewBounds.min.x < m_contentBounds.min.x)
                {
                    delta.x = Math.Max(m_viewBounds.min.x - m_contentBounds.min.x, m_viewBounds.max.x - m_contentBounds.max.x);
                }

                if (m_viewBounds.min.y < m_contentBounds.min.y)
                {
                    delta.y = Math.Max(m_viewBounds.min.y - m_contentBounds.min.y, m_viewBounds.max.y - m_contentBounds.max.y);
                }
                else if (m_viewBounds.max.y > m_contentBounds.max.y)
                {
                    delta.y = Math.Min(m_viewBounds.min.y - m_contentBounds.min.y, m_viewBounds.max.y - m_contentBounds.max.y);
                }
                if (delta.sqrMagnitude > float.Epsilon)
                {
                    contentPos = m_contentRect.anchoredPosition + delta;
                    if (!m_horizontal)
                        contentPos.x = m_contentRect.anchoredPosition.x;
                    if (!m_vertical)
                        contentPos.y = m_contentRect.anchoredPosition.y;
                    AdjustBounds(ref m_viewBounds, ref contentPivot, ref contentSize, ref contentPos);
                }
            }
        }

        protected virtual void AdjustBounds(ref Bounds viewBounds, ref Vector2 contentPivot, ref Vector3 contentSize, ref Vector3 contentPos)
        {
            // Make sure content bounds are at least as large as view by adding padding if not.
            // One might think at first that if the content is smaller than the view, scrolling should be allowed.
            // However, that's not how scroll views normally work.
            // Scrolling is *only* possible when content is *larger* than view.
            // We use the pivot of the content rect to decide in which directions the content bounds should be expanded.
            // E.g. if pivot is at top, bounds are expanded downwards.
            // This also works nicely when ContentSizeFitter is used on the content.
            Vector3 excess = viewBounds.size - contentSize;
            if (excess.x > 0)
            {
                contentPos.x -= excess.x * (contentPivot.x - 0.5f);
                contentSize.x = viewBounds.size.x;
            }
            if (excess.y > 0)
            {
                contentPos.y -= excess.y * (contentPivot.y - 0.5f);
                contentSize.y = viewBounds.size.y;
            }
        }

        protected virtual Bounds GetBounds()
        {
            if (m_contentRect == null)
                return new Bounds();
            m_contentRect.GetWorldCorners(m_Corners);
            var viewWorldToLocalMatrix = ViewRect.worldToLocalMatrix;
            return InternalGetBounds(m_Corners, ref viewWorldToLocalMatrix);
        }

        protected virtual Bounds InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix)
        {
            var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = viewWorldToLocalMatrix.MultiplyPoint3x4(corners[j]);
                vMin = Vector3.Min(v, vMin);
                vMax = Vector3.Max(v, vMax);
            }

            var bounds = new Bounds(vMin, Vector3.zero);
            bounds.Encapsulate(vMax);
            return bounds;
        }

        #endregion

        #region Offset

        protected virtual Vector2 CalculateOffset(Vector2 delta)
        {
            return InternalCalculateOffset(ref m_viewBounds, ref m_contentBounds, m_horizontal, m_vertical, m_movementType, ref delta);
        }

        protected virtual Vector2 InternalCalculateOffset(ref Bounds viewBounds, ref Bounds contentBounds, bool horizontal, bool vertical, EMovementType movementType, ref Vector2 delta)
        {
            Vector2 offset = Vector2.zero;
            if (movementType == EMovementType.Unrestricted)
                return offset;

            Vector2 min = contentBounds.min;
            Vector2 max = contentBounds.max;

            // min/max offset extracted to check if approximately 0 and avoid recalculating layout every frame (case 1010178)

            if (horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;

                float maxOffset = viewBounds.max.x - max.x;
                float minOffset = viewBounds.min.x - min.x;

                if (minOffset < -0.001f)
                    offset.x = minOffset;
                else if (maxOffset > 0.001f)
                    offset.x = maxOffset;
            }

            if (vertical)
            {
                min.y += delta.y;
                max.y += delta.y;

                float maxOffset = viewBounds.max.y - max.y;
                float minOffset = viewBounds.min.y - min.y;

                if (maxOffset > 0.001f)
                    offset.y = maxOffset;
                else if (minOffset < -0.001f)
                    offset.y = minOffset;
            }

            return offset;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Sets the velocity to zero on both axes so the content stops moving.
        /// </summary>
        public virtual void StopMovement()
        {
            m_velocity = Vector2.zero;
        }

        protected virtual float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

        /// <summary>
        /// Override to alter or add to the code that keeps the appearance of the scroll rect synced with its data.
        /// </summary>
        protected virtual void SetDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(Rect);
        }

        /// <summary>
        /// Override to alter or add to the code that caches data to avoid repeated heavy operations.
        /// </summary>
        protected virtual void SetDirtyCaching()
        {
            if (!IsActive())
                return;

            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(Rect);

            m_viewRect = null;
        }

        #endregion

        #region Editor 

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            SetDirtyCaching();
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(UIScrollRect), editorForChildClasses:true)]
    [CanEditMultipleObjects]
    public class UIScrollRectEditor : Editor
    {
        #region Members

        protected UIScrollRect m_scrollRect;

        protected SerializedProperty p_contentRect;
        protected SerializedProperty p_horizontal;
        protected SerializedProperty p_vertical;
        protected SerializedProperty p_movementType;
        protected SerializedProperty p_elasticity;
        protected SerializedProperty p_inertia;
        protected SerializedProperty p_decelerationRate;
        protected SerializedProperty p_scrollSensitivity;
        protected SerializedProperty p_viewportRect;
        protected SerializedProperty p_horizontalScrollbar;
        protected SerializedProperty p_verticalScrollbar;
        protected SerializedProperty p_horizontalScrollbarVisibility;
        protected SerializedProperty p_verticalScrollbarVisibility;
        protected SerializedProperty p_horizontalScrollbarSpacing;
        protected SerializedProperty p_verticalScrollbarSpacing;
          
        protected AnimBool m_showElasticity;
        protected AnimBool m_showDecelerationRate;
          
        protected bool m_viewportIsNotChild;
        protected bool m_hScrollbarIsNotChild;
        protected bool m_vScrollbarIsNotChild;
          
        protected static string _hError = "For this visibility mode, the Viewport property and the Horizontal Scrollbar property both needs to be set to a Rect Transform that is a child to the Scroll Rect.";
        protected static string _vError = "For this visibility mode, the Viewport property and the Vertical Scrollbar property both needs to be set to a Rect Transform that is a child to the Scroll Rect.";

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_scrollRect = target as UIScrollRect;

            p_contentRect = base.serializedObject.FindProperty("m_contentRect");
            p_horizontal = base.serializedObject.FindProperty("m_horizontal");
            p_vertical = base.serializedObject.FindProperty("m_vertical");
            p_movementType = base.serializedObject.FindProperty("m_movementType");
            p_elasticity = base.serializedObject.FindProperty("m_elasticity");
            p_inertia = base.serializedObject.FindProperty("m_inertia");
            p_decelerationRate = base.serializedObject.FindProperty("m_decelerationRate");
            p_scrollSensitivity = base.serializedObject.FindProperty("m_scrollSensitivity");
            p_viewportRect = base.serializedObject.FindProperty("m_viewportRect");
            p_horizontalScrollbar = base.serializedObject.FindProperty("m_horizontalScrollbar");
            p_verticalScrollbar = base.serializedObject.FindProperty("m_verticalScrollbar");
            p_horizontalScrollbarVisibility = base.serializedObject.FindProperty("m_horizontalScrollbarVisibility");
            p_verticalScrollbarVisibility = base.serializedObject.FindProperty("m_verticalScrollbarVisibility");
            p_horizontalScrollbarSpacing = base.serializedObject.FindProperty("m_horizontalScrollbarSpacing");
            p_verticalScrollbarSpacing = base.serializedObject.FindProperty("m_verticalScrollbarSpacing");

            m_showElasticity = new AnimBool(Repaint);
            m_showDecelerationRate = new AnimBool(Repaint);
            SetAnimBools(instant: true);
        }

        protected virtual void OnDisable()
        {
            m_showElasticity.valueChanged.RemoveListener(Repaint);
            m_showDecelerationRate.valueChanged.RemoveListener(Repaint);
        }

        #endregion

        #region Utility

        private void SetAnimBools(bool instant)
        {
            SetAnimBool(m_showElasticity, !p_movementType.hasMultipleDifferentValues && p_movementType.enumValueIndex == 1, instant);
            SetAnimBool(m_showDecelerationRate, !p_inertia.hasMultipleDifferentValues && p_inertia.boolValue, instant);
        }

        private void SetAnimBool(AnimBool a, bool value, bool instant)
        {
            if (instant)
            {
                a.value = value;
            }
            else
            {
                a.target = value;
            }
        }

        protected virtual void CalculateCachedValues()
        {
            m_viewportIsNotChild = false;
            m_hScrollbarIsNotChild = false;
            m_vScrollbarIsNotChild = false;
            if (base.targets.Length == 1)
            {
                Transform transform = m_scrollRect.transform;
                if (p_viewportRect.objectReferenceValue == null || ((RectTransform)p_viewportRect.objectReferenceValue).transform.parent != transform)
                {
                    m_viewportIsNotChild = true;
                }

                if (p_horizontalScrollbar.objectReferenceValue == null || ((UIScrollbar)p_horizontalScrollbar.objectReferenceValue).transform.parent != transform)
                {
                    m_hScrollbarIsNotChild = true;
                }

                if (p_verticalScrollbar.objectReferenceValue == null || ((UIScrollbar)p_verticalScrollbar.objectReferenceValue).transform.parent != transform)
                {
                    m_vScrollbarIsNotChild = true;
                }
            }
        }

        #endregion

        #region Core GUI

        public override void OnInspectorGUI()
        {
            SetAnimBools(instant: false);

            serializedObject.Update();

            CalculateCachedValues();
            EditorGUILayout.PropertyField(p_horizontal);
            EditorGUILayout.PropertyField(p_vertical);
            EditorGUILayout.PropertyField(p_movementType);
            if (EditorGUILayout.BeginFadeGroup(m_showElasticity.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(p_elasticity);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.PropertyField(p_inertia);
            if (EditorGUILayout.BeginFadeGroup(m_showDecelerationRate.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(p_decelerationRate);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.PropertyField(p_scrollSensitivity);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(p_viewportRect);
            EditorGUILayout.PropertyField(p_contentRect);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(p_horizontalScrollbar);
            if ((bool)p_horizontalScrollbar.objectReferenceValue && !p_horizontalScrollbar.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(p_horizontalScrollbarVisibility, EditorGUIUtility.TrTextContent("Visibility"));
                if (p_horizontalScrollbarVisibility.enumValueIndex == 2 && !p_horizontalScrollbarVisibility.hasMultipleDifferentValues)
                {
                    if (m_viewportIsNotChild || m_hScrollbarIsNotChild)
                    {
                        EditorGUILayout.HelpBox(_hError, MessageType.Error);
                    }

                    EditorGUILayout.PropertyField(p_horizontalScrollbarSpacing, EditorGUIUtility.TrTextContent("Spacing"));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(p_verticalScrollbar);
            if ((bool)p_verticalScrollbar.objectReferenceValue && !p_verticalScrollbar.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(p_verticalScrollbarVisibility, EditorGUIUtility.TrTextContent("Visibility"));
                if (p_verticalScrollbarVisibility.enumValueIndex == 2 && !p_verticalScrollbarVisibility.hasMultipleDifferentValues)
                {
                    if (m_viewportIsNotChild || m_vScrollbarIsNotChild)
                    {
                        EditorGUILayout.HelpBox(_vError, MessageType.Error);
                    }

                    EditorGUILayout.PropertyField(p_verticalScrollbarSpacing, EditorGUIUtility.TrTextContent("Spacing"));
                }

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif

    #endregion
}
