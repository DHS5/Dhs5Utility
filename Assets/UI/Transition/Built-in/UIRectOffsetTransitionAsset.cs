using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    #region STRUCT RectOffsets

    [Serializable]
    public struct RectOffsets
    {
        public RectOffsets(RectTransform rectTransform)
        {
            offsetMin = rectTransform.offsetMin;
            offsetMax = rectTransform.offsetMax;
        }

        public Vector2 offsetMin;
        public Vector2 offsetMax;

        public void ApplyOn(RectTransform rectTransform)
        {
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }

    #endregion

    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Asset/Rect Offsets")]
    public class UIRectOffsetTransitionAsset : UIGenericTransitionAsset<RectOffsets, UITransitionPreset<RectOffsets>>
    {
        #region Initial Values

        public override object GetGraphicInitialValue(Graphic graphic)
        {
            return new RectOffsets(graphic.rectTransform);
        }

        #endregion

        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, RectOffsets value, float duration, IUITransitionParam param)
        {
            if (instance.Payload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param.MonoBehaviour, tweenPayload.Tweens);
            }

            var tweens = RunTransitionTween<OffsetTween, Graphic>(param.MonoBehaviour, graphics, duration, value);

            return new UITransitionTweenPayload(tweens);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, RectOffsets value, IUITransitionParam param)
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

        protected override void GetDefaultValueAndDuration(out RectOffsets value, out float duration)
        {
            value = new RectOffsets()
            {
                offsetMin = Vector2.zero,
                offsetMax = Vector2.zero,
            };
            duration = 0.1f;
        }

        #endregion

        #region Tween

        public class OffsetTween : UITransitionTween<RectOffsets, Graphic>
        {
            private RectOffsets m_startOffsets;

            protected override void OnInit(Graphic graphic, RectOffsets targetValue)
            {
                m_startOffsets = new(graphic.rectTransform);
            }

            protected override void Update(Graphic graphic, float normalizedTime, RectOffsets targetValue)
            {
                graphic.rectTransform.offsetMin = Vector2.Lerp(m_startOffsets.offsetMin, targetValue.offsetMin, normalizedTime);
                graphic.rectTransform.offsetMax = Vector2.Lerp(m_startOffsets.offsetMax, targetValue.offsetMax, normalizedTime);
            }

            protected override void OnComplete(Graphic graphic, RectOffsets targetValue)
            {
                targetValue.ApplyOn(graphic.rectTransform);
            }
        }

        #endregion
    }
}
