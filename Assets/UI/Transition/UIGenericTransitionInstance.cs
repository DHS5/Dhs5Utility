using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.UI
{
    [Serializable]
    public class UIGenericTransitionInstance
    {
        #region Members

        [SerializeField] private UIGenericTransitionData m_data;
        [SerializeField] private int m_presetIndex;

        private Dictionary<Graphic, object> m_initialValues = new();
        private IUIGenericTransitionPayload m_payload;

        #endregion

        #region Properties

        public int PresetIndex => m_presetIndex;
        public IUIGenericTransitionPayload Payload => m_payload;
        public T GetInitialValue<T>(Graphic graphic)
        {
            return (T)m_initialValues[graphic];
        }

        #endregion

        #region Methods

        public void UpdateState(IEnumerable<Graphic> graphics, FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param)
        {
            if (m_data == null) return;

            foreach (var g in graphics)
            {
                if (!m_initialValues.ContainsKey(g))
                {
                    m_initialValues[g] = m_data.GetGraphicInitialValue(g);
                }
            }

            m_payload = m_data.UpdateState(this, graphics, oldStates, newStates, instant, param);
        }

        #endregion
    }

    #region Drawer

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(UIGenericTransitionInstance))]
    public class UIGenericTransitionInstanceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var p_data = property.FindPropertyRelative("m_data");
            var p_presetIndex = property.FindPropertyRelative("m_presetIndex");

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, 18f), p_data, true);
            var hasData = p_data.objectReferenceValue != null;
            if (EditorGUI.EndChangeCheck())
            {
                p_presetIndex.intValue = hasData ? 0 : -1;
            }

            if (hasData)
                EditorGUI.PropertyField(new Rect(position.x, position.y + 20f, position.width, 18f), p_presetIndex, true);

            EditorGUI.EndProperty();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.FindPropertyRelative("m_data").objectReferenceValue != null ? 40f : 20f;
        }
    }

#endif

    #endregion

    public interface IUIGenericTransitionPayload { }

    public struct UITransitionTweenPayload : IUIGenericTransitionPayload
    {
        public UITransitionTweenPayload(List<UITransitionTween> tweens) { m_tweens = tweens; }

        private readonly List<UITransitionTween> m_tweens;

        public readonly IEnumerable<UITransitionTween> Tweens
        {
            get
            {
                if (m_tweens != null)
                {
                    foreach (var t in m_tweens)
                    {
                        yield return t;
                    }
                }
            }
        }
    }
}
