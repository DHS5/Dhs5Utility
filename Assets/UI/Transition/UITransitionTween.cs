using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Dhs5.Utility.UI
{
    public abstract class UITransitionTween
    {
        protected MonoBehaviour m_monoBehaviour;

        public Coroutine Coroutine { get; protected set; }

        public virtual void Stop()
        {
            if (m_monoBehaviour != null && Coroutine != null)
            {
                m_monoBehaviour.StopCoroutine(Coroutine);
                Coroutine = null;
            }
        }
    }
    public abstract class UITransitionTween<T, G> : UITransitionTween where G : UnityEngine.Object
    {
        protected virtual bool IsValid(G graphic) => graphic != null;
        protected abstract void Update(G graphic, float normalizedTime, T targetValue);
        protected abstract void OnInit(G graphic, T targetValue);
        protected abstract void OnComplete(G graphic, T targetValue);

        public void Start(MonoBehaviour monoBehaviour, G graphic, float duration, T targetValue)
        {
            m_monoBehaviour = monoBehaviour;

            if (monoBehaviour == null
                && graphic == null
                && duration <= 0f)
            {
                Debug.LogError("Invalid tween");
                return;
            }

            Coroutine = monoBehaviour.StartCoroutine(TweenCoroutine(graphic, duration, targetValue));
        }
        public virtual IEnumerator TweenCoroutine(G graphic, float duration, T targetValue)
        {
            if (!IsValid(graphic))
            {
                yield break;
            }

            OnInit(graphic, targetValue);
            var elapsedTime = 0.0f;

            while (elapsedTime < duration)
            {
                if (!IsValid(graphic))
                {
                    Debug.LogError("Tween is not valid anymore, can't finish tween");
                    yield break;
                }

                elapsedTime += Time.unscaledDeltaTime;
                var percentage = Mathf.Clamp01(elapsedTime / duration);
                Update(graphic, percentage, targetValue);
                yield return null;
            }

            OnComplete(graphic, targetValue);
        }
    }
}
