using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dhs5.Utility.UI
{
    public class UIButton : UISelectable, 
        IPointerClickHandler, ISubmitHandler, IEventSystemHandler
    {
        #region Members

        [Header("Parameters")]
        [Tooltip("Whether right click is considered as a click by this button")]
        [SerializeField] protected bool m_acceptRightClick = false;
        [Tooltip("Duration of a simulated press")]
        [SerializeField] protected float m_simulationPressDuration = 0.1f;

        #endregion

        #region Properties

        public virtual bool IsSimulatingClick { get; protected set; }
        public override bool IsLeftPointerDown 
        { 
            get => base.IsLeftPointerDown || IsSimulatingClick; 
            protected set => base.IsLeftPointerDown = value; 
        }

        public virtual bool AcceptRightClick
        {
            get => m_acceptRightClick;
            set => m_acceptRightClick = value;
        }
        protected override bool ConsiderRightPressAsTransitionPressed() => AcceptRightClick;
        public virtual float SimulationPressDuration
        {
            get => m_simulationPressDuration;
            set => m_simulationPressDuration = value;
        }

        #endregion

        #region Events

        public event Action OnClick;

        #endregion


        #region IEventHandlers

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left
                || (AcceptRightClick && eventData.button == PointerEventData.InputButton.Right))
            {
                TryPress();
            }
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            if (TryPress())
            {
                SimulatePress(SimulationPressDuration);
            }
        }

        #endregion

        #region Press Behaviour

        protected virtual bool TryPress()
        {
            if (CanPress())
            {
                UISystemProfilerApi.AddMarker("Button.onClick", this);
                EventContext = this;
                OnClick.Invoke();
                return true;
            }
            return false;
        }
        protected virtual bool CanPress() => IsActive() && IsInteractable();

        #endregion

        #region Simulation Behaviour

        protected Coroutine m_simulationCoroutine;

        protected virtual void SimulatePress(float duration)
        {
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

        #region Public Simulation

        public virtual bool TrySimulateClick(float duration)
        {
            if (CanSimulateClick() && TryPress())
            {
                SimulatePress(duration);
                return true;
            }
            return false;
        }
        public virtual bool TrySimulateClick() => TrySimulateClick(SimulationPressDuration);

        protected virtual bool CanSimulateClick() => true;

        #endregion
    }
}
