using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public class UIListNavBox : UINavBox, IList<Selectable>
    {
        #region ENUM Axis

        public enum EAxis
        {
            VERTICAL = 0,
            HORIZONTAL = 1,
        }

        #endregion

        #region Members

        [Header("List")]
        [Tooltip("VERTICAL : From up to down\n" +
            "HORIZONTAL : From left to right")]
        [SerializeField] protected List<Selectable> m_selectables;
        [SerializeField] protected EAxis m_axis;
        [SerializeField] protected bool m_wrapAround;

        #endregion

        #region Properties

        public virtual int Count => m_selectables.Count;

        public virtual EAxis Axis
        {
            get => m_axis;
            set
            {
                if (m_axis != value)
                {
                    m_axis = value;
                    SetupChildren();
                }
            }
        }
        public virtual bool WrapAround
        {
            get => m_wrapAround;
            set
            {
                if (m_wrapAround != value)
                {
                    m_wrapAround = value;
                    SetupChildren();
                }
            }
        }

        #endregion


        #region IList<Selectable>

        public virtual Selectable this[int index] 
        { 
            get => m_selectables[index]; 
            set
            {
                var previous = m_selectables[index];
                m_selectables[index] = value;
                if (previous != value && !m_selectables.Contains(previous))
                    ReleaseChild(previous);

                SetupChildren(index);
            }
        }

        public virtual bool IsReadOnly => false;

        public virtual void Add(Selectable item)
        {
            if (item != null)
            {
                m_selectables.Add(item);
                SetupChildren(m_selectables.Count - 1);
            }
        }

        public virtual void AddRange(IEnumerable<Selectable> selectables)
        {
            m_selectables.AddRange(selectables);
            SetupChildren();
        }

        public virtual void Clear()
        {
            for (int i = 0; i < m_selectables.Count; i++)
                ReleaseChild(m_selectables[i]);

            m_selectables.Clear();
        }

        public virtual bool Contains(Selectable item) => m_selectables.Contains(item);

        public virtual void CopyTo(Selectable[] array, int arrayIndex) => m_selectables.CopyTo(array, arrayIndex);

        public virtual int IndexOf(Selectable item) => m_selectables.IndexOf(item);

        public virtual void Insert(int index, Selectable item)
        {
            if (item != null)
            {
                m_selectables.Insert(index, item);
                SetupChildren(index);
            }
        }

        public virtual bool Remove(Selectable item)
        {
            var index = m_selectables.IndexOf(item);
            if (index != -1)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public virtual void RemoveAt(int index)
        {
            var removed = m_selectables[index];
            m_selectables.RemoveAt(index);
            if (!m_selectables.Contains(removed))
                ReleaseChild(removed);

            SetupChildren(index);
        }
        
        public IEnumerator<Selectable> GetEnumerator()
        {
            return m_selectables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion


        #region Child Setup

        protected virtual void ValidateChildrenList()
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                if (m_selectables[i] == null || m_selectables[i] == this)
                {
                    m_selectables.RemoveAt(i);
                }
            }
        }
        public override void SetupChildren()
        {
            // Validate List
            ValidateChildrenList();

            // Setup
            for (int i = 0; i < Count; i++)
            {
                SetupChild(m_selectables[i], GetChildNavigation(i));
            }
        }
        protected virtual void SetupChildren(int index)
        {
            // Validate List
            var count = Count;
            ValidateChildrenList();

            // Indexes moved because invalid children were removed : full setup
            if (Count != count)
            {
                SetupChildren();
                return;
            }

            if (Count == 0) return;
            index = Mathf.Clamp(index, 0, Count - 1);

            // Setup the changed child and its neighbours
            for (int i = Mathf.Max(0, index - 1); i <= Mathf.Min(Count - 1, index + 1); i++)
            {
                SetupChild(m_selectables[i], GetChildNavigation(i));
            }

            // With wrap around, first and last children are neighbours too
            if (WrapAround)
            {
                if (index - 1 > 0) SetupChild(m_selectables[0], GetChildNavigation(0));
                if (index + 1 < Count - 1) SetupChild(m_selectables[^1], GetChildNavigation(Count - 1));
            }
        }

        #endregion

        #region Child Selection

        protected override Selectable GetDefaultFirstChild()
        {
            return GetFirstChildByDirection(MoveDirection.Right);
        }

        protected override Selectable GetFirstChildByDirection(MoveDirection moveDirection)
        {
            bool needSetup = false;

            switch (Axis)
            {
                case EAxis.VERTICAL when moveDirection is MoveDirection.Down or MoveDirection.Left or MoveDirection.Right:
                case EAxis.HORIZONTAL when moveDirection is MoveDirection.Right or MoveDirection.Up or MoveDirection.Down:
                    for (int i = 0; i < Count; i++)
                    {
                        var selectable = m_selectables[i];
                        if (selectable != null)
                        {
                            if (selectable.IsActive())
                            {
                                if (needSetup)
                                {
                                    SetupChildren();
                                }
                                return selectable;
                            }
                        }
                        else
                        {
                            needSetup = true;
                        }
                    }
                    return null;

                case EAxis.VERTICAL when moveDirection is MoveDirection.Up:
                case EAxis.HORIZONTAL when moveDirection is MoveDirection.Left:
                    for (int i = Count - 1; i >= 0; i--)
                    {
                        var selectable = m_selectables[i];
                        if (selectable != null)
                        {
                            if (selectable.IsActive())
                            {
                                if (needSetup)
                                {
                                    SetupChildren();
                                }
                                return selectable;
                            }
                        }
                        else
                        {
                            needSetup = true;
                        }
                    }
                    return null;

                default:
                    return GetDefaultFirstChild();
            }
        }

        #endregion

        #region Child Navigation

        protected virtual Navigation GetChildNavigation(int index)
        {
            Selectable next = GetNextSelectable(index, false), 
                previous = GetPreviousSelectable(index, false);

            switch (Axis)
            {
                case EAxis.HORIZONTAL:
                    return new Navigation()
                    {
                        mode = Navigation.Mode.Explicit,
                        selectOnRight = next,
                        selectOnLeft = previous,
                        selectOnDown = navigation.selectOnDown,
                        selectOnUp = navigation.selectOnUp,
                    };

                case EAxis.VERTICAL:
                    return new Navigation()
                    {
                        mode = Navigation.Mode.Explicit,
                        selectOnRight = navigation.selectOnRight,
                        selectOnLeft = navigation.selectOnLeft,
                        selectOnDown = next,
                        selectOnUp = previous,
                    };

                default:
                    throw new NotImplementedException();
            }
        }

        protected virtual Selectable GetPreviousSelectable(int index, bool availableOnly) => GetSelectable(index, -1, availableOnly);
        protected virtual Selectable GetNextSelectable(int index, bool availableOnly) => GetSelectable(index, 1, availableOnly);

        /// <summary>
        /// Returns the selectable after <paramref name="index"/> in <paramref name="step"/> direction (1 = next, -1 = previous).<br/>
        /// Reaching the end of the list (even past inactive children), the box's own neighbour has priority over wrap around
        /// </summary>
        protected virtual Selectable GetSelectable(int index, int step, bool availableOnly)
        {
            var lastIndex = step > 0 ? Count - 1 : 0;

            for (int iteration = 0; iteration < Count; iteration++)
            {
                if (index == lastIndex)
                {
                    var boxNeighbour = GetBoxNeighbour(step > 0);
                    if (boxNeighbour != null && boxNeighbour.IsActive())
                        return boxNeighbour;

                    if (!WrapAround || Count <= 1)
                        return null;

                    index = step > 0 ? 0 : Count - 1;
                }
                else
                {
                    index += step;
                }

                var selectable = m_selectables[index];
                if (selectable != null && (!availableOnly || selectable.IsActive()))
                    return selectable;
            }

            return null;
        }

        /// <summary>
        /// Returns the box's own neighbour after the end of the list (<paramref name="next"/>) or before its start
        /// </summary>
        protected Selectable GetBoxNeighbour(bool next)
        {
            return Axis switch
            {
                EAxis.HORIZONTAL => next ? navigation.selectOnRight : navigation.selectOnLeft,
                _ => next ? navigation.selectOnDown : navigation.selectOnUp,
            };
        }

        public override Selectable FindSelectableOnChildFailed(Selectable child, AxisEventData axisEventData)
        {
            // If move is not along list axis :
            // ask parent box for next available selectable
            switch (Axis)
            {
                case EAxis.VERTICAL when axisEventData.moveDir is MoveDirection.Left or MoveDirection.Right:
                    return Box != null ? Box.FindSelectableOnChildFailed(this, axisEventData) : null;

                case EAxis.HORIZONTAL when axisEventData.moveDir is MoveDirection.Up or MoveDirection.Down:
                    return Box != null ? Box.FindSelectableOnChildFailed(this, axisEventData) : null;
            }

            // Get next available child inside list, or the box's own neighbour past the end
            Selectable result = null;
            if (TryGetChildIndex(child, out var index))
            {
                switch (axisEventData.moveDir)
                {
                    case MoveDirection.Down:
                    case MoveDirection.Right:
                        result = GetNextSelectable(index, true);
                        break;

                    case MoveDirection.Up:
                    case MoveDirection.Left:
                        result = GetPreviousSelectable(index, true);
                        break;
                }
            }

            // Nothing in the list nor next to it : ask parent box
            if (result == null && Box != null)
                return Box.FindSelectableOnChildFailed(this, axisEventData);

            return result;
        }

        #endregion


        #region Utility

        public virtual bool TryGetChildIndex(Selectable child, out int index)
        {
            index = m_selectables.FindIndex(c => c == child);
            return index != -1;
        }

        #endregion
    }
}
