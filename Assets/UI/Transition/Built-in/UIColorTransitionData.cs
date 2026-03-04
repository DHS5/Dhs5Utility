using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Data/Color")]
    public class UIColorTransitionData : UIGenericTransitionData<Color, UITransitionPreset<Color>>
    {
        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, Color value, float duration, IUITransitionParam param)
        {
            foreach (Graphic g in graphics)
            {
                g.CrossFadeColor(value, duration, true, true);
            }

            return null;
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, Color value, IUITransitionParam param)
        {
            foreach (Graphic g in graphics)
            {
                g.CrossFadeColor(value, 0f, true, true);
            }

            return null;
        }

        #endregion

        #region Initialization

        protected override void GetDefaultValueAndDuration(out Color value, out float duration)
        {
            value = Color.white;
            duration = 0.1f;
        }

        #endregion

        #region Initial Value

        public override object GetGraphicInitialValue(Graphic graphic)
        {
            return graphic.color;
        }

        #endregion
    }
}
