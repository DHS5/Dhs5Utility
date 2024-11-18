using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Databases
{
    public interface IEnumDatabaseElement : IDatabaseElement
    {
        public int EnumIndex { get; }

#if UNITY_EDITOR

        void Editor_SetIndex(int index);

#endif
    }
}
