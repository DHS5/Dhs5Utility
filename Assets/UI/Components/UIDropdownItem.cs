using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public abstract class UIDropdownItem : MonoBehaviour, IPointerEnterHandler, ISelectHandler, ICancelHandler
    {
        #region Members

        [SerializeField, ReadOnly] protected int m_index;
        [SerializeField] protected RectTransform m_rectTransform;

        #endregion

        #region Properties

        public virtual int Index => m_index;
        public virtual RectTransform RectTransform => m_rectTransform;

        public abstract Selectable Selectable { get; }
        public abstract bool IsOn { get; set; }

        #endregion

        #region Events

        public event Action<int> Pressed;
        protected void TriggerPressed()
        {
            Pressed?.Invoke(Index);
        }

        public event Action<int, bool> Selected;
        protected void TriggerSelected(bool navigation)
        {
            Selected?.Invoke(Index, navigation);
        }
        
        public event Action<int> Cancelled;
        protected void TriggerCancelled()
        {
            Cancelled?.Invoke(Index);
        }

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }

        #endregion


        #region Methods

        public virtual void SetupPrevNavigation(UIDropdownItem prevItem)
        {
            if (Selectable != null)
            {
                Selectable.navigation = new Navigation()
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = prevItem != null ? prevItem.Selectable : null,
                    selectOnLeft = null,
                    selectOnRight = null,
                    selectOnDown = Selectable.navigation.selectOnDown,
                    wrapAround = false
                };
            }
        }
        public virtual void SetupNextNavigation(UIDropdownItem nextItem)
        {
            if (Selectable != null)
            {
                Selectable.navigation = new Navigation()
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnDown = nextItem != null ? nextItem.Selectable : null,
                    selectOnLeft = null,
                    selectOnRight = null,
                    selectOnUp = Selectable.navigation.selectOnUp,
                    wrapAround = false
                };
            }
        }
        public virtual void SelectAsFirst()
        {
            if (Selectable != null)
            {
                Selectable.Select();
            }
        }


        public void ApplyData(int index, UIDropdown.OptionData optionData)
        {
            m_index = index;
            OnApplyData(optionData);
        }
        protected abstract void OnApplyData(UIDropdown.OptionData optionData);

        #endregion

        #region IPointerEnterHandler

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            eventData.selectedObject = gameObject;
        }

        #endregion

        #region ISelectHandler

        public void OnSelect(BaseEventData eventData)
        {
            TriggerSelected(eventData is not PointerEventData);
        }

        #endregion

        #region ICancelHandler

        public virtual void OnCancel(BaseEventData eventData)
        {
            TriggerCancelled();
        }

        #endregion


        #region Editor

#if UNITY_EDITOR

        public virtual void OnValidate()
        {
            if (m_rectTransform == null) m_rectTransform = GetComponent<RectTransform>();
        }

#endif

        #endregion
    }
}
