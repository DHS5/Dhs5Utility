using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Databases
{
    public class FolderStructureEntry
    {
        #region Members

        public string content;
        public readonly int level;
        public readonly FolderStructureGroupEntry group;
        public object data;
        public bool IsGroup { get; protected set; }

        #endregion

        #region Constructor

        public FolderStructureEntry(string content, int level, FolderStructureGroupEntry group, object data = null)
        {
            this.content = content;
            this.level = level;
            this.group = group;
            this.data = data;
        }

        #endregion
    }
}
