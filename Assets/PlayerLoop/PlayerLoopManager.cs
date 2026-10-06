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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // In case the previous session didn't restore it (Application.quitting not called)
            ResetPlayerLoop();

            _modifiersRegistrationOpen = true;
            _modifiers.Clear();
            _disabledSystems.Clear();
            PlayerLoopInitialized = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AfterSceneLoad()
        {
            Application.quitting -= OnApplicationQuitting;
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
                SaveOriginalPlayerLoopIfNeeded();
                // Start from the current player loop and not the default one, to keep systems inserted by other packages
                var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

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

        /// <summary>
        /// Restores the player loop as it was before any modification made through this class
        /// </summary>
        public static void ResetPlayerLoop()
        {
            if (!_isPlayerLoopModified) return;

            PlayerLoop.SetPlayerLoop(_originalPlayerLoop);
            _originalPlayerLoop = default;
            _isPlayerLoopModified = false;
            _disabledSystems.Clear();
#if UNITY_EDITOR
            PlayerLoopWindow.TryRefresh();
#endif
        }
        
        public static void ResetPlayerLoopToDefault()
        {
            SaveOriginalPlayerLoopIfNeeded();
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());
            _disabledSystems.Clear();
#if UNITY_EDITOR
            PlayerLoopWindow.TryRefresh();
#endif
        }

        #endregion

        #region Original Player Loop

        // The player loop as it was before our first modification, including systems inserted by other packages
        // (GetDefaultPlayerLoop() would remove them).
        // Intentionally not cleared in ResetStatics : it is needed there to restore a loop the previous session left modified.
        private static PlayerLoopSystem _originalPlayerLoop;
        private static bool _isPlayerLoopModified;

        private static void SaveOriginalPlayerLoopIfNeeded()
        {
            if (_isPlayerLoopModified) return;

            // Separate GetCurrentPlayerLoop() call from the one being modified, so in-place changes to its arrays can't alter this copy
            _originalPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            _isPlayerLoopModified = true;
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

            SaveOriginalPlayerLoopIfNeeded();
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

            SaveOriginalPlayerLoopIfNeeded();
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
            SaveOriginalPlayerLoopIfNeeded();
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
            SaveOriginalPlayerLoopIfNeeded();
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            var mainSystems = playerLoop.subSystemList.ToList();
            for (int i = 0; i < mainSystems.Count; i++)
            {
                if (mainSystems[i].type == mainSystemToInsertBeforeType)
                {
                    mainSystems.Insert(i, system);
                    break;
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
            SaveOriginalPlayerLoopIfNeeded();
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            var mainSystems = playerLoop.subSystemList.ToList();
            for (int i = 0; i < mainSystems.Count; i++)
            {
                if (mainSystems[i].type == mainSystemToInsertAfterType)
                {
                    mainSystems.Insert(i + 1, system);
                    break;
                }
            }
            playerLoop.subSystemList = mainSystems.ToArray();

            PlayerLoop.SetPlayerLoop(playerLoop);
#if UNITY_EDITOR
            PlayerLoopWindow.TryRefresh();
#endif
        }

        #endregion

        #region Sub Systems

        public static void AddCustomSubSystemAtIndex(PlayerLoopSystem system, Type mainSystemType, int index)
        {
            SaveOriginalPlayerLoopIfNeeded();
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
            SaveOriginalPlayerLoopIfNeeded();
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
