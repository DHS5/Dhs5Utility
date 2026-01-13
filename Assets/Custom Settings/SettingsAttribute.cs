using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dhs5.Utility.Settings
{
    public enum Scope
    {
        User = 0,
        Project = 1
    }

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
        public bool overrideBaseType;

        #endregion
    }
}
