using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Scenes
{
    [Serializable]
    public class SceneReference
#if UNITY_EDITOR
        : ISerializationCallbackReceiver
#endif
    {
        #region Global Members

#if UNITY_EDITOR
        [SerializeField] private SceneAsset m_sceneAsset;
#endif

        [SerializeField] private int m_sceneIndex;
        [SerializeField] private string m_sceneName;
        [SerializeField] private string m_scenePath;

        #endregion

        #region Properties

        public int SceneIndex => m_sceneIndex;
        public string SceneName => m_sceneName;
        public string ScenePath => m_scenePath;

        public bool IsLoaded
        {
            get
            {
                var scene = SceneManager.GetSceneByBuildIndex(SceneIndex);
                return scene.IsValid() && scene.isLoaded;
            }
        }

        #endregion

        #region Utility Methods

        public bool LoadScene(LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (SceneIndex > -1)
            {
                SceneManager.LoadScene(SceneIndex, mode);
                return true;
            }
            return false;
        }
        public bool UnloadScene(UnloadSceneOptions options = UnloadSceneOptions.None)
        {
            var scene = SceneManager.GetSceneByName(SceneName);
            if (scene.IsValid())
            {
                SceneManager.UnloadSceneAsync(scene, options);
                return true;
            }
            return false;
        }

        #endregion


        #region Serialization Callbacks

#if UNITY_EDITOR
        public void OnBeforeSerialize()
        {
            FetchSceneInfosFromAsset();
        }

        public void OnAfterDeserialize()
        {
            EditorApplication.delayCall += OnAfterAfterDeserialize;
            //EditorApplication.update += OnAfterAfterDeserialize;
        }

        private void OnAfterAfterDeserialize()
        {
            //EditorApplication.update -= OnAfterAfterDeserialize;

            if (m_sceneAsset == null)
            {
                FetchAssetFromPath();
            }
        }
#endif

        #endregion

        #region Editor Utility

#if UNITY_EDITOR

        private void FetchSceneInfosFromAsset()
        {
            if (m_sceneAsset != null)
            {
                m_sceneName = m_sceneAsset.name;
                m_scenePath = AssetDatabase.GetAssetPath(m_sceneAsset);
                m_sceneIndex = SceneUtility.GetBuildIndexByScenePath(m_scenePath);
            }
            else
            {
                m_sceneName = string.Empty;
                m_scenePath = string.Empty;
                m_sceneIndex = -1;
            }
        }

        private void FetchAssetFromPath()
        {
            if (!string.IsNullOrWhiteSpace(m_scenePath))
            {
                m_sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            }
        }

#endif

        #endregion
    }

    #region Drawer

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        SerializedProperty p_sceneAsset;
        SerializedProperty p_sceneName;
        SerializedProperty p_scenePath;
        SerializedProperty p_sceneIndex;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            p_sceneAsset = property.FindPropertyRelative("m_sceneAsset");
            p_scenePath = property.FindPropertyRelative("m_scenePath");
            p_sceneName = property.FindPropertyRelative("m_sceneName");
            p_sceneIndex = property.FindPropertyRelative("m_sceneIndex");

            EditorGUI.BeginProperty(position, label, property);

            Rect rect = new Rect(position.x, position.y, position.width, 20f);
            float buttonWidth = 30f;
            var buttonRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
            // Click
            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && buttonRect.Contains(Event.current.mousePosition))
            {
                property.isExpanded = !property.isExpanded;
                Event.current.Use();
            }
            // Repaint
            if (Event.current.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(buttonRect, label, 0, property.isExpanded);
            }

            Rect propertyRect = new Rect(rect.x + EditorGUIUtility.labelWidth + 2f, rect.y, rect.width - EditorGUIUtility.labelWidth - 2f - buttonWidth, rect.height);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(propertyRect, p_sceneAsset, GUIContent.none, true);
            if (EditorGUI.EndChangeCheck() | GUI.Button(new Rect(rect.x + rect.width - buttonWidth, rect.y - 1f, buttonWidth, rect.height - 1f), EditorGUIUtility.IconContent("d_Refresh")))
            {
                if (p_sceneAsset.objectReferenceValue != null)
                {
                    p_scenePath.stringValue = AssetDatabase.GetAssetPath(p_sceneAsset.objectReferenceValue);
                    p_sceneName.stringValue = p_sceneAsset.objectReferenceValue.name;
                    p_sceneIndex.intValue = SceneUtility.GetBuildIndexByScenePath(p_scenePath.stringValue);
                }
            }

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                float space = 50f;
                GUIContent pathContent = new GUIContent("Path : " + p_scenePath.stringValue);
                float pathWidth = EditorStyles.label.CalcSize(pathContent).x + space;

                rect.y += rect.height;
                rect.width = pathWidth;
                EditorGUI.LabelField(rect, pathContent);

                rect.x += rect.width;
                rect.width = position.width - rect.x;
                EditorGUI.LabelField(rect, new GUIContent("Build Index : " + p_sceneIndex.intValue));

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded ? 42f : 22f;
        }
    }

#endif

    #endregion
}
