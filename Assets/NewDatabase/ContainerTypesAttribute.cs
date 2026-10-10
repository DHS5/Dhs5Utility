using System;
using UnityEngine;

namespace Dhs5.Utility.NewDatabase
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ContainerTypesAttribute : Attribute
    {
        public ContainerTypesAttribute(params Type[] types)
        {
            this.types = types;
        }

        /// <summary>
        /// Types of objects addable to the container, should implement <see cref="IContainerElement"/> interface
        /// </summary>
        public readonly Type[] types;
    }
}
