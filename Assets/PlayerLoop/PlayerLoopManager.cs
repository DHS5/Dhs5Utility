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

        #region Events

        public static event Action PlayerLoopInitialized;

        #endregion

        #region Player Loop Creation

        private static void CreatePlayerLoop()
        {
            if (_modifiers != null && _modifiers.Count > 0)
            {
                var playerLoop = PlayerLoop.GetDefaultPlayerLoop();

                SortModifiers();
                foreach (var modifier in _modifiers)
                {
                    playerLoop = modifier.ModifyPlayerLoop(playerLoop);
                }

                PlayerLoop.SetPlayerLoop(playerLoop);
#if UNITY_EDITOR
                PlayerLoopWindow.TryRefresh();
#endif
            }
            PlayerLoopInitialized?.Invoke();
            PlayerLoopInitialized = null;
        }

        public static void ResetPlayerLoop()
        {
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());
#if UNITY_EDITOR
            PlayerLoopWindow.TryRefresh();
#endif
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

                if (mainSystem.subSystemList != null)
                {
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

                if (mainSystem.subSystemList != null)
                {
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
        }

        public static bool IsSystemEnabled(Type type)
        {
            return !_disabledSystems.ContainsKey(type);
        }

        #endregion

        #region Custom Systems

        #region Main Systems

        public static void AddCustomMainSystemAtIndex(PlayerLoopSystem system, int index)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            var mainSystems = playerLoop.subSystemList.ToList();
            mainSystems.Insert(index, system);
            playerLoop.subSystemList = mainSystems.ToArray();

            PlayerLoop.SetPlayerLoop(playerLoop);
#if UNITY_EDITOR
            PlayerLoopWindow.TryRefresh();
#endif
        }
        public static void AddCustomMainSystemBefore(PlayerLoopSystem system, Type mainSystemToInsertBeforeType)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            var mainSystems = playerLoop.subSystemList.ToList();
            for (int i = 0; i < mainSystems.Count; i++)
            {
                if (mainSystems[i].type == mainSystemToInsertBeforeType)
                {
                    mainSystems.Insert(i, system);
                }
            }
            playerLoop.subSystemList = mainSystems.ToArray();

            PlayerLoop.SetPlayerLoop(playerLoop);
#if UNITY_EDITOR
            PlayerLoopWindow.TryRefresh();
#endif
        }
        public static void AddCustomMainSystemAfter(PlayerLoopSystem system, Type mainSystemToInsertAfterType)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            var mainSystems = playerLoop.subSystemList.ToList();
            for (int i = 0; i < mainSystems.Count; i++)
            {
                if (mainSystems[i].type == mainSystemToInsertAfterType)
                {
                    mainSystems.Insert(i + 1, system);
                }
            }
            playerLoop.subSystemList = mainSystems.ToArray();

            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        #endregion

        #region Sub Systems

        public static void AddCustomSubSystemAtIndex(PlayerLoopSystem system, Type mainSystemType, int index)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                if (playerLoop.subSystemList[i].type == mainSystemType)
                {
                    List<PlayerLoopSystem> systems;
                    if (playerLoop.subSystemList[i].subSystemList != null)
                    {
                        systems = playerLoop.subSystemList[i].subSystemList.ToList();
                        systems.Insert(index, system);
                    }
                    else
                    {
                        systems = new()
                        {
                            system
                        };
                    }
                    playerLoop.subSystemList[i].subSystemList = systems.ToArray();
                }
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
#if UNITY_EDITOR
            PlayerLoopWindow.TryRefresh();
#endif
        }
        public static void AddCustomSubSystemAtLast(PlayerLoopSystem system, Type mainSystemType)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            for (int i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                if (playerLoop.subSystemList[i].type == mainSystemType)
                {
                    List<PlayerLoopSystem> systems;
                    if (playerLoop.subSystemList[i].subSystemList != null)
                    {
                        systems = playerLoop.subSystemList[i].subSystemList.ToList();
                    }
                    else
                    {
                        systems = new();
                    }
                    systems.Add(system);
                    playerLoop.subSystemList[i].subSystemList = systems.ToArray();
                }
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
#if UNITY_EDITOR
            PlayerLoopWindow.TryRefresh();
#endif
        }

        #endregion

        #endregion
    }
}
