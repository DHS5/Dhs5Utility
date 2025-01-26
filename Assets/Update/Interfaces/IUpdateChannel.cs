using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public interface IUpdateChannel
    {
        public int ChannelIndex { get; }
        public EUpdatePass Pass { get; }
        public ushort Order { get; }
        public bool EnabledByDefault { get; }
        public EUpdateCondition Condition { get; }
        public float Frequency { get; }
        public float TimeScale { get; }
        public bool Realtime { get; }
    }
}
