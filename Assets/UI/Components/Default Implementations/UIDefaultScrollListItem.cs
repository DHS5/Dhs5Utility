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

        public override void ApplyData(UIScrollList.OptionData optionData)
        {
            m_text.text = optionData.Text;
        }

        #endregion
    }
}
