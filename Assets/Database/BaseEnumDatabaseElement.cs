using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Databases
{
    public abstract class BaseEnumDatabaseElement : BaseDataContainerScriptableElement, IEnumDatabaseElement
    {
        #region Members

        [SerializeField] protected int m_enumIndex;

        #endregion

        #region IEnumDatabaseElement

        public int EnumIndex => m_enumIndex;

#if UNITY_EDITOR

        public void Editor_SetIndex(int index)
        {
            m_enumIndex = index;
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(BaseEnumDatabaseElement), editorForChildClasses: true)]
    public class BaseEnumDatabaseElementEditor : BaseDataContainerScriptableElementEditor
    {
        #region Members

        protected SerializedProperty p_enumIndex;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            p_enumIndex = serializedObject.FindProperty("m_enumIndex");

            m_excludedProperties.Add(p_enumIndex.propertyPath);
        }

        #endregion
    }

#endif

    #endregion
}
