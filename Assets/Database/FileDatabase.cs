using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Dhs5.Utility.Databases
{
    public class FileDatabase<T> : Database<T> where T : FileDatabase<T>
    {
        #region Members

        [SerializeField] private string m_folderName;
        [SerializeField] private List<UnityEngine.Object> m_folderContent;

        #endregion

        #region Editor Callbacks

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

#endif

        #endregion

        #region Editor Utility

#if UNITY_EDITOR

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

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(FileDatabase<>), editorForChildClasses:true)]
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
        }

        #endregion

        #region GUI

        protected override void OnGUI()
        {
            DrawDefault();

            OnDatabaseInformationsGUI();

            EditorGUILayout.Space(10f);

            Rect dataListWindow = EditorGUILayout.GetControlRect(false, m_dataListWindowHeight);
            OnDatabaseContentListWindowGUI(new Rect(dataListWindow.x + 10f, dataListWindow.y, dataListWindow.width - 20f, dataListWindow.height));

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
    }

#endif

    #endregion
}
