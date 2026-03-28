using TMPro;
using UnityEngine;

namespace Dhs5.Utility.UI
{
    public class UIDefaultScrollListItem : UIScrollListItem
    {
        #region Members

        [SerializeField] protected TMP_Text m_text;

        #endregion


        #region Data

        public override void ApplyData(int value, UIScrollList.OptionData optionData)
        {
            if (optionData != null)
            {
                gameObject.SetActive(true);
                m_text.text = optionData.Text;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion


        #region Editor

#if UNITY_EDITOR

        public override void OnValidate()
        {
            base.OnValidate();

            if (m_text == null) m_text = GetComponent<TMP_Text>();
        }

#endif

        #endregion
    }
}
