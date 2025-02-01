using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Databases
{
    public class FolderStructureGroupEntry : FolderStructureEntry
    {
        #region Members

        public bool Open { get; private set; }

        #endregion

        #region Constructor

        public FolderStructureGroupEntry(string content, int level, FolderStructureGroupEntry group, object data = null) : base(content, level, group, data) { }

        #endregion
    }
}
