using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Asset/Alpha")]
    public class UIAlphaTransitionAsset : UIGenericTransitionAsset<float, UITransitionPreset<float>>
    {
        #region Apply

        // Tweens the CanvasRenderer alpha like Graphic.CrossFadeAlpha, but with its own tweens
        // so that it doesn't cancel other transitions on the same graphic (Graphic only has one color tween)

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, float value, float duration, IUITransitionParam param)
        {
            StopRunningTweens(instance, param);

            var tweens = RunTransitionTween<AlphaTween, Graphic>(param.MonoBehaviour, graphics, duration, value);

            return new UITransitionTweenPayload(tweens);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, float value, IUITransitionParam param)
        {
            StopRunningTweens(instance, param);

            foreach (Graphic g in graphics)
            {
                if (g != null) g.canvasRenderer.SetAlpha(value);
            }

            return new UITransitionTweenPayload(null);
        }

        protected void StopRunningTweens(UIGenericTransitionInstance instance, IUITransitionParam param)
        {
            if (instance.Payload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param?.MonoBehaviour, tweenPayload.Tweens);
            }
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

        #region Tween

        /// <summary>
        /// Only tweens the alpha, the current RGB is kept every frame
        /// </summary>
        public class AlphaTween : UITransitionTween<float, Graphic>
        {
            private float m_startAlpha;

            protected override void OnInit(Graphic graphic, float targetValue)
            {
                m_startAlpha = graphic.canvasRenderer.GetAlpha();
            }
            protected override void Update(Graphic graphic, float normalizedTime, float targetValue)
            {
                graphic.canvasRenderer.SetAlpha(Mathf.Lerp(m_startAlpha, targetValue, normalizedTime));
            }
            protected override void OnComplete(Graphic graphic, float targetValue)
            {
                graphic.canvasRenderer.SetAlpha(targetValue);
            }
        }

        #endregion
    }
}
