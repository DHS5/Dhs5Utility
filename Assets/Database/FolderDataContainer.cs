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
    public abstract class FolderDataContainer : BaseDataContainer
    {
        #region Members

        [SerializeField, FolderPicker] private string m_folderName;
        [SerializeField] private List<UnityEngine.Object> m_folderContent;

        #endregion

        #region Accessors

        public override int Count => m_folderContent.Count;

        public override UnityEngine.Object GetDataAtIndex(int index)
        {
            if (m_folderContent.IsIndexValid(index))
            {
                return m_folderContent[index];
            }
            return null;
        }

        public override bool TryGetDataByUID(int uid, out UnityEngine.Object obj)
        {
            foreach (var item in m_folderContent)
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

        internal override string Editor_GetDataPrefixedName(UnityEngine.Object obj)
        {
            if (string.IsNullOrWhiteSpace(m_folderName)) return base.Editor_GetDataPrefixedName(obj);

            if (obj != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(obj).Substring(m_folderName.Length).TrimStart('/');
                return assetPath.Substring(0, assetPath.LastIndexOf('.'));
            }
            return null;
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

        protected override bool Editor_OnDeleteElementByUID(int uid)
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                if (m_folderContent[i] is IDataContainerElement elem 
                    && elem.UID == uid)
                {
                    Database.DeleteAsset(m_folderContent[i], true);
                    return true;
                }
            }
            return false;
        }

        protected override void Editor_CleanUp()
        {
            
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(FolderDataContainer), editorForChildClasses:true)]
    public class FolderDataContainerEditor : BaseDataContainerEditor
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
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(p_folderName, true);
            if (EditorGUI.EndChangeCheck())
            {
                OnFolderSelected();
            }

            base.OnContainerInformationsContentGUI();
        }

        protected override string ContainerInvalidDataTypeMessage()
        {
            return "The data type of this Database is not valid.\n\n" +
                    "- Add the DatabaseAttribute to the top of your script.\n" +
                    "- Make sure the dataType parameter implements at least the IDataContainerElement interface.";
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
            serializedObject.ApplyModifiedProperties();

            ForceContainerContentRefresh();
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
                || Database.MoveAssetToFolder(obj, FolderName))
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
