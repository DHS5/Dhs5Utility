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
    public class UpdaterDatabaseElement : BaseEnumDatabaseElement, IComparable<UpdaterDatabaseElement>
    {
        #region Members

        [SerializeField] private EUpdatePass m_updatePass = EUpdatePass.CLASSIC;
        [Tooltip("Order in the selected update pass\n0 will be invoked first, bigger number last")]
        [SerializeField] private ushort m_order = 0;

        [SerializeField] private EUpdateCondition m_updateCondition = EUpdateCondition.ALWAYS;
        [SerializeField] private EnabledValue<float> m_customFrequency;
        [SerializeField] private float m_timescale = 1.0f;
        [SerializeField] private bool m_realtime = false;

        #endregion

        #region Properties

        public EUpdatePass Pass => m_updatePass;
        public ushort Order => m_order;
        public EUpdateCondition Condition => m_updateCondition;
        public float Frequency
        {
            get
            {
                if (m_customFrequency.IsEnabled(out var frequency))
                {
                    return Mathf.Max(0f, frequency);
                }
                return 0f;
            }
        }
        public float TimeScale => m_timescale;
        public bool Realtime => m_realtime;

        #endregion

        #region IComparable

        public int CompareTo(UpdaterDatabaseElement other)
        {
            return Order.CompareTo(other.Order);
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
        protected SerializedProperty p_timescale;
        protected SerializedProperty p_realtime;

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
            p_timescale = serializedObject.FindProperty("m_timescale");
            p_realtime = serializedObject.FindProperty("m_realtime");

            m_excludedProperties.Add(p_updatePass.propertyPath);
            m_excludedProperties.Add(p_order.propertyPath);
            m_excludedProperties.Add(p_updateCondition.propertyPath);
            m_excludedProperties.Add(p_customFrequency.propertyPath);
            m_excludedProperties.Add(p_timescale.propertyPath);
            m_excludedProperties.Add(p_realtime.propertyPath);
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
                EditorGUILayout.PropertyField(p_timescale);
                EditorGUILayout.PropertyField(p_realtime);
            }
        }

        #endregion
    }

#endif

    #endregion
}
