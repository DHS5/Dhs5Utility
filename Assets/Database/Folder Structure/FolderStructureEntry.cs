using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Databases
{
    public class FolderStructureEntry
    {
        #region Members

        public readonly string content;
        public readonly int level;
        public readonly object data;

        #endregion

        #region Constructor

        public FolderStructureEntry(string content, int level = 0, object data = null)
        {
            this.content = content;
            this.level = level;
            this.data = data;
        }

        #endregion
    }
}
