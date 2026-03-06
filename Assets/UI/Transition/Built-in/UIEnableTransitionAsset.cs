using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Asset/Enable")]
    public class UIEnableTransitionAsset : UIGenericTransitionAsset<bool, UITransitionPreset<bool>>
    {
        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, bool value, float duration, IUITransitionParam param)
        {
            return ApplyValueInstant(instance, graphics, value, param);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, bool value, IUITransitionParam param)
        {
            foreach (var g in graphics)
            {
                g.enabled = value;
            }

            return null;
        }

        #endregion

        #region Initialization

        protected override void GetDefaultValueAndDuration(out bool value, out float duration)
        {
            value = true;
            duration = 0f;
        }

        #endregion

        #region Initial Value

        public override object GetGraphicInitialValue(Graphic graphic)
        {
            return graphic.enabled;
        }

        #endregion
    }
}
