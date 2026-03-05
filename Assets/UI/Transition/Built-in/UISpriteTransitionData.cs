using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Data/Color")]
    public class UISpriteTransitionData : UIGenericTransitionAsset<Sprite, UITransitionPreset<Sprite>>
    {
        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, Sprite value, float duration, IUITransitionParam param)
        {
            return ApplyValueInstant(instance, graphics, value, param);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, Sprite value, IUITransitionParam param)
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

        #region Initialization

        protected override void GetDefaultValueAndDuration(out Sprite value, out float duration)
        {
            value = null;
            duration = 0.1f;
        }

        #endregion

        #region Initial Value

        public override object GetGraphicInitialValue(Graphic graphic)
        {
            return graphic is Image image ? image.overrideSprite : null;
        }

        #endregion
    }
}
