using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public delegate void UIStateChangeEvent(EUIStateChangeType type, bool value);

    public enum EUIStateChangeType
    {
        NONE = 0,
        HOVER = 1,
        LEFT_PRESS = 2,
        RIGHT_PRESS = 3,
        SELECTION = 4,
        INTERACTABLE = 5,
    }

    public class UISelectable : Selectable, IUIBoxable
    {
        #region Members

        [SerializeField] protected List<UITransitioner> m_transitioners;

        private UINavBox m_box;
        private bool m_interactable;

        private bool m_transitionInitialized;
        protected FUIState m_lastState;

        #endregion

        #region Properties

        public UINavBox Box
        {
            get => m_box;
            set
            {
                if (m_box != value)
                {
                    m_box = value;
                    OnSetParentBox(value);
                }
            }
        }

        public virtual bool IsPointerInside { get; protected set; }
        public virtual bool IsLeftPointerDown { get; protected set; }
        public virtual bool IsRightPointerDown { get; protected set; }
        public virtual bool HasSelection { get; protected set; }

        public virtual bool IsSimulatingClick { get; protected set; }

        #endregion

        #region Events

        public event UIStateChangeEvent StateChanged;

        protected virtual void TriggerStateChanged(EUIStateChangeType type, bool value)
        {
            UISystemProfilerApi.AddMarker("Selectable.StateChanged", this);
            EventContext = this;
            StateChanged?.Invoke(type, value);
        }

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            m_interactable = interactable;
        }

        #endregion


        #region Overrides

        // HOVER
        public override sealed void OnPointerEnter(PointerEventData eventData)
        {
            OnBeforePointerEnter(eventData);

            IsPointerInside = true;

            base.OnPointerEnter(eventData);
            if (IsActive() && !IsInteractable())
            {
                DoStateTransition(SelectionState.Disabled, false);
            }

            TriggerStateChanged(EUIStateChangeType.HOVER, true);

            OnAfterPointerEnter(eventData);
        }
        public override sealed void OnPointerExit(PointerEventData eventData)
        {
            OnBeforePointerExit(eventData);

            IsPointerInside = false;

            base.OnPointerExit(eventData);
            if (IsActive() && !IsInteractable())
            {
                DoStateTransition(SelectionState.Disabled, false);
            }

            TriggerStateChanged(EUIStateChangeType.HOVER, false);

            OnAfterPointerExit(eventData);
        }

        // PRESS
        public override sealed void OnPointerDown(PointerEventData eventData)
        {
            OnBeforePointerDown(eventData);

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                IsLeftPointerDown = true;
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                IsRightPointerDown = true;
            }

            base.OnPointerDown(eventData);
            if (IsActive() && !IsInteractable())
            {
                DoStateTransition(SelectionState.Disabled, false);
            }
            else if (eventData.button == PointerEventData.InputButton.Right
                && UseRightClick())
            {
                DoStateTransition(SelectionState.Pressed, false);
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                TriggerStateChanged(EUIStateChangeType.LEFT_PRESS, true);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                TriggerStateChanged(EUIStateChangeType.RIGHT_PRESS, true);
            }

            OnAfterPointerDown(eventData);
        }
        public override sealed void OnPointerUp(PointerEventData eventData)
        {
            OnBeforePointerUp(eventData);

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                IsLeftPointerDown = false;
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                IsRightPointerDown = false;
            }

            base.OnPointerUp(eventData);
            if (IsActive() && !IsInteractable())
            {
                DoStateTransition(SelectionState.Disabled, false);
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                TriggerStateChanged(EUIStateChangeType.LEFT_PRESS, false);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                TriggerStateChanged(EUIStateChangeType.RIGHT_PRESS, false);
            }

            OnAfterPointerUp(eventData);
        }

        // SELECT
        public override sealed void OnSelect(BaseEventData eventData)
        {
            OnBeforeSelect(eventData);

            HasSelection = true;

            base.OnSelect(eventData);
            if (IsActive() && !IsInteractable())
            {
                DoStateTransition(SelectionState.Disabled, false);
            }

            TriggerStateChanged(EUIStateChangeType.SELECTION, true);

            OnAfterSelect(eventData);
        }
        public override sealed void OnDeselect(BaseEventData eventData)
        {
            CheckInteractabilityChange();

            OnBeforeDeselect(eventData);

            HasSelection = false;

            base.OnDeselect(eventData);
            if (IsActive() && !IsInteractable())
            {
                DoStateTransition(SelectionState.Disabled, false);
            }

            TriggerStateChanged(EUIStateChangeType.SELECTION, false);

            OnAfterDeselect(eventData);
        }

        #endregion

        #region Virtuals

        // HOVER
        protected virtual void OnBeforePointerEnter(PointerEventData eventData) { }
        protected virtual void OnAfterPointerEnter(PointerEventData eventData) { }
        protected virtual void OnBeforePointerExit(PointerEventData eventData) { }
        protected virtual void OnAfterPointerExit(PointerEventData eventData) { }

        // PRESS
        protected virtual void OnBeforePointerDown(PointerEventData eventData) { }
        protected virtual void OnAfterPointerDown(PointerEventData eventData) { }
        protected virtual void OnBeforePointerUp(PointerEventData eventData) { }
        protected virtual void OnAfterPointerUp(PointerEventData eventData) { }

        // SELECT
        protected virtual void OnBeforeSelect(BaseEventData eventData) { }
        protected virtual void OnAfterSelect(BaseEventData eventData) { }
        protected virtual void OnBeforeDeselect(BaseEventData eventData) { }
        protected virtual void OnAfterDeselect(BaseEventData eventData) { }

        // INTERACTABLE
        protected virtual void OnBecameInteractable() { }
        protected virtual void OnBecameUninteractable() { }

        #endregion


        #region Interactability Tracking

        protected void CheckInteractabilityChange()
        {
            if (m_interactable != IsInteractable())
            {
                m_interactable = !m_interactable;

                if (m_interactable) OnBecameInteractable();
                else OnBecameUninteractable();

                TriggerStateChanged(EUIStateChangeType.INTERACTABLE, m_interactable);
            }
        }

        #endregion

        #region Transitions

        protected virtual FUIState GetCurrentState()
        {
            FUIState state = 0;

            if (IsPointerInside) state |= FUIState.HIGHLIGHTED;
            if (IsLeftPointerDown || IsSimulatingClick || (IsRightPointerDown && UseRightClick())) state |= FUIState.PRESSED;
            if (HasSelection) state |= FUIState.SELECTED;
            if (!IsInteractable()) state |= FUIState.DISABLED;

            if (state == 0) return FUIState.NORMAL;
            return state;
        }
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            CheckInteractabilityChange();

            if (!gameObject.activeInHierarchy)
                return;

            if (transition != Transition.None)
            {
                base.DoStateTransition(state, instant);
            }

            if (!m_transitioners.IsValid())
                return;

            if (!m_transitionInitialized)
            {
                m_lastState = 0;
                m_transitioners = m_transitioners.Where(t => t != null).ToList();
                m_transitioners.Sort();
                m_transitionInitialized = true;
            }

            var currentState = GetCurrentState();
            if (currentState == m_lastState)
                return;

            ApplyTransitions(currentState, instant);

            m_lastState = currentState;
        }
        protected virtual void ApplyTransitions(FUIState newState, bool instant)
        {
            foreach (var transitioner in m_transitioners)
            {
                if (transitioner != null && transitioner.enabled)
                {
                    transitioner.UpdateState(m_lastState, newState, instant, GetTransitionParam(transitioner, m_lastState, newState));
                }
            }
        }
        protected virtual IUITransitionParam GetTransitionParam(UITransitioner transitioner, FUIState oldState, FUIState newState) 
        { 
            return new UIDefaultTransitionParam(transitioner);
        }

        protected override void InstantClearState()
        {
            base.InstantClearState();

            IsPointerInside = false;
            IsLeftPointerDown = false;
            IsRightPointerDown = false;
            HasSelection = false;
            IsSimulatingClick = false;

            if (m_transitioners.IsValid())
            {
                ApplyTransitions(FUIState.NORMAL, true);
            }
        }

        #endregion

        #region Navigation

        public override void OnMove(AxisEventData eventData)
        {
            Selectable nextSelection = null;

            switch (eventData.moveDir)
            {
                case MoveDirection.Right:
                    nextSelection = FindSelectableOnRight();
                    break;

                case MoveDirection.Up:
                    nextSelection = FindSelectableOnUp();
                    break;

                case MoveDirection.Left:
                    nextSelection = FindSelectableOnLeft();
                    break;

                case MoveDirection.Down:
                    nextSelection = FindSelectableOnDown();
                    break;
            }

            if (!Navigate(eventData, nextSelection)
                && Box != null)
            {
                Navigate(eventData, Box.FindSelectableOnChildFailed(this, eventData));
            }
        }

        protected bool Navigate(AxisEventData eventData, Selectable sel)
        {
            if (sel != null && sel.IsActive())
            {
                eventData.selectedObject = sel.gameObject;
                return true;
            }

            return false;
        }

        #endregion

        #region Press Simulation

        protected Coroutine m_simulationCoroutine;

        protected virtual void SimulatePress(float duration)
        {
            if (!IsActive()) return;

            StopSimulationCoroutine();

            IsSimulatingClick = true;
            DoStateTransition(SelectionState.Pressed, instant: false);
            StartSimulationCoroutine(duration);
        }

        protected virtual void StartSimulationCoroutine(float duration)
        {
            m_simulationCoroutine = StartCoroutine(SimulationCoroutine(duration));
        }
        protected virtual IEnumerator SimulationCoroutine(float duration)
        {
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            IsSimulatingClick = false;
            DoStateTransition(base.currentSelectionState, instant: false);
        }
        protected void StopSimulationCoroutine()
        {
            if (m_simulationCoroutine != null)
            {
                StopCoroutine(m_simulationCoroutine);
                m_simulationCoroutine = null;
            }
        }

        #endregion

        #region Settings

        protected virtual bool UseRightClick() => GlobalUseRightClick;

        #endregion


        #region Box

        protected virtual void OnSetParentBox(UINavBox box) { }

        #endregion


        // --- STATIC ---

        #region Settings

        public static bool GlobalUseRightClick { get; set; } = false;

        #endregion

        #region Event Context

        public static UISelectable EventContext { get; protected set; }

        #endregion
    }
}
