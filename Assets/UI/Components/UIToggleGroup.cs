using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections;

namespace Dhs5.Utility.UI
{
    public class UIToggleGroup : UIBehaviour
    {
        #region Members

        [SerializeField] protected UIToggle m_defaultFirstToggle;
        [SerializeField] protected bool m_allowSwitchOff = false;

        protected List<UIToggle> m_toggles = new();

        protected bool m_waitingToTriggerStateChange;

        #endregion

        #region Properties

        public virtual UIToggle DefaultFirstToggle
        {
            get => m_defaultFirstToggle;
            set
            {
                if (m_defaultFirstToggle != value
                    && ValidateToggleIsInGroup(value, true))
                {
                    m_defaultFirstToggle = value;
                    if (m_defaultFirstToggle != null
                        && m_toggles.IndexOf(m_defaultFirstToggle) != 0)
                    {
                        m_toggles.Remove(m_defaultFirstToggle);
                        m_toggles.Insert(0, m_defaultFirstToggle);
                    }
                }
            }
        }

        /// <summary>
        /// Is it allowed that no toggle is switched on?
        /// </summary>
        /// <remarks>
        /// If this setting is enabled, pressing the toggle that is currently switched on will switch it off, so that no toggle is switched on. If this setting is disabled, pressing the toggle that is currently switched on will not change its state.
        /// Note that even if allowSwitchOff is false, the Toggle Group will not enforce its constraint right away if no toggles in the group are switched on when the scene is loaded or when the group is instantiated. It will only prevent the user from switching a toggle off.
        /// </remarks>
        public virtual bool AllowSwitchOff 
        { 
            get => m_allowSwitchOff;
            set 
            {
                if (m_allowSwitchOff != value)
                {
                    m_allowSwitchOff = value;
                    if (!m_allowSwitchOff) 
                        EnsureValidState();
                }
            } 
        }

        #endregion

        #region Events

        public event Action<UIToggleGroup> ContentChanged;
        public event Action<UIToggleGroup> StateChanged;

        protected virtual void TriggerContentChanged()
        {
            UISystemProfilerApi.AddMarker("ToggleGroup.content", this);
            ContentChanged?.Invoke(this);
        }
        protected virtual void TriggerStateChanged()
        {
            UISystemProfilerApi.AddMarker("ToggleGroup.state", this);
            StateChanged?.Invoke(this);
            Debug.Log("TRIGGER");
        }

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            if (didStart)
                EnsureValidState();

            base.OnEnable();
        }

        /// <summary>
        /// Because all the Toggles have registered themselves in the OnEnabled, Start should check to
        /// make sure at least one Toggle is active in groups that do not AllowSwitchOff
        /// </summary>
        protected override void Start()
        {
            if (m_toggles.IsValid())
            {
                TriggerContentChanged();
            }

            EnsureValidState();

            base.Start();
        }

        #endregion


        #region Behaviour

