using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Dhs5.Utility.UI
{
    public class UIToggle : UISelectable, 
        IPointerClickHandler, ISubmitHandler
    {
        #region Members

        [Header("Toggle")]
        [SerializeField] protected List<Graphic> m_checkmarks;
        [SerializeField] protected UIToggleGroup m_group;
        [Tooltip("Whether the toggle is ON or OFF")]
        [SerializeField] protected bool m_isOn = true;
        [Tooltip("Duration of the fade of checkmark graphics")]
        [SerializeField] protected float m_checkmarkFadeDuration = 0.1f;

        #endregion

        #region Properties

        /// <summary>
        /// Group the toggle belongs to
        /// </summary>
        public UIToggleGroup Group
        {
            get { return m_group; }
            set
            {
                SetToggleGroup(value, true);
                PlayCheckmarkEffect(CheckmarkFadeDuration);
            }
        }
        /// <summary>
        /// Whether the toggle is ON or OFF
        /// </summary>
        public virtual bool IsOn
        {
            get => m_isOn;
            set => Set(value, triggerEvent:true);
        }
        public virtual float CheckmarkFadeDuration
        {
            get => m_checkmarkFadeDuration;
            set => m_checkmarkFadeDuration = value;
        }

        #endregion

        #region Event

        public event Action<bool> ValueChanged;

        protected virtual void TriggerValueChanged()
        {
            UISystemProfilerApi.AddMarker("Toggle.value", this);
            EventContext = this;
            ValueChanged?.Invoke(m_isOn);
        }

        #endregion

        #region Core Behaviour

        /// <summary>
        /// Assume the correct visual state
        /// </summary>
        protected override void Start()
        {
            base.Start();

            PlayCheckmarkEffect(0f);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SetToggleGroup(m_group, false);
            PlayCheckmarkEffect(0f);
        }

        protected override void OnDisable()
        {
            SetToggleGroup(null, false);

            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            if (m_group != null)
                m_group.EnsureValidState();

            base.OnDestroy();
        }

        #endregion


        #region Group

        protected virtual void SetToggleGroup(UIToggleGroup newGroup, bool setMemberValue)
        {
            // Sometimes IsActive returns false in OnDisable so don't check for it.
            // Rather remove the toggle too often than too little.
            if (m_group != null)
                m_group.UnregisterToggle(this);

            // At runtime the group variable should be set but not when calling this method from OnEnable or OnDisable.
            // That's why we use the setMemberValue parameter.
            if (setMemberValue)
                m_group = newGroup;

            // Note: Don't refer to m_Group here as it's not guaranteed to have been set.
            // Only register to the new group if this Toggle is active.
            if (newGroup != null && IsActive())
                newGroup.RegisterToggle(this);

            // If we are in a new group, and this toggle is on, notify group.
            if (newGroup != null && IsOn && IsActive())
                newGroup.NotifyToggleOn(this);
        }
        protected virtual void CheckNewValueWithGroup(bool triggerEvent)
        {
            if (m_group != null && m_group.isActiveAndEnabled && IsActive())
            {
                if (m_isOn)
                {
                    m_group.NotifyToggleOn(this, triggerEvent);
                }
                else if (!m_group.AnyTogglesOn() && !m_group.AllowSwitchOff)
                {
                    m_isOn = true;
                }
                else
                {
                    m_group.NotifyToggleOff(this, triggerEvent);
                }
            }
        }

        #endregion

        #region Setters

        protected virtual bool TryToggle(bool triggerEvent)
        {
            if (CanToggle())
            {
                return Set(!m_isOn, triggerEvent);
            }
            return false;
        }
        protected virtual bool CanToggle() => IsActive() && IsInteractable();

        /// <summary>
        /// Set IsOn without invoking ValueChanged callback
        /// </summary>
        /// <param name="value">New Value for IsOn</param>
        public virtual void SetIsOnWithoutNotify(bool value)
        {
            Set(value, false);
        }

        protected virtual bool Set(bool value, bool triggerEvent = true)
        {
            if (m_isOn == value)
                return false;

            m_isOn = value;

            // Do group logic
            CheckNewValueWithGroup(triggerEvent);

            // Always send event when toggle is clicked, even if value didn't change
            // due to already active toggle in a toggle group being clicked.
            // Controls like Dropdown rely on this.
            // It's up to the user to ignore a selection being set to the same value it already was, if desired.
            PlayCheckmarkEffect(CheckmarkFadeDuration);
            if (triggerEvent)
            {
                TriggerValueChanged();
            }

            return m_isOn == value;
        }

        #endregion

        #region Checkmark

        protected virtual void PlayCheckmarkEffect(float duration)
        {
            if (!m_checkmarks.IsValid())
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                foreach (var checkmark in m_checkmarks.Where(c => c != null))
                {
                    checkmark.canvasRenderer.SetAlpha(m_isOn ? 1f : 0f);
                }
            }
            else
#endif
            {
                foreach (var checkmark in m_checkmarks.Where(c => c != null))
                {
                    checkmark.CrossFadeAlpha(m_isOn ? 1f : 0f, duration, true);
                }
            }
        }        

        #endregion

        #region IEventHandlers

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left
                || (UseRightClick() && eventData.button == PointerEventData.InputButton.Right))
            {
                TryToggle(true);
            }
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            TryToggle(true);
            SimulatePress(0.1f);
        }

        #endregion
    }
}
