using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Data/Text Size")]
    public class UITextSizeTransitionData : UIGenericTransitionData<float, TransitionPreset<float>>
    {
        // TODO :
        // - think about applying offsets on start value rather than hard setting values
        // in this case, maybe hide normal state

        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, float value, float duration, IUITransitionParam param)
        {
            if (instance.Payload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param.MonoBehaviour, tweenPayload.Tweens);
            }

            var tweens = RunTransitionTween<TextSizeTween>(param.MonoBehaviour, graphics, duration, value);

            return new UITransitionTweenPayload(tweens);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, float value, IUITransitionParam param)
        {
            if (instance.Payload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param.MonoBehaviour, tweenPayload.Tweens);
            }

            foreach (var g in graphics)
            {
                if (g is TMP_Text text)
                {
                    text.fontSize = value;
                }
            }

            return null;
        }

        #endregion

        #region Initialization

        protected override void GetDefaultValueAndDuration(out float value, out float duration)
        {
            value = 36f;
            duration = 0.1f;
        }

        #endregion

        #region Tween

        public class TextSizeTween : UITransitionTween<float>
        {
            private float m_startSize;
            private TMP_Text m_text;

            protected override void OnComplete(Graphic graphic, float targetValue)
            {
                if (m_text != null) m_text.fontSize = targetValue;
            }

            protected override void OnInit(Graphic graphic, float targetValue)
            {
                m_text = graphic as TMP_Text;
                if (m_text != null) m_startSize = m_text.fontSize;
            }

            protected override void Update(Graphic graphic, float normalizedTime, float targetValue)
            {
                if (m_text != null) m_text.fontSize = Mathf.Lerp(m_startSize, targetValue, normalizedTime);
            }
        }

        #endregion
    }
}
