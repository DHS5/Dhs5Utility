using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    #region STRUCT RectAnchors

    [Serializable]
    public struct RectAnchors
    {
        public RectAnchors(RectTransform rectTransform)
        {
            anchorMin = rectTransform.anchorMin;
            anchorMax = rectTransform.anchorMax;
        }

        public Vector2 anchorMin;
        public Vector2 anchorMax;

        public void ApplyOn(RectTransform rectTransform)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
        }
    }

    #endregion

    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Asset/Rect Anchors")]
    public class UIRectAnchorTransition : UIGenericTransitionAsset<RectAnchors, UITransitionPreset<RectAnchors>>
    {
        #region Initial Values

        public override object GetGraphicInitialValue(Graphic graphic)
        {
            return new RectAnchors(graphic.rectTransform);
        }

        #endregion

        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, RectAnchors value, float duration, IUITransitionParam param)
        {
            if (instance.Payload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param.MonoBehaviour, tweenPayload.Tweens);
            }

            var tweens = RunTransitionTween<AnchorTween, Graphic>(param.MonoBehaviour, graphics, duration, value);

            return new UITransitionTweenPayload(tweens);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, RectAnchors value, IUITransitionParam param)
        {
            if (instance.Payload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param.MonoBehaviour, tweenPayload.Tweens);
            }

            foreach (var graphic in graphics)
            {
                value.ApplyOn(graphic.rectTransform);
            }

            return null;
        }

        #endregion

        #region Preset Initialization

        protected override void GetDefaultValueAndDuration(out RectAnchors value, out float duration)
        {
            value = new RectAnchors()
            {
                anchorMin = Vector2.zero,
                anchorMax = Vector2.one,
            };
            duration = 0.1f;
        }

        #endregion

        #region Tween

        public class AnchorTween : UITransitionTween<RectAnchors, Graphic>
        {
            private RectAnchors m_startAnchors;

            protected override void OnInit(Graphic graphic, RectAnchors targetValue)
            {
                m_startAnchors = new(graphic.rectTransform);
            }

            protected override void Update(Graphic graphic, float normalizedTime, RectAnchors targetValue)
            {
                graphic.rectTransform.anchorMin = Vector2.Lerp(m_startAnchors.anchorMin, targetValue.anchorMin, normalizedTime);
                graphic.rectTransform.anchorMax = Vector2.Lerp(m_startAnchors.anchorMax, targetValue.anchorMax, normalizedTime);
            }

            protected override void OnComplete(Graphic graphic, RectAnchors targetValue)
            {
                targetValue.ApplyOn(graphic.rectTransform);
            }
        }

        #endregion
    }
}
