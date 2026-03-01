using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.UI
{
    public interface IUIGenericTransitionInstance
    {
        public void UpdateState(UIGenericTransitionData data, IEnumerable<Graphic> graphics, FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param);
    }
    [Serializable]
    public struct UIGenericTransitionInstance<T> : IUIGenericTransitionInstance
    {
        #region Members

        [SerializeField] private TransitionValue<T> m_normalState;
        [SerializeField] private EnabledTransitionValue<T> m_highlightedState;
        [SerializeField] private EnabledTransitionValue<T> m_pressedState;
        [SerializeField] private EnabledTransitionValue<T> m_selectedState;
        [SerializeField] private EnabledTransitionValue<T> m_disabledState;

        #endregion

        public void UpdateState(UIGenericTransitionData data, IEnumerable<Graphic> graphics, FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param)
        {

        }
    }

    [Serializable]
    public class UIGenericTransitionSelector
    {
        [SerializeField] private UIGenericTransitionData m_data;
        [SerializeReference] private IUIGenericTransitionInstance m_instance;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(UIGenericTransitionSelector))]
    public class UIGenericTransitionSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var p_data = property.FindPropertyRelative("m_data");
            var p_instance = property.FindPropertyRelative("m_instance");

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, 18f), p_data);
            if (EditorGUI.EndChangeCheck())
            {
                var d = p_data.objectReferenceValue;
                if (d is UIGenericTransitionData data)
                {
                    p_instance.managedReferenceValue = data.GetInstance();
                }
                else 
                {
                    p_instance.managedReferenceValue = null;
                    p_instance.isExpanded = false;
                }
            }
            EditorGUI.PropertyField(new Rect(position.x, position.y + 20f, position.width, 18f), p_instance, true);

            EditorGUI.EndProperty();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.FindPropertyRelative("m_instance").isExpanded ? 60f : 40f;
        }
    }
#endif
}
