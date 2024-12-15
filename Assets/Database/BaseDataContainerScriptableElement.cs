using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Databases
{
    public abstract class BaseDataContainerScriptableElement : ScriptableObject, IDataContainerElement
    {
        #region Members

        [SerializeField] protected int m_uid;

        #endregion

        #region IDataContainerElement

        public int UID => m_uid;

#if UNITY_EDITOR

        public virtual bool Editor_HasDataContainerElementName(out string name)
        {
            name = null;
            return false;
        }

        public virtual bool Editor_HasDataContainerElementTexture(out Texture2D texture)
        {
            texture = null;
            return false;
        }

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

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnGUI() { }

        #endregion
    }

#endif

    #endregion
}
