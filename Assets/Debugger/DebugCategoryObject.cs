using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Debugger
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
        /// <summary>
        /// Level used to filter logs : the user level in the editor if there is one, the project level otherwise
        /// </summary>
        public int Level =>
#if UNITY_EDITOR
            m_hasUserLevel ? m_userLevel : m_level;
#else
            m_level;
#endif
        /// <summary>
        /// Level saved in the asset, shared by the whole project
        /// </summary>
        public int ProjectLevel => m_level;

        #endregion

        #region Accessors

        public bool CanLog(LogType logType, int logLevel)
        {
            switch (logType)
            {
                case LogType.Log: return Active && logLevel <= Level;
                case LogType.Warning: return Active && logLevel <= Level;
                // Errors, exceptions and asserts always get through, even from a disabled category
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return true;
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


        #region Editor

#if UNITY_EDITOR

        public void Editor_SetColor(Color color)
        {
            m_color = color;
        }

        #region User Level

        // Per project and per user preference overriding the project level in the editor.
        // Saved in EditorUserSettings (UserSettings folder) and cached here so that logging never reads the settings
        [NonSerialized] private bool m_hasUserLevel;
        [NonSerialized] private int m_userLevel;
        [NonSerialized] private string m_userLevelConfigKey;

        public bool Editor_HasUserLevel => m_hasUserLevel;
        public int Editor_UserLevel => m_userLevel;

        private void OnEnable()
        {
            Editor_LoadUserLevel();
        }

        /// <summary>
        /// Key of the user level in EditorUserSettings, based on the asset GUID so that it survives renaming and reindexing
        /// </summary>
        internal string Editor_UserLevelConfigKey
        {
            get
            {
                if (string.IsNullOrEmpty(m_userLevelConfigKey)
                    && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out string guid, out long _))
                {
                    m_userLevelConfigKey = "Dhs5.Debugger.UserLevel." + guid;
                }
                return m_userLevelConfigKey;
            }
        }

        public void Editor_LoadUserLevel()
        {
            var key = Editor_UserLevelConfigKey;
            m_hasUserLevel = !string.IsNullOrEmpty(key)
                && int.TryParse(EditorUserSettings.GetConfigValue(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out m_userLevel);
            if (m_hasUserLevel)
            {
                m_userLevel = Mathf.Clamp(m_userLevel, -1, DebuggerAsset.MAX_DEBUGGER_LEVEL);
            }
        }
        public void Editor_SetUserLevel(int level)
        {
            var key = Editor_UserLevelConfigKey;
            if (string.IsNullOrEmpty(key)) return;

            m_userLevel = Mathf.Clamp(level, -1, DebuggerAsset.MAX_DEBUGGER_LEVEL);
            m_hasUserLevel = true;
            EditorUserSettings.SetConfigValue(key, m_userLevel.ToString(CultureInfo.InvariantCulture));
        }
        public void Editor_ClearUserLevel()
        {
            m_hasUserLevel = false;
            var key = Editor_UserLevelConfigKey;
            if (!string.IsNullOrEmpty(key))
            {
                EditorUserSettings.SetConfigValue(key, null);
            }
        }

        #endregion

#endif

        #endregion
    }
}