        /// <summary>
        /// Notify the group that the given toggle is enabled
        /// </summary>
        /// <param name="toggle">The toggle that got triggered on</param>
        /// <param name="triggerEvent">If other toggles should trigger ValueChanged</param>
        public virtual void NotifyToggleOn(UIToggle toggle, bool triggerEvent = true)
        {
            if (ValidateToggleIsInGroup(toggle, false))
            {
                var wasWaitingToTriggerStateChange = m_waitingToTriggerStateChange;
                m_waitingToTriggerStateChange = !wasWaitingToTriggerStateChange && triggerEvent;

                // Disable all toggles in the group
                for (var i = 0; i < m_toggles.Count; i++)
                {
                    if (m_toggles[i] == toggle)
                        continue;

                    if (triggerEvent)
                        m_toggles[i].IsOn = false;
                    else
                        m_toggles[i].SetIsOnWithoutNotify(false);
                }

                if (m_waitingToTriggerStateChange)
                {
#if UNITY_EDITOR
                    if (Application.isPlaying)
#endif
                    {
                        TriggerStateChanged();
                    }
                }
                m_waitingToTriggerStateChange = wasWaitingToTriggerStateChange;
            }
        }
        public virtual void NotifyToggleOff(UIToggle toggle, bool triggerEvent = true)
        {
            if (ValidateToggleIsInGroup(toggle, false))
            {
                if (!m_waitingToTriggerStateChange && triggerEvent)
                {
#if UNITY_EDITOR
                    if (Application.isPlaying)
#endif
                    {
                        TriggerStateChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Ensure that the toggle group still has a valid state. This is only relevant when a ToggleGroup is Started
        /// or a Toggle has been deleted from the group
        /// </summary>
        public virtual void EnsureValidState()
        {
            var wasWaitingToTriggerStateChange = m_waitingToTriggerStateChange;

            if (!AllowSwitchOff && !AnyTogglesOn() && m_toggles.Count != 0)
            {
                if (!wasWaitingToTriggerStateChange) m_waitingToTriggerStateChange = true;
                m_toggles[0].IsOn = true;
            }

            var activeToggles = GetActiveToggles();

            if (activeToggles.Count() > 1)
            {
                if (!wasWaitingToTriggerStateChange) m_waitingToTriggerStateChange = true;

                var firstActive = GetFirstActiveToggle();

                foreach (var toggle in activeToggles)
                {
                    if (toggle == firstActive)
                    {
                        continue;
                    }
                    toggle.IsOn = false;
                }
            }

            if (m_waitingToTriggerStateChange)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
#endif
                {
                    TriggerStateChanged();
                }
            }
            m_waitingToTriggerStateChange = wasWaitingToTriggerStateChange;
        }

        /// <summary>
        /// Switch all toggles off.
        /// </summary>
        /// <remarks>
        /// This method can be used to switch all toggles off, regardless of whether the allowSwitchOff property is enabled or not.
        /// </remarks>
        public void SetAllTogglesOff(bool triggerEvent = true)
        {
            if (!AnyTogglesOn()) return;

            var wasWaitingToTriggerStateChange = m_waitingToTriggerStateChange;
            m_waitingToTriggerStateChange = !wasWaitingToTriggerStateChange;

            var oldAllowSwitchOff = m_allowSwitchOff;
            m_allowSwitchOff = true;

            if (triggerEvent)
            {
                for (var i = 0; i < m_toggles.Count; i++)
                    m_toggles[i].IsOn = false;
            }
            else
            {
                for (var i = 0; i < m_toggles.Count; i++)
                    m_toggles[i].SetIsOnWithoutNotify(false);
            }

            m_allowSwitchOff = oldAllowSwitchOff;

            if (m_waitingToTriggerStateChange)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
#endif
                {
                    TriggerStateChanged();
                }
            }
            m_waitingToTriggerStateChange = wasWaitingToTriggerStateChange;
        }

#endregion

        #region Registration

        /// <summary>
        /// Register a toggle with the toggle group so it is watched for changes and notified if another toggle in the group changes
        /// </summary>
        /// <param name="toggle">The toggle to register with the group</param>
        public virtual void RegisterToggle(UIToggle toggle)
        {
            if (toggle != null && !m_toggles.Contains(toggle))
            {
                if (toggle == m_defaultFirstToggle) 
                    m_toggles.Insert(0, toggle);
                else 
                    m_toggles.Add(toggle);

                if (didStart)
                    TriggerContentChanged();
            }
        }

        /// <summary>
        /// Unregister a toggle from the group
        /// </summary>
        /// <param name="toggle">The toggle to remove</param>
        public virtual void UnregisterToggle(UIToggle toggle)
        {
            if (m_toggles.Remove(toggle))
            {
                if (didStart)
                    TriggerContentChanged();

                if (toggle.IsOn)
                    EnsureValidState();
            }
        }

        #endregion

        #region Accessors

        public virtual IEnumerable<UIToggle> GetAllToggles()
        {
            return m_toggles;
        }

        /// <summary>
        /// Are any of the toggles on?
        /// </summary>
        /// <returns>Are and of the toggles on?</returns>
        public virtual bool AnyTogglesOn()
        {
            return m_toggles.Find(x => x.IsOn) != null;
        }

        /// <summary>
        /// Returns the toggles in this group that are active.
        /// </summary>
        /// <returns>The active toggles in the group.</returns>
        /// <remarks>
        /// Toggles belonging to this group but are not active either because their GameObject is inactive or because the Toggle component is disabled, are not returned as part of the list.
        /// </remarks>
        public virtual IEnumerable<UIToggle> GetActiveToggles()
        {
            return m_toggles.Where(x => x.IsOn);
        }

        /// <summary>
        /// Returns the toggle that is the first in the list of active toggles.
        /// </summary>
        /// <returns>The first active toggle from m_Toggles</returns>
        /// <remarks>
        /// Get the active toggle for this group
        /// </remarks>
        public virtual UIToggle GetFirstActiveToggle()
        {
            var activeToggles = GetActiveToggles();
            return activeToggles.Count() > 0 ? activeToggles.First() : null;
        }

        #endregion

        #region Utility

        protected virtual bool ValidateToggleIsInGroup(UIToggle toggle, bool acceptNull)
        {
            if ((toggle == null && !acceptNull) || !m_toggles.Contains(toggle))
            {
                Debug.LogErrorFormat("Toggle {0} is not part of ToggleGroup {1}", new object[] { toggle, this });
                return false;
            }
            return true;
        }

        #endregion
    }
}