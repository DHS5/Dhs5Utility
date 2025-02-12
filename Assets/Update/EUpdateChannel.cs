using UnityEngine;
using System;
using Dhs5.Utility.Databases;

namespace Dhs5.Utility.Updates
{
    public enum EUpdateChannel
    {
        BASE = 0,
        SCREEN_LOG = 1,
    }

    [Flags]
    public enum EUpdateChannelFlags
    {
        BASE = 1 << 0,
        SCREEN_LOG = 1 << 1,
    }
    public struct BASE_UpdateChannel { }
    public struct SCREEN_LOG_UpdateChannel { }
    
    public static class UpdateChannelExtensions
    {
        public static IUpdateChannel GetValue(this EUpdateChannel e)
        {
            return Database.Get<UpdaterDatabase>().GetDataAtIndex<UpdaterDatabaseElement>((int)e);
        }
        public static Type GetChannelType(this EUpdateChannel e)
        {
            switch (e)
            {
                case EUpdateChannel.BASE: return typeof(BASE_UpdateChannel);
                case EUpdateChannel.SCREEN_LOG: return typeof(SCREEN_LOG_UpdateChannel);
                default: return typeof(Updater.DefaultUpdateChannel);
            }
        }

        public static bool Contains(this EUpdateChannelFlags flag, EUpdateChannel e)
        {
            return (flag & ((EUpdateChannelFlags)(1 << (int)e))) != 0;
        }

        public static bool Contains(this EUpdateChannelFlags flag, EUpdateChannelFlags other)
        {
            return (flag & other) != 0;
        }
    }
}
