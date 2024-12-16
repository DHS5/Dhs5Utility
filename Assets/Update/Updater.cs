using System;
using System.Collections;
using System.Collections.Generic;

namespace Dhs5.Utility.Updates
{
    public class Updater<UpdateEnum> : BaseUpdater where UpdateEnum : Enum
    {
        #region Callbacks Registration

        private static Dictionary<UpdateEnum, HashSet<ulong>> _registeredCallbackKeys = new();

        public static bool Register(bool register, UpdateEnum category, UpdateCallback callback, ref ulong key)
        {
            if (register) // Wants to register new callback
            {
                if (_registeredCallbackKeys.TryGetValue(category, out var keys)) // The callback category already exists
                {
                    if (keys.Contains(key)) // This callback is already registered
                    {
                        return false;
                    }

                    key = GetUniqueRegistrationKey();
                    keys.Add(key);
                    RegisterCallback(Convert.ToInt32(category), callback);

                    return true;
                }
                else // The callback category doesn't exists yet
                {
                    key = GetUniqueRegistrationKey();
                    _registeredCallbackKeys.Add(category, new HashSet<ulong>() { key });
                    RegisterCallback(Convert.ToInt32(category), callback);

                    return true;
                }
            }

            else if (IsInstanceValid()) // Wants to unregister callback
            {
                if (_registeredCallbackKeys.TryGetValue(category, out var keys) // The callback category exists
                    && keys.Remove(key)) // AND the key was registered and removed successfully
                {
                    UnregisterCallback(Convert.ToInt32(category), callback);
                    return true;
                }
            }

            return false;
        }

        #endregion


        #region Utility

        public static void Clear()
        {
            BaseUpdater.ResetRegistrationCount();
            _registeredCallbackKeys.Clear();

            GetInstance().Clear();
        }

        #endregion
    }
}
