﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace SilverSim.Threading
{
    public class RwLockedList<T> : IList<T>
    {
        readonly List<T> m_List;
        readonly ReaderWriterLock m_RwLock = new ReaderWriterLock();

        public RwLockedList()
        {
            m_List = new List<T>();
        }

        public RwLockedList(IEnumerable<T> collection)
        {
            m_List = new List<T>(collection);
        }

        public RwLockedList(int capacity)
        {
            m_List = new List<T>(capacity);
        }

        public int Count
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_List.Count;
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
        }

        public void Clear()
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_List.Clear();
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public IList<T> GetAndClear()
        {
            IList<T> res;
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                res = new List<T>(m_List);
                m_List.Clear();
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
            return res;
        }

        public bool Contains(T value)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_List.Contains(value);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T value)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                return m_List.Remove(value);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public int IndexOf(T value)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_List.IndexOf(value);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public void RemoveAt(int index)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_List.RemoveAt(index);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public delegate bool RemoveMatchDelegate(T val);

        public T RemoveMatch(RemoveMatchDelegate del)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                foreach(T val in m_List)
                {
                    if(del(val))
                    {
                        m_List.Remove(val);
                        return val;
                    }
                }
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
            return default(T);
        }

        public T this[int index]
        {
            get
            {
                m_RwLock.AcquireReaderLock(-1);
                try
                {
                    return m_List[index];
                }
                finally
                {
                    m_RwLock.ReleaseReaderLock();
                }
            }
            set
            {
                m_RwLock.AcquireWriterLock(-1);
                try
                {
                    m_List[index] = value;
                }
                finally
                {
                    m_RwLock.ReleaseWriterLock();
                }
            }
        }

        public void Add(T value)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_List.Add(value);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                m_List.CopyTo(array, arrayIndex);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public void Insert(int index, T value)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                m_List.Insert(index, value);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return (new List<T>(m_List)).GetEnumerator();
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /* support for non-copy enumeration */
        public void ForEach(Action<T> action)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                foreach (T val in m_List)
                {
                    action(val);
                }
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        [Serializable]
        public class ValueAlreadyExistsException : Exception
        {
            public ValueAlreadyExistsException()
            {

            }
        }

        public void AddIfNotExists(T val)
        {
            m_RwLock.AcquireWriterLock(-1);
            try
            {
                if(m_List.Contains(val))
                {
                    throw new ValueAlreadyExistsException();
                }
                m_List.Add(val);
            }
            finally
            {
                m_RwLock.ReleaseWriterLock();
            }
        }

        public List<T> FindAll(Predicate<T> match)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_List.FindAll(match);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }

        public T Find(Predicate<T> match)
        {
            m_RwLock.AcquireReaderLock(-1);
            try
            {
                return m_List.Find(match);
            }
            finally
            {
                m_RwLock.ReleaseReaderLock();
            }
        }
    }
}