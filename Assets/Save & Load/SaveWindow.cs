using UnityEngine;
using Dhs5.Utility.GUIs;
using Dhs5.Utility.Databases;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;

namespace Dhs5.Utility.SaveLoad
{
    public class SaveWindow : EditorWindow
    {
        #region Static Constructor

        [MenuItem("Window/Dhs5 Utility/Save & Load", priority = 100)]
        public static void OpenWindow()
        {
            SaveWindow window = GetWindow<SaveWindow>();
            window.titleContent = new GUIContent(EditorGUIHelper.SaveIcon) { text = "Save Load" };
        }

        #endregion

        #region Asset Management

        private SaveAsset m_saveAsset;
        private SaveAsset Asset
        {
            get
            {
                if (m_saveAsset == null)
                {
                    m_saveAsset = SaveAsset.Instance;
                }
                return m_saveAsset;
            }
        }

        #endregion

        #region Asset Editor

        private SaveAssetEditor m_saveAssetEditor;
        private SaveAssetEditor AssetEditor
        {
            get
            {
                if (m_saveAssetEditor == null && Asset != null)
                {
                    m_saveAssetEditor = Editor.CreateEditor(Asset, typeof(SaveAssetEditor)) as SaveAssetEditor;
                }
                return m_saveAssetEditor;
            }
        }

        #endregion


        #region Members

        private int m_currentWindow;

        // DEBUG
        private SaveObject m_debugSave_SaveObject;
        private TextAsset m_debugSave_TextAsset;

        private SaveObject m_debugLoad_SaveObject;
        private TextAsset m_debugLoad_TextAsset;

        #endregion

        #region GUI Content

        private GUIContent g_title = new GUIContent("Save & Load");
        private GUIContent[] g_windowOptions = new GUIContent[] { new GUIContent("Categories"), new GUIContent("Load"), new GUIContent("Debug"), new GUIContent("Settings") };

        private GUIContent g_saveTest = new GUIContent("Save Test");
        private GUIContent g_create = new GUIContent("Create");
        private GUIContent g_buttonSaveTest = new GUIContent("SAVE OBJECT INTO TEXT ASSET");
        
        private GUIContent g_loadTest = new GUIContent("Load Test");
        private GUIContent g_buttonLoadTest = new GUIContent("LOAD FROM TEXT ASSET");

        #endregion


        #region Core GUI

        private void OnGUI()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(g_title, GUIHelper.bigTitleLabel);

            EditorGUILayout.Space(10f);
            m_currentWindow = GUILayout.Toolbar(m_currentWindow, g_windowOptions);

            var rect = EditorGUILayout.GetControlRect(false, 2f);
            rect.x = 0f; rect.width = position.width;
            EditorGUI.DrawRect(rect, Color.white);
            EditorGUILayout.Space(5f);

            if (AssetEditor != null)
            {
                AssetEditor.serializedObject.Update();
            }
            switch (m_currentWindow)
            {
                // CATEGORIES
                case 0:
                    if (Asset != null && AssetEditor != null)
                    {
                        AssetEditor.DrawCategoriesGUI();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No active asset found", MessageType.Warning);
                    }
                    break;
                    
                // LOAD
                case 1:
                    if (Asset != null && AssetEditor != null)
                    {
                        AssetEditor.DrawLoadGUI();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No active asset found", MessageType.Warning);
                    }
                    break;

                // DEBUG
                case 2:
                    DrawDebugGUI();
                    break;
                    
                // SETTINGS
                case 3:
                    DrawSettingsGUI();
                    break;
            }

            if (AssetEditor != null)
            {
                AssetEditor.serializedObject.ApplyModifiedProperties();
            }
        }

        #endregion
        
        #region DEBUG GUI

