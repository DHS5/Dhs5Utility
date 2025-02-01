using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Dhs5.Utility.Databases
{
    public class FolderStructure
    {
        #region Members

        private Dictionary<int, List<FolderStructureEntry>> m_structure;
        private List<FolderStructureEntry> m_state;

        #endregion

        #region Constructor

        public FolderStructure()
        {
            m_structure = new();
            m_state = new();
        }

        #endregion

        #region Properties

        public int Count => m_structure.Count;

        #endregion


        #region Getters

        public FolderStructureEntry GetEntryAtIndex(int index)
        {
            if (m_state.IsIndexValid(index))
            {
                return m_state[index];
            }
            return null;
        }

        #region Valid Entries

        public IEnumerable<FolderStructureEntry> GetValidEntries()
        {
            bool lastGroupOpen = true;
            int lastGroupLevel = 0;

            foreach (var entry in m_structure)
            {
                // Skip entries in closed group
                if (!lastGroupOpen && entry.level > lastGroupLevel)
                {
                    continue;
                }

                // If group, keep group's infos
                if (entry is FolderStructureGroupEntry groupEntry)
                {
                    lastGroupOpen = groupEntry.Open;
                    lastGroupLevel = groupEntry.level;
                    yield return entry;
                }
                // Just show basic entries
                else
                {
                    yield return entry;
                }
            }
        }

        public IEnumerable<int> GetValidEntriesIndexes()
        {
            bool lastGroupOpen = true;
            int lastGroupLevel = 0;

            FolderStructureEntry entry;
            for (int i = 0; i < Count; i++)
            {
                entry = m_structure[i];

                // Skip entries in closed group
                if (!lastGroupOpen && entry.level > lastGroupLevel)
                {
                    continue;
                }

                // If group, keep group's infos
                if (entry is FolderStructureGroupEntry groupEntry)
                {
                    lastGroupOpen = groupEntry.Open;
                    lastGroupLevel = groupEntry.level;
                    yield return i;
                }
                // Just show basic entries
                else
                {
                    yield return i;
                }
            }
        }

        public int GetValidEntryCount()
        {
            bool lastGroupOpen = true;
            int lastGroupLevel = 0;
            int count = 0;

            foreach (var entry in m_structure)
            {
                // Skip entries in closed group
                if (!lastGroupOpen && entry.level > lastGroupLevel)
                {
                    continue;
                }

                // If group, keep group's infos
                if (entry is FolderStructureGroupEntry groupEntry)
                {
                    lastGroupOpen = groupEntry.Open;
                    lastGroupLevel = groupEntry.level;
                    count++;
                }
                // Just show basic entries
                else
                {
                    count++;
                }
            }

            return count;
        }

        #endregion

        #region Entries Filtered

        public IEnumerable<int> GetFilteredEntriesIndexes(string filter, bool includeFolders = false)
        {
            FolderStructureEntry entry;
            for (int i = 0; i < Count; i++)
            {
                entry = m_structure[i];

                if ((includeFolders || entry is not FolderStructureGroupEntry)
                    && entry.content.Contains(filter, System.StringComparison.OrdinalIgnoreCase))
                {
                    yield return i;
                }
            }
        }

        #endregion

        #endregion

        #region Setters

        public void Add(string content, object data = null)
        {
            InternalAdd(content, data);
            // recompute list
        }
        public void AddRange(Dictionary<string, object> dico)
        {
            foreach (var (content, data) in dico)
            {
                InternalAdd(content, data);
            }
            // recompute list
        }

        private void InternalAdd(string content, object data)
        {
            string[] splittedName = content.Split('/', System.StringSplitOptions.RemoveEmptyEntries);

            if (splittedName.Length == 0) return;

            int i = 0;
            FolderStructureGroupEntry group = null;
            for (; i < splittedName.Length - 1; i++)
            {
                if (!TryGetGroup(splittedName[i], i, out group))
                {
                    var newGroup = new FolderStructureGroupEntry(splittedName[i], i, group);
                    group = newGroup;
                }
            }

            if (splittedName.Length > 1) i++;
            FolderStructureEntry entry = new(splittedName[i], i, group, data);
            if (m_structure.TryGetValue(i, out var list))
            {
                list.Add(entry);
            }
            else
            {
                m_structure.Add(i, new() { entry });
            }
        }

        #endregion

        #region Actions

        public void EnsureVisibilityOfEntryAtIndex(int index)
        {
            if (index < 1) return;

            var entry = GetEntryAtIndex(index);
            int level = entry.level;
            if (entry == null || entry.level == 0) return;

            for (int i = index - 1; i >= 0; i--)
            {
                if (entry is FolderStructureGroupEntry group && group.level == level - 1)
                {
                    group.Open = true;
                    level--;
                    if (level == 0) return;
                }
            }
        }

        public void Clear()
        {
            m_structure.Clear();
        }

        #endregion

        #region Computations

        private void RecomputeState()
        {
            m_state.Clear();

            // Foreach level
            List<FolderStructureEntry> tempList;
            for (int i = 0; i < m_structure.Count; i++)
            {
                if (m_structure.TryGetValue(i, out var list) && list != null)
                {
                    tempList = new(list);
                    Sort(tempList);
                }
            }
        }

        private void Sort(List<FolderStructureEntry> list)
        {
            // groups last, default to alphabetical order, can be overriden by custom sort
        }

        #endregion

        #region Utility

        private bool TryGetGroup(string name, int level, out FolderStructureGroupEntry group)
        {
            if (m_structure.TryGetValue(level, out var list))
            {
                var entry = list.Find(e => e.content == name);
                if (entry is FolderStructureGroupEntry groupEntry)
                {
                    group = groupEntry;
                    return true;
                }
            }
            group = null;
            return false;
        }

        #endregion


        #region Debug

#if UNITY_EDITOR

        public void DebugContent()
        {
            StringBuilder sb = new();
            foreach (var entry in m_structure)
            {
                sb.Clear();

                for (int i = 0; i < entry.level; i++)
                {
                    sb.Append("- ");
                }
                sb.Append(entry.content);
                sb.Append(" (");
                sb.Append(entry.data);
                sb.AppendLine(")");

                Debug.Log(sb.ToString());
            }
        }

#endif

        #endregion
    }

    public static class FolderStructureExtensions
    {
        public static void FillFromNamesAndDatas(this FolderStructure structure, Dictionary<string, object> dico)
        {
            List<string> sortedNames = dico.Keys.ToList();
            sortedNames.Sort();

            List<string[]> splittedNames = new List<string[]>();
            foreach (var name in sortedNames)
            {
                splittedNames.Add(name.Split('/', System.StringSplitOptions.RemoveEmptyEntries));
            }

            for (int i = 0; i < splittedNames.Count; i++)
            {
                if (splittedNames[i].Length == 0)
                {
                    structure.Add(new FolderStructureEntry("_", 0, dico[sortedNames[i]]));
                    continue;
                }

                if (i == 0)
                {
                    for (int g = 0; g < splittedNames[i].Length - 1; g++)
                    {
                        structure.Add(new FolderStructureGroupEntry(splittedNames[i][g], g));
                    }
                }
                else
                {
                    for (int g = 0; g < splittedNames[i].Length - 1; g++)
                    {
                        if (splittedNames[i - 1].Length <= g || splittedNames[i - 1][g] != splittedNames[i][g])
                        {
                            structure.Add(new FolderStructureGroupEntry(splittedNames[i][g], g));
                        }
                    }
                }
                structure.Add(new FolderStructureEntry(splittedNames[i][splittedNames[i].Length - 1], splittedNames[i].Length - 1, dico[sortedNames[i]]));
            }
        }
    }
}
