using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


#if UNITY_EDITOR
using Dhs5.Utility.Editors;
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Dhs5.Utility.UI
{
    public abstract class UIGenericTransitionAsset : ScriptableObject
    {
        #region Process

        public abstract IUIGenericTransitionPayload UpdateState
            (UIGenericTransitionInstance instance, IEnumerable<Graphic> graphics, FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param);

        #endregion

        #region Instance Initialization

        public abstract object GetGraphicInitialValue(Graphic graphic);

        #endregion

        #region Preset Initialization

#if UNITY_EDITOR

        public abstract void Editor_OnAddNewPreset();

#endif

        #endregion
    }
    public abstract class UIGenericTransitionAsset<T, Preset> : UIGenericTransitionAsset where Preset : UITransitionPreset<T>
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

        #region Preset Initialization

#if UNITY_EDITOR

        public sealed override void Editor_OnAddNewPreset()
        {
            if (m_presets.IsValid())
            {
                var preset = m_presets[^1];
                Editor_OnAddNewPreset(preset);
            }
        }
        protected virtual void Editor_OnAddNewPreset(Preset preset)
        {
            preset.SetName("New preset");

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

        public virtual List<UITransitionTween> RunTransitionTween<Tween, G>(MonoBehaviour monoBehaviour, IEnumerable<Graphic> graphics, float duration, T targetValue) where Tween : UITransitionTween<T, G>, new() where G : Graphic
        {
            if (monoBehaviour == null)
            {
                Debug.LogError("MonoBehaviour is null, can't start coroutines");
                return null;
            }

            List<UITransitionTween> tweens = new();
            foreach (var g in graphics)
            {
                if (CanRunTween<G>(g, out var graphic))
                {
                    var value = targetValue;
                    if (OverrideTweenTargetValue(graphic, targetValue, out var overrideValue))
                    {
                        value = overrideValue;
                    }

                    var d = duration;
                    if (OverrideTweenDuration(graphic, duration, out var overrideDuration))
                    {
                        d = overrideDuration;
                    }

                    var tween = new Tween();
                    tween.Start(monoBehaviour, graphic, d, value);
                    tweens.Add(tween);
                }
            }

            return tweens;
        }
        protected virtual bool CanRunTween<G>(Graphic graphic, out G actualGraphic) where G : Graphic
        {
            if (graphic is G g)
            {
                actualGraphic = g;
                return true;
            }
            actualGraphic = null;
            return false;
        }
        protected virtual bool OverrideTweenTargetValue<G>(G graphic, T targetValue, out T overrideValue) where G : Graphic
        {
            overrideValue = default;
            return false;
        }
        protected virtual bool OverrideTweenDuration<G>(G graphic, float duration, out float overrideDuration) where G : Graphic
        {
            overrideDuration = 0.0f;
            return false;
        }

        protected virtual void StopTweenCoroutines(MonoBehaviour monoBehaviour, IEnumerable<UITransitionTween> tweens)
        {
            if (tweens != null)
            {
                foreach (var tween in tweens)
                {
                    if (tween != null)
                    {
                        tween.Stop();
                    }
                }
            }
        }

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(UIGenericTransitionAsset<,>), editorForChildClasses: true)]
    public class UIGenericTransitionDataEditor : Editor
    {
        #region Members

        protected UIGenericTransitionAsset m_asset;

        protected ReorderableList m_list;
        protected List<string> m_excludedProperties = new();

        protected SerializedProperty p_script;
        protected SerializedProperty p_presets;

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_asset = target as UIGenericTransitionAsset;

            p_script = serializedObject.FindProperty("m_Script");
            p_presets = serializedObject.FindProperty("m_presets");

            m_excludedProperties.Add(p_script.propertyPath);
            m_excludedProperties.Add(p_presets.propertyPath);

            if (p_presets != null)
            {
                m_list = InitializeList();
            }
        }

        #endregion


        #region Core GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.PropertyField(p_script);
            }
            if (m_list != null)
            {
                m_list.DoLayoutList();
            }
            DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region List GUI

        protected virtual ReorderableList InitializeList()
        {
            return new ReorderableList(serializedObject, p_presets, true, false, true, true)
            {
                drawElementCallback = OnDrawElement,
                elementHeightCallback = OnElementHeight,

                onAddCallback = OnAddToList,
            };
        }

        protected virtual float OnElementHeight(int index) => EditorGUI.GetPropertyHeight(p_presets.GetArrayElementAtIndex(index));
        protected virtual void OnDrawElement(Rect rect, int index, bool selected, bool focused)
        {
            EditorGUI.PropertyField(new Rect(rect.x + 8f, rect.y, rect.width - 8f, rect.height), p_presets.GetArrayElementAtIndex(index), true);
        }

        protected virtual void OnAddToList(ReorderableList list)
        {
            if (p_presets != null)
            {
                p_presets.InsertArrayElementAtIndex(p_presets.arraySize);
                serializedObject.ApplyModifiedProperties();
                m_asset.Editor_OnAddNewPreset();
            }
        }

        #endregion
    }

#endif

    #endregion


    #region TransitionValue

    [Serializable]
    public class UITransitionValue<T>
    {
        #region Constructor

        public UITransitionValue(T value, float duration)
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

    [CustomPropertyDrawer(typeof(UITransitionValue<>))]
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
    public class UIEnabledTransitionValue<T> : UITransitionValue<T>
    {
        #region Constructor

        public UIEnabledTransitionValue(bool enabled, T value, float duration) : base(value, duration)
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

    [CustomPropertyDrawer(typeof(UIEnabledTransitionValue<>))]
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
    public abstract class UITransitionTween<T, G> : UITransitionTween where G : Graphic
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

    #endregion

    #region TransitionPreset

    [Serializable]
    public class UITransitionPreset<T>
    {
        #region Members

        [SerializeField] protected string m_name;
        [SerializeField] protected UITransitionStateOrder m_stateOrder;
        [SerializeField] protected UITransitionValue<T> m_normalState = new(default, 0.1f);
        [SerializeField] protected UIEnabledTransitionValue<T> m_highlightedState = new(true, default, 0.1f);
        [SerializeField] protected UIEnabledTransitionValue<T> m_pressedState = new(true, default, 0.1f);
        [SerializeField] protected UIEnabledTransitionValue<T> m_selectedState = new(true, default, 0.1f);
        [SerializeField] protected UIEnabledTransitionValue<T> m_disabledState= new(true, default, 0.1f);

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
    public class UITransitionStateOrder : IEnumerable<int>
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

    [CustomPropertyDrawer(typeof(UITransitionStateOrder))]
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
