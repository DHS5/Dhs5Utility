using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dhs5.Utility.UI
{
    public class UIButton : UISelectable, 
        IPointerClickHandler, ISubmitHandler, IEventSystemHandler
    {
        #region Events

        public event Action Clicked;

        protected void TriggerClicked()
        {
            UISystemProfilerApi.AddMarker("Button.onClick", this);
            EventContext = this;
            Clicked?.Invoke();
        }

        #endregion


        #region IEventHandlers

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left
                || (UseRightClick() && eventData.button == PointerEventData.InputButton.Right))
            {
                TryPress();
            }
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            TryPress();
            SimulatePress(0.1f);
        }

        #endregion

        #region Press Behaviour

        protected virtual bool TryPress()
        {
            if (CanPress())
            {
                TriggerClicked();
                return true;
            }
            return false;
        }
        protected virtual bool CanPress() => IsActive() && IsInteractable();

        #endregion

        #region Public Simulation

        public virtual bool TrySimulateClick(float duration = 0.1f)
        {
            if (CanSimulateClick() && TryPress())
            {
                SimulatePress(duration);
                return true;
            }
            return false;
        }

        protected virtual bool CanSimulateClick() => true;

        #endregion
    }
}
