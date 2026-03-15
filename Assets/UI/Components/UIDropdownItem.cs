using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dhs5.Utility.UI
{
    public abstract class UIDropdownItem : MonoBehaviour, IPointerEnterHandler, ICancelHandler
    {
        #region Members

        [SerializeField] protected RectTransform m_rectTransform;
        //[SerializeField] protected TMP_Text m_text;

        #endregion

        #region Properties

        public RectTransform RectTransform => m_rectTransform;

        #endregion

        #region Events

        public event Action Cancelled;

        protected void TriggerCancelled()
        {
            Cancelled?.Invoke();
        }

        #endregion


        #region Methods

        public abstract void ApplyData(UIDropdown.OptionData optionData);

        #endregion

        #region IPointerEnterHandler

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        #endregion

        #region ICancelHandler

        public virtual void OnCancel(BaseEventData eventData)
        {
            TriggerCancelled();
        }

        #endregion
    }
}
