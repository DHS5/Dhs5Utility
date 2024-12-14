using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Databases
{
    public class FolderDatabase<T> : Database<T> where T : FolderDatabase<T>
    {
        #region Members

        [SerializeField] private string m_folderName;
        [SerializeField] private List<UnityEngine.Object> m_folderContent;

        #endregion

        #region Editor Content Management

#if UNITY_EDITOR

        internal override void Editor_ShouldRecomputeDatabaseContent()
        {
            if (string.IsNullOrWhiteSpace(m_folderName)) return;

            if (m_folderContent == null) m_folderContent = new();
            else m_folderContent.Clear();

            UnityEngine.Object obj;
            string objPath;
            string dataPath = Application.dataPath.Replace("Assets", "");
            string folderPath = dataPath + m_folderName;
            
            if (Directory.Exists(folderPath))
            {
                foreach (var path in Directory.GetFiles(folderPath))
                {
                    objPath = path.Substring(dataPath.Length);
                    obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(objPath);
                    if (Editor_IsElementValid(obj))
                    {
                        m_folderContent.Add(obj);
                    }
                }
            }
            else
            {
                Debug.LogError("Invalid folder path");
            }

            base.Editor_ShouldRecomputeDatabaseContent();
        }

        internal override IEnumerable<UnityEngine.Object> Editor_GetDatabaseContent()
        {
            if (m_folderContent != null)
            {
                foreach (var item in m_folderContent)
                {
                    yield return item;
                }
            }
        }

        protected override bool Editor_OnDeleteElementAtIndex(int index)
        {
            if (index >= 0 
                && index < m_folderContent.Count
                && BaseDatabase.IsAssetDeletableFromCode(m_folderContent[index]))
            {
                BaseDatabase.DeleteAsset(m_folderContent[index], true);
                return true;
            }
            return false;
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(FolderDatabase<>), editorForChildClasses:true)]
    public class FileDatabaseEditor : BaseDatabaseEditor
    {
        #region Members

        protected SerializedProperty p_folderName;
        protected SerializedProperty p_folderContent;

        protected string m_currentFolderName;

        protected float m_dataListWindowHeight = 170f;

        #endregion

        #region Properties

        protected string FolderName => p_folderName.stringValue;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            p_folderName = serializedObject.FindProperty("m_folderName");
            p_folderContent = serializedObject.FindProperty("m_folderContent");

            m_excludedProperties.Add(p_folderName.propertyPath);
            m_excludedProperties.Add(p_folderContent.propertyPath);

            m_currentFolderName = p_folderName.stringValue;
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
            OnDatabaseContentListWindowGUI(dataListWindowRect, refreshButton:true, addButton:true, contextButtons:true);
            
            EditorGUILayout.Space(5f);
            Separator(2f, Color.white);
            EditorGUILayout.Space(10f);
            
            DisplayCurrentDatabaseContentListSelection();
        }

        protected override void OnDatabaseInformationsContentGUI()
        {
            EditorGUIHelper.FolderPicker(p_folderName, new GUIContent("Folder"), OnFolderSelected);
        }

        #endregion

        #region Callbacks

        protected virtual void OnFolderSelected() 
        {
            if (m_currentFolderName != p_folderName.stringValue)
            {
                OnFolderChanged(m_currentFolderName, p_folderName.stringValue);
            }
        }
        protected virtual void OnFolderChanged(string former, string current)
        {
            m_currentFolderName = current;

            ForceDatabaseContentRefresh();
        }

        protected override void OnDatabaseContentChanged()
        {
            base.OnDatabaseContentChanged();

            DatabaseContentListSelectionIndex = -1;
        }

        #endregion

        #region Database Content

        protected override int DatabaseContentListCount
        {
            get
            {
                if (p_folderContent != null)
                {
                    return p_folderContent.arraySize;
                }
                return -1;
            }
        }

        protected override UnityEngine.Object GetDatabaseContentElementAtIndex(int index)
        {
            int count = DatabaseContentListCount;
            if (count > 0 && index >= 0 && index < count)
            {
                return p_folderContent.GetArrayElementAtIndex(index).objectReferenceValue;
            }
            return null;
        }

        #endregion

        #region Data Creation

        protected override bool OnCreateNewData(out UnityEngine.Object obj)
        {
            return CreateNewData(FolderName + "/New", out obj);
        }
        protected override void OnAddNewDataToDatabase(Object obj)
        {
            if (EditorUtils.GetAssetContainingFolder(obj) == FolderName
                || BaseDatabase.MoveAssetToFolder(obj, FolderName))
            {
                base.OnAddNewDataToDatabase(obj);
            }
        }

        #endregion

        #region Preview

        public override bool HasPreviewGUI()
        {
            var editor = GetOrCreateEditorFor(GetDatabaseCurrentSelection());
            if (editor != null)
            {
                return editor.HasPreviewGUI();
            }
            return false;
        }
        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            var editor = GetOrCreateEditorFor(GetDatabaseCurrentSelection());
            if (editor != null)
            {
                editor.OnPreviewGUI(r, background);
            }
        }

        #endregion
    }

#endif

    #endregion
}
