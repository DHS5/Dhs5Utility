using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Settings
{
    public enum Scope
    {
        /// <summary>
        /// Shown in Preferences. Stored per user in the UserSettings folder (not shared, not in builds) : editor only.
        /// Sub settings aren't supported.
        /// </summary>
        User = 0,
        /// <summary>
        /// Shown in Project Settings. Stored as an asset in Assets/Resources/Settings (shared, in builds).
        /// </summary>
        Project = 1
    }

    /// <summary>
    /// Path and scope of a settings class in the Settings window and Project Settings / Preferences.<br></br>
    /// A settings class with a non-abstract subclass is never shown nor used : the most derived class replaces it
    /// (with its own [Settings], or at the same path if it has none).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SettingsAttribute : Attribute
    {
        #region Constructor

        public SettingsAttribute(string path, Scope scope)
        {
            this.path = (scope == Scope.User ? "Preferences/" : "Project/") + path;
            this.scope = scope;
        }

        #endregion

        #region Members

        public readonly string path;
        public readonly Scope scope;

        #endregion
    }
}
