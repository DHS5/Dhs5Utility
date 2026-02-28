using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace Dhs5.Utility.UI
{
    [CreateAssetMenu(menuName = "Dhs5 Utility/UI/Transition Data/Scale")]
    public class UIScaleTransitionData : UIGenericTransitionData<Vector2>
    {
        #region Members

        private List<Coroutine> m_coroutines;

        #endregion

        #region Apply

        protected void StopCoroutines(MonoBehaviour monoBehaviour)
        {
            if (m_coroutines.IsValid())
            {
                foreach (var coroutine in m_coroutines)
                {
                    if (coroutine != null)
                    {
                        monoBehaviour.StopCoroutine(coroutine);
                    }
                }
            }
        }

        protected override void ApplyValue(IEnumerable<Graphic> graphics, Vector2 value, float duration, IUITransitionParam param)
        {
            StopCoroutines(param.MonoBehaviour);

            m_coroutines = ApplyScaleTransition(param.MonoBehaviour, graphics, duration, value);
        }

        protected override void ApplyValueInstant(IEnumerable<Graphic> graphics, Vector2 value, IUITransitionParam param)
        {
            StopCoroutines(param.MonoBehaviour);

            foreach (Graphic g in graphics)
            {
                g.transform.localScale = value;
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


        #region STATIC

        public static List<Coroutine> ApplyScaleTransition(MonoBehaviour monoBehaviour, IEnumerable<Graphic> graphics, float duration, Vector3 scale)
        {
            if (monoBehaviour == null)
            {
                Debug.LogError("MonoBehaviour is null, can't start coroutines");
                return null;
            }

            List<Coroutine> coroutines = new();
            foreach (var g in graphics)
            {
                coroutines.Add(monoBehaviour.StartCoroutine(ScaleTween(g.transform, duration, scale)));
            }

            return coroutines;
        }

        protected static IEnumerator ScaleTween(Transform transform, float duration, Vector3 scale)
        {
            if (transform == null)
            {
                yield break;
            }

            var startScale = transform.localScale;
            var elapsedTime = 0.0f;

            while (elapsedTime < duration)
            {
                if (transform == null)
                {
                    Debug.LogError("Transform is null, can't finish scale tween");
                    yield break;
                }

                elapsedTime += Time.unscaledDeltaTime;
                var percentage = Mathf.Clamp01(elapsedTime / duration);
                transform.localScale = Vector3.Lerp(startScale, scale, percentage);
                yield return null;
            }

            if (transform != null)
            {
                transform.localScale = scale;
            }
        }

        #endregion
    }
}
