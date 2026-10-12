#if !UNITY_6000_6_OR_NEWER
using System;

// The statics cleanup attributes only exist from Unity 6.6 : without effect before, so that the project still compiles
namespace Unity.Scripting.LifecycleManagement
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event)]
    internal sealed class AutoStaticsCleanupAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event)]
    internal sealed class NoAutoStaticsCleanupAttribute : Attribute { }
}
#endif
