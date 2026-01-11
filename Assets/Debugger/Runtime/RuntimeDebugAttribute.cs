using UnityEngine;
using System;

namespace Dhs5.Utility.Debugger
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class RuntimeDebugAttribute : Attribute
    {
        
    }
}
