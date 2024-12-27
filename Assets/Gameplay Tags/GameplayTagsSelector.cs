#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Dhs5.Utility.Databases;
using System.Text;

namespace Dhs5.Utility.Tags
{
    public class GameplayTagsSelector : EditorWindow
    {
        public const string CommandName = "GameplayTagsSelected";

        #region Creator

        private static GameplayTagsSelector _window;
        private static EditorWindow _tagsListWindow;

        public static void OpenTagsSelector(int id, List<int> tags)
        {
            CurrentID = id;
            _tags = tags;
            _tagsListWindow = focusedWindow;

            //Vector2 mousePos = EditorGUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            //var rect = new Rect(mousePos.x - 500f, mousePos.y - 200f, 500f, 200f);
            //GetWindowWithRect<GameplayTagsSelector>(rect, true, "Gameplay Tags List", true);
            _window = GetWindow<GameplayTagsSelector>(true, "Gameplay Tags List", true);
            _window.Init();
        }

        #endregion


        #region Members

        private FolderStructure m_folderStructure = new();

        private Vector2 m_scrollPos;

        #endregion

        #region Static Members

        private static List<int> _tags;

        #endregion

        #region Static Properties

        public static int CurrentID { get; private set; }
        public static IEnumerable<int> GetUpdatedTags()
        {
            foreach (var tag in _tags)
            {
                yield return tag;
            }
        }

        #endregion

        #region Styles

        private Color m_background1;
        private Color m_background2;

        private void RefreshStyles()
        {
            m_background1 = new Color(0f, 0f, 0f, 0.3f);
            m_background2 = new Color(0f, 0f, 0f, 0.5f);
        }

        #endregion


        #region Core Behaviour

        private void Init()
        {
            ComputeFolderStructure();
            RefreshStyles();
        }

        #endregion

        #region Core GUI

        private void OnGUI()
        {
            if (m_folderStructure != null)
            {
                EditorGUI.indentLevel = 0;
                FolderStructureEntry entry;
                int visibleIndex = 0;

                m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos);

                foreach (var index in m_folderStructure.GetValidEntriesIndexes())
                {
                    entry = m_folderStructure.GetEntryAtIndex(index);

                    EditorGUI.indentLevel = entry.level;
                    if (entry is FolderStructureGroupEntry group)
                    {
                        OnGroupGUI(index, visibleIndex, group);
                    }
                    else
                    {
                        OnElementGUI(visibleIndex, entry);
                    }
                    visibleIndex++;
                }
                GUI.FocusControl(null);

                EditorGUILayout.EndScrollView();
            }
        }

        #endregion

        #region Elements GUI

        private void OnGroupGUI(int index, int visibleIndex, FolderStructureGroupEntry group)
        {
            // Rect
            Rect rect = EditorGUILayout.GetControlRect(false, 18f);
            Rect backgroundRect = new Rect(0f, rect.y - 1f, position.width, 20f);

            // Group infos
            bool hasContentOn = false;
            int nextEntryIndex = index + 1;
            FolderStructureEntry nextEntry = m_folderStructure.GetEntryAtIndex(nextEntryIndex);
            while (nextEntry != null && nextEntry.level > group.level)
            {
                if (nextEntry.data is int uid && _tags.Contains(uid))
                {
                    hasContentOn = true;
                    break;
                }
                nextEntryIndex++;
                nextEntry = m_folderStructure.GetEntryAtIndex(nextEntryIndex);
            }

            // Background
            EditorGUI.DrawRect(backgroundRect, visibleIndex % 2 == 0 ? m_background1 : m_background2);

            // GUI
            var guiColor = GUI.contentColor;
            if (hasContentOn) GUI.color = Color.green;
            group.open = EditorGUI.Foldout(rect, group.open, group.content, true);
            GUI.color = guiColor;
        }
        private void OnElementGUI(int visibleIndex, FolderStructureEntry entry)
        {
            // Rect
            Rect rect = EditorGUILayout.GetControlRect(false, 18f);
            Rect backgroundRect = new Rect(0f, rect.y - 1f, position.width, 20f);

            // Entry infos
            int uid = (int)entry.data;
            bool isOn = _tags.Contains(uid);

            // Background
            EditorGUI.DrawRect(backgroundRect, visibleIndex % 2 == 0 ? m_background1 : m_background2);

            // GUI
            var guiColor = GUI.contentColor;
            if (isOn) GUI.color = Color.green;

            EditorGUI.BeginChangeCheck();
            EditorGUI.ToggleLeft(rect, entry.content, isOn);
            if (EditorGUI.EndChangeCheck())
            {
                // Event on value change
                if (isOn)
                {
                    _tags.Remove(uid);
                    _tagsListWindow.SendEvent(EditorGUIUtility.CommandEvent(CommandName));
                }
                else
                {
                    _tags.Add(uid);
                    _tagsListWindow.SendEvent(EditorGUIUtility.CommandEvent(CommandName));
                }
            }

            GUI.color = guiColor;
        }

        #endregion


        #region Utility

        private void ComputeFolderStructure()
        {
            m_folderStructure.Clear();

            Dictionary<string, object> folderStructureDatas = new();
            foreach (var (uid, obj) in Database.Get<GameplayTagsDatabase>().Editor_GetContainerDicoContent())
            {
                if (!folderStructureDatas.TryAdd(GetObjectDisplayName(obj), uid))
                {
                    folderStructureDatas.Add(GetObjectDisplayName(obj) + "_" + uid, uid);
                }
            }

            m_folderStructure.FillFromNamesAndDatas(folderStructureDatas);
        }

        private string GetObjectDisplayName(UnityEngine.Object obj)
        {
            if (obj is IDataContainerPrefixableElement prefixableElement && !string.IsNullOrWhiteSpace(prefixableElement.DataNamePrefix))
            {
                StringBuilder sb = new();
                sb.Append(prefixableElement.DataNamePrefix);
                if (!prefixableElement.DataNamePrefix.EndsWith("/")) sb.Append("/");
                sb.Append(obj.name);
                return sb.ToString();
            }
            return obj.name;
        }

        #endregion
    }
}

#endif