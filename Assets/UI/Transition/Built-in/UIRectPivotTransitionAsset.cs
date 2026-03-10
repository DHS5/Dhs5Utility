using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Asset/Rect Pivot")]
    public class UIRectPivotTransitionAsset : UIGenericTransitionAsset<Vector2, UITransitionPreset<Vector2>>
    {
        #region Initial Value

        public override object GetGraphicInitialValue(Graphic graphic)
        {
            return graphic.rectTransform.pivot;
        }

        #endregion

        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, Vector2 value, float duration, IUITransitionParam param)
        {
            if (instance.Payload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param.MonoBehaviour, tweenPayload.Tweens);
            }

            var tweens = RunTransitionTween<PivotTween, Graphic>(param.MonoBehaviour, graphics, duration, value);

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
                g.rectTransform.pivot = value;
            }

            return null;
        }

        #endregion

        #region Preset Initialization

        protected override void GetDefaultValueAndDuration(out Vector2 value, out float duration)
        {
            value = new Vector2(0.5f, 0.5f);
            duration = 0.1f;
        }

        #endregion

        #region Tween

        public class PivotTween : UITransitionTween<Vector2, Graphic>
        {
            private Vector2 m_startPivot;

            protected override void OnInit(Graphic graphic, Vector2 targetValue)
            {
                m_startPivot = graphic.rectTransform.pivot;
            }

            protected override void Update(Graphic graphic, float normalizedTime, Vector2 targetValue)
            {
                graphic.rectTransform.pivot = Vector2.Lerp(m_startPivot, targetValue, normalizedTime);
            }

            protected override void OnComplete(Graphic graphic, Vector2 targetValue)
            {
                graphic.rectTransform.pivot = targetValue;
            }
        }

        #endregion
    }
}
