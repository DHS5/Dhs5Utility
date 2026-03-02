using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Data/Active")]
    public class UIActiveTransitionData : UIGenericTransitionData<bool>
    {
        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(IEnumerable<Graphic> graphics, bool value, float duration, IUITransitionParam param)
        {
            return ApplyValueInstant(graphics, value, param);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(IEnumerable<Graphic> graphics, bool value, IUITransitionParam param)
        {
            foreach (var g in graphics)
            {
                g.gameObject.SetActive(value);
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
            m_normalState = new(true, 0f);
            m_highlightedState = new(true, true, 0f);
            m_pressedState = new(true, true, 0f);
            m_selectedState = new(true, true, 0f);
            m_disabledState = new(true, true, 0f);
        }

        #endregion
    }
}
