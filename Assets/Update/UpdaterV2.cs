using Dhs5.Utility.PlayerLoops;
using System.Linq;
using UnityEngine.LowLevel;
using UnityEngine;
using UnityEngine.PlayerLoop;
using System.Collections.Generic;
using System;

namespace Dhs5.Utility.Updates
{
    public sealed class UpdaterV2 : IPlayerLoopModifier
    {
        #region Instance

        public static UpdaterV2 Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitBeforeSceneLoad()
        {
            Instance = new UpdaterV2();
            PlayerLoopManager.RegisterModifier(Instance);
        }

        #endregion

        #region Constructor

        public UpdaterV2()
        {

        }

        #endregion


        #region Player Loop

        #region IPlayerLoopModifier

        public int Priority => 0;

        public PlayerLoopSystem ModifyPlayerLoop(PlayerLoopSystem playerLoopSystem)
        {
            var mainSystems = playerLoopSystem.subSystemList.ToList();

            // --- FIXED UPDATE ---
            var fixedUpdate = mainSystems[3];
            var fixedUpdateSystems = fixedUpdate.subSystemList.ToList();
            // Before Fixed Update
            fixedUpdateSystems.Insert(4, BeforeFixedUpdate.GetSystem());
            int index = 5;
            foreach (var system in GetChannelsSystemsForPass(EUpdatePass.BEFORE_FIXED_UPDATE))
            {
                fixedUpdateSystems.Insert(index, system);
                index++;
            }
            // After Physics Fixed Update
            fixedUpdateSystems.Add(AfterPhysicsFixedUpdate.GetSystem());
            foreach (var system in GetChannelsSystemsForPass(EUpdatePass.AFTER_PHYSICS_FIXED_UPDATE))
            {
                fixedUpdateSystems.Add(system);
            }
            // Set
            fixedUpdate.subSystemList = fixedUpdateSystems.ToArray();
            mainSystems[3] = fixedUpdate;

            // --- MAIN SYSTEMS ---
            // After Late Update
            var afterLateUpdateSystem = AfterLateUpdate.GetSystem();
            afterLateUpdateSystem.subSystemList = GetChannelsSystemsForPass(EUpdatePass.AFTER_LATE_UPDATE).ToArray();
            mainSystems.Insert(7, afterLateUpdateSystem);
            // After Update
            var afterUpdateSystem = AfterUpdate.GetSystem();
            afterUpdateSystem.subSystemList = GetChannelsSystemsForPass(EUpdatePass.AFTER_UPDATE).ToArray();
            mainSystems.Insert(6, afterUpdateSystem);
            // Before Update
            var beforeUpdateSystem = BeforeUpdate.GetSystem();
            beforeUpdateSystem.subSystemList = GetChannelsSystemsForPass(EUpdatePass.BEFORE_UPDATE).ToArray();
            mainSystems.Insert(5, beforeUpdateSystem);
            // After Early Update
            var afterEarlyUpdateSystem = AfterEarlyUpdate.GetSystem();
            afterEarlyUpdateSystem.subSystemList = GetChannelsSystemsForPass(EUpdatePass.AFTER_EARLY_UPDATE).ToArray();
            mainSystems.Insert(3, afterEarlyUpdateSystem);

            playerLoopSystem.subSystemList = mainSystems.ToArray();

            return playerLoopSystem;
        }

        #endregion

        #region Main Systems

