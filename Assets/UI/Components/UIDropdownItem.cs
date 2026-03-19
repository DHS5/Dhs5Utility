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

        public event Action<int> Selected;
        protected void TriggerSelected()
        {
            Selected?.Invoke(Index);
        }
        
        public event Action<int> Cancelled;
        protected void TriggerCancelled()
        {
            Cancelled?.Invoke(Index);
        }

        #endregion


        #region Methods

        public abstract void SetupPrevNavigation(UIDropdownItem prevItem);
        public abstract void SetupNextNavigation(UIDropdownItem nextItem);
        public abstract void SelectAsFirst();


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
            TriggerSelected();
        }

        #endregion

        #region ICancelHandler

        public virtual void OnCancel(BaseEventData eventData)
        {
            TriggerCancelled();
        }

        #endregion
    }

    public class UIDefaultDropdownItem : UIDropdownItem
    {
        #region Members

        [SerializeField] protected UIToggle m_toggle;
        [SerializeField] protected TMP_Text m_text;

        #endregion

        #region Properties

        public override Selectable Selectable => m_toggle;
        public override bool IsOn 
        { 
            get => m_toggle.IsOn;
            set
            {
                if (value != m_toggle.IsOn) m_toggle.SetIsOnWithoutNotify(value);
            }
        }

        #endregion

        #region Methods

        public override void SelectAsFirst()
        {
            m_toggle.Select();
        }

        public override void SetupNextNavigation(UIDropdownItem nextItem)
        {
            m_toggle.navigation = new Navigation()
            {
                mode = Navigation.Mode.Explicit,
                selectOnDown = nextItem.Selectable,
                selectOnLeft = null,
                selectOnRight = null,
                selectOnUp = m_toggle.navigation.selectOnUp,
                wrapAround = false
            };
        }

        public override void SetupPrevNavigation(UIDropdownItem prevItem)
        {
            m_toggle.navigation = new Navigation()
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = prevItem.Selectable,
                selectOnLeft = null,
                selectOnRight = null,
                selectOnDown = m_toggle.navigation.selectOnDown,
                wrapAround = false
            };
        }

        protected override void OnApplyData(UIDropdown.OptionData optionData)
        {
            m_text.text = optionData.Text;
        }

        #endregion
    }
}
