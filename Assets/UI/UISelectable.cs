using System;
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
    }

    public class UISelectable : Selectable
    {
        #region Members

        [Header("References")]
        [SerializeField, ReadOnly] private UINavigationBox m_box;

        #endregion

        #region Properties

        public virtual bool IsPointerInside { get; private set; }
        public virtual bool IsLeftPointerDown { get; private set; }
        public virtual bool IsRightPointerDown { get; private set; }
        public virtual bool HasSelection { get; private set; }

        #endregion

        #region Events

        public event UIStateChangeEvent StateChanged;

        #endregion


        #region Overrides

        // HOVER
        public override sealed void OnPointerEnter(PointerEventData eventData)
        {
            OnBeforePointerEnter(eventData);

            IsPointerInside = true;

            base.OnPointerEnter(eventData);

            StateChanged?.Invoke(EUIStateChangeType.HOVER, true);

            OnAfterPointerEnter(eventData);
        }
        public override sealed void OnPointerExit(PointerEventData eventData)
        {
            OnBeforePointerExit(eventData);

            IsPointerInside = false;

            base.OnPointerExit(eventData);

            StateChanged?.Invoke(EUIStateChangeType.HOVER, false);

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

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                StateChanged?.Invoke(EUIStateChangeType.LEFT_PRESS, true);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                StateChanged?.Invoke(EUIStateChangeType.RIGHT_PRESS, true);
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

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                StateChanged?.Invoke(EUIStateChangeType.LEFT_PRESS, false);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                StateChanged?.Invoke(EUIStateChangeType.RIGHT_PRESS, false);
            }

            OnAfterPointerUp(eventData);
        }

        // SELECT
        public override sealed void OnSelect(BaseEventData eventData)
        {
            OnBeforeSelect(eventData);

            HasSelection = true;

            base.OnSelect(eventData);

            StateChanged?.Invoke(EUIStateChangeType.SELECTION, true);

            OnAfterSelect(eventData);
        }
        public override sealed void OnDeselect(BaseEventData eventData)
        {
            OnBeforeDeselect(eventData);

            HasSelection = false;

            base.OnDeselect(eventData);

            StateChanged?.Invoke(EUIStateChangeType.SELECTION, false);

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

        #endregion


        #region Transitions

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (!gameObject.activeInHierarchy)
                return;

            base.DoStateTransition(state, instant);
        }

        #endregion
    }
}
