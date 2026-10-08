using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Asset/Color")]
    public class UIColorTransitionAsset : UIGenericTransitionAsset<Color, UITransitionPreset<Color>>
    {
        #region Members

        [Tooltip("Whether the alpha of the preset colors is applied.\n" +
            "Disable it to combine this transition with an Alpha transition on the same graphics")]
        [SerializeField] protected bool m_useAlpha = true;

        #endregion

        #region Apply

        // Tweens the CanvasRenderer color like Graphic.CrossFadeColor, but with its own tweens
        // so that it doesn't cancel other transitions on the same graphic (Graphic only has one color tween)

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, Color value, float duration, IUITransitionParam param)
        {
            StopRunningTweens(instance, param);

            var tweens = m_useAlpha ?
                RunTransitionTween<ColorTween, Graphic>(param.MonoBehaviour, graphics, duration, value) :
                RunTransitionTween<ColorRGBTween, Graphic>(param.MonoBehaviour, graphics, duration, value);

            return new UITransitionTweenPayload(tweens);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, Color value, IUITransitionParam param)
        {
            StopRunningTweens(instance, param);

            foreach (Graphic g in graphics)
            {
                if (g == null) continue;

                var color = value;
                if (!m_useAlpha) color.a = g.canvasRenderer.GetAlpha();
                g.canvasRenderer.SetColor(color);
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

        #region Tweens

        public class ColorTween : UITransitionTween<Color, Graphic>
        {
            private Color m_startColor;

            protected override void OnInit(Graphic graphic, Color targetValue)
            {
                m_startColor = graphic.canvasRenderer.GetColor();
            }
            protected override void Update(Graphic graphic, float normalizedTime, Color targetValue)
            {
                graphic.canvasRenderer.SetColor(Color.Lerp(m_startColor, targetValue, normalizedTime));
            }
            protected override void OnComplete(Graphic graphic, Color targetValue)
            {
                graphic.canvasRenderer.SetColor(targetValue);
            }
        }

        /// <summary>
        /// Only tweens RGB, the current alpha is kept every frame
        /// </summary>
        public class ColorRGBTween : UITransitionTween<Color, Graphic>
        {
            private Color m_startColor;

            protected override void OnInit(Graphic graphic, Color targetValue)
            {
                m_startColor = graphic.canvasRenderer.GetColor();
            }
            protected override void Update(Graphic graphic, float normalizedTime, Color targetValue)
            {
                var color = Color.Lerp(m_startColor, targetValue, normalizedTime);
                color.a = graphic.canvasRenderer.GetAlpha();
                graphic.canvasRenderer.SetColor(color);
            }
            protected override void OnComplete(Graphic graphic, Color targetValue)
            {
                targetValue.a = graphic.canvasRenderer.GetAlpha();
                graphic.canvasRenderer.SetColor(targetValue);
            }
        }

        #endregion
    }
}
