using System;
using UnityEngine;

namespace Dhs5.Utility.Settings
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SubSettingsAttribute : Attribute
    {
        public readonly Type editorType;
    }
}
