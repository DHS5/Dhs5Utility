using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.Updates
{
    public class UpdateChannelObject : ScriptableObject, IUpdateChannel
    {
        #region Members

        [Tooltip("Index inside the enum")]
        [SerializeField, ReadOnly] private int m_enumIndex;
        [Tooltip("Update pass on which this update channel will be updated")]
        [SerializeField] private EUpdatePass m_updatePass = EUpdatePass.CLASSIC_UPDATE;
        [Tooltip("Order in the selected update pass\n0 will be invoked first, bigger number last")]
        [SerializeField] private ushort m_order = 0;

        [Tooltip("Whether this update channel should be enabled by default or manually")]
        [SerializeField] private bool m_enabledByDefault = true;
        [Tooltip("Condition for this update channel to be updated, those conditions can be overriden in a custom updater")]
        [SerializeField] private EUpdateCondition m_updateCondition = EUpdateCondition.ALWAYS;

        [Tooltip("If checked, this update channel callback will be triggered only every x seconds\n" +
            "If not, the callback will be triggered on every pass update")]
        [SerializeField] private EnabledValue<float> m_customFrequency;
        [Tooltip("Timescale of this update channel, delta time will be multiplied by this value every update")]
        [SerializeField] private float m_timescale = 1.0f;
        [Tooltip("Whether this update channel should use realtime or depend on the game timescale")]
        [SerializeField] private bool m_realtime = false;

        #endregion

        #region Properties

        public int EnumIndex => m_enumIndex;
        public EUpdateChannel Channel => (EUpdateChannel)EnumIndex;
        public EUpdatePass Pass => m_updatePass;
        public ushort Order => m_order;
        public bool EnabledByDefault => m_enabledByDefault;
        public EUpdateCondition Condition => m_updateCondition;
        public float Frequency
        {
            get
            {
                if (m_customFrequency.IsEnabled(out var frequency))
                {
                    return Mathf.Max(0f, frequency);
                }
                return 0f;
            }
        }
        public float TimeScale => m_timescale;
        public bool Realtime => m_realtime;

        #endregion
    }
}
