using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Unity.VisualScripting;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.UI
{
    public abstract class UIGenericTransitionData : ScriptableObject
    {
        public abstract void UpdateState(IEnumerable<Graphic> graphics, FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param);

        public abstract IUIGenericTransitionInstance GetInstance();
    }
    public abstract class UIGenericTransitionData<T> : UIGenericTransitionData
    {
        // TODO :
        // custom inspector to draw states order list, with reset button

        #region Members

        [Header("Order")]
        // Base States Order :
        // - 0 = Normal
        // - 1 = Selected
        // - 2 = Highlighted
        // - 3 = Pressed
        // - 4 = Disabled
        [SerializeField] protected List<int> m_statesOrder;

        [Header("States")]
        [SerializeField] protected TransitionValue<T> m_normalState;
        [SerializeField] protected EnabledTransitionValue<T> m_highlightedState;
        [SerializeField] protected EnabledTransitionValue<T> m_pressedState;
        [SerializeField] protected EnabledTransitionValue<T> m_selectedState;
        [SerializeField] protected EnabledTransitionValue<T> m_disabledState;

        #endregion

        #region Process

        public override void UpdateState(IEnumerable<Graphic> graphics, FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param)
        {
            (T value, float duration) = GetValueAndDuration(newStates);

            if (instant || Mathf.Approximately(0f, duration))
            {
                ApplyValueInstant(graphics, value, param);
            }
            else
            {
                ApplyValue(graphics, value, duration, param);
            }
        }

        protected abstract void ApplyValue(IEnumerable<Graphic> graphics, T value, float duration, IUITransitionParam param);
        protected abstract void ApplyValueInstant(IEnumerable<Graphic> graphics, T value, IUITransitionParam param);

        #endregion

        #region Accessors

        protected virtual (T, float) GetValueAndDuration(FUIState states)
        {
            foreach (var state in states.GetStates(m_statesOrder))
            {
                switch (state)
                {
                    case EUIState.HIGHLIGHTED:
                        if (m_highlightedState.IsEnabled(out var value, out var duration))
                            return (value, duration);
                        break;

                    case EUIState.PRESSED:
                        if (m_pressedState.IsEnabled(out value, out duration))
                            return (value, duration);
                        break;

                    case EUIState.SELECTED:
                        if (m_selectedState.IsEnabled(out value, out duration))
                            return (value, duration);
                        break;

                    case EUIState.DISABLED:
                        if (m_disabledState.IsEnabled(out value, out duration))
                            return (value, duration);
                        break;
                }
            }

            return m_normalState.GetValue();
        }

        public override IUIGenericTransitionInstance GetInstance() => new UIGenericTransitionInstance<T>();

        #endregion

        #region Initialization

        protected virtual void Reset()
        {
            OnInitValues();
        }

        protected abstract void OnInitValues();

        #endregion

        #region Utility

        public List<UITransitionTween> RunTransitionTween<Tween>(MonoBehaviour monoBehaviour, IEnumerable<Graphic> graphics, float duration, T targetValue) where Tween : UITransitionTween<T>, new()
        {
            if (monoBehaviour == null)
            {
                Debug.LogError("MonoBehaviour is null, can't start coroutines");
                return null;
            }

            List<UITransitionTween> tweens = new();
            foreach (var g in graphics)
            {
                var tween = new Tween();
                tween.Start(monoBehaviour, g, duration, targetValue);
                tweens.Add(tween);
            }

            return tweens;
        }

        #endregion
    }

    #region TransitionValue

    [Serializable]
    public class TransitionValue<T>
    {
        #region Constructor

        public TransitionValue(T value, float duration)
        {
            m_value = value;
            m_duration = duration;
        }

        #endregion

        #region Members

        [SerializeField] protected T m_value;
        [SerializeField] protected float m_duration;

        #endregion

        #region Accessors

        public (T, float) GetValue()
        {
            return (m_value, m_duration);
        }

        public void SetValue(T value, float duration)
        {
            m_value = value;
            m_duration = duration;
        }

        #endregion
    }

    #region Drawer

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(TransitionValue<>))]
    public class TransitionValueDrawer : PropertyDrawer
    {
        SerializedProperty p_value;
        SerializedProperty p_duration;

        GUIContent m_durationLabel = new GUIContent("D");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            p_value = property.FindPropertyRelative("m_value");
            p_duration = property.FindPropertyRelative("m_duration");

            var durationWidth = 60f;

            EditorGUI.BeginProperty(position, label, property);

            var r_value = new Rect(position.x, position.y, position.width - durationWidth - 5f, position.height);
            EditorGUI.PropertyField(r_value, p_value, label, true);

            var r_duration = new Rect(position.x + position.width - durationWidth, position.y, durationWidth, position.height);
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 15f;
            EditorGUI.PropertyField(r_duration, p_duration, m_durationLabel, true);
            EditorGUIUtility.labelWidth = labelWidth;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_value"));
        }
    }

#endif

    #endregion

    [Serializable]
    public class EnabledTransitionValue<T> : TransitionValue<T>
    {
        #region Constructor

        public EnabledTransitionValue(bool enabled, T value, float duration) : base(value, duration)
        {
            m_enabled = enabled;
        }

        #endregion

        #region Members

        [SerializeField] protected bool m_enabled;

        #endregion

        #region Accessors

        public bool IsEnabled(out T value, out float duration)
        {
            value = m_value;
            duration = m_duration;
            return m_enabled;
        }

        public void SetValue(bool enabled, T value, float duration)
        {
            m_enabled = enabled;
            m_value = value;
            m_duration = duration;
        }

        #endregion
    }

    #region Drawer

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(EnabledTransitionValue<>))]
    public class EnabledTransitionValueDrawer : TransitionValueDrawer
    {
        SerializedProperty p_enabled;
        float toggleWidth = 17f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            p_enabled = property.FindPropertyRelative("m_enabled");

            Rect rect = new Rect(position.x, position.y, position.width, 18f);

            EditorGUI.BeginProperty(position, label, property);

            p_enabled.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, toggleWidth + EditorGUI.indentLevel * 15f, rect.height), GUIContent.none, p_enabled.boolValue);
            property.isExpanded = p_enabled.boolValue;

            Rect propertyRect = new Rect(rect.x + toggleWidth, rect.y, rect.width - toggleWidth, rect.height);
            var previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth -= toggleWidth;

            if (property.isExpanded)
            {
                base.OnGUI(propertyRect, property, label);
            }
            else
            {
                EditorGUI.LabelField(propertyRect, label);
            }

            EditorGUIUtility.labelWidth = previousLabelWidth;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded ? base.GetPropertyHeight(property, label) : 18f;
        }
    }

#endif

    #endregion

    #endregion

    #region TransitionTween

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
    public abstract class UITransitionTween<T> : UITransitionTween
    {
        protected virtual bool IsValid(Graphic graphic) => graphic != null;
        protected abstract void Update(Graphic graphic, float normalizedTime, T targetValue);
        protected abstract void OnInit(Graphic graphic, T targetValue);
        protected abstract void OnComplete(Graphic graphic, T targetValue);

        public void Start(MonoBehaviour monoBehaviour, Graphic graphic, float duration, T targetValue)
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
        public virtual IEnumerator TweenCoroutine(Graphic graphic, float duration, T targetValue)
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

    #endregion
}
