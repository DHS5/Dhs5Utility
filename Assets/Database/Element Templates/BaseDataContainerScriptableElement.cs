using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dhs5.Utility.Attributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Databases
{
    public abstract class BaseDataContainerScriptableElement : ScriptableObject, IDataContainerElement
    {
        #region Members

        [SerializeField, ReadOnly] protected int m_uid;

        #endregion

        #region IDataContainerElement

        public int UID => m_uid;

#if UNITY_EDITOR

        public void Editor_SetUID(int uid)
        {
            m_uid = uid;
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(BaseDataContainerScriptableElement), editorForChildClasses: true)]
    public class BaseDataContainerScriptableElementEditor : Editor
    {
        #region Members

        protected SerializedProperty p_script;
        protected SerializedProperty p_uid;

        protected List<string> m_excludedProperties;

        #endregion

        #region Core Behaviour

        protected virtual void OnEnable()
        {
            p_script = serializedObject.FindProperty("m_Script");
            p_uid = serializedObject.FindProperty("m_uid");

            m_excludedProperties = new()
        {
            p_script.propertyPath,
            p_uid.propertyPath,
        };
        }
        protected virtual void OnDisable() { }

        #endregion

        #region GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnGUI();

            EditorGUILayout.Space(5f);

            DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());

            if (Database.DebugModeEnabled)
            {
                OnDatabaseDebugModeGUI();
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnGUI() { }

        protected virtual void OnDatabaseDebugModeGUI()
        {
            p_uid.intValue = EditorGUILayout.IntField(p_uid.displayName, p_uid.intValue);
        }

        #endregion
    }

#endif

    #endregion
}
