using System;
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

        #region Systems Enabling

        private static Dictionary<Type, PlayerLoopSystem> _disabledSystems = new();

        public static void DisableSystem(Type type)
        {
            if (_disabledSystems.ContainsKey(type)) return;

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            PlayerLoopSystem mainSystem, system;
            for (int mi = 0; mi < playerLoop.subSystemList.Length; mi++)
            {
                mainSystem = playerLoop.subSystemList[mi];
                if (mainSystem.type == type)
                {
                    _disabledSystems[type] = mainSystem;
                    playerLoop.subSystemList[mi] = new PlayerLoopSystem() { type = type };
                    PlayerLoop.SetPlayerLoop(playerLoop);
                    break;
                }

                for (int si = 0; si < mainSystem.subSystemList.Length; si++)
                {
                    system = mainSystem.subSystemList[si];
                    if (system.type == type)
                    {
                        _disabledSystems[type] = system;
                        mainSystem.subSystemList[si] = new PlayerLoopSystem() { type = type };
                        playerLoop.subSystemList[mi] = mainSystem;
                        PlayerLoop.SetPlayerLoop(playerLoop);
                        break;
                    }
                }
            }
        }
        public static void ReenableSystem(Type type)
        {
            if (!_disabledSystems.ContainsKey(type)) return;

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            PlayerLoopSystem mainSystem, system;
            for (int mi = 0; mi < playerLoop.subSystemList.Length; mi++)
            {
                mainSystem = playerLoop.subSystemList[mi];
                if (mainSystem.type == type)
                {
                    playerLoop.subSystemList[mi] = _disabledSystems[type];
                    PlayerLoop.SetPlayerLoop(playerLoop);
                    _disabledSystems.Remove(type);
                    break;
                }

                for (int si = 0; si < mainSystem.subSystemList.Length; si++)
                {
                    system = mainSystem.subSystemList[si];
                    if (system.type == type)
                    {
                        mainSystem.subSystemList[si] = _disabledSystems[type];
                        playerLoop.subSystemList[mi] = mainSystem;
                        PlayerLoop.SetPlayerLoop(playerLoop);
                        _disabledSystems.Remove(type);
                        break;
                    }
                }
            }
        }

        #endregion

        #region New Systems

        public static void AddNewMainSystem(PlayerLoopSystem system, int index)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            var mainSystems = playerLoop.subSystemList.ToList();
            mainSystems.Insert(index, system);
            playerLoop.subSystemList = mainSystems.ToArray();

            PlayerLoop.SetPlayerLoop(playerLoop);
        }
        public static void AddNewSubSystem(PlayerLoopSystem system, Type mainSystemType, int index)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                if (playerLoop.subSystemList[i].type == mainSystemType)
                {
                    var systems = playerLoop.subSystemList[i].subSystemList.ToList();
                    systems.Insert(index, system);
                    playerLoop.subSystemList[i].subSystemList = systems.ToArray();
                }
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
        }
        public static void AddNewSubSystemAtLast(PlayerLoopSystem system, Type mainSystemType)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                if (playerLoop.subSystemList[i].type == mainSystemType)
                {
                    var systems = playerLoop.subSystemList[i].subSystemList.ToList();
                    systems.Add(system);
                    playerLoop.subSystemList[i].subSystemList = systems.ToArray();
                }
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
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
