using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Databases
{
    public class FolderStructureGroupEntry : FolderStructureEntry
    {
        #region Members

        public bool open;

        #endregion

        #region Constructor

        public FolderStructureGroupEntry(string content, int level = 0, object data = null) : base(content, level, data) { }

        #endregion
    }
}
