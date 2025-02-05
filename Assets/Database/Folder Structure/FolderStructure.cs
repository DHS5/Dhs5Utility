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
        private List<int> m_validIndexes;

        #endregion

        #region Constructor

        public FolderStructure()
        {
            m_structure = new();
            m_state = new();
            m_validIndexes = new();
        }

        #endregion

        #region Properties

        public System.Comparison<object> CustomSort { get; set; }

        public int TotalCount => m_state.Count;
        public int ValidCount => m_validIndexes.Count;

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
            foreach (var index in m_validIndexes)
            {
                yield return m_state[index];
            }
        }

        public IEnumerable<int> GetValidEntriesIndexes()
        {
            foreach (var index in m_validIndexes)
            {
                yield return index;
            }
        }

        public int GetValidEntryCount()
        {
            return m_validIndexes.Count;
        }

        #endregion

        #region Entries Filtered

        public IEnumerable<int> GetFilteredEntriesIndexes(string filter, bool includeGroups = false, bool useGroupName = true)
        {
            bool includeChildren = false;
            int parentLevel = 0;

            for (int i = 0; i < m_state.Count; i++)
            {
                var entry = m_state[i];
                if (includeChildren && entry.level <= parentLevel) includeChildren = false;

                var contentMatch = entry.content.Contains(filter, System.StringComparison.OrdinalIgnoreCase);

                if (entry is FolderStructureGroupEntry group)
                {
                    if (contentMatch)
                    {
                        if (useGroupName && !includeChildren)
                        {
                            includeChildren = true;
                            parentLevel = group.level;
                        }
                        if (includeGroups)
                        {
                            yield return i;
                        }
                    }
                }
                else if (contentMatch || includeChildren)
                {
                    yield return i;
                }
            }
        }

        #endregion

        #endregion

        #region Setters

        public void UpdateContent(Dictionary<string, object> dico)
        {
            SaveGroupsState();
            Clear();
            AddRange(dico);
            LoadGroupsState();
            RecomputeState();
        }

        public void Add(string content, object data = null)
        {
            InternalAdd(content, data);
            RecomputeState();
        }
        private void AddRange(Dictionary<string, object> dico)
        {
            foreach (var (content, data) in dico)
            {
                InternalAdd(content, data);
            }
            RecomputeState();
        }

        private void InternalAdd(string content, object data)
        {
            string[] splittedName = content.Split('/', System.StringSplitOptions.RemoveEmptyEntries);

            if (splittedName.Length == 0) return;

            int i = 0;
            FolderStructureGroupEntry group = null;
            for (; i < splittedName.Length - 1; i++)
            {
                if (!TryGetGroup(splittedName[i], i, out var newGroup))
                {
                    newGroup = new FolderStructureGroupEntry(splittedName[i], i, group);
                    if (m_structure.TryGetValue(i, out var currentList))
                    {
                        currentList.Add(newGroup);
                    }
                    else
                    {
                        m_structure.Add(i, new() { newGroup });
                    }
                }
                group = newGroup;
            }

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

        public void EnsureVisibilityOfEntry(FolderStructureEntry entry)
        {
            while (entry.group != null)
            {
                entry.group.SetOpen(true);
                entry = entry.group;
            }
            RecomputeValidEntries();
        }

        public void SetOpen(FolderStructureGroupEntry group, bool open)
        {
            group.SetOpen(open);
            RecomputeValidEntries();
        }

        public void Clear()
        {
            m_structure.Clear();
        }

        #endregion

        #region Computations

        public void RecomputeState()
        {
            m_state.Clear();
            m_validIndexes.Clear();

            if (m_structure.TryGetValue(0, out var list) && list != null && list.Count > 0)
            {
                List<FolderStructureEntry> tempList = new(list);
                Sort(tempList);

                foreach (var entry in tempList)
                {
                    if (entry is FolderStructureGroupEntry group)
                    {
                        RecursiveAddGroupContentToState(group, true);
                    }
                    else
                    {
                        m_state.Add(entry);
                        m_validIndexes.Add(m_state.Count - 1);
                    }
                }
            }
        }
        private void RecursiveAddGroupContentToState(FolderStructureGroupEntry group, bool open)
        {
            if (m_structure.TryGetValue(group.level + 1, out var list) && list != null && list.Count > 0)
            {
                List<FolderStructureEntry> groupContent = new();
                foreach (var entry in list)
                {
                    if (entry.group == group)
                    {
                        groupContent.Add(entry);
                    }
                }

                if (groupContent.Count > 0)
                {
                    // If group is valid, add it
                    m_state.Add(group);
                    if (open) m_validIndexes.Add(m_state.Count - 1);

                    // Get the actual open state for children
                    open = open && group.Open;

                    // Then sort and add content
                    Sort(groupContent);
                    foreach (var entry in groupContent)
                    {
                        if (entry is FolderStructureGroupEntry childGroup)
                        {
                            RecursiveAddGroupContentToState(childGroup, open);
                        }
                        else
                        {
                            m_state.Add(entry);
                            if (open) m_validIndexes.Add(m_state.Count - 1);
                        }
                    }
                }
            }
        }

        public void RecomputeValidEntries()
        {
            m_validIndexes.Clear();

            int lastValidLevel = 0;
            for (int i = 0; i < m_state.Count; i++)
            {
                var entry = m_state[i];

                if (entry.level <= lastValidLevel)
                {
                    m_validIndexes.Add(i);

                    if (entry is FolderStructureGroupEntry group && group.Open) lastValidLevel = entry.level + 1;
                    else lastValidLevel = entry.level;
                }
            }
        }

        private void Sort(List<FolderStructureEntry> list)
        {
            list.Sort((e1, e2) =>
            {
                // Both groups
                if (e1.IsGroup && e2.IsGroup)
                {
                    return e1.content.CompareTo(e2.content);
                }
                else if (!e1.IsGroup && !e2.IsGroup)
                {
                    if (CustomSort != null) return CustomSort.Invoke(e1, e2);
                    return e1.content.CompareTo(e2.content);
                }
                else if (e1.IsGroup)
                {
                    return 1;
                }
                return -1;
            });
        }

        #endregion

        #region Save Groups State

        private Dictionary<int, List<string>> m_openGroups;

        private void SaveGroupsState()
        {
            if (m_structure == null) return;

            m_openGroups = new();

            foreach (var (level, entries) in m_structure)
            {
                List<string> list = new();

                foreach (var entry in entries)
                {
                    if (entry is FolderStructureGroupEntry group
                        && group.Open)
                    {
                        list.Add(group.content);
                    }
                }

                m_openGroups.Add(level, list);
            }
        }
        private void LoadGroupsState()
        {
            if (m_openGroups == null) return;

            foreach (var (level, groups) in m_openGroups)
            {
                if (m_structure.TryGetValue(level, out var entries))
                {
                    foreach (var entry in entries)
                    {
                        if (entry is FolderStructureGroupEntry group && groups.Contains(group.content))
                        {
                            group.SetOpen(true);
                        }
                    }
                }
            }
            m_openGroups.Clear();
            m_openGroups = null;
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

            sb.AppendLine("State :");
            for (int i = 0; i < m_state.Count; i++)
            {
                var entry = m_state[i];

                for (int j = 0; j < entry.level; j++)
                {
                    sb.Append("- ");
                }
                sb.Append(entry.content);
                if (m_validIndexes.Contains(i)) sb.Append(" valid");
                sb.Append(" (");
                sb.Append(entry.data);
                sb.AppendLine(")");
            }

            sb.AppendLine();
            sb.AppendLine("Structure :");

            for (int i = 0; i < m_structure.Count; i++)
            {
                sb.Append("Level ");
                sb.AppendLine(i.ToString());
                if (m_structure.TryGetValue(i, out var list))
                {
                    foreach (var entry in list)
                    {
                        sb.AppendLine(entry.content);
                    }
                }
            }

            Debug.Log(sb.ToString());
        }

#endif

        #endregion
    }
}
