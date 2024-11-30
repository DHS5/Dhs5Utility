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

        [SerializeField, Min(1f)] private float m_defaultScreenLogDuration = 5.0f;
        [SerializeField] private bool m_showScreenLogsTime = true;

        public static float DefaultScreenLogDuration => I != null ? I.m_defaultScreenLogDuration : 5.0f;
        public static bool ShowScreenLogsTime => I != null ? I.m_showScreenLogsTime : true;

        #endregion

        #region Screen Debugger GUI

        [Header("Screen Debugger GUI")]

        [SerializeField] private Rect m_screenLogsRect = new Rect(10f, 10f, 700f, 500f);
        [SerializeField, Range(10f, 40f)] private float m_screenLogHeight = 20f;

        [Space(10f)]

        [SerializeField, Range(10, 30)] private int m_screenLog0Size = 20;
        [SerializeField, Range(10, 30)] private int m_screenLog1Size = 18;
        [SerializeField, Range(10, 30)] private int m_screenLog2Size = 16;

        [Space(10f)]

        [SerializeField, Range(10, 30)] private int m_screenLogsTimeSize = 18;

        public static Rect ScreenLogsRect => I != null ? I.m_screenLogsRect : new Rect(10f, 10f, 700f, 500f);
        public static float ScreenLogHeight => I != null ? I.m_screenLogHeight : 20f;
        public static int MaxLogsOnScreen => I != null ? Mathf.FloorToInt(I.m_screenLogsRect.height / I.m_screenLogHeight) : 25;

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

        public static int ScreenLogsTimeSize => I != null ? I.m_screenLogsTimeSize : 18;

        #endregion
    }
}
