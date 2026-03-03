using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


#if UNITY_EDITOR
using Dhs5.Utility.Editors;
using UnityEditor;
#endif

namespace Dhs5.Utility.UI
{
    public abstract class UIGenericTransitionData : ScriptableObject
    {
        public abstract IUIGenericTransitionPayload UpdateState
            (UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param);
    }
    public abstract class UIGenericTransitionData<T, Preset> : UIGenericTransitionData where Preset : TransitionPreset<T>
    {
        #region Members

        [SerializeField] protected List<Preset> m_presets;

        #endregion

        #region Process

        public override IUIGenericTransitionPayload UpdateState
            (UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param)
        {
            if (TryGetValueAndDuration(instance.PresetIndex, newStates, out var value, out var duration))
            {
                if (instant || Mathf.Approximately(0f, duration))
                {
                    return ApplyValueInstant(instance, graphics, value, param);
                }
                else
                {
                    return ApplyValue(instance, graphics, value, duration, param);
                }
            }
            return null;
        }

        protected abstract IUIGenericTransitionPayload ApplyValue(
            UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, T value, float duration, IUITransitionParam param);
        protected abstract IUIGenericTransitionPayload ApplyValueInstant(
            UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, T value, IUITransitionParam param);

        #endregion

        #region Accessors

        protected virtual bool TryGetPreset(int index, out Preset preset)
        {
            return m_presets.IsIndexValid(index, out preset);
        }
        protected virtual bool TryGetValueAndDuration(int presetIndex, FUIState states, out T value, out float duration)
        {
            if (TryGetPreset(presetIndex, out var preset))
            {
                (value, duration) = preset.GetValueAndDuration(states);
                return true;
            }

            value = default;
            duration = 0f;
            return false;
        }

        #endregion

        #region Editor Initialization

#if UNITY_EDITOR

        protected virtual void Reset()
        {
            
        }

        protected virtual void Editor_OnAddNewPreset(Preset preset)
        {
            GetDefaultValueAndDuration(out var value, out var duration);

            preset.SetState(EUIState.NORMAL, true, value, duration);
            preset.SetState(EUIState.HIGHLIGHTED, true, value, duration);
            preset.SetState(EUIState.PRESSED, true, value, duration);
            preset.SetState(EUIState.SELECTED, true, value, duration);
            preset.SetState(EUIState.DISABLED, true, value, duration);
        }

#endif

        protected abstract void GetDefaultValueAndDuration(out T value, out float duration);


        #endregion

        #region Tween Utility

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
        protected void StopTweenCoroutines(MonoBehaviour monoBehaviour, IEnumerable<UITransitionTween> tweens)
        {
            foreach (var tween in tweens)
            {
                if (tween != null)
                {
                    tween.Stop();
                }
            }
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

    #region TransitionPreset

    [Serializable]
    public class TransitionPreset<T>
    {
        #region Members

        [SerializeField] protected string m_name;
        [SerializeField] protected TransitionStateOrder m_stateOrder;
        [SerializeField] protected TransitionValue<T> m_normalState = new(default, 0.1f);
        [SerializeField] protected EnabledTransitionValue<T> m_highlightedState = new(true, default, 0.1f);
        [SerializeField] protected EnabledTransitionValue<T> m_pressedState = new(true, default, 0.1f);
        [SerializeField] protected EnabledTransitionValue<T> m_selectedState = new(true, default, 0.1f);
        [SerializeField] protected EnabledTransitionValue<T> m_disabledState= new(true, default, 0.1f);

        #endregion

        #region Accessors

        public string Name => m_name;
        public IEnumerable<int> StateOrder => m_stateOrder;
        public virtual (T, float) GetValueAndDuration(FUIState states)
        {
            foreach (var state in states.GetStates(m_stateOrder))
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

        #endregion

        #region Setters

        public void SetName(string name) => m_name = name;
        public void SetStateOrder(IEnumerable<int> order)
        {
            m_stateOrder.Set(order);
        }
        public void SetState(EUIState state, bool enabled, T value, float duration)
        {
            switch (state)
            {
                case EUIState.NORMAL:
                    m_normalState.SetValue(value, duration);
                    break;
                
                case EUIState.HIGHLIGHTED:
                    m_highlightedState.SetValue(enabled, value, duration);
                    break;
                
                case EUIState.PRESSED:
                    m_pressedState.SetValue(enabled, value, duration);
                    break;
                
                case EUIState.SELECTED:
                    m_selectedState.SetValue(enabled, value, duration);
                    break;
                
                case EUIState.DISABLED:
                    m_disabledState.SetValue(enabled, value, duration);
                    break;
            }
        }

        #endregion
    }

    #endregion

    #region TransitionStateOrder

    [Serializable]
    public class TransitionStateOrder : IEnumerable<int>
    {
        #region Members

        // Base States Order :
        // - 0 = Normal
        // - 1 = Selected
        // - 2 = Highlighted
        // - 3 = Pressed
        // - 4 = Disabled
        [SerializeField] protected List<int> m_order;

        #endregion

        #region IEnumerable<int>

        public IEnumerator<int> GetEnumerator()
        {
            foreach (var i in m_order) yield return i;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Setter

        public virtual void Set(IEnumerable<int> order)
        {
            m_order = order.ToList();
            for (int i = m_order.Count - 1; i >= 0; i--)
            {
                if (m_order[i] <= 0 || m_order[i] > 4)
                    m_order.RemoveAt(i);
            }

            while (m_order.Count > 4)
            {
                m_order.RemoveAt(0);
            }
        }

        #endregion
    }

    #region Drawer

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(TransitionStateOrder))]
    public class TransitionStateOrderDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var p_order = property.FindPropertyRelative("m_order");

            // Initialize
            if (p_order.arraySize != 4 || GUI.Button(new Rect(position.x + position.width - 50f, position.y, 50f, 20f), "Reset"))
            {
                p_order.ClearArray();
                p_order.InsertArrayElementAtIndex(0);
                p_order.InsertArrayElementAtIndex(0);
                p_order.InsertArrayElementAtIndex(0);
                p_order.InsertArrayElementAtIndex(0);
                p_order.GetArrayElementAtIndex(0).intValue = 4;
                p_order.GetArrayElementAtIndex(1).intValue = 2;
                p_order.GetArrayElementAtIndex(2).intValue = 1;
                p_order.GetArrayElementAtIndex(3).intValue = 3;
            }

            property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width - 52f, 18f), property.isExpanded, label, true);
            if (property.isExpanded)
            {
                // List
                Rect rect = new Rect(position.x, position.y + 20f, position.width, 18f);
                for (int i = 0; i < p_order.arraySize; i++)
                {
                    var r_element = new Rect(rect.x, rect.y, rect.width - 64f, rect.height);
                    var r_upButton = new Rect(rect.x + rect.width - 62f, rect.y, 30f, 20f);
                    var r_downButton = new Rect(rect.x + rect.width - 30f, rect.y, 30f, 20f);

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.EnumPopup(r_element, (EUIState)p_order.GetArrayElementAtIndex(i).intValue);
                    EditorGUI.EndDisabledGroup();

                    if (i < p_order.arraySize - 1 && GUI.Button(r_downButton, EditorGUIHelper.DownIcon)) p_order.MoveArrayElement(i, i + 1);
                    if (i > 0 && GUI.Button(r_upButton, EditorGUIHelper.UpIcon)) p_order.MoveArrayElement(i, i - 1);

                    rect.y += 20f;
                }
            }

            EditorGUI.EndProperty();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded ? 100f : 20f;
        }
    }

#endif

    #endregion

    #endregion
}
