using UnityEngine;
using System;

namespace Dhs5.Utility.Debugger
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ConsoleCommandAttribute : Attribute
    {
        public ConsoleCommandAttribute(string name, ConsoleCommand.EScope scope = ConsoleCommand.EScope.RUNTIME)
        {
            this.name = name;
            this.scope = scope;
        }

        public readonly string name;
        public readonly ConsoleCommand.EScope scope;
    }
}
