using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UIScrollbar : UISelectable, IBeginDragHandler, IDragHandler, IInitializePotentialDragHandler
    {
        #region ENUM Direction

        public enum EDirection
        {
            /// <summary>
            /// Starting position is the Left.
            /// </summary>
            LeftToRight,

            /// <summary>
            /// Starting position is the Right
            /// </summary>
            RightToLeft,

            /// <summary>
            /// Starting position is the Bottom.
            /// </summary>
            BottomToTop,

            /// <summary>
            /// Starting position is the Top.
            /// </summary>
            TopToBottom,
        }

        #endregion

        #region ENUM Axis

        public enum EAxis
        {
            Horizontal = 0,
            Vertical = 1
        }

        #endregion


        #region Members

        [Header("Scrollbar")]
        [SerializeField] protected RectTransform m_handleRect;
        [SerializeField] protected EDirection m_direction;
        [Range(0f, 1f), SerializeField] protected float m_value;
        [Range(0f, 1f), SerializeField] protected float m_size = 0.2f;
        [Range(0, 20), SerializeField] protected int m_numberOfSteps = 0;

        protected RectTransform m_containerRect;

        // The offset from handle position to mouse down position
        protected Vector2 m_offset = Vector2.zero;

        // field is never assigned warning
#pragma warning disable 649
        protected DrivenRectTransformTracker m_tracker;
#pragma warning restore 649
        protected Coroutine m_pointerDownRepeat;
        protected bool m_isPointerDownAndNotDragging = false;

        // This "delayed" mechanism is required for case 1037681.
#if UNITY_EDITOR
        private bool m_delayedUpdateVisuals = false;
#endif

        #endregion

        #region Properties

        /// <summary>
        /// The RectTransform to use for the handle.
        /// </summary>
        public virtual RectTransform HandleRect 
        { 
            get => m_handleRect;
            set 
            { 
                if (m_handleRect != value) 
                { 
                    m_handleRect = value;
                    UpdateCachedReferences(); 
                    UpdateVisuals(); 
                } 
            } 
        }

        /// <summary>
        /// The direction of the scrollbar from minimum to maximum value.
        /// </summary>
        public virtual EDirection Direction 
        { 
            get => m_direction; 
            set 
            { 
                if (m_direction != value) 
                {
                    m_direction = value;
                    UpdateVisuals(); 
                }
            } 
        }

        /// <summary>
        /// The current value of the scrollbar, between 0 and 1.
        /// </summary>
        public virtual float Value
        {
            get
            {
                float val = m_value;
                if (m_numberOfSteps > 1)
                    val = Mathf.Round(val * (m_numberOfSteps - 1)) / (m_numberOfSteps - 1);
                return val;
            }
            set => Set(value);
        }

        /// <summary>
        /// The size of the scrollbar handle where 1 means it fills the entire scrollbar.
        /// </summary>
        public virtual float Size 
        { 
            get => m_size;
            set 
            {
                var v = Mathf.Clamp01(value);
                if (m_size != v)
                {
                    m_size = v;
                    UpdateVisuals();
                }
            } 
        }

        /// <summary>
        /// The number of steps to use for the value. A value of 0 disables use of steps.
        /// </summary>
        public virtual int NumberOfSteps 
        { 
            get => m_numberOfSteps; 
            set 
            { 
                if (m_numberOfSteps != value) 
                { 
                    m_numberOfSteps = value;
                    Set(m_value); 
                    UpdateVisuals(); 
                } 
            } 
        }

        // Size of each step.
        public float StepSize => (m_numberOfSteps > 1) ? 1f / (m_numberOfSteps - 1) : 0.1f;
        public EAxis Axis => (m_direction == EDirection.LeftToRight || m_direction == EDirection.RightToLeft) ? EAxis.Horizontal : EAxis.Vertical;
        public bool ReverseValue => m_direction == EDirection.RightToLeft || m_direction == EDirection.TopToBottom;

        #endregion

        #region Events

        public event Action<float> ValueChanged;

        protected void TriggerValueChanged()
        {
            UISystemProfilerApi.AddMarker("Scrollbar.value", this);
            EventContext = this;
            ValueChanged?.Invoke(Value);
        }

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            UpdateCachedReferences();
            Set(m_value, false);
            // Update rects since they need to be initialized correctly.
            UpdateVisuals();
        }

        protected override void OnDisable()
        {
            m_tracker.Clear();

            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (!IsActive())
                return;

            UpdateVisuals();
        }

        #endregion


        #region Set Process

        protected virtual void Set(float input, bool triggerEvent = true)
        {
            float currentValue = m_value;

            // bugfix (case 802330) clamp01 input in callee before calling this function, this allows inertia from dragging content to go past extremities without being clamped
            m_value = input;

            // If the stepped value doesn't match the last one, it's time to update
            if (currentValue == Value)
                return;

            UpdateVisuals();
            if (triggerEvent)
            {
                TriggerValueChanged();
            }
        }

        /// <summary>
        /// Set the value of the scrollbar without invoking onValueChanged callback.
        /// </summary>
        /// <param name="input">The new value for the scrollbar.</param>
        public virtual void SetValueWithoutNotify(float input)
        {
            Set(input, false);
        }

        #endregion

        #region Setters

        /// <summary>
        /// Set the direction of the scrollbar, optionally setting the layout as well.
        /// </summary>
        /// <param name="direction">The direction of the scrollbar.</param>
        /// <param name="includeRectLayouts">Should the layout be flipped together with the direction?</param>
        public virtual void SetDirection(EDirection direction, bool includeRectLayouts)
        {
            EAxis oldAxis = Axis;
            bool oldReverse = ReverseValue;
            this.Direction = direction;

            if (!includeRectLayouts)
                return;

            if (Axis != oldAxis)
                RectTransformUtility.FlipLayoutAxes(transform as RectTransform, true, true);

            if (ReverseValue != oldReverse)
                RectTransformUtility.FlipLayoutOnAxis(transform as RectTransform, (int)Axis, true, true);
        }

        #endregion

        #region Update References & Visuals

        protected virtual void UpdateCachedReferences()
        {
            if (m_handleRect && m_handleRect.parent != null)
                m_containerRect = m_handleRect.parent.GetComponent<RectTransform>();
            else
                m_containerRect = null;
        }

        // Force-update the scroll bar. Useful if you've changed the properties and want it to update visually.
        protected virtual void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif
            m_tracker.Clear();

            if (m_containerRect != null)
            {
                m_tracker.Add(this, m_handleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                float movement = Mathf.Clamp01(Value) * (1 - Size);
                if (ReverseValue)
                {
                    anchorMin[(int)Axis] = 1 - movement - Size;
                    anchorMax[(int)Axis] = 1 - movement;
                }
                else
                {
                    anchorMin[(int)Axis] = movement;
                    anchorMax[(int)Axis] = movement + Size;
                }

                m_handleRect.anchorMin = anchorMin;
                m_handleRect.anchorMax = anchorMax;
            }
        }

        #endregion


        #region Drag

        protected virtual bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        // Update the scroll bar's position based on the mouse.
        protected virtual void UpdateDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (m_containerRect == null)
                return;

            Vector2 position = Vector2.zero;
            if (!MultipleDisplayUtilities.GetRelativeMousePositionForDrag(eventData, ref position))
                return;

            UpdateDrag(m_containerRect, position, eventData.pressEventCamera);
        }

        protected virtual void UpdateDrag(RectTransform containerRect, Vector2 position, Camera camera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, position, camera, out var localCursor))
                return;

            var handleCenterRelativeToContainerCorner = localCursor - m_offset - m_containerRect.rect.position;
            var handleCorner = handleCenterRelativeToContainerCorner - (m_handleRect.rect.size - m_handleRect.sizeDelta) * 0.5f;

            float parentSize = Axis == 0 ? m_containerRect.rect.width : m_containerRect.rect.height;
            float remainingSize = parentSize * (1 - Size);
            if (remainingSize <= 0)
                return;

            DoUpdateDrag(handleCorner, remainingSize);
        }

        protected virtual void DoUpdateDrag(Vector2 handleCorner, float remainingSize)
        {
            switch (m_direction)
            {
                case EDirection.LeftToRight:
                    Set(Mathf.Clamp01(handleCorner.x / remainingSize));
                    break;
                case EDirection.RightToLeft:
                    Set(Mathf.Clamp01(1f - (handleCorner.x / remainingSize)));
                    break;
                case EDirection.BottomToTop:
                    Set(Mathf.Clamp01(handleCorner.y / remainingSize));
                    break;
                case EDirection.TopToBottom:
                    Set(Mathf.Clamp01(1f - (handleCorner.y / remainingSize)));
                    break;
            }
        }

        /// <summary>
        /// Handling for when the scrollbar value is begin being dragged.
        /// </summary>
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            m_isPointerDownAndNotDragging = false;

            if (!MayDrag(eventData))
                return;

            if (m_containerRect == null)
                return;

            m_offset = Vector2.zero;
            if (RectTransformUtility.RectangleContainsScreenPoint(m_handleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_handleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos))
                    m_offset = localMousePos - m_handleRect.rect.center;
            }
        }

        /// <summary>
        /// Handling for when the scrollbar value is dragged.
        /// </summary>
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            if (m_containerRect != null)
                UpdateDrag(eventData);
        }

        #endregion

        #region Pointer Down

        protected override void OnAfterPointerDown(PointerEventData eventData, bool didBaseLogic)
        {
            base.OnAfterPointerDown(eventData, true);

            if (!MayDrag(eventData))
                return;

            m_isPointerDownAndNotDragging = true;
            m_pointerDownRepeat = StartCoroutine(ClickRepeat(eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera));
        }

        protected override void OnAfterPointerUp(PointerEventData eventData)
        {
            base.OnAfterPointerUp(eventData);

            m_isPointerDownAndNotDragging = false;
        }

        protected IEnumerator ClickRepeat(PointerEventData eventData)
        {
            return ClickRepeat(eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera);
        }
        /// <summary>
        /// Coroutine function for handling continual press during Scrollbar.OnPointerDown.
        /// </summary>
        protected virtual IEnumerator ClickRepeat(Vector2 screenPosition, Camera camera)
        {
            while (m_isPointerDownAndNotDragging)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(m_handleRect, screenPosition, camera))
                {
                    UpdateDrag(m_containerRect, screenPosition, camera);
                }
                yield return new WaitForEndOfFrame();
            }
            StopCoroutine(m_pointerDownRepeat);
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Handling for movement events.
        /// </summary>
        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (Axis == EAxis.Horizontal && FindSelectableOnLeft() == null)
                        Set(Mathf.Clamp01(ReverseValue ? Value + StepSize : Value - StepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (Axis == EAxis.Horizontal && FindSelectableOnRight() == null)
                        Set(Mathf.Clamp01(ReverseValue ? Value - StepSize : Value + StepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (Axis == EAxis.Vertical && FindSelectableOnUp() == null)
                        Set(Mathf.Clamp01(ReverseValue ? Value - StepSize : Value + StepSize));
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (Axis == EAxis.Vertical && FindSelectableOnDown() == null)
                        Set(Mathf.Clamp01(ReverseValue ? Value + StepSize : Value - StepSize));
                    else
                        base.OnMove(eventData);
                    break;
            }
        }

        /// <summary>
        /// Prevents selection if we we move on the Horizontal axis. See Selectable.FindSelectableOnLeft.
        /// </summary>
        public override Selectable FindSelectableOnLeft()
        {
            if (navigation.mode == Navigation.Mode.Automatic && Axis == EAxis.Horizontal)
                return null;
            return base.FindSelectableOnLeft();
        }

        /// <summary>
        /// Prevents selection if we we move on the Horizontal axis.  See Selectable.FindSelectableOnRight.
        /// </summary>
        public override Selectable FindSelectableOnRight()
        {
            if (navigation.mode == Navigation.Mode.Automatic && Axis == EAxis.Horizontal)
                return null;
            return base.FindSelectableOnRight();
        }

        /// <summary>
        /// Prevents selection if we we move on the Vertical axis. See Selectable.FindSelectableOnUp.
        /// </summary>
        public override Selectable FindSelectableOnUp()
        {
            if (navigation.mode == Navigation.Mode.Automatic && Axis == EAxis.Vertical)
                return null;
            return base.FindSelectableOnUp();
        }

        /// <summary>
        /// Prevents selection if we we move on the Vertical axis. See Selectable.FindSelectableOnDown.
        /// </summary>
        public override Selectable FindSelectableOnDown()
        {
            if (navigation.mode == Navigation.Mode.Automatic && Axis == EAxis.Vertical)
                return null;
            return base.FindSelectableOnDown();
        }

        /// <summary>
        /// See: IInitializePotentialDragHandler.OnInitializePotentialDrag
        /// </summary>
        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        #endregion


        #region Editor

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();

            m_size = Mathf.Clamp01(m_size);

            //This can be invoked before OnEnabled is called. So we shouldn't be accessing other objects, before OnEnable is called.
            if (IsActive())
            {
                UpdateCachedReferences();
                Set(m_value, false);
                // Update rects (in next update) since other things might affect them even if value didn't change.
                m_delayedUpdateVisuals = true;
            }
        }

        /// <summary>
        /// Update the rect based on the delayed update visuals.
        /// Got around issue of calling sendMessage from onValidate.
        /// </summary>
        private void Update()
        {
            if (m_delayedUpdateVisuals)
            {
                m_delayedUpdateVisuals = false;
                UpdateVisuals();
            }
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(UIScrollbar), editorForChildClasses:true)]
    [CanEditMultipleObjects]
    public class UIScrollbarEditor : UISelectableEditor
    {
        protected SerializedProperty p_handleRect;
        protected SerializedProperty p_direction;
        protected SerializedProperty p_value;
        protected SerializedProperty p_size;
        protected SerializedProperty p_numberOfSteps;

        protected override void OnEnable()
        {
            base.OnEnable();

            p_handleRect = serializedObject.FindProperty("m_handleRect");
            p_direction = serializedObject.FindProperty("m_direction");
            p_value = serializedObject.FindProperty("m_value");
            p_size = serializedObject.FindProperty("m_size");
            p_numberOfSteps = serializedObject.FindProperty("m_numberOfSteps");

            m_propertiesToExclude.Add(p_handleRect.propertyPath);
            m_propertiesToExclude.Add(p_direction.propertyPath);
            m_propertiesToExclude.Add(p_value.propertyPath);
            m_propertiesToExclude.Add(p_size.propertyPath);
            m_propertiesToExclude.Add(p_numberOfSteps.propertyPath);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Scrollbar", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            RectTransform newRectTransform = EditorGUILayout.ObjectField("Handle Rect", p_handleRect.objectReferenceValue, typeof(RectTransform), true) as RectTransform;
            if (EditorGUI.EndChangeCheck())
            {
                // Handle Rect will modify its GameObject RectTransform drivenBy, so we need to Record the old and new RectTransform.
                List<UnityEngine.Object> modifiedObjects = new();
                modifiedObjects.Add(newRectTransform);
                foreach (var target in p_handleRect.serializedObject.targetObjects)
                {
                    MonoBehaviour mb = target as MonoBehaviour;
                    if (mb == null)
                        continue;

                    modifiedObjects.Add(mb);
                    modifiedObjects.Add(mb.GetComponent<RectTransform>());
                }
                Undo.RecordObjects(modifiedObjects.ToArray(), "Change Handle Rect");
                p_handleRect.objectReferenceValue = newRectTransform;
            }

            if (p_handleRect.objectReferenceValue != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(p_direction);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(serializedObject.targetObjects, "Change Scrollbar Direction");

                    UIScrollbar.EDirection direction = (UIScrollbar.EDirection)p_direction.enumValueIndex;
                    foreach (var obj in serializedObject.targetObjects)
                    {
                        if (obj is UIScrollbar scrollbar)
                            scrollbar.SetDirection(direction, true);
                    }
                }

                EditorGUILayout.PropertyField(p_value);
                EditorGUILayout.PropertyField(p_size);
                EditorGUILayout.PropertyField(p_numberOfSteps);

                bool warning = false;
                foreach (var obj in serializedObject.targetObjects)
                {
                    if (obj is UIScrollbar scrollbar)
                    {
                        UIScrollbar.EDirection dir = scrollbar.Direction;
                        if (dir == UIScrollbar.EDirection.LeftToRight || dir == UIScrollbar.EDirection.RightToLeft)
                            warning = (scrollbar.navigation.mode != Navigation.Mode.Automatic && scrollbar.navigation.mode != Navigation.Mode.Horizontal && (scrollbar.FindSelectableOnLeft() != null || scrollbar.FindSelectableOnRight() != null));
                        else
                            warning = (scrollbar.navigation.mode != Navigation.Mode.Automatic && scrollbar.navigation.mode != Navigation.Mode.Vertical && (scrollbar.FindSelectableOnDown() != null || scrollbar.FindSelectableOnUp() != null));
                    }
                }

                if (warning)
                    EditorGUILayout.HelpBox("The selected scrollbar direction conflicts with navigation. Not all navigation options may work.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Specify a RectTransform for the scrollbar handle. It must have a parent RectTransform that the handle can slide within.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

#endif

    #endregion
}
