using Dhs5.Utility.PlayerLoops;
using System.Linq;
using UnityEngine.LowLevel;
using UnityEngine;

namespace Dhs5.Utility.Updates
{
    public class UpdaterV2 : IPlayerLoopModifier
    {
        #region Instance

        public static UpdaterV2 Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            Instance = new UpdaterV2();
            PlayerLoopManager.RegisterModifier(Instance);
        }

        #endregion

        #region IPlayerLoopModifier

        public int Priority => 0;

        public PlayerLoopSystem ModifyPlayerLoop(PlayerLoopSystem playerLoopSystem)
        {
            var mainSystems = playerLoopSystem.subSystemList.ToList();

            mainSystems.Insert(6, AfterUpdate.GetSystem());

            var beforeUpdateSystem = BeforeUpdate.GetSystem();
            beforeUpdateSystem.subSystemList = new PlayerLoopSystem[] { BeforeUpdateSub.GetSystem() };
            mainSystems.Insert(5, beforeUpdateSystem);

            playerLoopSystem.subSystemList = mainSystems.ToArray();

            return playerLoopSystem;
        }

        #endregion

        #region Player Loop Main Systems

        public struct BeforeUpdate
        {
            public static PlayerLoopSystem GetSystem()
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(BeforeUpdate),
                    updateDelegate = OnUpdate
                };
            }

            public static void OnUpdate()
            {
                Debug.Log("Update " + typeof(BeforeUpdate).Name);
            }
        }
        public struct BeforeUpdateSub
        {
            public static PlayerLoopSystem GetSystem()
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(BeforeUpdateSub),
                    updateDelegate = OnUpdate
                };
            }

            public static void OnUpdate()
            {
                Debug.Log("Update " + typeof(BeforeUpdateSub).Name);
            }
        }
        
        public struct AfterUpdate
        {
            public static PlayerLoopSystem GetSystem()
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(AfterUpdate),
                    updateDelegate = OnUpdate
                };
            }

            public static void OnUpdate()
            {
                Debug.Log("Update " + typeof(AfterUpdate).Name);
            }
        }

        #endregion
    }
}
