using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public class UIStandaloneTransitioner : UITransitioner,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        #region Members

        [SerializeField] protected List<Graphic> m_graphics;
        [SerializeField] protected List<UIGenericTransitionInstance> m_transitionInstances;

        protected FUIState m_lastState;

        #endregion

        #region Properties

        public virtual bool IsPointerInside { get; protected set; }
        public virtual bool IsLeftPointerDown { get; protected set; }
        public virtual bool IsRightPointerDown { get; protected set; }

        #endregion

        #region Process

        public override void UpdateState(FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param)
        {
            var graphics = m_graphics.Where(g => g != null);

            foreach (var instance in m_transitionInstances)
            {
                instance.UpdateState(graphics, oldStates, newStates, instant, param);
            }
        }

        #endregion


        #region Core Behaviour

        protected virtual void OnEnable()
        {
            CheckForStateChange();
        }
        protected virtual void OnDisable()
        {
            CheckForStateChange();
        }

        #endregion

        #region IPointerHandlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsPointerInside = true;
            CheckForStateChange();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsPointerInside = false;
            CheckForStateChange();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                IsLeftPointerDown = true;
                CheckForStateChange();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                IsRightPointerDown = true;
                CheckForStateChange();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                IsLeftPointerDown = false;
                CheckForStateChange();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                IsRightPointerDown = false;
                CheckForStateChange();
            }
        }

        #endregion

        #region Transition

        protected virtual FUIState GetCurrentState()
        {
            FUIState state = 0;

            if (IsPointerInside) state |= FUIState.HIGHLIGHTED;
            if (IsLeftPointerDown || IsRightPointerDown) state |= FUIState.PRESSED;
            if (!enabled) state |= FUIState.DISABLED;

            if (state == 0) return FUIState.NORMAL;
            return state;
        }

        protected virtual void CheckForStateChange()
        {
            var currentState = GetCurrentState();
            if (currentState == m_lastState)
                return;

            UpdateState(m_lastState, currentState, false, GetTransitionParam(m_lastState, currentState));

            m_lastState = currentState;
        }
        protected virtual IUITransitionParam GetTransitionParam(FUIState oldState, FUIState newState)
        {
            return new UIDefaultTransitionParam(this);
        }

        #endregion
    }
}
