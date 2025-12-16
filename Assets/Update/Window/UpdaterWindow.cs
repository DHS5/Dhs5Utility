using UnityEngine;
using Dhs5.Utility.Databases;
using Dhs5.Utility.GUIs;
using System.IO;
using Dhs5.Utility.Settings;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;

namespace Dhs5.Utility.Updates
{
    public class UpdaterWindow : EditorWindow
    {
        #region Static Constructor

        [MenuItem("Window/Dhs5 Utility/Updater", priority = 100)]
        public static void OpenWindow()
        {
            UpdaterWindow window = GetWindow<UpdaterWindow>();
            window.titleContent = new GUIContent(EditorGUIUtility.IconContent("d_UnityEditor.AnimationWindow")) { text = "Updater" };
        }

        #endregion

        #region Asset Management

        private UpdaterAsset m_updaterAsset;
        private UpdaterAsset Asset
        {
            get
            {
                if (m_updaterAsset == null)
                {
                    m_updaterAsset = UpdaterAsset.Instance;
                }
                return m_updaterAsset;
            }
        }

        #endregion

        #region Asset Editor

        private UpdaterAssetEditor m_updaterAssetEditor;
        private UpdaterAssetEditor AssetEditor
        {
            get
            {
                if (m_updaterAssetEditor == null && Asset != null)
                {
                    m_updaterAssetEditor = Editor.CreateEditor(Asset, typeof(UpdaterAssetEditor)) as UpdaterAssetEditor;
                }
                return m_updaterAssetEditor;
            }
        }

        #endregion


        #region Members

        private int m_currentWindow;

        #endregion

        #region GUI Content

        private GUIContent g_title = new GUIContent("Updater");
        private GUIContent[] g_windowOptions = new GUIContent[] { new GUIContent("Channels"), new GUIContent("Conditions"), new GUIContent("Timelines"), new GUIContent("Settings") };

        #endregion


        #region Core Behaviour

        private void OnDisable()
        {
            if (m_updaterAssetEditor != null)
            {
                DestroyImmediate(m_updaterAssetEditor);
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
                // CHANNELS
                case 0:
                    if (Asset != null && AssetEditor != null)
                    {
                        AssetEditor.DrawChannelsGUI();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No active asset found", MessageType.Warning);
                    }
                    break;
                    
                // UPDATE CONDITIONS
                case 1:
                    if (Asset != null && AssetEditor != null)
                    {
                        AssetEditor.DrawConditonsGUI();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No active asset found", MessageType.Warning);
                    }
                    break;
                    
                // TIMELINES
                case 2:
                    if (Asset != null && AssetEditor != null)
                    {
                        AssetEditor.DrawTimelinesGUI();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No active asset found", MessageType.Warning);
                    }
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

        #region SETTINGS GUI

        private void DrawSettingsGUI()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Asset", EditorStyles.boldLabel);

            // Assets
            var array = Resources.LoadAll<UpdaterAsset>("Updater");
            if (array != null && array.Length > 0)
            {
                if (array.Length == 1)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(array[0], typeof(UpdaterAsset), false);
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.HelpBox("Too much Updater Assets", MessageType.Error);
                    for (int i = 0; i < array.Length; i++)
                    {
                        var rect = EditorGUILayout.GetControlRect(false, 22f);

                        EditorGUI.BeginDisabledGroup(true);
                        var objectRect = new Rect(rect.x, rect.y, rect.width - 32f, rect.height - 2f);
                        EditorGUI.ObjectField(objectRect, array[i], typeof(UpdaterAsset), false);
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
                    Database.CreateAssetOfType(typeof(UpdaterAsset), "Assets/Resources/Updater/Updater.asset");
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
