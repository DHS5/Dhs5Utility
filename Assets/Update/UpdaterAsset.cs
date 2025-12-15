using UnityEngine;
using System.IO;
using Dhs5.Utility.Databases;
using System.Collections.Generic;
using System;
using Dhs5.Utility.GUIs;
using Dhs5.Utility.Editors;

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

        private UpdaterAsset m_updaterAsset;

        private Vector2 m_channelsScrollPos;

        #endregion

        #region Serialized Properties

        private SerializedProperty p_updateChannels;
        private SerializedProperty p_updateConditions;

        private SerializedProperty p_updateChannelsTextAsset;
        private SerializedProperty p_updateConditionsTextAsset;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            m_updaterAsset = target as UpdaterAsset;

            p_updateChannels = serializedObject.FindProperty("m_updateChannels");
            p_updateConditions = serializedObject.FindProperty("m_updateConditions");

            p_updateChannelsTextAsset = serializedObject.FindProperty("m_updateChannelsTextAsset");
            p_updateConditionsTextAsset = serializedObject.FindProperty("m_updateConditionsTextAsset");
        }

        #endregion


        #region CHANNELS GUI

        public void DrawChannelsGUI()
        {
            EditorGUILayout.BeginVertical();

            // List
            DrawChannelsList();

            // Footer
            DrawChannelsFooter();

            EditorGUILayout.EndVertical();
        }

        private void DrawChannelsList()
        {
            m_channelsScrollPos = EditorGUILayout.BeginScrollView(m_channelsScrollPos);

            for (int i = 0; i < p_updateChannels.arraySize; i++)
            {
                if (p_updateChannels.GetArrayElementAtIndex(i).objectReferenceValue is UpdaterDatabaseElement element)
                {
                    DrawListElement(element, i);
                    if (i < p_updateChannels.arraySize - 1)
                    {
                        EditorGUILayout.Space(2f);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
        private void DrawListElement(UpdaterDatabaseElement element, int index)
        {
            var rect = EditorGUILayout.GetControlRect(false, 95f);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            var so = new SerializedObject(element);
            if (so != null)
            {
                var marginedRect = new Rect(rect.x + 5f, rect.y + 4f, rect.width - 10f, rect.height - 8f);
                var halfWidth = marginedRect.width / 2f;

                // Enabled toggle
                var p_enabledByDefault = so.FindProperty("m_enabledByDefault");
                var r_enabledToggle = new Rect(marginedRect.x, marginedRect.y, 20f, 20f);
                p_enabledByDefault.boolValue = EditorGUI.ToggleLeft(r_enabledToggle, GUIContent.none, p_enabledByDefault.boolValue);

                // Up/Down Buttons
                bool ret = false;
                var r_upButton = new Rect(marginedRect.x + marginedRect.width - 62f, marginedRect.y - 2f, 32f, 20f);
                using (new EditorGUI.DisabledGroupScope(index == 0))
                {
                    if (GUI.Button(r_upButton, EditorGUIHelper.UpIcon))
                    {
                        p_updateChannels.MoveArrayElement(index, index - 1);
                        ret = true;
                    }
                }
                var r_downButton = new Rect(marginedRect.x + marginedRect.width - 30f, marginedRect.y - 2f, 32f, 20f);
                using (new EditorGUI.DisabledGroupScope(index == p_updateChannels.arraySize - 1))
                {
                    if (GUI.Button(r_downButton, EditorGUIHelper.DownIcon))
                    {
                        p_updateChannels.MoveArrayElement(index, index + 1);
                        ret = true;
                    }
                }

                if (ret)
                {
                    so.Dispose();
                    return;
                }

                // Name
                var enabledToggleTotalWidth = 22f;
                var buttonsTotalWidth = 70f;
                var r_indexLabel = new Rect(marginedRect.x + enabledToggleTotalWidth, marginedRect.y, 20f, 20f);
                var r_nameTextField = new Rect(marginedRect.x + enabledToggleTotalWidth + 20f, marginedRect.y, marginedRect.width - enabledToggleTotalWidth - buttonsTotalWidth - 20f, 20f);
                EditorGUI.LabelField(r_indexLabel, index.ToString(), EditorStyles.boldLabel);
                element.name = EditorGUI.DelayedTextField(r_nameTextField, element.name);

                marginedRect.y += 25f;
                marginedRect.height -= 25f;

                // Update Pass
                var p_updatePass = so.FindProperty("m_updatePass");
                var r_updatePass = new Rect(marginedRect.x, marginedRect.y, Mathf.Max(200f, marginedRect.width - 150f), 20f);
                using (new EditorGUIHelper.LabelWidthScope(105f))
                {
                    EditorGUI.PropertyField(r_updatePass, p_updatePass);
                }

                // Order
                var p_order = so.FindProperty("m_order");
                var updatePassTotalWidth = r_updatePass.width + 10f;
                var r_order = new Rect(r_updatePass.x + updatePassTotalWidth, marginedRect.y, marginedRect.width - updatePassTotalWidth, 20f);
                using (new EditorGUIHelper.LabelWidthScope(50f))
                {
                    EditorGUI.PropertyField(r_order, p_order);
                }

                marginedRect.y += 22f;
                marginedRect.height -= 22f;

                // Condition
                var p_updateCondition = so.FindProperty("m_updateCondition");
                var r_updateCondition = new Rect(marginedRect.x, marginedRect.y, halfWidth - 2f, 20f);
                using (new EditorGUIHelper.LabelWidthScope(105f))
                {
                    EditorGUI.PropertyField(r_updateCondition, p_updateCondition);
                }
                
                // Custom Frequency
                var p_customFrequency = so.FindProperty("m_customFrequency");
                var r_customFrequency = new Rect(marginedRect.x + halfWidth + 2f, marginedRect.y, halfWidth - 2f, 20f);
                using (new EditorGUIHelper.LabelWidthScope(135f))
                {
                    EditorGUI.PropertyField(r_customFrequency, p_customFrequency);
                }

                marginedRect.y += 22f;
                marginedRect.height -= 22f;

                // Timescale
                var p_timescale = so.FindProperty("m_timescale");
                var r_timescale = new Rect(marginedRect.x, marginedRect.y, halfWidth - 2f, 18f);
                using (new EditorGUIHelper.LabelWidthScope(105f))
                {
                    EditorGUI.PropertyField(r_timescale, p_timescale);
                }
                
                // Realtime
                var p_realtime = so.FindProperty("m_realtime");
                var r_realtime = new Rect(marginedRect.x + halfWidth + 2f, marginedRect.y, halfWidth - 2f, 20f);
                using (new EditorGUIHelper.LabelWidthScope(135f))
                {
                    EditorGUI.PropertyField(r_realtime, p_realtime);
                }

                so.ApplyModifiedProperties();
                so.Dispose();
            }
        }

        private void DrawChannelsFooter()
        {
            var rect = EditorGUILayout.BeginVertical();
            //GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            EditorGUILayout.Space(3f);
            using (new GUIHelper.GUIBackgroundColorScope(Color.green))
            {
                if (GUILayout.Button("ADD NEW CHANNEL", GUILayout.Height(25f)))
                {

                }
            }
            using (new GUIHelper.GUIBackgroundColorScope(DoesUpdateChannelScriptNeedUpdate() ? Color.cyan : Color.grey))
            {
                if (GUILayout.Button("UPDATE CHANNEL SCRIPT", GUILayout.Height(25f)))
                {

                }
            }
            EditorGUILayout.Space(3f);

            EditorGUILayout.EndVertical();
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
            // SCRIPTS
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

            // ASSET SANITY
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Asset Sanity", EditorStyles.boldLabel);

            if (GUILayout.Button("Ensure channel objects are inside asset"))
            {
                var updaterAssetPath = AssetDatabase.GetAssetPath(m_updaterAsset);
                for (int i = 0; i < p_updateChannels.arraySize; i++)
                {
                    if (p_updateChannels.GetArrayElementAtIndex(i).objectReferenceValue is UpdaterDatabaseElement channelObject
                        && AssetDatabase.GetAssetPath(channelObject) != updaterAssetPath)
                    {
                        if (AssetDatabase.IsSubAsset(channelObject))
                        {
                            AssetDatabase.RemoveObjectFromAsset(channelObject);
                            AssetDatabase.AddObjectToAsset(channelObject, updaterAssetPath);
                        }
                    }
                }
            }
            if (GUILayout.Button("Ensure no null objects in list"))
            {
                for (int i = p_updateChannels.arraySize - 1; i >= 0; i--)
                {
                    if (p_updateChannels.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        p_updateChannels.DeleteArrayElementAtIndex(i);
                    }
                }
            }
            if (GUILayout.Button("Ensure no intrusive object inside asset"))
            {

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

        #region Script Check

        private bool DoesUpdateChannelScriptNeedUpdate()
        {
            if (p_updateChannelsTextAsset.objectReferenceValue != null)
            {
                int i = 0;
                foreach (var obj in Enum.GetValues(typeof(EUpdateChannel)))
                {
                    var value = (EUpdateChannel)obj;
                    if (p_updateChannels.arraySize <= i
                        || value.ToString() != p_updateChannels.GetArrayElementAtIndex(i).objectReferenceValue.name)
                    {
                        return true;
                    }
                    i++;
                }
            }
            return false;
        }

        #endregion
    }

#endif

    #endregion
}
