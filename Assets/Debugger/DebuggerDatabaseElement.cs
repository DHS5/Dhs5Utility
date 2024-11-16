using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Debugger
{
    public class DebuggerDatabaseElement : ScriptableObject
    {
        #region Members

        [SerializeField] private Color m_color;
        [SerializeField, Range(-1, 2)] private int m_level;

        #endregion

        #region Properties

        public Color Color => m_color;

        #endregion
    }
}
