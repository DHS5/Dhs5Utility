using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Data/Color")]
    public class UIColorTransitionData : UIGenericTransitionData<Color>
    {
        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(IEnumerable<Graphic> graphics, Color value, float duration, IUITransitionParam param)
        {
            foreach (Graphic g in graphics)
            {
                g.CrossFadeColor(value, duration, true, true);
            }

            return null;
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(IEnumerable<Graphic> graphics, Color value, IUITransitionParam param)
        {
            foreach (Graphic g in graphics)
            {
                g.CrossFadeColor(value, 0f, true, true);
            }

            return null;
        }

        #endregion

        #region Payload Handling

        public override void HandlePreviousPayload(IUIGenericTransitionPayload previousPayload, IUITransitionParam param)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region Initialization

        protected override void OnInitValues()
        {
            m_normalState = new(Color.white, 0.1f);
            m_highlightedState = new(true, Color.white, 0.1f);
            m_pressedState = new(true, Color.white, 0.1f);
            m_selectedState = new(true, Color.white, 0.1f);
            m_disabledState = new(true, Color.white, 0.1f);
        }

        #endregion
    }
}
