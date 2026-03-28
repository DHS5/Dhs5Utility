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


        #region Data

        public abstract void ApplyData(int value, UIScrollList.OptionData optionData);

        #endregion

        #region Rect

        public virtual Vector2 GetSize()
        {
            return RectTransform.rect.size;
        }
        public virtual Vector2 GetOffset()
        {
            return RectTransform.anchoredPosition;
        }

        public virtual void Offset(Vector2 offset)
        {
            RectTransform.anchoredPosition = offset;
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
