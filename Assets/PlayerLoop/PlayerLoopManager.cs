using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Dhs5.Utility.PlayerLoops
{
    public static class PlayerLoopManager
    {
        #region Engine Callbacks

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            Application.quitting += OnApplicationQuitting;

            _modifiersRegistrationOpen = false;
            CreatePlayerLoop();
            ClearModifiers();
        }

        static void OnApplicationQuitting()
        {
            ResetPlayerLoop();
        }

        #endregion

        #region Player Loop Creation

        private static void CreatePlayerLoop()
        {
            var playerLoop = PlayerLoop.GetDefaultPlayerLoop();

            if (_modifiers != null && _modifiers.Count > 0)
            {
                SortModifiers();
                foreach (var modifier in _modifiers)
                {
                    playerLoop = modifier.ModifyPlayerLoop(playerLoop);
                }
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        public static void ResetPlayerLoop()
        {
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());
        }

        #endregion

        #region Player Loop Modifiers

        private static bool _modifiersRegistrationOpen = true;
        private static List<IPlayerLoopModifier> _modifiers = new();

        /// <summary>
        /// Registration should be done before the <see cref="RuntimeInitializeLoadType.AfterSceneLoad"/> callback
        /// </summary>
        public static bool RegisterModifier(IPlayerLoopModifier modifier)
        {
            if (_modifiersRegistrationOpen)
            {
                _modifiers.Add(modifier);
                return true;
            }
            return false;
        }

        private static void SortModifiers()
        {
            _modifiers.Sort((m1, m2) => m1.Priority.CompareTo(m2.Priority));
        }
        private static void ClearModifiers()
        {
            _modifiers.Clear();
        }

        #endregion


        #region Custom Updates

        public struct CustomUpdate
        {
            public static PlayerLoopSystem GetSystem()
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(CustomUpdate),
                    updateDelegate = OnUpdate,
                };
            }

            static void OnUpdate()
            {
                Debug.Log("update");
            }
        }

        #endregion
    }
}
