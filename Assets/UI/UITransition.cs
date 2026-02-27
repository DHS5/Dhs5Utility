using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public abstract class UITransition : MonoBehaviour, IComparable<UITransition>
    {
        // TODO :
        // custom inspector to draw states order list, with reset button

        #region Members

        [Header("Global Parameters")]
        [Tooltip("Smaller numbers will be treated first")]
        [SerializeField] private uint m_priority;
        // Base States Order :
        // - 0 = Normal
        // - 1 = Selected
        // - 2 = Highlighted
        // - 3 = Pressed
        // - 4 = Disabled
        [SerializeField] private List<int> m_statesOrder;

        protected readonly Dictionary<EUIState, IUIStateTransitioner> m_transitioners = new();

        #endregion

        #region Properties

        public virtual uint Priority
        {
            get => m_priority;
            protected set => m_priority = value;
        }

        #endregion

        #region Core Behaviour

        protected virtual void Awake()
        {
            InitTransitionersDico();
        }

        #endregion


        #region Abstract Methods

        protected abstract void InitTransitionersDico();
        protected abstract IEnumerable<Graphic> GetGraphics();

        #endregion

        #region Process

        public virtual void UpdateState(FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param)
        {
            var graphics = GetGraphics().Where(g => g != null).ToList();

            // Remove state not present anymore
            var removedStates = oldStates & ~newStates;
            foreach (var state in removedStates.GetStates())
            {
                if (TryGetStateTransitioner(state, out var transitioner))
                {
                    transitioner.OnRemove(graphics, instant, param);
                }
            }

            // Update state still present && Apply new states given states order
            foreach (var state in newStates.GetStates())
            {
                if (TryGetStateTransitioner(state, out var transitioner))
                {
                    // If already in oldStates, UPDATE
                    if (oldStates.Contains(state))
                    {
                        transitioner.OnUpdate(graphics, instant, param);
                    }
                    // If new state, APPLY
                    else
                    {
                        transitioner.OnApply(graphics, instant, param);
                    }
                }
            }
        }

        #endregion


        #region Utility

        protected virtual bool TryGetStateTransitioner(EUIState state, out IUIStateTransitioner transitioner)
        {
            return m_transitioners.TryGetValue(state, out transitioner);
        }

        #endregion

        #region IComparable

        public int CompareTo(UITransition other)
        {
            return m_priority.CompareTo(other.m_priority);
        }

        #endregion
    }

    public enum EUIState
    {
        NORMAL = 0,
        HIGHLIGHTED = 1,
        PRESSED = 2,
        SELECTED = 3,
        DISABLED = 4,
    }
    [Flags]
    public enum FUIState
    {
        NORMAL = 1 << 0,
        HIGHLIGHTED = 1 << 1,
        PRESSED = 1 << 2,
        SELECTED = 1 << 3,
        DISABLED = 1 << 4,
    }
    public static class UIStateExtensions
    {
        public static bool Contains(this FUIState f, EUIState state)
        {
            return (f & ((FUIState)(1 << (int)state))) != 0;
        }
        public static IEnumerable<EUIState> GetStates(this FUIState state)
        {
            for (int i = 0; i < 5; i++)
            {
                if ((state & ((FUIState)(1 << i))) != 0)
                {
                    yield return (EUIState)i;
                }
            }
        }
        public static IEnumerable<EUIState> GetStates(this FUIState state, List<int> customOrder)
        {
            foreach(var i in customOrder)
            {
                if ((state & ((FUIState)(1 << i))) != 0)
                {
                    yield return (EUIState)i;
                }
            }
        }
    }

    public interface IUIStateTransitioner
    {
        public void OnApply(IEnumerable<Graphic> graphics, bool instant, IUITransitionParam param);
        public void OnUpdate(IEnumerable<Graphic> graphics, bool instant, IUITransitionParam param);
        public void OnRemove(IEnumerable<Graphic> graphics, bool instant, IUITransitionParam param);
    }

    public interface IUITransitionParam { }
}
