using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public abstract class UITransitioner : MonoBehaviour, IComparable<UITransitioner>
    {
        #region Members

        [Header("Global Parameters")]
        [Tooltip("Smaller numbers will be treated first")]
        [SerializeField] private uint m_priority;

        #endregion

        #region Properties

        public virtual uint Priority
        {
            get => m_priority;
            protected set => m_priority = value;
        }

        #endregion


        #region Process

        public abstract void UpdateState(FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param);

        #endregion


        #region IComparable

        public int CompareTo(UITransitioner other)
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

    public interface IUITransitionParam 
    {
        MonoBehaviour MonoBehaviour { get; }
    }
    public struct UIDefaultTransitionParam : IUITransitionParam
    {
        public UIDefaultTransitionParam(MonoBehaviour monoBehaviour)
        {
            MonoBehaviour = monoBehaviour;
        }

        public MonoBehaviour MonoBehaviour { get; }
    }
}
