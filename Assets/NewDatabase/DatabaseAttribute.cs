using System;
using UnityEngine;

namespace Dhs5.Utility.NewDatabase
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DatabaseAttribute : ContainerTypesAttribute
    {
        public DatabaseAttribute(string path, params Type[] types) : base(types)
        {
            this.path = path;
        }

        /// <summary>
        /// Path of the database in the <see cref="DatabaseWindow"/>
        /// </summary>
        public readonly string path;

        public bool showInDatabaseWindow = true;
    }
}
