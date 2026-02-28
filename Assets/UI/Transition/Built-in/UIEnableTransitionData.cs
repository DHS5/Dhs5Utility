using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Data/Enable")]
    public class UIEnableTransitionData : UIGenericTransitionData<bool>
    {
        #region Apply

        protected override void ApplyValue(IEnumerable<Graphic> graphics, bool value, float duration, IUITransitionParam param)
        {
            ApplyValueInstant(graphics, value, param);
        }

        protected override void ApplyValueInstant(IEnumerable<Graphic> graphics, bool value, IUITransitionParam param)
        {
            foreach (var g in graphics)
            {
                g.enabled = value;
            }
        }

        #endregion

        #region Initialization

        protected override void OnInitValues()
        {
            m_normalState = new(true, 0f);
            m_highlightedState = new(true, true, 0f);
            m_pressedState = new(true, true, 0f);
            m_selectedState = new(true, true, 0f);
            m_disabledState = new(true, true, 0f);
        }

        #endregion
    }
}
