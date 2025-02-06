using Dhs5.Utility.Settings;
using UnityEngine;
using System;

namespace Dhs5.Utility.Databases
{
    public abstract class DatabaseSettings : CustomSettings<DatabaseSettings>
    {
        public static bool TryGetDatabase(Type type, out BaseDataContainer db)
        {
            if (I != null)
            {
                db = I.GetDatabase(type);
                return db != null;
            }
            db = null;
            return false;
        }

        protected abstract BaseDataContainer GetDatabase(Type type);
    }
}
