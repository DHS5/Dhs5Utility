using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Data/Color")]
    public class UITextSizeTransitionData : UIGenericTransitionData<float>
    {
        // TODO :
        // - generalize tweening of values like Unity
        // - think about applying offsets on start value rather than hard setting values
        // in this case, maybe hide normal state

        #region Apply

        protected override void ApplyValue(IEnumerable<Graphic> graphics, float value, float duration, IUITransitionParam param)
        {
            
        }

        protected override void ApplyValueInstant(IEnumerable<Graphic> graphics, float value, IUITransitionParam param)
        {
            foreach (var g in graphics)
            {
                if (g is TMP_Text text)
                {
                    text.fontSize = value;
                }
            }
        }

        #endregion

        #region Initialization

        protected override void OnInitValues()
        {
            m_normalState = new(22f, 0.1f);
            m_highlightedState = new(true, 22f, 0.1f);
            m_pressedState = new(true, 22f, 0.1f);
            m_selectedState = new(true, 22f, 0.1f);
            m_disabledState = new(true, 22f, 0.1f);
        }

        #endregion
    }
}
