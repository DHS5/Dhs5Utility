using UnityEngine;
using Dhs5.Utility.GUIs;
using Dhs5.Utility.Databases;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;

namespace Dhs5.Utility.Console
{
    public class DebuggerWindow : EditorWindow
    {
        #region Static Constructor

        [MenuItem("Window/Dhs5 Utility/Debugger", priority = 100)]
        public static void OpenWindow()
        {
            DebuggerWindow window = GetWindow<DebuggerWindow>();
            window.titleContent = new GUIContent(EditorGUIHelper.DebugIcon) { text = "Debugger" };
        }

        #endregion

        #region Asset Management

        private DebuggerAsset m_debuggerAsset;
        private DebuggerAsset Asset
        {
            get
            {
                if (m_debuggerAsset == null)
                {
                    m_debuggerAsset = DebuggerAsset.Instance;
                }
                return m_debuggerAsset;
            }
        }

        #endregion

        #region Asset Editor

        private DebuggerAssetEditor m_debuggerAssetEditor;
        private DebuggerAssetEditor AssetEditor
        {
            get
            {
                if (m_debuggerAssetEditor == null && Asset != null)
                {
                    m_debuggerAssetEditor = Editor.CreateEditor(Asset, typeof(DebuggerAssetEditor)) as DebuggerAssetEditor;
                }
                return m_debuggerAssetEditor;
            }
        }

        #endregion


        #region Members

        private int m_currentWindow;

        #endregion

        #region GUI Content

        private GUIContent g_title = new GUIContent("Debugger");
        private GUIContent[] g_windowOptions = new GUIContent[] { new GUIContent("Categories"), new GUIContent("Runtime"), new GUIContent("Settings") };

        #endregion


        #region Core Behaviour

        private void OnDisable()
        {
            if (m_debuggerAssetEditor != null)
            {
                DestroyImmediate(m_debuggerAssetEditor);
            }
        }

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

                // RUNTIME
                case 1:
                    if (Application.isPlaying)
                    {
                        
                    }
                    break;

                // SETTINGS
                case 2:
                    DrawSettingsGUI();
                    break;
            }

            if (AssetEditor != null)
            {
                AssetEditor.serializedObject.ApplyModifiedProperties();
            }
        }

        #endregion

        #region SETTINGS GUI

        private void DrawSettingsGUI()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Asset", EditorStyles.boldLabel);

            // Assets
            var array = Resources.LoadAll<DebuggerAsset>("Debugger");
            if (array != null && array.Length > 0)
            {
                if (array.Length == 1)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(array[0], typeof(DebuggerAsset), false);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.HelpBox("Too much Debugger Assets", MessageType.Error);
                    for (int i = 0; i < array.Length; i++)
                    {
                        var rect = EditorGUILayout.GetControlRect(false, 22f);

                        EditorGUI.BeginDisabledGroup(true);
                        var objectRect = new Rect(rect.x, rect.y, rect.width - 32f, rect.height - 2f);
                        EditorGUI.ObjectField(objectRect, array[i], typeof(DebuggerAsset), false);
                        EditorGUI.EndDisabledGroup();

                        var deleteButtonRect = new Rect(rect.x + rect.width - 30f, rect.y + 1f, 30f, rect.height - 2f);
                        using (new GUIHelper.GUIBackgroundColorScope(Color.red))
                        {
                            if (GUI.Button(deleteButtonRect, EditorGUIHelper.DeleteIcon))
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
                    Database.CreateAssetOfType(typeof(DebuggerAsset), "Assets/Resources/Debugger/Debugger.asset");
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