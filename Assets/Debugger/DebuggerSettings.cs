using Dhs5.Utility.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Debuggers
{
    [Settings("Editor/Debugger", Scope.User)]
    public class DebuggerSettings : CustomSettings<DebuggerSettings>
    {
        #region Console Debugger

        [Header("Console Debugger")]

        [SerializeField, Range(8, 18)] private int m_consoleLog0Size = 14;
        [SerializeField, Range(8, 18)] private int m_consoleLog1Size = 12;
        [SerializeField, Range(8, 18)] private int m_consoleLog2Size = 11;

        public static int GetConsoleLogSize(int level)
        {
            if (I != null)
            {
                switch (level)
                {
                    case 0: return I.m_consoleLog0Size;
                    case 1: return I.m_consoleLog1Size;
                    case 2: return I.m_consoleLog2Size;
                }
            }
            return 12;
        }

        #endregion

        #region Screen Debugger

        [Header("Screen Debugger")]

        [SerializeField, Range(10, 30)] private int m_screenLog0Size = 20;
        [SerializeField, Range(10, 30)] private int m_screenLog1Size = 18;
        [SerializeField, Range(10, 30)] private int m_screenLog2Size = 16;

        [Space(10f)]

        [SerializeField, Min(1f)] private float m_defaultScreenLogDuration = 5.0f;

        public static int GetScreenLogSize(int level)
        {
            if (I != null)
            {
                switch (level)
                {
                    case 0: return I.m_screenLog0Size;
                    case 1: return I.m_screenLog1Size;
                    case 2: return I.m_screenLog2Size;
                }
            }
            return 18;
        }

        public static float DefaultScreenLogDuration => I != null ? I.m_defaultScreenLogDuration : 5.0f;

        #endregion
    }
}
