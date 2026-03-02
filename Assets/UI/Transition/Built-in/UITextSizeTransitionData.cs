using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Data/Text Size")]
    public class UITextSizeTransitionData : UIGenericTransitionData<float>
    {
        // TODO :
        // - think about applying offsets on start value rather than hard setting values
        // in this case, maybe hide normal state

        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(IEnumerable<Graphic> graphics, float value, float duration, IUITransitionParam param)
        {
            var tweens = RunTransitionTween<TextSizeTween>(param.MonoBehaviour, graphics, duration, value);

            return new UITransitionTweenPayload(tweens);
        }

        protected override IUIGenericTransitionPayload ApplyValueInstant(IEnumerable<Graphic> graphics, float value, IUITransitionParam param)
        {
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
            m_normalState = new(22f, 0.1f);
            m_highlightedState = new(true, 22f, 0.1f);
            m_pressedState = new(true, 22f, 0.1f);
            m_selectedState = new(true, 22f, 0.1f);
            m_disabledState = new(true, 22f, 0.1f);
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
