using UnityEngine;

namespace Dhs5.Utility.UI
{
    public abstract class UIScrollListItem : MonoBehaviour
    {
        #region Members

        [SerializeField] protected RectTransform m_rectTransform;

        #endregion

        #region Properties

        public RectTransform RectTransform => m_rectTransform;

        #endregion


        #region Methods

        public abstract void ApplyData(int value, UIScrollList.OptionData optionData);

        #endregion
    }
}
