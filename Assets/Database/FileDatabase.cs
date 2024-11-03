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
            Debug.Log(folderPath);
            if (Directory.Exists(folderPath))
            {
                foreach (var path in Directory.GetFiles(folderPath))
                {
                    objPath = path.Substring(dataPath.Length);
                    obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(objPath);
                    if (obj != null)
                    {
                        m_folderContent.Add(obj);
                    }
                }
            }
            else
            {
                Debug.LogError("Invalid folder path");
            }
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

        protected bool m_fileDatabaseFoldoutOpen;
        protected Vector2 m_dataListWindowScrollPos;
        protected int m_selectionIndex = -1;

        protected float m_dataListWindowHeight = 150f;
        protected float m_elementHeight = 20f;

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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            m_fileDatabaseFoldoutOpen = EditorGUILayout.Foldout(m_fileDatabaseFoldoutOpen, "Database Informations", true, EditorGUIHelper.foldoutStyle);
            if (m_fileDatabaseFoldoutOpen)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.Space(5f);
                OnDatabaseInformationsGUI();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10f);
            Rect dataListWindow = EditorGUILayout.GetControlRect(false, m_dataListWindowHeight);
            OnDataListWindowGUI(new Rect(dataListWindow.x + 10f, dataListWindow.y, dataListWindow.width - 20f, dataListWindow.height));

            EditorGUILayout.Space(15f);

            DrawDefault();
        }

        protected virtual void OnDatabaseInformationsGUI()
        {
            EditorGUIHelper.FolderPicker(p_folderName, new GUIContent("Folder"), OnFolderChanged);
        }

        protected virtual void OnDataListWindowGUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, EditorGUIHelper.transparentBlack01);

            List<UnityEngine.Object> content = m_database.Editor_GetDatabaseContent().ToList();

            bool needScrollRect = content.Count * m_elementHeight > m_dataListWindowHeight;
            if (needScrollRect)
            {
                Rect viewRect = new Rect(0, 0, rect.width - 15f, content.Count * m_elementHeight);
                m_dataListWindowScrollPos = GUI.BeginScrollView(rect, m_dataListWindowScrollPos, viewRect);

                Rect dataRect = new Rect(0, 0, viewRect.width, m_elementHeight);
                for (int i = 0; i < content.Count; i++)
                {
                    OnElementGUI(dataRect, i, content[i]);
                    dataRect.y += m_elementHeight;
                }

                GUI.EndScrollView();
            }
            else
            {
                Rect dataRect = new Rect(rect.x, rect.y, rect.width, m_elementHeight);
                for (int i = 0; i < content.Count; i++)
                {
                    OnElementGUI(dataRect, i, content[i]);
                    dataRect.y += m_elementHeight;
                }
            }
        }
        protected virtual void OnElementGUI(Rect rect, int index, UnityEngine.Object element)
        {
            if (GUI.Button(rect, GUIContent.none, new GUIStyle()))
            {
                m_selectionIndex = index;
            }
            bool selected = m_selectionIndex == index;
            OnElementBackgroundGUI(rect, index, selected, element);

            switch (element)
            {
                case null:
                    OnNullElementGUI(rect, index, element); break;
                case GameObject go:
                    OnGameObjectElementGUI(rect, index, selected, go); break;
                case ScriptableObject so:
                    OnScriptableObjectElementGUI(rect, index, selected, so); break;
                default:
                    OnOtherObjectElementGUI(rect, index, selected, element); break;
            }
        }
        protected virtual void OnElementBackgroundGUI(Rect rect, int index, bool selected, UnityEngine.Object element)
        {
            EditorGUI.DrawRect(rect, selected ? Color.grey : (index % 2 == 0 ? EditorGUIHelper.transparentBlack02 : EditorGUIHelper.transparentBlack04));
        }

        protected virtual void OnNullElementGUI(Rect rect, int index, bool selected)
        {
            Rect labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 5f, rect.height);
            EditorGUI.LabelField(labelRect, "Null");
        }
        protected virtual void OnGameObjectElementGUI(Rect rect, int index, bool selected, GameObject element)
        {
            Rect labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 5f, rect.height);
            EditorGUI.LabelField(labelRect, element.name);
        }
        protected virtual void OnScriptableObjectElementGUI(Rect rect, int index, bool selected, ScriptableObject element)
        {
            Rect labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 5f, rect.height);
            EditorGUI.LabelField(labelRect, element.name);
        }
        protected virtual void OnOtherObjectElementGUI(Rect rect, int index, bool selected, UnityEngine.Object obj)
        {
            Rect labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 5f, rect.height);
            EditorGUI.LabelField(labelRect, obj.name);
        }

        #endregion

        #region Callbacks

        protected virtual void OnFolderChanged() 
        {
            m_database.Editor_ShouldRecomputeDatabaseContent();
        }

        #endregion
    }

#endif

    #endregion
}
