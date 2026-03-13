using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class UISlider : UISelectable, IDragHandler, IInitializePotentialDragHandler
    {
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

        #region ENUM Axis
        public enum EAxis
        {
            Horizontal = 0,
            Vertical = 1
        }

        #endregion


        #region Members

        [Header("Slider")]
        [SerializeField] protected RectTransform m_fillRect;
        [SerializeField] protected RectTransform m_handleRect;
        [SerializeField] protected EDirection m_direction = EDirection.LeftToRight;
        [SerializeField] protected float m_minValue = 0;
        [SerializeField] protected float m_maxValue = 1;
        [SerializeField] protected bool m_wholeNumbers = false;
        [SerializeField] protected float m_value;

        // Private fields
        protected Image m_fillImage;
        protected Transform m_fillTransform;
        protected RectTransform m_fillContainerRect;
        protected Transform m_handleTransform;
        protected RectTransform m_handleContainerRect;

        // The offset from handle position to mouse down position
        protected Vector2 m_offset = Vector2.zero;

        // field is never assigned warning
#pragma warning disable 649
        protected DrivenRectTransformTracker m_tracker;
#pragma warning restore 649

#if UNITY_EDITOR
        // This "delayed" mechanism is required for case 1037681.
        private bool m_delayedUpdateVisuals = false;
#endif

        #endregion

        #region Properties

        public virtual RectTransform FillRect 
        { 
            get => m_fillRect; 
            set 
            { 
                if (m_fillRect != value) 
                {
                    m_fillRect = value;
                    UpdateCachedReferences(); 
                    UpdateVisuals(); 
                } 
            } 
        }
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

        public virtual float MinValue 
        { 
            get => m_minValue; 
            set 
            { 
                if (m_minValue != value) 
                { 
                    m_minValue = value;
                    Set(m_value); 
                    UpdateVisuals(); 
                } 
            } 
        }
        public virtual float MaxValue 
        { 
            get => m_maxValue; 
            set 
            { 
                if (m_maxValue != value) 
                { 
                    m_maxValue = value;
                    Set(m_value); 
                    UpdateVisuals(); 
                } 
            } 
        }
        public virtual bool WholeNumbers 
        { 
            get => m_wholeNumbers; 
            set 
            { 
                if (m_wholeNumbers != value) 
                { 
                    m_wholeNumbers = value;
                    Set(m_value); 
                    UpdateVisuals(); 
                } 
            } 
        }
        public virtual float Value
        {
            get => WholeNumbers ? Mathf.Round(m_value) : m_value;
            set => Set(value);
        }
        public virtual float NormalizedValue
        {
            get
            {
                if (Mathf.Approximately(MinValue, MaxValue))
                    return 0;
                return Mathf.InverseLerp(MinValue, MaxValue, Value);
            }
            set
            {
                this.Value = Mathf.Lerp(MinValue, MaxValue, value);
            }
        }

        public EAxis Axis => (m_direction == EDirection.LeftToRight || m_direction == EDirection.RightToLeft) ? EAxis.Horizontal : EAxis.Vertical;
        public bool ReverseValue => m_direction == EDirection.RightToLeft || m_direction == EDirection.TopToBottom;
        public virtual float StepSize => WholeNumbers ? 1 : (MaxValue - MinValue) * 0.1f;

        #endregion

        #region Events

        public event Action<float> ValueChanged;

        protected void TriggerValueChanged()
        {
            UISystemProfilerApi.AddMarker("Slider.value", this);
            EventContext = this;
            ValueChanged?.Invoke(m_value);
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

#if UNITY_EDITOR
            Undo.undoRedoEvent -= OnUndoRedoEvent;
            Undo.undoRedoEvent += OnUndoRedoEvent;
#endif
        }

        protected override void OnDisable()
        {
#if UNITY_EDITOR
            Undo.undoRedoEvent -= OnUndoRedoEvent;
#endif
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

        /// <summary>
        /// Set the value of the slider.
        /// </summary>
        /// <param name="input">The new value for the slider.</param>
        /// <param name="triggerEvent">If the OnValueChanged callback should be invoked.</param>
        /// <remarks>
        /// Process the input to ensure the value is between min and max value. If the input is different set the value and send the callback is required.
        /// </remarks>
        protected virtual void Set(float input, bool triggerEvent = true)
        {
            // Clamp the input
            float newValue = ClampValue(input);

            // If the stepped value doesn't match the last one, it's time to update
            if (m_value == newValue)
                return;

            m_value = newValue;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            UpdateVisuals();
            if (triggerEvent)
            {
                TriggerValueChanged();
            }
        }

        /// <summary>
        /// Set the value of the slider without invoking onValueChanged callback.
        /// </summary>
        /// <param name="input">The new value for the slider.</param>
        public virtual void SetValueWithoutNotify(float input)
        {
            Set(input, false);
        }

        #endregion

        #region Setters

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

        #region Visual & Ref Update

        // Force-update the slider. Useful if you've changed the properties and want it to update visually.
        protected virtual void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif

            m_tracker.Clear();

            if (m_fillContainerRect != null)
            {
                m_tracker.Add(this, m_fillRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                if (m_fillImage != null && m_fillImage.type == Image.Type.Filled)
                {
                    m_fillImage.fillAmount = NormalizedValue;
                }
                else
                {
                    if (ReverseValue)
                        anchorMin[(int)Axis] = 1 - NormalizedValue;
                    else
                        anchorMax[(int)Axis] = NormalizedValue;
                }

                m_fillRect.anchorMin = anchorMin;
                m_fillRect.anchorMax = anchorMax;
            }

            if (m_handleContainerRect != null)
            {
                m_tracker.Add(this, m_handleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;
                anchorMin[(int)Axis] = anchorMax[(int)Axis] = (ReverseValue ? (1 - NormalizedValue) : NormalizedValue);
                m_handleRect.anchorMin = anchorMin;
                m_handleRect.anchorMax = anchorMax;
            }
        }

        protected virtual void UpdateCachedReferences()
        {
            if (m_fillRect && m_fillRect != (RectTransform)transform)
            {
                m_fillTransform = m_fillRect.transform;
                m_fillImage = m_fillRect.GetComponent<Image>();
                if (m_fillTransform.parent != null)
                    m_fillContainerRect = m_fillTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                m_fillRect = null;
                m_fillContainerRect = null;
                m_fillImage = null;
            }

            if (m_handleRect && m_handleRect != (RectTransform)transform)
            {
                m_handleTransform = m_handleRect.transform;
                if (m_handleTransform.parent != null)
                    m_handleContainerRect = m_handleTransform.parent.GetComponent<RectTransform>();
            }
            else
            {
                m_handleRect = null;
                m_handleContainerRect = null;
            }
        }

        #endregion

        #region Utility

        protected virtual float ClampValue(float input)
        {
            float newValue = Mathf.Clamp(input, MinValue, MaxValue);
            if (WholeNumbers)
                newValue = Mathf.Round(newValue);
            return newValue;
        }

        #endregion


        #region Drag

        protected virtual bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }
        
        protected override void OnAfterPointerDown(PointerEventData eventData)
        {
            base.OnAfterPointerDown(eventData);

            if (!MayDrag(eventData))
                return;

            m_offset = Vector2.zero;
            if (m_handleContainerRect != null && RectTransformUtility.RectangleContainsScreenPoint(m_handleRect, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                Vector2 localMousePos;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_handleRect, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos))
                    m_offset = localMousePos;
            }
            else
            {
                // Outside the slider handle - jump to this point instead
                UpdateDrag(eventData, eventData.pressEventCamera);
            }
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;

            UpdateDrag(eventData, eventData.pressEventCamera);
        }

        // Update the slider's position based on the mouse.
        protected virtual void UpdateDrag(PointerEventData eventData, Camera cam)
        {
            RectTransform clickRect = m_handleContainerRect ?? m_fillContainerRect;
            if (clickRect != null && clickRect.rect.size[(int)Axis] > 0)
            {
                Vector2 position = Vector2.zero;
                if (!MultipleDisplayUtilities.GetRelativeMousePositionForDrag(eventData, ref position))
                    return;

                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, position, cam, out localCursor))
                    return;
                localCursor -= clickRect.rect.position;

                float val = Mathf.Clamp01((localCursor - m_offset)[(int)Axis] / clickRect.rect.size[(int)Axis]);
                NormalizedValue = (ReverseValue ? 1f - val : val);
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

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    if (Axis == EAxis.Horizontal && FindSelectableOnLeft() == null)
                        Set(ReverseValue ? Value + StepSize : Value - StepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Right:
                    if (Axis == EAxis.Horizontal && FindSelectableOnRight() == null)
                        Set(ReverseValue ? Value - StepSize : Value + StepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Up:
                    if (Axis == EAxis.Vertical && FindSelectableOnUp() == null)
                        Set(ReverseValue ? Value - StepSize : Value + StepSize);
                    else
                        base.OnMove(eventData);
                    break;
                case MoveDirection.Down:
                    if (Axis == EAxis.Vertical && FindSelectableOnDown() == null)
                        Set(ReverseValue ? Value + StepSize : Value - StepSize);
                    else
                        base.OnMove(eventData);
                    break;
            }
        }

        public override Selectable FindSelectableOnLeft()
        {
            if (navigation.mode == Navigation.Mode.Automatic && Axis == EAxis.Horizontal)
                return null;
            return base.FindSelectableOnLeft();
        }

        public override Selectable FindSelectableOnRight()
        {
            if (navigation.mode == Navigation.Mode.Automatic && Axis == EAxis.Horizontal)
                return null;
            return base.FindSelectableOnRight();
        }

        public override Selectable FindSelectableOnUp()
        {
            if (navigation.mode == Navigation.Mode.Automatic && Axis == EAxis.Vertical)
                return null;
            return base.FindSelectableOnUp();
        }

        public override Selectable FindSelectableOnDown()
        {
            if (navigation.mode == Navigation.Mode.Automatic && Axis == EAxis.Vertical)
                return null;
            return base.FindSelectableOnDown();
        }

        #endregion


        #region Editor

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();

            if (WholeNumbers)
            {
                m_minValue = Mathf.Round(m_minValue);
                m_maxValue = Mathf.Round(m_maxValue);
            }

            //Onvalidate is called before OnEnabled. We need to make sure not to touch any other objects before OnEnable is run.
            if (IsActive())
            {
                UpdateCachedReferences();
                // Update rects in next update since other things might affect them even if value didn't change.
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
                Set(m_value, false);
                UpdateVisuals();
            }
        }

        void OnUndoRedoEvent(in UndoRedoInfo undo)
        {
            UpdateVisuals();
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(UISlider), editorForChildClasses:true)]
    [CanEditMultipleObjects]
    /// <summary>
    /// Custom Editor for the Slider Component.
    /// Extend this class to write a custom editor for a component derived from Slider.
    /// </summary>
    public class UISliderEditor : UISelectableEditor
    {
        SerializedProperty p_direction;
        SerializedProperty p_fillRect;
        SerializedProperty p_handleRect;
        SerializedProperty p_minValue;
        SerializedProperty p_maxValue;
        SerializedProperty p_wholeNumbers;
        SerializedProperty p_value;

        protected override void OnEnable()
        {
            base.OnEnable();

            p_fillRect = serializedObject.FindProperty("m_fillRect");
            p_handleRect = serializedObject.FindProperty("m_handleRect");
            p_direction = serializedObject.FindProperty("m_direction");
            p_minValue = serializedObject.FindProperty("m_minValue");
            p_maxValue = serializedObject.FindProperty("m_maxValue");
            p_wholeNumbers = serializedObject.FindProperty("m_wholeNumbers");
            p_value = serializedObject.FindProperty("m_value");

            m_propertiesToExclude.Add(p_direction.propertyPath);
            m_propertiesToExclude.Add(p_minValue.propertyPath);
            m_propertiesToExclude.Add(p_maxValue.propertyPath);
            m_propertiesToExclude.Add(p_wholeNumbers.propertyPath);
            m_propertiesToExclude.Add(p_value.propertyPath);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            if (p_fillRect.objectReferenceValue != null || p_handleRect.objectReferenceValue != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(p_direction);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(serializedObject.targetObjects, "Change Slider Direction");

                    UISlider.EDirection direction = (UISlider.EDirection)p_direction.enumValueIndex;
                    foreach (var obj in serializedObject.targetObjects)
                    {
                        if (obj is UISlider slider)
                            slider.SetDirection(direction, true);
                    }
                }

                EditorGUI.BeginChangeCheck();
                float newMin = EditorGUILayout.FloatField("Min Value", p_minValue.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    if (p_wholeNumbers.boolValue ? Mathf.Round(newMin) < p_maxValue.floatValue : newMin < p_maxValue.floatValue)
                    {
                        p_minValue.floatValue = newMin;
                        if (p_value.floatValue < newMin)
                            p_value.floatValue = newMin;
                    }
                }

                EditorGUI.BeginChangeCheck();
                float newMax = EditorGUILayout.FloatField("Max Value", p_maxValue.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    if (p_wholeNumbers.boolValue ? Mathf.Round(newMax) > p_minValue.floatValue : newMax > p_minValue.floatValue)
                    {
                        p_maxValue.floatValue = newMax;
                        if (p_value.floatValue > newMax)
                            p_value.floatValue = newMax;
                    }
                }

                EditorGUILayout.PropertyField(p_wholeNumbers);

                bool areMinMaxEqual = (p_minValue.floatValue == p_maxValue.floatValue);

                if (areMinMaxEqual)
                    EditorGUILayout.HelpBox("Min Value and Max Value cannot be equal.", MessageType.Warning);

                if (p_wholeNumbers.boolValue)
                    p_value.floatValue = Mathf.Round(p_value.floatValue);

                EditorGUI.BeginDisabledGroup(areMinMaxEqual);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.Slider(p_value, p_minValue.floatValue, p_maxValue.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    // Apply the change before sending the event
                    serializedObject.ApplyModifiedProperties();
                }
                EditorGUI.EndDisabledGroup();

                bool warning = false;
                foreach (var obj in serializedObject.targetObjects)
                {
                    if (obj is UISlider slider)
                    {
                        UISlider.EDirection dir = slider.Direction;
                        if (dir == UISlider.EDirection.LeftToRight || dir == UISlider.EDirection.RightToLeft)
                            warning = (slider.navigation.mode != Navigation.Mode.Automatic && (slider.FindSelectableOnLeft() != null || slider.FindSelectableOnRight() != null));
                        else
                            warning = (slider.navigation.mode != Navigation.Mode.Automatic && (slider.FindSelectableOnDown() != null || slider.FindSelectableOnUp() != null));
                    }
                }

                if (warning)
                    EditorGUILayout.HelpBox("The selected slider direction conflicts with navigation. Not all navigation options may work.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("Specify a RectTransform for the slider fill or the slider handle or both. Each must have a parent RectTransform that it can slide within.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

#endif

    #endregion
}
