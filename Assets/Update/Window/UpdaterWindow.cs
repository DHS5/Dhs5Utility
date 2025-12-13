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
                    m_updaterAsset = FindUpdaterAssetInProject();
                }
                return m_updaterAsset;
            }
        }

        private UpdaterAsset FindUpdaterAssetInProject()
        {
            var array = Resources.LoadAll<UpdaterAsset>("Updater");

            if (array != null && array.Length > 0)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Used)
                    {
                        return array[i];
                    }
                }
            }

            return null;
        }

        #endregion


        #region Members

        private int m_currentWindow;

        #endregion

        #region GUI Content

        private GUIContent g_title = new GUIContent("Updater");

        private GUIContent[] m_windowOptions = new GUIContent[] { new GUIContent("Channels"), new GUIContent("Update Conditions"), new GUIContent("Timelines"), new GUIContent("Settings") };

        #endregion


        #region Core GUI

        private void OnGUI()
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(g_title, GUIHelper.bigTitleLabel);

            EditorGUILayout.Space(10f);
            m_currentWindow = GUILayout.Toolbar(m_currentWindow, m_windowOptions);

            var rect = EditorGUILayout.GetControlRect(false, 2f);
            rect.x = 0f; rect.width = position.width;
            EditorGUI.DrawRect(rect, Color.white);

            EditorGUILayout.Space(5f);
            switch (m_currentWindow)
            {
                // CHANNELS
                case 0:
                    if (Asset != null)
                    {

                    }
                    break;
                    
                // UPDATE CONDITIONS
                case 1:
                    if (Asset != null)
                    {

                    }
                    break;
                    
                // TIMELINES
                case 2:
                    if (Asset != null)
                    {

                    }
                    break;
                    
                // SETTINGS
                case 3:
                    DrawSettingsGUI();
                    break;
            }
        }

        #endregion

        #region SETTINGS GUI

        private void DrawSettingsGUI()
        {
            EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);

            // List of assets
            var array = Resources.LoadAll<UpdaterAsset>("Updater");
            if (array != null && array.Length > 0)
            {
                EditorGUI.BeginDisabledGroup(true);
                for (int i = 0; i < array.Length; i++)
                {
                    var rect = EditorGUILayout.GetControlRect(false, 22f);

                    var objectRect = new Rect(rect.x, rect.y, rect.width - 32f, rect.height - 2f);
                    EditorGUI.ObjectField(objectRect, array[i], typeof(UpdaterAsset), false);
                }
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.HelpBox("No assets found", MessageType.Warning);
            }
            EditorGUILayout.Space(5f);

            // Create asset button
            if (GUILayout.Button("Create new asset"))
            {
                //if (!Directory.Exists(Application.dataPath + "Resources"))
                //{
                //    Directory.CreateDirectory(Application.dataPath + "Resources");
                //}
                //if (!Directory.Exists(Application.dataPath + "Resources/Updater"))
                //{
                //    Directory.CreateDirectory(Application.dataPath + "Resources/Updater");
                //}

                Database.CreateAssetOfType(typeof(UpdaterAsset), "Assets/Resources/Updater/Updater.asset");
                AssetDatabase.SaveAssets();
            }
        }

        #endregion
    }
}

#endif
