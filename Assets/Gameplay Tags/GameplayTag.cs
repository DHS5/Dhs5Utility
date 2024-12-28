using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.GUIs;
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
        #region Members

        protected SerializedProperty p_category;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            p_category = serializedObject.FindProperty("m_category");
        }

        #endregion

        #region GUI

        protected override void OnGUI()
        {
            base.OnGUI();

            StringBuilder sb = new();
            sb.Append(p_category.stringValue);
            if (!p_category.stringValue.EndsWith('/'))
            {
                sb.Append('/');
            }
            sb.Append(target.name);
            sb.Append(" (");
            sb.Append(p_uid.intValue);
            sb.Append(")");

            EditorGUILayout.LabelField(sb.ToString(), GUIHelper.centeredBoldLabel);
        }

        #endregion
    }

#endif

    #endregion
}
