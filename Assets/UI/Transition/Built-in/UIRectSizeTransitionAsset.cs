using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Asset/Rect Size")]
    public class UIRectSizeTransitionAsset : UIGenericTransitionAsset<Vector2, UITransitionPreset<Vector2>>
    {
        #region Initial Value

        public override object GetGraphicInitialValue(Graphic graphic)
        {
            return graphic.rectTransform.rect.size;
        }

        #endregion

        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, Vector2 value, float duration, IUITransitionParam param)
        {
            if (instance.Payload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param.MonoBehaviour, tweenPayload.Tweens);
            }

            var tweens = RunTransitionTween<SizeTween, Graphic>(param.MonoBehaviour, graphics, duration, value);

            return new UITransitionTweenPayload(tweens);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, Vector2 value, IUITransitionParam param)
        {
            if (instance.Payload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param.MonoBehaviour, tweenPayload.Tweens);
            }

            foreach (var g in graphics)
            {
                g.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value.x);
                g.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value.y);
            }

            return null;
        }

        #endregion

        #region Preset Initialization

        protected override void GetDefaultValueAndDuration(out Vector2 value, out float duration)
        {
            value = new Vector2(100f, 100f);
            duration = 0.1f;
        }

        #endregion

        #region Tween

        public class SizeTween : UITransitionTween<Vector2, Graphic>
        {
            private Vector2 m_startSize;

            protected override void OnInit(Graphic graphic, Vector2 targetValue)
            {
                m_startSize = graphic.rectTransform.rect.size;
            }

            protected override void Update(Graphic graphic, float normalizedTime, Vector2 targetValue)
            {
                graphic.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Lerp(m_startSize.x, targetValue.x, normalizedTime));
                graphic.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(m_startSize.y, targetValue.y, normalizedTime));
            }

            protected override void OnComplete(Graphic graphic, Vector2 targetValue)
            {
                graphic.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetValue.x);
                graphic.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetValue.y);
            }
        }

        #endregion
    }
}
