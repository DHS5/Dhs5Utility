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
                m_selectables[index] = value;
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

        public virtual void Clear() => m_selectables.Clear();

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
            if (m_selectables.Remove(item))
            {
                SetupChildren();
                return true;
            }
            return false;
        }

        public virtual void RemoveAt(int index)
        {
            m_selectables.RemoveAt(index);
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
            ValidateChildrenList();

            // Setup
            for (int i = Mathf.Max(0, index - 1); i <= Mathf.Min(Count - 1, index + 1); i++)
            {
                SetupChild(m_selectables[i], GetChildNavigation(i));
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
                        if (m_selectables != null)
                        {
                            var selectable = m_selectables[i];
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
                        if (m_selectables != null)
                        {
                            var selectable = m_selectables[i];
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
                    throw new NotImplementedException();
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

        protected virtual Selectable GetPreviousSelectable(int index, bool availableOnly)
        {
            if (index == 0)
            {
                switch (Axis)
                {
                    case EAxis.HORIZONTAL:
                        if (navigation.selectOnLeft != null && navigation.selectOnLeft.IsActive())
                            return navigation.selectOnLeft;
                        break;

                    case EAxis.VERTICAL:
                        if (navigation.selectOnUp != null && navigation.selectOnUp.IsActive())
                            return navigation.selectOnUp;
                        break;
                }
            }

            Selectable previous = null;
            int iteration = 0;

            do
            {
                if (index == 0)
                {
                    if (WrapAround && Count > 1)
                    {
                        previous = m_selectables[^1];
                        index = Count - 1;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    previous = m_selectables[index - 1];
                    index--;
                }

                if (availableOnly && previous != null && !previous.IsActive()) previous = null;
                iteration++;
            } while (iteration < Count && previous == null);

            return previous;
        }
        protected virtual Selectable GetNextSelectable(int index, bool availableOnly)
        {
            if (index == Count - 1)
            {
                switch (Axis)
                {
                    case EAxis.HORIZONTAL:
                        if (navigation.selectOnRight != null && navigation.selectOnRight.IsActive())
                            return navigation.selectOnRight;
                        break;

                    case EAxis.VERTICAL:
                        if (navigation.selectOnDown != null && navigation.selectOnDown.IsActive())
                            return navigation.selectOnDown;
                        break;
                }
            }

            Selectable next = null;
            int iteration = 0;

            do
            {
                if (index == Count - 1)
                {
                    if (WrapAround && Count > 1)
                    {
                        next = m_selectables[0];
                        index = 0;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    next = m_selectables[index + 1];
                    index++;
                }

                if (availableOnly && next != null && !next.IsActive()) next = null;
                iteration++;
            } while (iteration < Count && next == null);

            return next;
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

            // Get next available child inside list
            if (TryGetChildIndex(child, out var index))
            {
                switch (axisEventData.moveDir)
                {
                    case MoveDirection.Down:
                    case MoveDirection.Right:
                        return GetNextSelectable(index, true);
                    
                    case MoveDirection.Up:
                    case MoveDirection.Left:
                        return GetPreviousSelectable(index, true);
                }
            }

            return null;
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
