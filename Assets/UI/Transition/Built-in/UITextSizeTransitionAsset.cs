using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Asset/Text Size")]
    public class UITextSizeTransitionAsset : UIGenericTransitionAsset<float, UITransitionPreset<float>>
    {
        #region Initial Values

        public override object GetGraphicInitialValue(Graphic graphic)
        {
            return graphic is TMP_Text text ? text.fontSize : 0f;
        }

        #endregion

        #region Apply

        protected override IUIGenericTransitionPayload ApplyValue(UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, float value, float duration, IUITransitionParam param)
        {
            if (instance.Payload is UITransitionTweenPayload tweenPayload)
            {
                StopTweenCoroutines(param.MonoBehaviour, tweenPayload.Tweens);
            }

            var tweens = RunTransitionTween<TextSizeTween, TMP_Text>(param.MonoBehaviour, graphics, duration,
                text => GetTargetSize(instance, text, value));

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
                    text.fontSize = GetTargetSize(instance, text, value);
                }
            }

            return new UITransitionTweenPayload(null);
        }

        /// <summary>
        /// Value is an offset from the text's initial size, never below 0
        /// </summary>
        protected virtual float GetTargetSize(UIGenericTransitionInstance instance, TMP_Text text, float value)
        {
            return Mathf.Max(0f, instance.GetInitialValue<float>(text) + value);
        }

        #endregion

        #region Preset Initialization

        protected override void GetDefaultValueAndDuration(out float value, out float duration)
        {
            value = 0f;
            duration = 0.1f;
        }

        #endregion

        #region Tween

        public class TextSizeTween : UITransitionTween<float, TMP_Text>
        {
            private float m_startSize;

            protected override void OnComplete(TMP_Text graphic, float targetValue)
            {
                graphic.fontSize = targetValue;
            }

            protected override void OnInit(TMP_Text graphic, float targetValue)
            {   
                m_startSize = graphic.fontSize;
            }

            protected override void Update(TMP_Text graphic, float normalizedTime, float targetValue)
            {
                graphic.fontSize = Mathf.Lerp(m_startSize, targetValue, normalizedTime);
            }
        }

        #endregion
    }
}
