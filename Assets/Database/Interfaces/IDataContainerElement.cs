using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Dhs5.Utility.Databases
{
    public interface IDataContainerElement
    {
        int UID { get; }

        // UnityEngine.Object properties and methods
        string name { get; set; }
        Type GetType();

#if UNITY_EDITOR
        void Editor_SetUID(int uid);
#endif
    }

    public static class DataContainerElementExtensions
    {
        public static string GetName(this IDataContainerElement element)
        {
            if (element is IDataContainerNameableElement nameableElement) return nameableElement.DataDisplayName;
            return element.name;
        }
    }
}
