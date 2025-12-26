using System;

namespace Dhs5.Utility.Updates
{
    public enum EUpdateChannel : Int16
    {
        BASE = 0,
        SCREEN_LOG = 1,
    }

    public struct BASE_UpdateChannel { }
    public struct SCREEN_LOG_UpdateChannel { }

    public static class UpdateChannelExtensions
    {
        public static Type GetChannelType(this EUpdateChannel e)
        {
            switch (e)
            {
                case EUpdateChannel.BASE: return typeof(BASE_UpdateChannel);
                case EUpdateChannel.SCREEN_LOG: return typeof(SCREEN_LOG_UpdateChannel);
                default: return typeof(Updater.DefaultUpdateChannel);
            }
        }
    }
}
