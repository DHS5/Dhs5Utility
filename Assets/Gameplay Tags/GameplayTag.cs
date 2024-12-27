using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Tags
{
    public class GameplayTag : BaseDataContainerScriptableElement, IDataContainerPrefixableElement
    {
        #region Members

        [SerializeField] private string m_category;

        #endregion

        #region IDataContainerPrefixableElement

        public string DataNamePrefix { get => m_category; set => m_category = value; }

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(GameplayTag))]
    public class GameplayTagEditor : BaseDataContainerScriptableElementEditor
    {
        protected override void OnGUI()
        {
            base.OnGUI();

            EditorGUILayout.PropertyField(p_uid);
        }
    }

#endif

    #endregion
}
