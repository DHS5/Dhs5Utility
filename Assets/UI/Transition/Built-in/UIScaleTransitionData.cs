using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Data/Scale")]
    public class UIScaleTransitionData : UIGenericTransitionData<Vector2>
    {
        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(IEnumerable<Graphic> graphics, Vector2 value, float duration, IUITransitionParam param)
        {
            var tweens = RunTransitionTween<ScaleTween>(param.MonoBehaviour, graphics, duration, value);

            return new UITransitionTweenPayload(tweens);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(IEnumerable<Graphic> graphics, Vector2 value, IUITransitionParam param)
        {
            foreach (Graphic g in graphics)
            {
                g.transform.localScale = value;
            }

            return null;
        }

        #endregion

        #region Payload Handling

        public override void HandlePreviousPayload(IUIGenericTransitionPayload previousPayload, IUITransitionParam param)
        {
            if (previousPayload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param.MonoBehaviour, tweenPayload.Tweens);
            }
        }

        #endregion

        #region Initialization

        protected override void OnInitValues()
        {
            m_normalState = new(Vector2.one, 0.1f);
            m_highlightedState = new(true, Vector2.one, 0.1f);
            m_pressedState = new(true, Vector2.one, 0.1f);
            m_selectedState = new(true, Vector2.one, 0.1f);
            m_disabledState = new(true, Vector2.one, 0.1f);
        }

        #endregion

        #region Tween

        public class ScaleTween : UITransitionTween<Vector2>
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
