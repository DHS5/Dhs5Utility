using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Dhs5.Utility.Databases
{
    public abstract class ScriptableDataContainer : BaseDataContainer
    {
        #region Members

        [SerializeField] private List<ScriptableObject> m_content;

        #endregion

        #region Accessors

        public override int Count => m_content.Count;

        public override UnityEngine.Object GetDataAtIndex(int index)
        {
            if (m_content.IsIndexValid(index))
            {
                return m_content[index];
            }
            return null;
        }

        public override bool TryGetDataByUID(int uid, out UnityEngine.Object obj)
        {
            foreach (var item in m_content)
            {
                if (item is IDataContainerElement elem && elem.UID == uid)
                {
                    obj = item;
                    return true;
                }
            }

            obj = null;
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

            List<UnityEngine.Object> toDestroy = new();
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)))
            {
                if (Editor_IsElementValid(obj)
                    && obj is ScriptableObject so)
                {
                    m_content.Add(so);
                }
                else
                {
                    toDestroy.Add(obj);
                }
            }
            foreach (var item in toDestroy)
            {
                DestroyImmediate(item, true);
            }

            Editor_SortContent();

            base.Editor_ShouldRecomputeContainerContent();
        }

        protected override IEnumerable<UnityEngine.Object> Editor_GetContainerContent()
        {
            if (m_content != null)
            {
                foreach (var item in m_content)
                {
                    yield return item;
                }
            }
        }

        protected override void Editor_CleanUp()
        {
            for (int i = m_content.Count - 1; i >= 0; i--)
            {
                if (m_content[i] == null)
                {
                    m_content.RemoveAt(i);
                }
            }

            List<UnityEngine.Object> toDestroy = new();
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)))
            {
                if (!m_content.Contains(obj))
                {
                    toDestroy.Add(obj);
                }
            }
            foreach (var item in toDestroy)
            {
                DestroyImmediate(item);
            }

            Debug.Log("cleaned up");
        }

        protected override bool Editor_OnDeleteElementAtIndex(int index)
        {
            if (index >= 0 && index < m_content.Count)
            {
                Database.DeleteAsset(m_content[index], true);
                return true;
            }
            return false;
        }

        protected void Editor_SortContent()
        {
            m_content.Sort(Editor_SortComparison());
        }
        protected virtual Comparison<ScriptableObject> Editor_SortComparison()
        {
            return BaseDataContainer.Sort_ByName;
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(ScriptableDataContainer), editorForChildClasses:true)]
    public class ScriptableDataContainerEditor : BaseDataContainerEditor
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

        protected override EContentListDisplayType GetContentListDisplayType()
        {
            return EContentListDisplayType.FOLDERS;
        }

        #endregion

        #region Data Creation

        protected override bool OnCreateNewData(out UnityEngine.Object obj)
        {
            if (ContainerHasValidDataType)
            {
                obj = Database.CreateScriptableAndAddToAsset(DataType, m_container);
                return obj != null;
            }
            obj = null;
            return false;
        }
        protected override void OnAddNewDataToContainer(UnityEngine.Object obj)
        {
            if (AssetDatabase.IsMainAsset(obj))
            {
                Database.AddAssetToOtherAsset(obj, m_container);
            }

            base.OnAddNewDataToContainer(obj);
        }

        #endregion
    }

#endif

    #endregion
}
