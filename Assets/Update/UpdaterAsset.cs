using UnityEngine;
using System.IO;
using Dhs5.Utility.Databases;
using System.Collections.Generic;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Updates
{
    public class UpdaterAsset : ScriptableObject
    {
        #region STRUCT UpdateConditionElement

        [Serializable]
        private struct UpdateConditionElement
        {
            [SerializeField] private string m_name;
            [SerializeField] private UpdateConditionObject m_object;
        }

        #endregion

        #region Members

        [SerializeField] private List<UpdaterDatabaseElement> m_updateChannels;
        [SerializeField] private List<UpdateConditionElement> m_updateConditions;

        #endregion

        #region Properties


        #endregion



        // --- EDITOR ---

#if UNITY_EDITOR

        #region Editor Members

        [Tooltip("EDITOR ONLY\nText Asset used to write the Update Channels enum")]
        [SerializeField] private TextAsset m_updateChannelsTextAsset;
        [Tooltip("EDITOR ONLY\nText Asset used to write the Update Conditions enum")]
        [SerializeField] private TextAsset m_updateConditionsTextAsset;

        #endregion

#endif
    }

    #region Editor

#if UNITY_EDITOR

    public class UpdaterAssetEditor : Editor
    {
        #region Members

        private SerializedProperty p_updateChannels;
        private SerializedProperty p_updateConditions;

        private SerializedProperty p_updateChannelsTextAsset;
        private SerializedProperty p_updateConditionsTextAsset;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            p_updateChannels = serializedObject.FindProperty("m_updateChannels");
            p_updateConditions = serializedObject.FindProperty("m_updateConditions");

            p_updateChannelsTextAsset = serializedObject.FindProperty("m_updateChannelsTextAsset");
            p_updateConditionsTextAsset = serializedObject.FindProperty("m_updateConditionsTextAsset");
        }

        #endregion


        #region CHANNELS GUI

        public void DrawChannelsGUI()
        {

        }

        #endregion
        
        #region CONDITIONS GUI

        public void DrawConditonsGUI()
        {

        }

        #endregion
        
        #region TIMELINES GUI

        public void DrawTimelinesGUI()
        {

        }

        #endregion
        
        #region SETTINGS GUI

        public void DrawSettingsGUI()
        {
            EditorGUILayout.LabelField("Scripts", EditorStyles.boldLabel);

            //if (p_updateChannelsTextAsset.objectReferenceValue == null
            //    || p_updateConditionsTextAsset.objectReferenceValue == null)
            //{
            //    var scriptObj = serializedObject.FindProperty("m_Script").objectReferenceValue;
            //    if (scriptObj != null)
            //    {
            //        string path;
            //        if (p_updateChannelsTextAsset.objectReferenceValue == null)
            //        {
            //            path = ProjectWindowUtil.GetContainingFolder(AssetDatabase.GetAssetPath(scriptObj)) + "/EUpdateChannel.cs";
            //            p_updateChannelsTextAsset.objectReferenceValue = Database.CreateOrOverwriteScript(path, GetUpdateChannelScriptContent());
            //        }
            //        if (p_updateConditionsTextAsset.objectReferenceValue == null)
            //        {
            //            path = ProjectWindowUtil.GetContainingFolder(AssetDatabase.GetAssetPath(scriptObj)) + "/EUpdateCondition.cs";
            //            p_updateConditionsTextAsset.objectReferenceValue = Database.CreateOrOverwriteScript(path, GetUpdateConditionScriptContent());
            //        }
            //    }
            //}
            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.ObjectField(p_updateChannelsTextAsset);
                EditorGUILayout.ObjectField(p_updateConditionsTextAsset);
            }
        }

        #endregion


        #region Script Generation

        private string GetUpdateChannelScriptContent()
        {
            return null;
        }
        private string GetUpdateConditionScriptContent()
        {
            return null;
        }

        #endregion
    }

#endif

    #endregion
}
