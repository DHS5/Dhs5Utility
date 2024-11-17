using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        protected int FindIndexOfElement(UnityEngine.Object element)
        {
            return m_content.FindIndex(e => e == element);
        }
        protected virtual void SortContent()
        {
            m_content.Sort(BaseDatabase.Sort_ByName);
        }

        #endregion

        #region Editor Content Management

#if UNITY_EDITOR

        internal override void Editor_ShouldRecomputeDatabaseContent()
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
            SortContent();

            base.Editor_ShouldRecomputeDatabaseContent();
        }

        internal override IEnumerable<Object> Editor_GetDatabaseContent()
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
            OnDatabaseContentListWindowGUI(dataListWindowRect, refreshButton: true, addButton: true, contextButtons: true);

            EditorGUILayout.Space(5f);
            Separator(2f, Color.white);
            EditorGUILayout.Space(10f);

            DisplayCurrentDatabaseContentListSelection();
        }

        #endregion

        #region Database Content

        protected override int DatabaseContentListCount
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

        protected override Object GetDatabaseContentElementAtIndex(int index)
        {
            int count = DatabaseContentListCount;
            if (count > 0 && index >= 0 && index < count)
            {
                return p_content.GetArrayElementAtIndex(index).objectReferenceValue;
            }
            return null;
        }

        #endregion

        #region Data Creation

        protected override bool OnCreateNewData(out Object obj)
        {
            if (BaseDatabase.HasDataType(m_database.GetType(), out var dataType)
                && dataType.IsSubclassOf(typeof(ScriptableObject)))
            {
                obj = BaseDatabase.CreateScriptableAndAddToAsset(dataType, m_database);
                return obj != null;
            }
            obj = null;
            return false;
        }
        protected override void OnAddNewDataToDatabase(Object obj)
        {
            if (AssetDatabase.IsMainAsset(obj))
            {
                BaseDatabase.AddAssetToOtherAsset(obj, m_database);
            }

            base.OnAddNewDataToDatabase(obj);
        }

        #endregion
    }

#endif

    #endregion
}
