using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public class UIGridNavBox : UINavBox
    {
        #region STRUCT Grid

        [Serializable]
        public struct Grid : IEnumerable<Column>
        {
            #region Members

            [SerializeField] private List<Column> m_columns;

            #endregion

            #region Properties

            public Selectable this[Vector2Int coord]
            {
                get
                {
                    if (m_columns.IsIndexValid(coord.x, out var column)
                        && column.IsInitialized
                        && column.IsIndexValid(coord.y, out var selectable))
                    {
                        return selectable;
                    }
                    return null;
                }
                set
                {
                    Column column;
                    while (!m_columns.IsIndexValid(coord.x, out column))
                    {
                        m_columns.Add(default);
                    }

                    if (!column.IsInitialized) column.Initialize();

                    column[coord.y] = value;
                    m_columns[coord.x] = column;
                }
            }

            #endregion

            #region Default Accessors

            public int GetColumnCount() => m_columns.Count;
            public int GetLineCount(int columnIndex)
            {
                if (m_columns.IsIndexValid(columnIndex, out var column)
                    && column.IsInitialized)
                {
                    return column.Count;
                }
                return 0;
            }
            public int GetMaxLineCount()
            {
                var max = 0;
                for (int i = 0; i < m_columns.Count; i++)
                {
                    var column = m_columns[i];
                    if (column.IsInitialized && column.Count > max)
                    {
                        max = column.Count;
                    }
                }
                return max;
            }
            public int GetMinLineCount()
            {
                if (m_columns.Count == 0) return 0;

                bool hasMin = false;
                int min = 0;
                for (int i = 0; i < m_columns.Count; i++)
                {
                    var column = m_columns[i];
                    if (column.IsInitialized && (!hasMin || column.Count < min))
                    {
                        min = column.Count;
                        hasMin = true;
                    }
                }
                return min;
            }

            public bool Contains(Selectable selectable)
            {
                foreach (var column in m_columns)
                {
                    if (column.IsInitialized && column.Contains(selectable))
                        return true;
                }
                return false;
            }

            public IEnumerator<Column> GetEnumerator()
            {
                return m_columns.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            #region Main Accessors

            public bool TryGetSelectableCoord(Selectable selectable, out Vector2Int coord)
            {
                for (int x = 0; x < GetColumnCount(); x++)
                {
                    var column = m_columns[x];
                    if (column.IsInitialized)
                    {
                        for (int y = 0; y < column.Count; y++)
                        {
                            if (column[y] == selectable)
                            {
                                coord = new Vector2Int(x, y);
                                return true;
                            }
                        }
                    }
                }
                coord = default;
                return false;
            }

            public Selectable GetFirstAvailableSelectable(MoveDirection moveDirection, int secondaryDirection)
            {
                switch (moveDirection)
                {
                    case MoveDirection.Up when secondaryDirection > 0:
                        {
                            for (int y = 0; y < GetMaxLineCount(); y++)
                            {
                                for (int x = 0; x < GetColumnCount(); x++)
                                {
                                    var column = m_columns[x];
                                    if (column.IsInitialized 
                                        && column.IsIndexValid(y, out var selectable)
                                        && selectable != null
                                        && selectable.IsActive())
                                    {
                                        return selectable;
                                    }
                                }
                            }

                            return null;
                        }
                    case MoveDirection.Up when secondaryDirection < 0:
                        {
                            for (int y = 0; y < GetMaxLineCount(); y++)
                            {
                                for (int x = GetColumnCount() - 1; x >= 0; x--)
                                {
                                    var column = m_columns[x];
                                    if (column.IsInitialized 
                                        && column.IsIndexValid(y, out var selectable)
                                        && selectable != null
                                        && selectable.IsActive())
                                    {
                                        return selectable;
                                    }
                                }
                            }

                            return null;
                        }
                    
                    case MoveDirection.Down when secondaryDirection > 0:
                        {
                            for (int y = GetMaxLineCount() - 1; y >= 0 ; y--)
                            {
                                for (int x = 0; x < GetColumnCount(); x++)
                                {
                                    var column = m_columns[x];
                                    if (column.IsInitialized 
                                        && column.IsIndexValid(y, out var selectable)
                                        && selectable != null
                                        && selectable.IsActive())
                                    {
                                        return selectable;
                                    }
                                }
                            }

                            return null;
                        }
                    case MoveDirection.Down when secondaryDirection < 0:
                        {
                            for (int y = GetMaxLineCount() - 1; y >= 0 ; y--)
                            {
                                for (int x = GetColumnCount() - 1; x >= 0; x--)
                                {
                                    var column = m_columns[x];
                                    if (column.IsInitialized 
                                        && column.IsIndexValid(y, out var selectable)
                                        && selectable != null
                                        && selectable.IsActive())
                                    {
                                        return selectable;
                                    }
                                }
                            }

                            return null;
                        }
                    
                    case MoveDirection.Right when secondaryDirection > 0:
                        {
                            for (int x = 0; x < GetColumnCount(); x++)
                            {
                                var column = m_columns[x];
                                if (column.IsInitialized)
                                {
                                    for (int y = 0; y < column.Count; y++)
                                    {
                                        var selectable = column[y];
                                        if (selectable != null
                                            && selectable.IsActive())
                                        {
                                            return selectable;
                                        }
                                    }
                                }
                            }

                            return null;
                        }
                    case MoveDirection.Right when secondaryDirection < 0:
                        {
                            for (int x = 0; x < GetColumnCount(); x++)
                            {
                                var column = m_columns[x];
                                if (column.IsInitialized)
                                {
                                    for (int y = column.Count - 1; y >= 0; y--)
                                    {
                                        var selectable = column[y];
                                        if (selectable != null
                                            && selectable.IsActive())
                                        {
                                            return selectable;
                                        }
                                    }
                                }
                            }

                            return null;
                        }
                    
                    case MoveDirection.Left when secondaryDirection > 0:
                        {
                            for (int x = GetColumnCount() - 1; x >= 0; x--)
                            {
                                var column = m_columns[x];
                                if (column.IsInitialized)
                                {
                                    for (int y = 0; y < column.Count; y++)
                                    {
                                        var selectable = column[y];
                                        if (selectable != null
                                            && selectable.IsActive())
                                        {
                                            return selectable;
                                        }
                                    }
                                }
                            }

                            return null;
                        }
                    case MoveDirection.Left when secondaryDirection < 0:
                        {
                            for (int x = GetColumnCount() - 1; x >= 0; x--)
                            {
                                var column = m_columns[x];
                                if (column.IsInitialized)
                                {
                                    for (int y = column.Count - 1; y >= 0; y--)
                                    {
                                        var selectable = column[y];
                                        if (selectable != null
                                            && selectable.IsActive())
                                        {
                                            return selectable;
                                        }
                                    }
                                }
                            }

                            return null;
                        }
                }

                return null;
            }

            public Selectable GetLeftSelectable(Vector2Int coord, bool availableOnly, bool wrapAround)
            {
                Selectable result = null;
                int iteration = 0;
                var count = GetColumnCount();

                do
                {
                    if (coord.x == 0)
                    {
                        if (wrapAround && count > 1)
                        {
                            coord.x = count - 1;
                            result = this[coord];
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        coord.x--;
                        result = this[coord];
                    }

                    if (availableOnly && result != null && !result.IsActive()) result = null;
                    iteration++;
                } while (iteration < count && result == null);

                return result;
            }
            public Selectable GetRightSelectable(Vector2Int coord, bool availableOnly, bool wrapAround)
            {
                Selectable result = null;
                int iteration = 0;
                var count = GetColumnCount();

                do
                {
                    if (coord.x == count - 1)
                    {
                        if (wrapAround && count > 1)
                        {
                            coord.x = 0;
                            result = this[coord];
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        coord.x++;
                        result = this[coord];
                    }

                    if (availableOnly && result != null && !result.IsActive()) result = null;
                    iteration++;
                } while (iteration < count && result == null);

                return result;
            }
            public Selectable GetDownSelectable(Vector2Int coord, bool availableOnly, bool wrapAround)
            {
                Selectable result = null;
                int iteration = 0;
                var count = GetLineCount(coord.x);

                if (count == 0) return null;

                do
                {
                    if (coord.y == 0)
                    {
                        if (wrapAround && count > 1)
                        {
                            coord.y = count - 1;
                            result = this[coord];
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        coord.y--;
                        result = this[coord];
                    }

                    if (availableOnly && result != null && !result.IsActive()) result = null;
                    iteration++;
                } while (iteration < count && result == null);

                return result;
            }
            public Selectable GetUpSelectable(Vector2Int coord, bool availableOnly, bool wrapAround)
            {
                Selectable result = null;
                int iteration = 0;
                var count = GetLineCount(coord.x);

                if (count == 0) return null;

                do
                {
                    if (coord.y == count - 1)
                    {
                        if (wrapAround && count > 1)
                        {
                            coord.y = 0;
                            result = this[coord];
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        coord.y++;
                        result = this[coord];
                    }

                    if (availableOnly && result != null && !result.IsActive()) result = null;
                    iteration++;
                } while (iteration < count && result == null);

                return result;
            }

            public IEnumerable<(Vector2Int, Selectable)> GetAllSelectables()
            {
                Vector2Int coord = Vector2Int.zero;
                foreach (var column in m_columns)
                {
                    if (column.IsInitialized)
                    {
                        coord.y = 0;
                        foreach (var selectable in column)
                        {
                            if (selectable != null)
                            {
                                yield return (coord, selectable);
                            }
                            coord.y++;
                        }
                    }
                    coord.x++;
                }
            }

            #endregion

            #region Setters

            public void EnsureValidity()
            {
                for (int x = 0; x < m_columns.Count; x++)
                {
                    var column = m_columns[x];
                    if (column.IsInitialized)
                    {
                        for (int y = column.Count - 1; y >= 0; y--)
                        {
                            if (column[y] == null)
                            {
                                column.RemoveAt(y);
                            }
                            else
                            {
                                break;
                            }
                        }
                        m_columns[x] = column;
                    }
                }
            }

            public void AddColumn(params Selectable[] selectables) => m_columns.Add(new Column(selectables));
            public void AddColumn(IEnumerable<Selectable> selectables) => m_columns.Add(new Column(selectables));

            public void Add(int columnIndex, Selectable selectable, bool createColumnIfNecessary = false)
            {
                if (columnIndex < 0) throw new IndexOutOfRangeException();

                if (m_columns.IsIndexValid(columnIndex, out var column))
                {
                    if (!column.IsInitialized) column.Initialize();

                    column.Add(selectable);
                }
                else if (createColumnIfNecessary)
                {
                    while (!m_columns.IsIndexValid(columnIndex, out column))
                    {
                        m_columns.Add(default);
                    }

                    if (!column.IsInitialized) column.Initialize();

                    column.Add(selectable);
                }
            }

            public void InsertColumn(int index, params Selectable[] selectables) => m_columns.Insert(index, new Column(selectables));
            public void InsertColumn(int index, IEnumerable<Selectable> selectables) => m_columns.Insert(index, new Column(selectables));

            public void Insert(Vector2Int coord, Selectable selectable) => Insert(coord.x, coord.y, selectable);
            public void Insert(int columnIndex, int lineIndex, Selectable selectable)
            {
                if (m_columns.IsIndexValid(columnIndex, out var column))
                {
                    if (!column.IsInitialized) column.Initialize();

                    column.Insert(lineIndex, selectable);
                }
            }

            public void RemoveColumnAt(int index) => m_columns.RemoveAt(index);
            public void RemoveLastColumn() => m_columns.RemoveAt(m_columns.Count - 1);

            public bool Remove(int columnIndex, Selectable selectable)
            {
                if (m_columns.IsIndexValid(columnIndex, out var column)
                    && column.IsInitialized
                    && column.Remove(selectable))
                {
                    m_columns[columnIndex] = column;
                    return true;
                }
                return false;
            }
            public bool RemoveAt(Vector2Int coord) => RemoveAt(coord.x, coord.y);
            public bool RemoveAt(int columnIndex, int lineIndex)
            {
                if (m_columns.IsIndexValid(columnIndex, out var column)
                    && column.IsInitialized
                    && column.Count > lineIndex)
                {
                    column.RemoveAt(lineIndex);
                    m_columns[columnIndex] = column;
                    return true;
                }
                return false;
            }

            public bool ReplaceAt(Vector2Int coord, Selectable selectable) => ReplaceAt(coord.x, coord.y, selectable);
            public bool ReplaceAt(int columnIndex, int lineIndex, Selectable selectable)
            {
                if (m_columns.IsIndexValid(columnIndex, out var column)
                        && column.IsInitialized
                        && column.IsIndexValid(lineIndex))
                {
                    column[lineIndex] = selectable;
                    m_columns[columnIndex] = column;
                    return true;
                }
                return false;
            }

            public bool ClearColumn(int columnIndex)
            {
                if (m_columns.IsIndexValid(columnIndex, out var column))
                {
                    column.Clear();
                    m_columns[columnIndex] = column;
                    return true;
                }
                return false;
            }
            public void Clear() => m_columns.Clear();

            #endregion
        }

        #endregion

        #region STRUCT Column

        [Serializable]
        public struct Column : IList<Selectable>
        {
            #region Constructor

            public Column(IEnumerable<Selectable> selectables)
            {
                m_selectables = new(selectables);
            }

            #endregion

            #region Members

            [SerializeField] private List<Selectable> m_selectables;

            #endregion

            #region Accessors

            public bool IsInitialized => m_selectables != null;
            public void Initialize() => m_selectables = new();

            #endregion

            #region IList<Selectable>

            public Selectable this[int index] 
            { 
                get => m_selectables[index]; 
                set
                {
                    if (index < 0) throw new IndexOutOfRangeException();

                    while (!m_selectables.IsIndexValid(index))
                    {
                        m_selectables.Add(null);
                    }
                    m_selectables[index] = value;
                }
            }

            public int Count => m_selectables.Count;

            public bool IsReadOnly => false;

            public void Add(Selectable item) => m_selectables.Add(item);

            public void Clear() => m_selectables.Clear();

            public bool Contains(Selectable item) => m_selectables.Contains(item);

            public void CopyTo(Selectable[] array, int arrayIndex) => m_selectables.CopyTo(array, arrayIndex);

            public IEnumerator<Selectable> GetEnumerator() => m_selectables.GetEnumerator();

            public int IndexOf(Selectable item) => m_selectables.IndexOf(item);

            public void Insert(int index, Selectable item)
            {
                if (index < 0) throw new IndexOutOfRangeException();

                while (!m_selectables.IsIndexValid(index))
                {
                    m_selectables.Add(null);
                }
                m_selectables.Insert(index, item);
            }

            public bool Remove(Selectable item) => m_selectables.Remove(item);

            public void RemoveAt(int index) => m_selectables.RemoveAt(index);

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        #endregion

        #region ENUM StartCorner

        public enum EStartCorner
        {
            BOTTOM_LEFT = 0,
            TOP_LEFT = 1,
            TOP_RIGHT = 2,
            BOTTOM_RIGHT = 3,
        }

        #endregion


        #region Members

        [Header("Grid")]
        [SerializeField] protected Grid m_grid;
        [SerializeField] protected EStartCorner m_startCorner;
        [SerializeField] protected bool m_wrapAround;

        #endregion

        #region Properties

        public virtual EStartCorner StartCorner
        {
            get => m_startCorner;
            set
            {
                if (m_startCorner != value)
                {
                    m_startCorner = value;
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


        #region Child Setup

        protected virtual void ValidateGrid()
        {
            m_grid.EnsureValidity();
        }
        public override void SetupChildren()
        {
            ValidateGrid();

            foreach (var (coord, selectable) in m_grid.GetAllSelectables())
            {
                SetupChild(selectable, GetChildNavigation(coord));
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
            switch (StartCorner)
            {
                case EStartCorner.TOP_LEFT:
                    switch (moveDirection)
                    {
                        case MoveDirection.Up: moveDirection = MoveDirection.Down; break;
                        case MoveDirection.Down: moveDirection = MoveDirection.Up; break;
                    }
                    return m_grid.GetFirstAvailableSelectable(moveDirection, 1);
                
                case EStartCorner.TOP_RIGHT:
                    switch (moveDirection)
                    {
                        case MoveDirection.Up: moveDirection = MoveDirection.Down; break;
                        case MoveDirection.Down: moveDirection = MoveDirection.Up; break;
                        case MoveDirection.Left: moveDirection = MoveDirection.Right; break;
                        case MoveDirection.Right: moveDirection = MoveDirection.Left; break;
                    }
                    return m_grid.GetFirstAvailableSelectable(moveDirection, 1);

                case EStartCorner.BOTTOM_RIGHT:
                    switch (moveDirection)
                    {
                        case MoveDirection.Left: moveDirection = MoveDirection.Right; break;
                        case MoveDirection.Right: moveDirection = MoveDirection.Left; break;
                    }
                    return m_grid.GetFirstAvailableSelectable(moveDirection, 1);

                default:
                    return m_grid.GetFirstAvailableSelectable(moveDirection, 1);
            }
        }

        #endregion

        #region Child Navigation

        protected virtual Navigation GetChildNavigation(Vector2Int coord)
        {
            Selectable left = GetLeftSelectable(coord, false),
                right = GetRightSelectable(coord, false),
                down = GetDownSelectable(coord, false),
                up = GetUpSelectable(coord, false);

            return new Navigation()
            {
                mode = Navigation.Mode.Explicit,
                selectOnRight = right,
                selectOnLeft = left,
                selectOnDown = down,
                selectOnUp = up,
            };
        }

        protected virtual Selectable GetLeftSelectable(Vector2Int coord, bool availableOnly)
        {
            if (coord.x == 0)
            {
                if (navigation.selectOnLeft != null && navigation.selectOnLeft.IsActive())
                    return navigation.selectOnLeft;
            }

            return m_grid.GetLeftSelectable(coord, availableOnly, WrapAround);
        }
        protected virtual Selectable GetRightSelectable(Vector2Int coord, bool availableOnly)
        {
            if (coord.x == m_grid.GetColumnCount() - 1)
            {
                if (navigation.selectOnRight != null && navigation.selectOnRight.IsActive())
                    return navigation.selectOnRight;
            }

            return m_grid.GetRightSelectable(coord, availableOnly, WrapAround);
        }
        protected virtual Selectable GetDownSelectable(Vector2Int coord, bool availableOnly)
        {
            if (coord.y == 0)
            {
                if (navigation.selectOnDown != null && navigation.selectOnDown.IsActive())
                    return navigation.selectOnDown;
            }

            return m_grid.GetDownSelectable(coord, availableOnly, WrapAround);
        }
        protected virtual Selectable GetUpSelectable(Vector2Int coord, bool availableOnly)
        {
            if (coord.y == m_grid.GetLineCount(coord.x) - 1)
            {
                if (navigation.selectOnUp != null && navigation.selectOnUp.IsActive())
                    return navigation.selectOnUp;
            }

            return m_grid.GetUpSelectable(coord, availableOnly, WrapAround);
        }

        public override Selectable FindSelectableOnChildFailed(Selectable child, AxisEventData axisEventData)
        {
            Selectable result = null;

            if (TryGetChildCoord(child, out var coord))
            {
                switch (axisEventData.moveDir)
                {
                    case MoveDirection.Left:
                        result = m_grid.GetLeftSelectable(coord, true, WrapAround);
                        break;
                    
                    case MoveDirection.Right:
                        result = m_grid.GetRightSelectable(coord, true, WrapAround);
                        break;
                    
                    case MoveDirection.Down:
                        result = m_grid.GetDownSelectable(coord, true, WrapAround);
                        break;
                    
                    case MoveDirection.Up:
                        result = m_grid.GetUpSelectable(coord, true, WrapAround);
                        break;
                }
            }

            if (result == null && Box != null)
            {
                return Box.FindSelectableOnChildFailed(this, axisEventData);
            }

            return result;
        }

        #endregion


        #region Utility

        public virtual bool TryGetChildCoord(Selectable child, out Vector2Int coord)
        {
            return m_grid.TryGetSelectableCoord(child, out coord);
        }

        #endregion
    }
}
