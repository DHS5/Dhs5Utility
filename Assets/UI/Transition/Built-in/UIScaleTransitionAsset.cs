using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Asset/Scale")]
    public class UIScaleTransitionAsset : UIGenericTransitionAsset<Vector2, UITransitionPreset<Vector2>>
    {
        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, Vector2 value, float duration, IUITransitionParam param)
        {
            if (instance.Payload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param.MonoBehaviour, tweenPayload.Tweens);
            }

            var tweens = RunTransitionTween<ScaleTween, Graphic>(param.MonoBehaviour, graphics, duration, value);

            return new UITransitionTweenPayload(tweens);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, Vector2 value, IUITransitionParam param)
        {
            if (instance.Payload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param.MonoBehaviour, tweenPayload.Tweens);
            }

            foreach (Graphic g in graphics)
            {
                g.transform.localScale = value;
            }

            return null;
        }

        #endregion

        #region Initialization

        protected override void GetDefaultValueAndDuration(out Vector2 value, out float duration)
        {
            value = Vector2.one;
            duration = 0.1f;
        }

        #endregion

        #region Initial Value

        public override object GetGraphicInitialValue(Graphic graphic)
        {
            return graphic.transform.localScale;
        }

        #endregion

        #region Tween

        public class ScaleTween : UITransitionTween<Vector2, Graphic>
        {
            private Vector3 m_startScale;

            protected override void OnComplete(Graphic graphic, Vector2 targetValue)
            {
                graphic.transform.localScale = targetValue;
            }

            protected override void OnInit(Graphic graphic, Vector2 targetValue)
            {
                m_startScale = graphic.transform.localScale;
            }

            protected override void Update(Graphic graphic, float normalizedTime, Vector2 targetValue)
            {
                graphic.transform.localScale = Vector3.Lerp(m_startScale, targetValue, normalizedTime);
            }
        }

        #endregion
    }
}