        private void DrawDebugGUI()
        {
            // SAVE TEST
            {
                EditorGUILayout.LabelField(g_saveTest, EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                m_debugSave_SaveObject = EditorGUILayout.ObjectField(m_debugSave_SaveObject, typeof(SaveObject), false) as SaveObject;
                if (m_debugSave_SaveObject == null)
                {
                    if (GUILayout.Button(g_create))
                    {
                        var path = EditorUtility.SaveFilePanelInProject("New asset location", "TestSaveObject", "asset", "Choose location to create the instance");
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            m_debugSave_SaveObject = Database.CreateScriptableAsset<SaveObject>(path, triggerRename:false);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (m_debugSave_SaveObject != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    m_debugSave_TextAsset = EditorGUILayout.ObjectField(m_debugSave_TextAsset, typeof(TextAsset), false) as TextAsset;
                    if (m_debugSave_TextAsset == null)
                    {
                        if (GUILayout.Button(g_create))
                        {
                            var path = EditorUtility.SaveFilePanelInProject("New asset location", "TestTextAsset", "txt", "Choose location to create the instance");
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                m_debugSave_TextAsset = Database.CreateEmptyTextAsset(path);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (m_debugSave_TextAsset != null && GUILayout.Button(g_buttonSaveTest))
                    {
                        m_debugSave_SaveObject.Editor_RefreshDictionaryFromArray();
                        var content = m_debugSave_SaveObject.GetSaveContent();
                        Database.CreateOrOverwriteTextAsset(m_debugSave_TextAsset, content);
                    }
                }
            }

            EditorGUILayout.Space(5f);

            // LOAD TEST
            {
                EditorGUILayout.LabelField(g_loadTest, EditorStyles.boldLabel);
                m_debugLoad_TextAsset = EditorGUILayout.ObjectField(m_debugLoad_TextAsset, typeof(TextAsset), false) as TextAsset;

                if (m_debugLoad_TextAsset != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    m_debugLoad_SaveObject = EditorGUILayout.ObjectField(m_debugLoad_SaveObject, typeof(SaveObject), false) as SaveObject;
                    if (m_debugLoad_SaveObject == null)
                    {
                        if (GUILayout.Button(g_create))
                        {
                            var path = EditorUtility.SaveFilePanelInProject("New asset location", "TestSaveObject", "asset", "Choose location to create the instance");
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                m_debugLoad_SaveObject = Database.CreateScriptableAsset<SaveObject>(path, triggerRename: false);
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (m_debugLoad_SaveObject != null && GUILayout.Button(g_buttonLoadTest))
                    {
                        m_debugLoad_SaveObject.Load(m_debugLoad_TextAsset.text);
                    }
                }
            }
        }

        #endregion

        #region SETTINGS GUI

        private void DrawSettingsGUI()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Asset", EditorStyles.boldLabel);

            // Assets
            var array = Resources.LoadAll<SaveAsset>("Save");
            if (array != null && array.Length > 0)
            {
                if (array.Length == 1)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(array[0], typeof(SaveAsset), false);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.HelpBox("Too much SaveLoad Assets", MessageType.Error);
                    for (int i = 0; i < array.Length; i++)
                    {
                        var rect = EditorGUILayout.GetControlRect(false, 22f);

                        EditorGUI.BeginDisabledGroup(true);
                        var objectRect = new Rect(rect.x, rect.y, rect.width - 32f, rect.height - 2f);
                        EditorGUI.ObjectField(objectRect, array[i], typeof(SaveAsset), false);
                        EditorGUI.EndDisabledGroup();

                        var usedButtonRect = new Rect(rect.x + rect.width - 30f, rect.y + 1f, 30f, rect.height - 2f);
                        using (new GUIHelper.GUIBackgroundColorScope(Color.red))
                        {
                            if (GUI.Button(usedButtonRect, EditorGUIHelper.DeleteIcon))
                            {
                                Database.DeleteAsset(array[i], true);
                                AssetDatabase.SaveAssets();
                            }
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No assets found", MessageType.Warning);
                EditorGUILayout.Space(5f);

                // Create asset button
                if (GUILayout.Button("Create new asset"))
                {
                    Database.CreateAssetOfType(typeof(SaveAsset), "Assets/Resources/SaveLoad/SaveLoad.asset");
                    AssetDatabase.SaveAssets();
                }
            }

            // Assets Settings
            if (AssetEditor != null)
            {
                EditorGUILayout.Space(5f);
                AssetEditor.DrawSettingsGUI();
            }
        }

        #endregion
    }
}

#endif