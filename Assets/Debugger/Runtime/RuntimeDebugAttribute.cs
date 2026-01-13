using UnityEngine;
using System;

namespace Dhs5.Utility.Debugger
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class RuntimeDebugAttribute : Attribute
    {
        
    }
}
