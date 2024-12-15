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
    public class UpdaterDatabaseElement : ScriptableObject, IEnumDatabaseElement
    {
        #region Members

        [SerializeField] private int m_uid;
        [SerializeField] private int m_enumIndex;

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


        #region IEnumDatabaseElement

        public int UID => m_uid;
        public int EnumIndex => m_enumIndex;

#if UNITY_EDITOR

        public void Editor_SetUID(int uid)
        {
            m_uid = uid;
        }
        public void Editor_SetIndex(int index)
        {
            m_enumIndex = index;
        }

        public bool Editor_HasDataContainerElementName(out string name)
        {
            name = null;
            return false;
        }

        public bool Editor_HasDataContainerElementTexture(out Texture2D texture)
        {
            texture = null;
            return false;
        }
#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(UpdaterDatabaseElement), editorForChildClasses:true)]
    public class UpdateDatabaseElementEditor : Editor
    {
        #region Members

        protected UpdaterDatabaseElement m_element;

        protected SerializedProperty p_script;
        protected SerializedProperty p_uid;
        protected SerializedProperty p_enumIndex;
        protected SerializedProperty p_updatePass;
        protected SerializedProperty p_order;
        protected SerializedProperty p_updateCondition;
        protected SerializedProperty p_customFrequency;
        protected SerializedProperty p_timescaleIndependent;

        protected List<string> m_excludedProperties;

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            m_element = (UpdaterDatabaseElement)target;

            p_script = serializedObject.FindProperty("m_Script");
            p_uid = serializedObject.FindProperty("m_uid");
            p_enumIndex = serializedObject.FindProperty("m_enumIndex");
            p_updatePass = serializedObject.FindProperty("m_updatePass");
            p_order = serializedObject.FindProperty("m_order");
            p_updateCondition = serializedObject.FindProperty("m_updateCondition");
            p_customFrequency = serializedObject.FindProperty("m_customFrequency");
            p_timescaleIndependent = serializedObject.FindProperty("m_timescaleIndependent");

            m_excludedProperties = new();
            m_excludedProperties.Add(p_script.propertyPath);
            m_excludedProperties.Add(p_uid.propertyPath);
            m_excludedProperties.Add(p_enumIndex.propertyPath);
            m_excludedProperties.Add(p_updatePass.propertyPath);
            m_excludedProperties.Add(p_order.propertyPath);
            m_excludedProperties.Add(p_updateCondition.propertyPath);
            m_excludedProperties.Add(p_customFrequency.propertyPath);
            m_excludedProperties.Add(p_timescaleIndependent.propertyPath);
        }

        #endregion

        #region GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            {
                EditorGUILayout.PropertyField(p_updatePass);
                EditorGUILayout.PropertyField(p_order);

                EditorGUILayout.Space(10f);

                EditorGUILayout.PropertyField(p_updateCondition);
                EditorGUILayout.PropertyField(p_customFrequency);
                EditorGUILayout.PropertyField(p_timescaleIndependent);
            }

            EditorGUILayout.Space(5f);

            DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif

    #endregion
}
