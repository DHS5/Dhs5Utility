using System.Linq;
using UnityEngine.LowLevel;
using UnityEngine;
using UnityEngine.PlayerLoop;
using System.Collections.Generic;
using System;
using Dhs5.Utility.PlayerLoops;
using UnityEditor.PackageManager;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.SocialPlatforms;

namespace Dhs5.Utility.Updates
{
    public sealed class UpdaterV2 : IPlayerLoopModifier
    {
        #region Instance

        internal static UpdaterV2 Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitBeforeSceneLoad()
        {
            Instance = new UpdaterV2();
            PlayerLoopManager.RegisterModifier(Instance);
        }

        #endregion

        #region Constructor

        private UpdaterV2()
        {
            PlayerLoopManager.PlayerLoopInitialized += OnPlayerLoopInitialized;

            InitChannels();
        }

        #endregion

        #region Static Events

        public static event UpdateCallback AfterEarlyUpdated;
        public static event UpdateCallback ClassicUpdated;
        public static event UpdateCallback AfterUpdated;
        public static event UpdateCallback AfterLateUpdated;
               
        public static event UpdateCallback BeforeFixedUpdated;
        public static event UpdateCallback AfterPhysicsFixedUpdated;

        #endregion


        #region Properties

        // TIME
        public float Time { get; private set; }
        public float DeltaTime { get; private set; }
        // REALTIME
        public float RealTime { get; private set; }
        public float RealDeltaTime { get; private set; }
        // FRAME
        public int Frame { get; private set; }

        // GAME STATE
        public bool TimePaused { get; private set; }

        #endregion

        #region Overrider

        public static IUpdaterOverrider Overrider { get; set; }

        #endregion


        #region Player Loop

        #region IPlayerLoopModifier

        public int Priority => 0;

        public PlayerLoopSystem ModifyPlayerLoop(PlayerLoopSystem playerLoopSystem)
        {
            var mainSystems = playerLoopSystem.subSystemList.ToList();

            // --- TIME UPDATE ---
            var timeUpdate = mainSystems[0];
            var timeUpdateSystems = mainSystems[0].subSystemList.ToList();
            timeUpdateSystems.Add(UpdaterTimeUpdate.GetSystem(OnTimeUpdate));
            timeUpdate.subSystemList = timeUpdateSystems.ToArray();
            mainSystems[0] = timeUpdate;

            // --- INITIALIZATION
            var initUpdate = mainSystems[1];
            var initUpdateSystems = mainSystems[1].subSystemList.ToList();
            initUpdateSystems.Add(UpdaterInitialization.GetSystem(OnInitializationUpdate));
            initUpdate.subSystemList = initUpdateSystems.ToArray();
            mainSystems[1] = initUpdate;

            // --- FRAME END ---
            //var postLateUpdate = mainSystems[7];
            //var postLateUpdateSystems = postLateUpdate.subSystemList.ToList();
            //postLateUpdateSystems.Add(UpdaterEndFrame.GetSystem(OnFrameEndUpdate));
            //postLateUpdate.subSystemList = postLateUpdateSystems.ToArray();
            //mainSystems[7] = postLateUpdate;

            // --- FIXED UPDATE ---
            var fixedUpdate = mainSystems[3];
            var fixedUpdateSystems = fixedUpdate.subSystemList.ToList();
            // Before Fixed Update
            fixedUpdateSystems.Insert(4, BeforeFixedUpdate.GetSystem(OnBeforeFixedUpdate));
            int index = 5;
            foreach (var system in GetChannelsSystemsForPass(EUpdatePass.BEFORE_FIXED_UPDATE))
            {
                fixedUpdateSystems.Insert(index, system);
                index++;
            }
            // After Physics Fixed Update
            fixedUpdateSystems.Add(AfterPhysicsFixedUpdate.GetSystem(OnAfterPhysicsFixedUpdate));
            foreach (var system in GetChannelsSystemsForPass(EUpdatePass.AFTER_PHYSICS_FIXED_UPDATE))
            {
                fixedUpdateSystems.Add(system);
            }
            // Set
            fixedUpdate.subSystemList = fixedUpdateSystems.ToArray();
            mainSystems[3] = fixedUpdate;

            // --- MAIN SYSTEMS ---
            // After Late Update
            var afterLateUpdateSystem = AfterLateUpdate.GetSystem(OnAfterLateUpdate);
            afterLateUpdateSystem.subSystemList = GetChannelsSystemsForPass(EUpdatePass.AFTER_LATE_UPDATE).ToArray();
            mainSystems.Insert(7, afterLateUpdateSystem);
            // After Update
            var afterUpdateSystem = AfterUpdate.GetSystem(OnAfterUpdate);
            afterUpdateSystem.subSystemList = GetChannelsSystemsForPass(EUpdatePass.AFTER_UPDATE).ToArray();
            mainSystems.Insert(6, afterUpdateSystem);
            // Before Update
            var beforeUpdateSystem = BeforeUpdate.GetSystem(OnBeforeUpdate);
            beforeUpdateSystem.subSystemList = GetChannelsSystemsForPass(EUpdatePass.CLASSIC_UPDATE).ToArray();
            mainSystems.Insert(5, beforeUpdateSystem);
            // After Early Update
            var afterEarlyUpdateSystem = AfterEarlyUpdate.GetSystem(OnAfterEarlyUpdate);
            afterEarlyUpdateSystem.subSystemList = GetChannelsSystemsForPass(EUpdatePass.AFTER_EARLY_UPDATE).ToArray();
            mainSystems.Insert(3, afterEarlyUpdateSystem);

            playerLoopSystem.subSystemList = mainSystems.ToArray();

            return playerLoopSystem;
        }

