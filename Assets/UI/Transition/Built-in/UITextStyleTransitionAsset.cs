using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Asset/Text Style")]
    public class UITextStyleTransitionAsset : UIGenericTransitionAsset<FontStyles, UITransitionPreset<FontStyles>>
    {
        #region Initial Values

        public override object GetGraphicInitialValue(Graphic graphic)
        {
            return graphic is TMP_Text text ? text.fontStyle : null;
        }

        #endregion

        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, FontStyles value, float duration, IUITransitionParam param)
        {
            return ApplyValueInstant(instance, graphics, value, param);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, FontStyles value, IUITransitionParam param)
        {
            foreach (var g in graphics)
            {
                if (g is TMP_Text text)
                {
                    text.fontStyle = value;
                }
            }

            return null; 
        }

        #endregion

        #region Preset Initialization

        protected override void GetDefaultValueAndDuration(out FontStyles value, out float duration)
        {
            value = FontStyles.Normal;
            duration = 0.1f;
        }

        #endregion
    }
}
