using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Data/Color")]
    public class UISpriteTransitionData : UIGenericTransitionData<Sprite>
    {
        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(IEnumerable<Graphic> graphics, Sprite value, float duration, IUITransitionParam param)
        {
            return ApplyValueInstant(graphics, value, param);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(IEnumerable<Graphic> graphics, Sprite value, IUITransitionParam param)
        {
            foreach (var g in graphics)
            {
                if (g is Image image)
                {
                    image.overrideSprite = value;
                }
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
            m_normalState = new(null, 0.1f);
            m_highlightedState = new(true, null, 0.1f);
            m_pressedState = new(true, null, 0.1f);
            m_selectedState = new(true, null, 0.1f);
            m_disabledState = new(true, null, 0.1f);
        }

        #endregion
    }
}
