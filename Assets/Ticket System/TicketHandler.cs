using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Dhs5.Utility.TicketSystem
{
    public class TicketHandler
    {
        #region Members

        private ulong m_firstAvailableUID = 1;

        #endregion

        #region Methods

        public virtual void Reset()
        {
            m_firstAvailableUID = 1;
        }

        public Ticket GetTicket()
        {
            var ticket = new Ticket(m_firstAvailableUID);
            m_firstAvailableUID++;

            return ticket;
        }

        #endregion
    }

    public class TicketHandler<T> : TicketHandler, IEnumerable<KeyValuePair<Ticket, T>>
    {
        #region Members

        private Dictionary<ulong, T> m_dictionary = new();

        #endregion

        #region Methods

        public override void Reset()
        {
            base.Reset();

            m_dictionary.Clear();
        }

        public Ticket Register(T value)
        {
            var ticket = GetTicket();
            m_dictionary[ticket.GetUID()] = value;

            return ticket;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Unregister(Ticket ticket)
        {
            return m_dictionary.Remove(ticket.GetUID());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Unregister(Ticket ticket, out T value)
        {
            return m_dictionary.Remove(ticket.GetUID(), out value);
        }

        public bool SetValue(Ticket ticket, T value)
        {
            if (m_dictionary.ContainsKey(ticket.GetUID()))
            {
                m_dictionary[ticket.GetUID()] = value;
                return true;
            }
            return false;
        }

        #endregion

        #region Accessors

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasContent()
        {
            return m_dictionary.IsValid();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Ticket ticket)
        {
            return m_dictionary.ContainsKey(ticket.GetUID());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue(Ticket ticket)
        {
            return m_dictionary[ticket.GetUID()];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(Ticket ticket, out T value)
        {
            return m_dictionary.TryGetValue(ticket.GetUID(), out value);
        }

        public T this[Ticket ticket] => GetValue(ticket);

        #endregion

        #region IEnumerable<KeyValuePair<Ticket, T>>

        public IEnumerator<KeyValuePair<Ticket, T>> GetEnumerator()
        {
            foreach (var kvp in m_dictionary) yield return new KeyValuePair<Ticket, T>(new Ticket(kvp.Key), kvp.Value);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
