using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Console
{
    public class DebugCategoryObject : ScriptableObject
    {
        #region Members

        [SerializeField] private int m_enumIndex;
        [SerializeField] private Color m_color;
        [SerializeField] private string m_colorString;
        [SerializeField, Range(-1, DebuggerAsset.MAX_DEBUGGER_LEVEL)] private int m_level;

        #endregion

        #region Properties

        public int EnumIndex => m_enumIndex;
        public Color Color => m_color;
        public string ColorString => m_colorString;

        public bool Active => Level >= 0;
        public int Level => m_level;

        #endregion

        #region Accessors

        public bool CanLog(LogType logType, int logLevel)
        {
            switch (logType)
            {
                case LogType.Log: return Active && logLevel <= Level;
                case LogType.Warning: return Active && logLevel <= Level;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return Active;
                default: return false;
            }
        }

        #endregion

        #region Utility

        public void RefreshColorString()
        {
            m_colorString = ColorUtility.ToHtmlStringRGB(Color);
        }

        #endregion
    }
}
