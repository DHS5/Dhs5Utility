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

            #region Accessors

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

            #region Setters

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


        #region Members

        [Header("Grid")]
        [SerializeField] protected Grid m_grid;

        #endregion


        #region Child Setup

        public override void SetupChildren()
        {
            
        }

        #endregion

        #region Child Selection

        protected override Selectable GetDefaultFirstChild()
        {
            return null;
        }

        protected override Selectable GetFirstChildByDirection(MoveDirection moveDirection)
        {
            return null;
        }

        #endregion

        #region Child Navigation

        public override Selectable FindSelectableOnChildFailed(Selectable child, AxisEventData axisEventData)
        {
            return null;
        }


        #endregion
    }
}
