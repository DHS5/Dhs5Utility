using System;
using System.Collections;
using System.Collections.Generic;

namespace Dhs5.Utility.Updates
{
    public class Updater<UpdateEnum> : BaseUpdater where UpdateEnum : Enum
    {
        #region Callbacks Registration

        private static ulong _registrationCount = 0;
        private static Dictionary<UpdateEnum, HashSet<ulong>> _registeredCallbackKeys = new();

        private static ulong GetUniqueRegistrationKey()
        {
            _registrationCount++;
            return _registrationCount;
        }

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
                    GetInstance().RegisterCallback(Convert.ToInt32(category), callback);

                    return true;
                }
                else // The callback category doesn't exists yet
                {
                    key = GetUniqueRegistrationKey();
                    _registeredCallbackKeys.Add(category, new HashSet<ulong>() { key });
                    GetInstance().RegisterCallback(Convert.ToInt32(category), callback);

                    return true;
                }
            }

            else if (IsInstanceValid()) // Wants to unregister callback
            {
                if (_registeredCallbackKeys.TryGetValue(category, out var keys) // The callback category exists
                    && keys.Remove(key)) // AND the key was registered and removed successfully
                {
                    GetInstance().UnregisterCallback(Convert.ToInt32(category), callback);
                    return true;
                }
            }

            return false;
        }

        #endregion


        #region Utility

        public static void Clear()
        {
            _registrationCount = 0;
            _registeredCallbackKeys.Clear();

            GetInstance().Clear();
        }

        #endregion
    }
}
