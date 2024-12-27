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

        public static void OpenTagsSelector(int id, Vector2 mousePos, List<int> tags)
        {
            CurrentID = id;
            TagsUpdated = false;
            _tags = tags;

            var rect = new Rect(mousePos.x - 500f, mousePos.y - 200f, 500f, 200f);
            _window = GetWindowWithRect<GameplayTagsSelector>(rect, true, "Gameplay Tags List", true);
            _window.Init();
        }

        #endregion


        #region Members

        private FolderStructure m_folderStructure = new();

        #endregion

        #region Static Members

        private static List<int> _tags;

        #endregion

        #region Static Properties

        public static int CurrentID { get; private set; }
        public static bool TagsUpdated { get; private set; }
        public static IEnumerable<int> GetUpdatedTags()
        {
            TagsUpdated = false;
            foreach (var tag in _tags)
            {
                yield return tag;
            }
        }

        #endregion


        #region Core Behaviour

        private void Init()
        {
            ComputeFolderStructure();
        }

        #endregion

        #region Core GUI

        private void OnGUI()
        {
            if (m_folderStructure != null)
            {
                EditorGUI.indentLevel = 0;
                FolderStructureEntry entry;
                foreach (var index in m_folderStructure.GetValidEntriesIndexes())
                {
                    entry = m_folderStructure.GetEntryAtIndex(index);

                    EditorGUI.indentLevel = entry.level;
                    if (entry is FolderStructureGroupEntry group)
                    {
                        OnGroupGUI(index, group);
                    }
                    else
                    {
                        OnElementGUI(index, entry);
                    }
                }
            }
        }

        #endregion

        #region Elements GUI

        private void OnGroupGUI(int index, FolderStructureGroupEntry group)
        {
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

            group.open = EditorGUILayout.Foldout(group.open, group.content + (hasContentOn ? " ON" : ""), true);
        }
        private void OnElementGUI(int index, FolderStructureEntry entry)
        {
            int uid = (int)entry.data;
            bool isOn = _tags.Contains(uid);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.ToggleLeft(entry.content, isOn);
            if (EditorGUI.EndChangeCheck())
            {
                if (isOn)
                {
                    _tags.Remove(uid);
                    TagsUpdated = true;
                }
                else
                {
                    _tags.Add(uid);
                    TagsUpdated = true;
                }
            }
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