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

        private List<FolderStructureEntry> m_structureList;

        #endregion

        #region Constructor

        public FolderStructure()
        {
            m_structureList = new();
        }

        #endregion

        #region Properties

        public int Count => m_structureList.Count;

        #endregion


        #region Getters

        public FolderStructureEntry GetEntryAtIndex(int index)
        {
            if (m_structureList.IsIndexValid(index))
            {
                return m_structureList[index];
            }
            return null;
        }

        #region Valid Entries

        public IEnumerable<FolderStructureEntry> GetValidEntries()
        {
            bool lastGroupOpen = true;
            int lastGroupLevel = 0;

            foreach (var entry in m_structureList)
            {
                // Skip entries in closed group
                if (!lastGroupOpen && entry.level > lastGroupLevel)
                {
                    continue;
                }

                // If group, keep group's infos
                if (entry is FolderStructureGroupEntry groupEntry)
                {
                    lastGroupOpen = groupEntry.open;
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
                entry = m_structureList[i];

                // Skip entries in closed group
                if (!lastGroupOpen && entry.level > lastGroupLevel)
                {
                    continue;
                }

                // If group, keep group's infos
                if (entry is FolderStructureGroupEntry groupEntry)
                {
                    lastGroupOpen = groupEntry.open;
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

            foreach (var entry in m_structureList)
            {
                // Skip entries in closed group
                if (!lastGroupOpen && entry.level > lastGroupLevel)
                {
                    continue;
                }

                // If group, keep group's infos
                if (entry is FolderStructureGroupEntry groupEntry)
                {
                    lastGroupOpen = groupEntry.open;
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
                entry = m_structureList[i];

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

        public void Add(FolderStructureEntry entry)
        {
            m_structureList.Add(entry);
        }

        #endregion

        #region Utility

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
                    group.open = true;
                    level--;
                    if (level == 0) return;
                }
            }
        }

        public void Clear()
        {
            m_structureList.Clear();
        }

        #endregion


        #region Debug

#if UNITY_EDITOR

        public void DebugContent()
        {
            StringBuilder sb = new();
            foreach (var entry in m_structureList)
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
