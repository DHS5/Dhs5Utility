using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public class UIListNavBox : UINavBox
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
        [SerializeField] private List<Selectable> m_selectables;
        [SerializeField] private EAxis m_axis;
        [SerializeField] private bool m_wrapAround;

        #endregion

        #region Properties

        public int Count => m_selectables.Count;

        public EAxis Axis
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
        public bool WrapAround
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


        #region Child Setup

        public override void SetupChildren()
        {
            // Validate List
            for (int i = Count - 1; i >= 0; i--)
            {
                if (m_selectables[i] == null)
                {
                    m_selectables.RemoveAt(i);
                }
            }

            // Setup
            for (int i = 0; i < Count; i++)
            {
                SetupChild(m_selectables[i], GetChildNavigation(i));
            }
        }

        #endregion

        #region Child Selection

        protected override Selectable GetDefaultFirstChild()
        {
            if (m_selectables.IsIndexValid(0, out var selectable))
            {
                return selectable;
            }
            return null;
        }

        protected override Selectable GetFirstChildByDirection(MoveDirection moveDirection)
        {
            bool needSetup = false;

            switch (moveDirection)
            {
                case MoveDirection.Right:
                case MoveDirection.Down:
                    for (int i = 0; i < Count; i++)
                    {
                        if (m_selectables != null)
                        {
                            var selectable = m_selectables[i];
                            if (needSetup)
                            {
                                SetupChildren();
                            }
                            return selectable;
                        }
                        else
                        {
                            needSetup = true;
                        }
                    }
                    return null;

                case MoveDirection.Left:
                case MoveDirection.Up:
                    for (int i = Count - 1; i >= 0; i--)
                    {
                        if (m_selectables != null)
                        {
                            var selectable = m_selectables[i];
                            if (needSetup)
                            {
                                SetupChildren();
                            }
                            return selectable;
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

            switch (m_axis)
            {
                case EAxis.HORIZONTAL:
                    return new Navigation()
                    {
                        mode = Navigation.Mode.Explicit,
                        selectOnRight = next,
                        selectOnLeft = previous,
                        selectOnDown = null,
                        selectOnUp = null,
                    };

                case EAxis.VERTICAL:
                    return new Navigation()
                    {
                        mode = Navigation.Mode.Explicit,
                        selectOnRight = null,
                        selectOnLeft = null,
                        selectOnDown = next,
                        selectOnUp = previous,
                    };

                default:
                    throw new NotImplementedException();
            }
        }

        protected virtual Selectable GetPreviousSelectable(int index, bool availableOnly)
        {
            Selectable previous = null;

            do
            {
                if (index == 0)
                {
                    if (m_wrapAround && Count > 1)
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
            } while (availableOnly && (previous == null || !previous.IsActive()));

            return previous;
        }
        protected virtual Selectable GetNextSelectable(int index, bool availableOnly)
        {
            Selectable next = null;

            do
            {
                if (index == Count - 1)
                {
                    if (m_wrapAround && Count > 1)
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
            } while (availableOnly && (next == null || !next.IsActive()));

            return next;
        }

        public override Selectable FindSelectableOnChildFailed(Selectable child, AxisEventData axisEventData)
        {
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

        public bool TryGetChildIndex(Selectable child, out int index)
        {
            index = m_selectables.FindIndex(c => c == child);
            return index != -1;
        }

        #endregion
    }
}
