using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
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

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            m_toggle.ValueChanged += OnValueChanged;
        }
        protected override void OnDisable()
        {
            base.OnDisable();

            m_toggle.ValueChanged -= OnValueChanged;
        }

        #endregion


        #region Methods

        protected override void OnApplyData(UIDropdown.OptionData optionData)
        {
            m_text.text = optionData.Text;
        }

        #endregion

        #region Callbacks

        protected virtual void OnValueChanged(bool _)
        {
            TriggerPressed();
        }

        #endregion


        #region Editor

#if UNITY_EDITOR

        public override void OnValidate()
        {
            base.OnValidate();

            if (m_toggle == null) m_toggle = GetComponent<UIToggle>();
            if (m_text == null) m_text = GetComponentInChildren<TMP_Text>();
        }

#endif

        #endregion
    }
}
