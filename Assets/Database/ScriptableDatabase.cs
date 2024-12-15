using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Dhs5.Utility.Databases
{
    public class ScriptableDatabase<T> : Database<T> where T : ScriptableDatabase<T>
    {
        #region Members

        [SerializeField] private List<ScriptableObject> m_content;

        #endregion

        #region Properties

        public int Count => m_content.Count;

        #endregion

        #region Accessors

        public override bool TryGetObjectByUID(int uid, out IDataContainerElement obj)
        {
            foreach (var item in m_content)
            {
                if (item is IDataContainerElement elem && elem.UID == uid)
                {
                    obj = elem;
                    return true;
                }
            }

            obj = null;
            return false;
        }

        #endregion

        #region Utility

        public ScriptableObject GetElementAtIndex(int index)
        {
            if (m_content.IsIndexValid(index)) return m_content[index];
            return null;
        }
        public bool TryGetElementAtIndex<U>(int index, out U uValue) where U : ScriptableObject
        {
            if (m_content.IsIndexValid(index)
                && m_content[index] is U u)
            {
                uValue = u;
                return true;
            }
            uValue = null;
            return false;
        }

        #endregion

        #region Editor Data Type

#if UNITY_EDITOR

        internal override bool Editor_ContainerHasValidDataType(out Type dataType)
        {
            if (base.Editor_ContainerHasValidDataType(out dataType))
            {
                return dataType.IsSubclassOf(typeof(ScriptableObject));
            }
            return false;
        }

#endif

        #endregion

        #region Editor Content Management

#if UNITY_EDITOR

        internal override void Editor_ShouldRecomputeContainerContent()
        {
            if (m_content == null) m_content = new();
            else m_content.Clear();

            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)))
            {
                if (Editor_IsElementValid(obj)
                    && obj is ScriptableObject so)
                {
                    m_content.Add(so);
                }
            }
            Editor_SortContent();

            base.Editor_ShouldRecomputeContainerContent();
        }

        internal override IEnumerable<UnityEngine.Object> Editor_GetContainerContent()
        {
            if (m_content != null)
            {
                foreach (var item in m_content)
                {
                    yield return item;
                }
            }
        }

        protected override bool Editor_OnDeleteElementAtIndex(int index)
        {
            if (index >= 0 && index < m_content.Count)
            {
                BaseDatabase.DeleteAsset(m_content[index], true);
                return true;
            }
            return false;
        }

        protected void Editor_SetContent(List<ScriptableObject> content)
        {
            if (m_content != null)
            {
                m_content.Clear();
                m_content.AddRange(content);
            }
            else
            {
                m_content = new(content);
            }
        }

        protected virtual void Editor_SortContent()
        {
            m_content.Sort(BaseDataContainer.Sort_ByName);
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(ScriptableDatabase<>), editorForChildClasses:true)]
    public class ScriptableDatabaseEditor : BaseDatabaseEditor
    {
        #region Members

        protected SerializedProperty p_content;

        protected float m_dataListWindowHeight = 170f;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            p_content = serializedObject.FindProperty("m_content");

            m_excludedProperties.Add(p_content.propertyPath);
        }

        #endregion

        #region GUI

        protected override void OnGUI()
        {
            DrawDefault();

            Rect dataListWindowRect = EditorGUILayout.GetControlRect(false, m_dataListWindowHeight);
            dataListWindowRect.x += 10f;
            dataListWindowRect.width -= 20f;
            OnContainerContentListWindowGUI(dataListWindowRect, refreshButton: true, addButton: true, contextButtons: true);

            EditorGUILayout.Space(5f);
            Separator(2f, Color.white);
            EditorGUILayout.Space(10f);

            DisplayContainerCurrentSelection();
        }

        protected override string ContainerInvalidDataTypeMessage()
        {
            return "The data type of this Database is not valid.\n\n" +
                    "- Add the DatabaseAttribute to the top of your script.\n" +
                    "- Make sure the dataType parameter inherits from ScriptableObject and implements at least the IDatabaseElement interface.";
        }

        #endregion

        #region Database Content

        protected override int ContentListCount
        {
            get
            {
                if (p_content != null)
                {
                    return p_content.arraySize;
                }
                return -1;
            }
        }

        protected override UnityEngine.Object GetContainerElementAtIndex(int index)
        {
            int count = ContentListCount;
            if (count > 0 && index >= 0 && index < count)
            {
                return p_content.GetArrayElementAtIndex(index).objectReferenceValue;
            }
            return null;
        }

        #endregion

        #region Data Creation

        protected override bool OnCreateNewData(out UnityEngine.Object obj)
        {
            if (ContainerHasValidDataType)
            {
                obj = BaseDatabase.CreateScriptableAndAddToAsset(DataType, m_database);
                return obj != null;
            }
            obj = null;
            return false;
        }
        protected override void OnAddNewDataToContainer(UnityEngine.Object obj)
        {
            if (AssetDatabase.IsMainAsset(obj))
            {
                BaseDatabase.AddAssetToOtherAsset(obj, m_database);
            }

            base.OnAddNewDataToContainer(obj);
        }

        #endregion
    }

#endif

    #endregion
}