        public struct AfterEarlyUpdate
        {
            public static PlayerLoopSystem GetSystem()
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(AfterEarlyUpdate),
                    updateDelegate = OnAfterEarlyUpdate
                };
            }
        }
        public struct BeforeUpdate
        {
            public static PlayerLoopSystem GetSystem()
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(BeforeUpdate),
                    updateDelegate = OnBeforeUpdate
                };
            }
        }
        public struct AfterUpdate
        {
            public static PlayerLoopSystem GetSystem()
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(AfterUpdate),
                    updateDelegate = OnAfterUpdate
                };
            }
        }
        public struct AfterLateUpdate
        {
            public static PlayerLoopSystem GetSystem()
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(AfterLateUpdate),
                    updateDelegate = OnAfterLateUpdate
                };
            }
        }

        #endregion

        #region Sub Systems

        public struct BeforeFixedUpdate
        {
            public static PlayerLoopSystem GetSystem()
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(BeforeFixedUpdate),
                    updateDelegate = OnBeforeFixedUpdate
                };
            }
        }
        public struct AfterPhysicsFixedUpdate
        {
            public static PlayerLoopSystem GetSystem()
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(AfterPhysicsFixedUpdate),
                    updateDelegate = OnAfterPhysicsFixedUpdate
                };
            }
        }

        #endregion

        #region Update Channels PlayerLoop Insertion

        private List<PlayerLoopSystem> GetChannelsSystemsForPass(EUpdatePass pass)
        {
            List<IUpdateChannel> channels = new();
            foreach (var obj in Enum.GetValues(typeof(EUpdateChannel)))
            {
                var channel = ((EUpdateChannel)obj).GetValue();

                if (channel.Pass == pass)
                {
                    channels.Add(channel);
                }
            }

            channels.Sort((c1, c2) => c1.Order.CompareTo(c2.Order));

            List<PlayerLoopSystem> systems = new();
            foreach (var channel in channels)
            {
                systems.Add(new PlayerLoopSystem()
                {
                    type = ((EUpdateChannel)channel.ChannelIndex).GetChannelType(),
                    updateDelegate = GetChannelUpdate(channel.ChannelIndex)
                });
            }

            return systems;
        }

        #endregion

        #region Custom System Registration

        public void RegisterCustomSystem(PlayerLoopSystem customSystem, EUpdatePass pass)
        {
            switch (pass)
            {
                case EUpdatePass.AFTER_EARLY_UPDATE:
                    PlayerLoopManager.AddCustomSubSystemAtLast(customSystem, typeof(AfterEarlyUpdate));
                    break;
                
                case EUpdatePass.BEFORE_UPDATE:
                    PlayerLoopManager.AddCustomSubSystemAtLast(customSystem, typeof(BeforeUpdate));
                    break;
                
                case EUpdatePass.AFTER_UPDATE:
                    PlayerLoopManager.AddCustomSubSystemAtLast(customSystem, typeof(AfterUpdate));
                    break;
                
                case EUpdatePass.AFTER_LATE_UPDATE:
                    PlayerLoopManager.AddCustomSubSystemAtLast(customSystem, typeof(AfterLateUpdate));
                    break;
                
                case EUpdatePass.BEFORE_FIXED_UPDATE:
                    PlayerLoopManager.AddCustomSubSystemAtIndex(customSystem, typeof(FixedUpdate), 5);
                    break;
                
                case EUpdatePass.AFTER_PHYSICS_FIXED_UPDATE:
                    PlayerLoopManager.AddCustomSubSystemAtLast(customSystem, typeof(FixedUpdate));
                    break;
            }
        }

        #endregion

        #region Default Update Channel System

        public struct UpdateChannel { }

        #endregion

        #endregion

        #region Updates

        #region Main Systems

        private static void OnAfterEarlyUpdate()
        {
            Debug.Log("Update " + typeof(AfterEarlyUpdate).Name);
        }
        private static void OnBeforeUpdate()
        {
            Debug.Log("Update " + typeof(BeforeUpdate).Name);
        }
        private static void OnAfterUpdate()
        {
            Debug.Log("Update " + typeof(AfterUpdate).Name);
        }
        private static void OnAfterLateUpdate()
        {
            Debug.Log("Update " + typeof(AfterLateUpdate).Name);
        }

        private static void OnBeforeFixedUpdate()
        {
            Debug.Log("Fixed Update " + typeof(BeforeFixedUpdate).Name);
        }
        private static void OnAfterPhysicsFixedUpdate()
        {
            Debug.Log("Fixed Update " + typeof(AfterPhysicsFixedUpdate).Name);
        }

        #endregion

        #region Channels

        private PlayerLoopSystem.UpdateFunction GetChannelUpdate(int index)
        {
            return index switch
            {
                0 => OnChannel0Update,
                1 => OnChannel1Update,
                2 => OnChannel2Update,
                3 => OnChannel3Update,
                4 => OnChannel4Update,
                5 => OnChannel5Update,
                6 => OnChannel6Update,
                7 => OnChannel7Update,
                8 => OnChannel8Update,
                9 => OnChannel9Update,
                10 => OnChannel10Update,
                11 => OnChannel11Update,
                12 => OnChannel12Update,
                13 => OnChannel13Update,
                14 => OnChannel14Update,
                15 => OnChannel15Update,
                16 => OnChannel16Update,
                17 => OnChannel17Update,
                18 => OnChannel18Update,
                19 => OnChannel19Update,
                20 => OnChannel20Update,
                21 => OnChannel21Update,
                22 => OnChannel22Update,
                23 => OnChannel23Update,
                24 => OnChannel24Update,
                25 => OnChannel25Update,
                26 => OnChannel26Update,
                27 => OnChannel27Update,
                28 => OnChannel28Update,
                29 => OnChannel29Update,
                30 => OnChannel30Update,
                31 => OnChannel31Update,
                _ => null,
            };
        }

        private static void OnChannel0Update() { }
        private static void OnChannel1Update() { }
        private static void OnChannel2Update() { }
        private static void OnChannel3Update() { }
        private static void OnChannel4Update() { }
        private static void OnChannel5Update() { }
        private static void OnChannel6Update() { }
        private static void OnChannel7Update() { }
        private static void OnChannel8Update() { }
        private static void OnChannel9Update() { }
        private static void OnChannel10Update() { }
        private static void OnChannel11Update() { }
        private static void OnChannel12Update() { }
        private static void OnChannel13Update() { }
        private static void OnChannel14Update() { }
        private static void OnChannel15Update() { }
        private static void OnChannel16Update() { }
        private static void OnChannel17Update() { }
        private static void OnChannel18Update() { }
        private static void OnChannel19Update() { }
        private static void OnChannel20Update() { }
        private static void OnChannel21Update() { }
        private static void OnChannel22Update() { }
        private static void OnChannel23Update() { }
        private static void OnChannel24Update() { }
        private static void OnChannel25Update() { }
        private static void OnChannel26Update() { }
        private static void OnChannel27Update() { }
        private static void OnChannel28Update() { }
        private static void OnChannel29Update() { }
        private static void OnChannel30Update() { }
        private static void OnChannel31Update() { }

        #endregion

        #endregion
    }
}
