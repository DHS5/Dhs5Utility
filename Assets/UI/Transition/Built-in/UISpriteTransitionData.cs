using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Data/Color")]
    public class UISpriteTransitionData : UIGenericTransitionData<Sprite>
    {
        #region Apply

        protected override void ApplyValue(IEnumerable<Graphic> graphics, Sprite value, float duration, IUITransitionParam param)
        {
            ApplyValueInstant(graphics, value, param);
        }

        protected override void ApplyValueInstant(IEnumerable<Graphic> graphics, Sprite value, IUITransitionParam param)
        {
            foreach (var g in graphics)
            {
                if (g is Image image)
                {
                    image.overrideSprite = value;
                }
            }
        }

        #endregion

        #region Initialization

        protected override void OnInitValues()
        {
            m_normalState = new(null, 0.1f);
            m_highlightedState = new(true, null, 0.1f);
            m_pressedState = new(true, null, 0.1f);
            m_selectedState = new(true, null, 0.1f);
            m_disabledState = new(true, null, 0.1f);
        }

        #endregion
    }
}
