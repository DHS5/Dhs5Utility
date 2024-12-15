using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Updates
{
    public class UpdaterDatabaseElement : BaseEnumDatabaseElement
    {
        #region Members

        [SerializeField] private UpdatePass m_updatePass = UpdatePass.CLASSIC;
        [Tooltip("Order in the selected update pass\n0 will be invoked first, bigger number last")]
        [SerializeField] private ushort m_order = 0;

        [SerializeField] private UpdateCondition m_updateCondition = UpdateCondition.ALWAYS;
        [SerializeField] private EnabledValue<float> m_customFrequency;
        [SerializeField] private bool m_timescaleIndependent = false;

        #endregion

        #region Properties

        public UpdatePass Pass => m_updatePass;
        public ushort Order => m_order;
        public UpdateCondition Condition => m_updateCondition;
        public bool TimescaleIndependent => m_timescaleIndependent;

        public bool HasCustomFrequency(out float frequency)
        {
            if (m_customFrequency.IsEnabled(out frequency))
            {
                return frequency > 0f;
            }
            return false;
        }

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(UpdaterDatabaseElement), editorForChildClasses:true)]
    public class UpdateDatabaseElementEditor : BaseEnumDatabaseElementEditor
    {
        #region Members

        protected UpdaterDatabaseElement m_element;

        protected SerializedProperty p_updatePass;
        protected SerializedProperty p_order;
        protected SerializedProperty p_updateCondition;
        protected SerializedProperty p_customFrequency;
        protected SerializedProperty p_timescaleIndependent;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            m_element = (UpdaterDatabaseElement)target;

            p_updatePass = serializedObject.FindProperty("m_updatePass");
            p_order = serializedObject.FindProperty("m_order");
            p_updateCondition = serializedObject.FindProperty("m_updateCondition");
            p_customFrequency = serializedObject.FindProperty("m_customFrequency");
            p_timescaleIndependent = serializedObject.FindProperty("m_timescaleIndependent");

            m_excludedProperties.Add(p_updatePass.propertyPath);
            m_excludedProperties.Add(p_order.propertyPath);
            m_excludedProperties.Add(p_updateCondition.propertyPath);
            m_excludedProperties.Add(p_customFrequency.propertyPath);
            m_excludedProperties.Add(p_timescaleIndependent.propertyPath);
        }

        #endregion

        #region GUI

        protected override void OnGUI()
        {
            {
                EditorGUILayout.PropertyField(p_updatePass);
                EditorGUILayout.PropertyField(p_order);

                EditorGUILayout.Space(10f);

                EditorGUILayout.PropertyField(p_updateCondition);
                EditorGUILayout.PropertyField(p_customFrequency);
                EditorGUILayout.PropertyField(p_timescaleIndependent);
            }
        }

        #endregion
    }

#endif

    #endregion
}