        #endregion

        #region PlayerLoopManager Callbacks

        private void OnPlayerLoopInitialized()
        {
            InitUpdateChannelEnabling();
        }

        #endregion

        #region Main Systems

        public struct AfterEarlyUpdate
        {
            public static PlayerLoopSystem GetSystem(PlayerLoopSystem.UpdateFunction updateFunction)
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(AfterEarlyUpdate),
                    updateDelegate = updateFunction
                };
            }
        }
        public struct BeforeUpdate
        {
            public static PlayerLoopSystem GetSystem(PlayerLoopSystem.UpdateFunction updateFunction)
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(BeforeUpdate),
                    updateDelegate = updateFunction
                };
            }
        }
        public struct AfterUpdate
        {
            public static PlayerLoopSystem GetSystem(PlayerLoopSystem.UpdateFunction updateFunction)
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(AfterUpdate),
                    updateDelegate = updateFunction
                };
            }
        }
        public struct AfterLateUpdate
        {
            public static PlayerLoopSystem GetSystem(PlayerLoopSystem.UpdateFunction updateFunction)
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(AfterLateUpdate),
                    updateDelegate = updateFunction
                };
            }
        }

        #endregion

        #region Sub Systems

        public struct UpdaterTimeUpdate
        {
            public static PlayerLoopSystem GetSystem(PlayerLoopSystem.UpdateFunction updateFunction)
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(UpdaterTimeUpdate),
                    updateDelegate = updateFunction
                };
            }
        }
        public struct UpdaterInitialization
        {
            public static PlayerLoopSystem GetSystem(PlayerLoopSystem.UpdateFunction updateFunction)
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(UpdaterInitialization),
                    updateDelegate = updateFunction
                };
            }
        }
        public struct UpdaterEndFrame
        {
            public static PlayerLoopSystem GetSystem(PlayerLoopSystem.UpdateFunction updateFunction)
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(UpdaterEndFrame),
                    updateDelegate = updateFunction
                };
            }
        }
        public struct BeforeFixedUpdate
        {
            public static PlayerLoopSystem GetSystem(PlayerLoopSystem.UpdateFunction updateFunction)
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(BeforeFixedUpdate),
                    updateDelegate = updateFunction
                };
            }
        }
        public struct AfterPhysicsFixedUpdate
        {
            public static PlayerLoopSystem GetSystem(PlayerLoopSystem.UpdateFunction updateFunction)
            {
                return new PlayerLoopSystem()
                {
                    type = typeof(AfterPhysicsFixedUpdate),
                    updateDelegate = updateFunction
                };
            }
        }

        #endregion

        #region Update Channels PlayerLoop Insertion

        private List<PlayerLoopSystem> GetChannelsSystemsForPass(EUpdatePass pass)
        {
            List<UpdateChannel> channels = new();
            foreach (var channel in m_channels.Values)
            {
                if (channel.pass == pass)
                {
                    channels.Add(channel);
                }
            }

            channels.Sort((c1, c2) => c1.order.CompareTo(c2.order));

            List<PlayerLoopSystem> systems = new();
            foreach (var channel in channels)
            {
                systems.Add(new PlayerLoopSystem()
                {
                    type = channel.type,
                    updateDelegate = GetChannelUpdate((int)channel.channel)
                });
            }

            return systems;
        }

        #endregion

        #region Update Channels Enabling

        private void InitUpdateChannelEnabling()
        {
            foreach (var channel in m_channels.Values)
            {
                if (!channel.Enabled)
                {
                    PlayerLoopManager.DisableSystem(channel.type);
                }
            }
        }

        private void EnableUpdateChannel(bool enable, int index)
        {
            if (m_channels.TryGetValue(index, out var channel))
            {
                if (enable)
                {
                    PlayerLoopManager.ReenableSystem(channel.type);
                }
                else
                {
                    PlayerLoopManager.DisableSystem(channel.type);
                }
            }
        }

        #endregion

        #region Custom System Registration

        public static void RegisterCustomPlayerLoopSystem(PlayerLoopSystem customSystem, EUpdatePass pass)
        {
            switch (pass)
            {
                case EUpdatePass.AFTER_EARLY_UPDATE:
                    PlayerLoopManager.AddCustomSubSystemAtLast(customSystem, typeof(AfterEarlyUpdate));
                    break;
                
                case EUpdatePass.CLASSIC_UPDATE:
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

        public struct DefaultUpdateChannel { }

        #endregion

        #endregion

        #region Updates

        #region Updater

        private void OnTimeUpdate()
        {
            Time = UnityEngine.Time.time;
            DeltaTime = UnityEngine.Time.deltaTime;

            RealTime = UnityEngine.Time.realtimeSinceStartup;
            RealDeltaTime = UnityEngine.Time.unscaledDeltaTime;

            Frame = UnityEngine.Time.frameCount;

            TimePaused = DeltaTime != 0f;
        }
        private void OnInitializationUpdate()
        {
            ResetCurrentFramePasses();
            PerformDelayedCallsRegistraton();
        }
        private void OnFrameEndUpdate()
        {
            
        }

        #endregion

        #region Main Systems

        private void OnAfterEarlyUpdate()
        {
            UpdateDelayedCalls(EUpdatePass.AFTER_EARLY_UPDATE, DeltaTime);
            AfterEarlyUpdated?.Invoke(DeltaTime);

            m_currentFramePasses.Add(EUpdatePass.AFTER_EARLY_UPDATE);
        }
        private void OnBeforeUpdate()
        {
            UpdateDelayedCalls(EUpdatePass.CLASSIC_UPDATE, DeltaTime);
            ClassicUpdated?.Invoke(DeltaTime);

            m_currentFramePasses.Add(EUpdatePass.CLASSIC_UPDATE);
        }
        private void OnAfterUpdate()
        {
            UpdateDelayedCalls(EUpdatePass.AFTER_UPDATE, DeltaTime);
            AfterUpdated?.Invoke(DeltaTime);

            m_currentFramePasses.Add(EUpdatePass.AFTER_UPDATE);
        }
        private void OnAfterLateUpdate()
        {
            UpdateDelayedCalls(EUpdatePass.AFTER_LATE_UPDATE, DeltaTime);
            AfterLateUpdated?.Invoke(DeltaTime);

            m_currentFramePasses.Add(EUpdatePass.AFTER_LATE_UPDATE);
        }

        private void OnBeforeFixedUpdate()
        {
            UpdateDelayedCalls(EUpdatePass.BEFORE_FIXED_UPDATE, DeltaTime);
            BeforeFixedUpdated?.Invoke(UnityEngine.Time.fixedDeltaTime);

            m_currentFramePasses.Add(EUpdatePass.BEFORE_FIXED_UPDATE);
        }
        private void OnAfterPhysicsFixedUpdate()
        {
            UpdateDelayedCalls(EUpdatePass.AFTER_PHYSICS_FIXED_UPDATE, DeltaTime);
            AfterPhysicsFixedUpdated?.Invoke(UnityEngine.Time.fixedDeltaTime);

            m_currentFramePasses.Add(EUpdatePass.AFTER_PHYSICS_FIXED_UPDATE);
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

        private void OnChannel0Update() { UpdateChannelByIndex(0, DeltaTime, RealDeltaTime); }
        private void OnChannel1Update() { UpdateChannelByIndex(1, DeltaTime, RealDeltaTime); }
        private void OnChannel2Update() { UpdateChannelByIndex(2, DeltaTime, RealDeltaTime); }
        private void OnChannel3Update() { UpdateChannelByIndex(3, DeltaTime, RealDeltaTime); }
        private void OnChannel4Update() { UpdateChannelByIndex(4, DeltaTime, RealDeltaTime); }
        private void OnChannel5Update() { UpdateChannelByIndex(5, DeltaTime, RealDeltaTime); }
        private void OnChannel6Update() { UpdateChannelByIndex(6, DeltaTime, RealDeltaTime); }
        private void OnChannel7Update() { UpdateChannelByIndex(7, DeltaTime, RealDeltaTime); }
        private void OnChannel8Update() { UpdateChannelByIndex(8, DeltaTime, RealDeltaTime); }
        private void OnChannel9Update() { UpdateChannelByIndex(9, DeltaTime, RealDeltaTime); }
        private void OnChannel10Update() { UpdateChannelByIndex(10, DeltaTime, RealDeltaTime); }
        private void OnChannel11Update() { UpdateChannelByIndex(11, DeltaTime, RealDeltaTime); }
        private void OnChannel12Update() { UpdateChannelByIndex(12, DeltaTime, RealDeltaTime); }
        private void OnChannel13Update() { UpdateChannelByIndex(13, DeltaTime, RealDeltaTime); }
        private void OnChannel14Update() { UpdateChannelByIndex(14, DeltaTime, RealDeltaTime); }
        private void OnChannel15Update() { UpdateChannelByIndex(15, DeltaTime, RealDeltaTime); }
        private void OnChannel16Update() { UpdateChannelByIndex(16, DeltaTime, RealDeltaTime); }
        private void OnChannel17Update() { UpdateChannelByIndex(17, DeltaTime, RealDeltaTime); }
        private void OnChannel18Update() { UpdateChannelByIndex(18, DeltaTime, RealDeltaTime); }
        private void OnChannel19Update() { UpdateChannelByIndex(19, DeltaTime, RealDeltaTime); }
        private void OnChannel20Update() { UpdateChannelByIndex(20, DeltaTime, RealDeltaTime); }
        private void OnChannel21Update() { UpdateChannelByIndex(21, DeltaTime, RealDeltaTime); }
        private void OnChannel22Update() { UpdateChannelByIndex(22, DeltaTime, RealDeltaTime); }
        private void OnChannel23Update() { UpdateChannelByIndex(23, DeltaTime, RealDeltaTime); }
        private void OnChannel24Update() { UpdateChannelByIndex(24, DeltaTime, RealDeltaTime); }
        private void OnChannel25Update() { UpdateChannelByIndex(25, DeltaTime, RealDeltaTime); }
        private void OnChannel26Update() { UpdateChannelByIndex(26, DeltaTime, RealDeltaTime); }
        private void OnChannel27Update() { UpdateChannelByIndex(27, DeltaTime, RealDeltaTime); }
        private void OnChannel28Update() { UpdateChannelByIndex(28, DeltaTime, RealDeltaTime); }
        private void OnChannel29Update() { UpdateChannelByIndex(29, DeltaTime, RealDeltaTime); }
        private void OnChannel30Update() { UpdateChannelByIndex(30, DeltaTime, RealDeltaTime); }
        private void OnChannel31Update() { UpdateChannelByIndex(31, DeltaTime, RealDeltaTime); }

        #endregion

        #endregion

        #region Pass Management

        private List<EUpdatePass> m_currentFramePasses = new();

        private void ResetCurrentFramePasses()
        {
            m_currentFramePasses.Clear();
        }
        public static bool PassHasBeenTriggeredThisFrame(EUpdatePass pass)
        {
            return Instance.m_currentFramePasses.Contains(pass);
        }

        #endregion


        #region Channels

        #region CLASS UpdateChannel

        private class UpdateChannel
        {
            #region Constructors

            public UpdateChannel(IUpdateChannel updateChannel)
            {
                this.channel = updateChannel.Channel;
                this.type = channel.GetChannelType();
                this.pass = updateChannel.Pass;
                this.order = updateChannel.Order;
                this.condition = updateChannel.Condition;
                this.realtime = updateChannel.Realtime;

                Enabled = updateChannel.EnabledByDefault;
                Frequency = updateChannel.Frequency;
                Timescale = updateChannel.TimeScale;

                TimeSinceLastUpdate = 0f;
            }

            #endregion

            #region Members

            public readonly EUpdateChannel channel;
            public readonly Type type;
            public readonly EUpdatePass pass;
            public readonly ushort order;
            public readonly EUpdateCondition condition;
            public readonly bool realtime;

            private bool customFrequency;
            private float frequency;
            public bool Enabled { get; set; }
            public float Timescale { get; set; }

            #endregion

            #region Properties

            public float TimeSinceLastUpdate { get; private set; }
            public float Frequency
            {
                get => frequency;
                set
                {
                    frequency = value;
                    customFrequency = value > 0f;
                }
            }

            #endregion

            #region Behaviour

            public bool Update(float deltaTime, out float actualDeltaTime)
            {
                if (!customFrequency)
                {
                    actualDeltaTime = deltaTime;
                    return true;
                }
                else
                {
                    TimeSinceLastUpdate += deltaTime * Timescale;
                    actualDeltaTime = TimeSinceLastUpdate;
                    if (TimeSinceLastUpdate >= Frequency)
                    {
                        TimeSinceLastUpdate -= Frequency;
                        return true;
                    }
                }
                return false;
            }

            #endregion
        }

        #endregion

        #region Creation & Deletion

        private readonly Dictionary<int, UpdateChannel> m_channels = new();

        private void InitChannels()
        {
            foreach (var obj in Enum.GetValues(typeof(EUpdateChannel)))
            {
                var channel = ((EUpdateChannel)obj).GetValue();

                m_channels[(int)channel.Channel] = new(channel);
            }
        }

        private void ClearChannels()
        {
            m_channels.Clear();
            m_channelCallbacks.Clear();
        }

        #endregion

        #region Callbacks

        private readonly Dictionary<int, UpdateCallback> m_channelCallbacks = new();

        public static void RegisterChannelCallback(bool register, EUpdateChannel channel, UpdateCallback callback)
        {
            int channelIndex = (int)channel;

            if (register)
            {
                Instance.RegisterChannelCallback(channelIndex, callback);
            }
            else
            {
                Instance.UnregisterChannelCallback(channelIndex, callback);
            }
        }
        private void RegisterChannelCallback(int channelIndex, UpdateCallback callback)
        {
            if (m_channelCallbacks.ContainsKey(channelIndex))
            {
                m_channelCallbacks[channelIndex] += callback;
            }
            else
            {
                m_channelCallbacks.Add(channelIndex, callback);
            }
        }
        private void UnregisterChannelCallback(int channelIndex, UpdateCallback callback)
        {
            if (m_channelCallbacks.ContainsKey(channelIndex))
            {
                m_channelCallbacks[channelIndex] -= callback;
            }
        }

        private void TriggerChannelCallback(int channelIndex, float deltaTime)
        {
            if (m_channelCallbacks.TryGetValue(channelIndex, out var callback))
            {
                callback?.Invoke(deltaTime);
            }
        }

        #endregion

        #region Updates

        private void UpdateChannelByIndex(int index, float deltaTime, float realDeltaTime)
        {
            if (m_channels.TryGetValue(index, out var channel))
            {
                if (IsChannelValid(channel)
                        && channel.Update(channel.realtime ? realDeltaTime : deltaTime, out var actualDeltaTime))
                {
                    TriggerChannelCallback(index, actualDeltaTime);
                }
            }
        }

        #endregion

        #region Setters

        public static void SetChannelEnable(EUpdateChannel channel, bool enabled)
        {
            int channelIndex = (int)channel;
            if (Instance.m_channels.TryGetValue(channelIndex, out var chan)
                && chan.Enabled != enabled)
            {
                chan.Enabled = enabled;
                Instance.EnableUpdateChannel(enabled, channelIndex);
            }
        }
        public static void SetChannelTimescale(EUpdateChannel channel, float timescale)
        {
            int channelIndex = (int)channel;
            if (Instance.m_channels.TryGetValue(channelIndex, out var chan))
            {
                chan.Timescale = timescale;
            }
        }
        public static void SetChannelFrequency(EUpdateChannel channel, float frequency)
        {
            int channelIndex = (int)channel;
            if (Instance.m_channels.TryGetValue(channelIndex, out var chan))
            {
                chan.Frequency = Mathf.Max(frequency, 0f);
            }
        }

        #endregion

        #region Validity

        private bool IsChannelValid(UpdateChannel channel)
        {
            return channel.Enabled && IsConditionFulfilled(channel.condition);
        }

        private bool IsConditionFulfilled(EUpdateCondition condition)
        {
            if (Overrider != null
                && Overrider.OverrideConditionFulfillment(condition, out bool fulfilled))
            {
                return fulfilled;
            }

            switch (condition)
            {
                case EUpdateCondition.ALWAYS: return true;
                case EUpdateCondition.GAME_PLAYING: return UnityEngine.Time.timeScale > 0f;
                case EUpdateCondition.GAME_PAUSED: return UnityEngine.Time.timeScale == 0f;
                case EUpdateCondition.GAME_OVER: return false;
            }
            return false;
        }

        #endregion

        #endregion

        #region Timelines

        #region Creation & Deletion

        private readonly Dictionary<ulong, UpdateTimelineInstance> m_updateTimelineInstances = new();

        private bool CreateUpdateTimelineInstance(IUpdateTimeline updateTimeline, ulong key)
        {
            if (updateTimeline == null || m_updateTimelineInstances.ContainsKey(key))
            {
                return false;
            }

            if (updateTimeline.Duration > 0f)
            {
                var state = new UpdateTimelineInstance(updateTimeline);
                m_updateTimelineInstances[key] = state;
                RegisterChannelCallback((int)updateTimeline.UpdateChannel, state.OnUpdate);
                return true;
            }
            else
            {
                Debug.LogError("You tried to register an UpdateTimeline that has no valid update or a duration equal to 0");
                return false;
            }
        }
        private void DestroyUpdateTimelineInstance(ulong key)
        {
            if (m_updateTimelineInstances.TryGetValue(key, out UpdateTimelineInstance state))
            {
                UnregisterChannelCallback((int)state.updateChannel, state.OnUpdate);
                m_updateTimelineInstances.Remove(key);
            }
        }

        private void ClearUpdateTimelineInstances()
        {
            foreach (var key in m_updateTimelineInstances.Keys)
            {
                DestroyUpdateTimelineInstance(key);
            }
            m_updateTimelineInstances.Clear();
        }

        #endregion

        #region Static Creation & Deletion

        /// <summary>
        /// Creates an <see cref="UpdateTimelineInstance"/> from the parameters and out a handle for it
        /// </summary>
        /// <returns>Whether the instance was successfully registered</returns>
        public static bool CreateTimelineInstance(EUpdateChannel channel, float duration, out UpdateTimelineInstanceHandle handle, bool loop = false, float timescale = 1f, List<IUpdateTimeline.Event> events = null, int uid = 0)
        {
            return CreateTimelineInstance(new ScriptedUpdateTimeline(channel, duration, loop, timescale, events, uid), out handle);
        }

        /// <summary>
        /// Creates an Instance of <paramref name="timeline"/> and out a handle for it
        /// </summary>
        /// <returns>Whether the instance was successfully registered</returns>
        public static bool CreateTimelineInstance(IUpdateTimeline timeline, out UpdateTimelineInstanceHandle handle)
        {
            var key = GetUniqueRegistrationKey();
            handle = new(key);
            return Instance.CreateUpdateTimelineInstance(timeline, key);
        }

        /// <summary>
        /// Destroys the <see cref="UpdateTimelineInstance"/> with <paramref name="handle"/>.key
        /// </summary>
        public static void KillTimelineInstance(UpdateTimelineInstanceHandle handle)
        {
            Instance.DestroyUpdateTimelineInstance(handle.key);
        }

        #endregion

        #region Acessors

        internal bool TimelineInstanceExist(ulong key) => m_updateTimelineInstances.ContainsKey(key);
        internal bool TryGetUpdateTimelineInstance(ulong key, out UpdateTimelineInstance state) => m_updateTimelineInstances.TryGetValue(key, out state);

        public static bool TryGetUpdateTimelineInstanceHandle(int timelineUID, out UpdateTimelineInstanceHandle handle)
        {
            foreach (var (key, instance) in Instance.m_updateTimelineInstances)
            {
                if (instance.timelineUID == timelineUID)
                {
                    handle = new(key);
                    return true;
                }
            }

            handle = UpdateTimelineInstanceHandle.Empty;
            return false;
        }

        #endregion

        #endregion

        #region Delayed Calls

        #region CLASSES

        private abstract class DelayedCall
        {
            #region Members

            public readonly EUpdatePass pass;
            public readonly EUpdateCondition condition;
            protected Action m_callback;

            #endregion

            #region Constructor

            public DelayedCall(EUpdatePass pass, EUpdateCondition condition, Action callback)
            {
                this.pass = pass;
                this.condition = condition;
                m_callback = callback;
            }

            #endregion

            #region Update

            public abstract bool Update(float deltaTime);

            #endregion
        }
        private class TimedDelayedCall : DelayedCall
        {
            #region Members

            private float m_remainingTime;

            #endregion

            #region Constructor

            public TimedDelayedCall(float delay, EUpdatePass pass, EUpdateCondition condition, Action callback) : base(pass, condition, callback)
            {
                m_remainingTime = delay;
            }

            #endregion

            #region Update

            public override bool Update(float deltaTime)
            {
                m_remainingTime -= deltaTime;
                if (m_remainingTime <= 0f)
                {
                    m_callback?.Invoke();
                    return true;
                }
                return false;
            }

            #endregion

            #region Accessors

            public float GetRemainingTime()
            {
                return m_remainingTime;
            }

            #endregion
        }
        private class FrameDelayedCall : DelayedCall
        {
            #region Members

            private int m_remainingFrames;

            #endregion

            #region Constructor

            public FrameDelayedCall(int framesToWait, EUpdatePass pass, EUpdateCondition condition, Action callback) : base(pass, condition, callback)
            {
                m_remainingFrames = framesToWait;
            }

            #endregion

            #region Update

            public override bool Update(float deltaTime)
            {
                m_remainingFrames -= 1;
                if (m_remainingFrames == 0)
                {
                    m_callback?.Invoke();
                    return true;
                }
                return false;
            }

            #endregion

            #region Accessors

            public int GetRemainingFrames()
            {
                return m_remainingFrames;
            }

            #endregion
        }
        private class WaitDelayedCall : DelayedCall
        {
            #region Members

            private Func<bool> m_predicate;
            private bool m_waitUntil;

            #endregion

            #region Constructor

            public WaitDelayedCall(Func<bool> predicate, bool waitUntil, EUpdatePass pass, EUpdateCondition condition, Action callback) : base(pass, condition, callback)
            {
                m_predicate = predicate;
                m_waitUntil = waitUntil;
            }

            #endregion

            #region Update

            public override bool Update(float deltaTime)
            {
                bool predicateResult = m_predicate.Invoke();
                if (predicateResult == m_waitUntil)
                {
                    m_callback?.Invoke();
                    return true;
                }
                return false;
            }

            #endregion
        }

        #endregion

        #region Internal Registration

        private readonly Dictionary<ulong, DelayedCall> m_delayedCalls = new();
        private readonly Dictionary<ulong, DelayedCall> m_delayedCallsToRegister = new();

        private void PreRegisterDelayedCall(ulong key, DelayedCall delayedCall)
        {
            m_delayedCallsToRegister.Add(key, delayedCall);
        }
        private void PerformDelayedCallsRegistraton()
        {
            foreach (var (key, call) in m_delayedCallsToRegister)
            {
                m_delayedCalls.Add(key, call);
            }

            m_delayedCallsToRegister.Clear();
        }

        private void UnregisterDelayedCall(ulong key)
        {
            m_delayedCalls.Remove(key);
        }

        #endregion

        #region Typed Registration

        private void RegisterTimedDelayedCall(ulong key, float delay, EUpdatePass pass, EUpdateCondition condition, Action callback)
        {
            // If delay = 0f and condition is fulfilled
            // we need to trigger the callback and don't register the delayed call
            if (delay == 0f
                && IsConditionFulfilled(condition))
            {
                callback?.Invoke();
                return;
            }

            PreRegisterDelayedCall(key, new TimedDelayedCall(delay, pass, condition, callback));
        }
        private void RegisterFrameDelayedCall(ulong key, int framesToWait, EUpdatePass pass, EUpdateCondition condition, Action callback)
        {
            // If framesToWait = 0 and condition is fulfilled
            // we need to trigger the callback and don't register the delayed call
            if (framesToWait == 0
                && IsConditionFulfilled(condition))
            {
                callback?.Invoke();
                return;
            }

            PreRegisterDelayedCall(key, new FrameDelayedCall(framesToWait, pass, condition, callback));
        }
        private void RegisterWaitUntilDelayedCall(ulong key, Func<bool> predicate, EUpdatePass pass, EUpdateCondition condition, Action callback)
        {
            // If predicate is true and condition is fulfilled
            // we need to trigger the callback and don't register the delayed call
            if (predicate.Invoke()
                && IsConditionFulfilled(condition))
            {
                callback?.Invoke();
                return;
            }

            PreRegisterDelayedCall(key, new WaitDelayedCall(predicate, true, pass, condition, callback));
        }
        private void RegisterWaitWhileDelayedCall(ulong key, Func<bool> predicate, EUpdatePass pass, EUpdateCondition condition, Action callback)
        {
            // If predicate is false and condition is fulfilled
            // we need to trigger the callback and don't register the delayed call
            if (!predicate.Invoke()
                && IsConditionFulfilled(condition))
            {
                callback?.Invoke();
                return;
            }

            PreRegisterDelayedCall(key, new WaitDelayedCall(predicate, false, pass, condition, callback));
        }

        #endregion

        #region Static Registration

        /// <summary>
        /// Register a callback to be called once in <paramref name="framesToWait"/> number of frames
        /// </summary>
        public static void CallInXFrames(int framesToWait, Action callback, out DelayedCallHandle handle, EUpdatePass pass = EUpdatePass.CLASSIC_UPDATE, EUpdateCondition condition = EUpdateCondition.ALWAYS)
        {
            if (callback == null || framesToWait < 0)
            {
                handle = DelayedCallHandle.Empty;
                return;
            }

            var key = GetUniqueRegistrationKey();
            Instance.RegisterFrameDelayedCall(key, framesToWait, pass, condition, callback);
            handle = new DelayedCallHandle(key);
        }
        /// <summary>
        /// Register a callback to be called once in <paramref name="time"/> seconds
        /// </summary>
        public static void CallInXSeconds(float time, Action callback, out DelayedCallHandle handle, EUpdatePass pass = EUpdatePass.CLASSIC_UPDATE, EUpdateCondition condition = EUpdateCondition.ALWAYS)
        {
            if (callback == null || time < 0f)
            {
                handle = DelayedCallHandle.Empty;
                return;
            }

            var key = GetUniqueRegistrationKey();
            Instance.RegisterTimedDelayedCall(key, time, pass, condition, callback);
            handle = new DelayedCallHandle(key);
        }
        /// <summary>
        /// Register a callback to be called once <paramref name="predicate"/> becomes true
        /// </summary>
        public static void CallWhenTrue(Func<bool> predicate, Action callback, out DelayedCallHandle handle, EUpdatePass pass = EUpdatePass.CLASSIC_UPDATE, EUpdateCondition condition = EUpdateCondition.ALWAYS)
        {
            if (callback == null || predicate == null)
            {
                handle = DelayedCallHandle.Empty;
                return;
            }

            var key = GetUniqueRegistrationKey();
            Instance.RegisterWaitUntilDelayedCall(key, predicate, pass, condition, callback);
            handle = new DelayedCallHandle(key);
        }
        /// <summary>
        /// Register a callback to be called once <paramref name="predicate"/> becomes false
        /// </summary>
        public static void CallWhenFalse(Func<bool> predicate, Action callback, out DelayedCallHandle handle, EUpdatePass pass = EUpdatePass.CLASSIC_UPDATE, EUpdateCondition condition = EUpdateCondition.ALWAYS)
        {
            if (callback == null || predicate == null)
            {
                handle = DelayedCallHandle.Empty;
                return;
            }

            var key = GetUniqueRegistrationKey();
            Instance.RegisterWaitWhileDelayedCall(key, predicate, pass, condition, callback);
            handle = new DelayedCallHandle(key);
        }

        public static void KillDelayedCall(DelayedCallHandle handle)
        {
            Instance.UnregisterDelayedCall(handle.key);
        }

        #endregion

        #region Update

        private void UpdateDelayedCalls(EUpdatePass pass, float deltaTime)
        {
            List<ulong> toDestroy = new();
            foreach (var (key, delayedCall) in m_delayedCalls)
            {
                if (IsConditionFulfilled(delayedCall.condition)
                    && delayedCall.pass == pass
                    && delayedCall.Update(deltaTime))
                {
                    toDestroy.Add(key);
                }
            }

            foreach (var key in toDestroy)
            {
                UnregisterDelayedCall(key);
            }
        }

        #endregion

        #region Accessors

        internal bool DoesDelayedCallExist(ulong key)
        {
            return m_delayedCalls.ContainsKey(key) || m_delayedCallsToRegister.ContainsKey(key);
        }
        internal bool GetDelayedCallTimeLeft(ulong key, out float timeLeft)
        {
            if (m_delayedCalls.TryGetValue(key, out var delayedCall)
                && delayedCall is TimedDelayedCall timedDelayedCall)
            {
                timeLeft = timedDelayedCall.GetRemainingTime();
                return true;
            }
            timeLeft = -1f;
            return false;
        }
        internal bool GetDelayedCallFramesLeft(ulong key, out int framesLeft)
        {
            if (m_delayedCalls.TryGetValue(key, out var delayedCall)
                && delayedCall is FrameDelayedCall frameDelayedCall)
            {
                framesLeft = frameDelayedCall.GetRemainingFrames();
                return true;
            }
            framesLeft = -1;
            return false;
        }

        #endregion

        #region Utility

        private void ClearDelayedCalls()
        {
            m_delayedCalls.Clear();
            m_delayedCallsToRegister.Clear();
        }

        #endregion

        #endregion


        #region Registration Keys

        private static ulong _registrationCount = 0;
        private static ulong GetUniqueRegistrationKey()
        {
            _registrationCount++;
            return _registrationCount;
        }

        #endregion

        #region Utility

        public static void PauseTime(bool pause)
        {
            UnityEngine.Time.timeScale = pause ? 0f : 1f;
        }

        internal void Clear()
        {
            // PASSES
            m_currentFramePasses.Clear();

            // CHANNELS
            ClearChannels();

            // DELAYED CALLS
            ClearDelayedCalls();

            // TIMELINES
            ClearUpdateTimelineInstances();

            // DEFAULT EVENTS
            AfterEarlyUpdated = null;
            AfterLateUpdated = null;
            AfterPhysicsFixedUpdated = null;
            AfterUpdated = null;
            BeforeFixedUpdated = null;
            ClassicUpdated = null;

            // REGISTRATION KEYS
            _registrationCount = 0;
        }

        #endregion
    }
}
