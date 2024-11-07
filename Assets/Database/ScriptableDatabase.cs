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

        #region Utility

        protected int FindIndexOfElement(UnityEngine.Object element)
        {
            return m_content.FindIndex(e => e == element);
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
                BaseDatabase.DeleteNestedAsset(m_content[index], this, true);
                return true;
            }
            return false;
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

            OnDatabaseInformationsGUI();

            EditorGUILayout.Space(10f);

            Rect dataListWindowRect = EditorGUILayout.GetControlRect(false, m_dataListWindowHeight);
            dataListWindowRect.x += 10f;
            dataListWindowRect.width -= 20f;
            OnDatabaseContentListWindowGUI(dataListWindowRect, refreshButton: true, addButton: true, deleteButtons: true);

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

        #endregion
    }

#endif

    #endregion
}
