using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.OldDatabase
{
    public interface IEnumDatabaseElement : IDataContainerElement
    {
        public int EnumIndex { get; }

#if UNITY_EDITOR

        void Editor_SetIndex(int index);

#endif
    }
}
