using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Asset/Alpha")]
    public class UIAlphaTransitionAsset : UIGenericTransitionAsset<float, UITransitionPreset<float>>
    {
        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, float value, float duration, IUITransitionParam param)
        {
            foreach (Graphic g in graphics)
            {
                g.CrossFadeAlpha(value, duration, true);
            }

            return null;
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, float value, IUITransitionParam param)
        {
            foreach (Graphic g in graphics)
            {
                g.CrossFadeAlpha(value, 0f, true);
            }

            return null;
        }

        #endregion

        #region Initialization

        protected override void GetDefaultValueAndDuration(out float value, out float duration)
        {
            value = 1f;
            duration = 0.1f;
        }

        #endregion

        #region Initial Value

        public override object GetGraphicInitialValue(Graphic graphic)
        {
            return graphic.color.a;
        }

        #endregion
    }
}
