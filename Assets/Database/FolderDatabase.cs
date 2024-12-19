using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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

        #region Accessors

        public override bool TryGetObjectByUID(int uid, out IDataContainerElement obj)
        {
            foreach (var item in m_folderContent)
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

        #region Editor Content Management

#if UNITY_EDITOR

        internal override void Editor_ShouldRecomputeContainerContent()
        {
            if (string.IsNullOrWhiteSpace(m_folderName)) return;

            if (m_folderContent == null) m_folderContent = new();
            else m_folderContent.Clear();

            string dataPath = Application.dataPath.Replace("Assets", "");
            string folderPath = dataPath + m_folderName;
            
            if (Directory.Exists(folderPath))
            {
                Editor_AddAllValidElementsInFolder(folderPath, dataPath.Length);
            }
            else
            {
                Debug.LogError("Invalid folder path");
            }

            base.Editor_ShouldRecomputeContainerContent();
        }
        private void Editor_AddAllValidElementsInFolder(string folderPath, int dataPathLength)
        {
            foreach (var insideFolderPath in Directory.GetDirectories(folderPath))
            {
                Editor_AddAllValidElementsInFolder(insideFolderPath, dataPathLength);
            }
            foreach (var path in Directory.GetFiles(folderPath))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path.Substring(dataPathLength));
                if (obj != null && Editor_IsElementValid(obj))
                {
                    m_folderContent.Add(obj);
                }
            }
        }

        internal override string Editor_GetDataName(UnityEngine.Object obj)
        {
            if (string.IsNullOrWhiteSpace(m_folderName)) return base.Editor_GetDataName(obj);

            var assetPath = AssetDatabase.GetAssetPath(obj).Substring(m_folderName.Length).TrimStart('/');
            return assetPath.Substring(0, assetPath.LastIndexOf("."));
        }

        protected override IEnumerable<UnityEngine.Object> Editor_GetContainerContent()
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
    public class FolderDatabaseEditor : BaseDatabaseEditor
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
            
            OnContainerInformationsGUI("Folder Informations");
            
            EditorGUILayout.Space(10f);
            
            Rect dataListWindowRect = EditorGUILayout.GetControlRect(false, m_dataListWindowHeight);
            dataListWindowRect.x += 10f;
            dataListWindowRect.width -= 20f;
            OnContainerContentListWindowGUI(dataListWindowRect, refreshButton:true, addButton:true, contextButtons:true);
            
            EditorGUILayout.Space(5f);
            Separator(2f, Color.white);
            EditorGUILayout.Space(10f);
            
            DisplayContainerCurrentSelection();
        }

        protected override void OnContainerInformationsContentGUI()
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

            ForceContainerContentRefresh();
        }

        #endregion

        #region Database Content

        //protected override int ContentListCount
        //{
        //    get
        //    {
        //        if (p_folderContent != null)
        //        {
        //            return p_folderContent.arraySize;
        //        }
        //        return -1;
        //    }
        //}

        //protected override UnityEngine.Object GetContainerElementAtIndex(int index)
        //{
        //    int count = ContentListCount;
        //    if (count > 0 && index >= 0 && index < count)
        //    {
        //        return p_folderContent.GetArrayElementAtIndex(index).objectReferenceValue;
        //    }
        //    return null;
        //}

        protected override EContentListDisplayType GetContentListDisplayType()
        {
            return EContentListDisplayType.FOLDERS;
        }

        #endregion

        #region Data Creation

        protected override bool OnCreateNewData(out UnityEngine.Object obj)
        {
            return CreateNewDataAtPath(FolderName + "/New", out obj);
        }
        protected virtual bool CreateNewDataAtPath(string path, out UnityEngine.Object obj)
        {
            if (ContainerHasValidDataType)
            {
                path = AssetDatabase.GenerateUniqueAssetPath(path);

                if (DataType.IsSubclassOf(typeof(ScriptableObject)))
                {
                    obj = OnCreateNewScriptableObject(path, DataType);
                    return obj != null;
                }
                else if (DataType.IsSubclassOf(typeof(Component)))
                {
                    obj = OnCreateNewPrefabWithComponent(path, DataType);
                    return obj != null;
                }
                else if (DataType == typeof(GameObject))
                {
                    obj = OnCreateNewEmptyPrefab(path);
                    return obj != null;
                }
            }
            obj = null;
            return false;
        }

        protected override void OnAddNewDataToContainer(UnityEngine.Object obj)
        {
            if (EditorUtils.GetAssetContainingFolder(obj) == FolderName
                || BaseDatabase.MoveAssetToFolder(obj, FolderName))
            {
                base.OnAddNewDataToContainer(obj);
            }
        }

        #endregion

        #region Preview

        public override bool HasPreviewGUI()
        {
            var editor = GetOrCreateEditorFor(GetContainerCurrentSelection());
            if (editor != null)
            {
                return editor.HasPreviewGUI();
            }
            return false;
        }
        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            var editor = GetOrCreateEditorFor(GetContainerCurrentSelection());
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
