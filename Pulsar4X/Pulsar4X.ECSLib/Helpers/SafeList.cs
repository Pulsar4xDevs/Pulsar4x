using System;
using System.Collections;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class SafeList<T> : IEnumerable<T>
    {
        private readonly List<T> _innerList = new List<T>();
        private readonly object _lock = new object();

        public void Add(T item)
        {
            lock(_lock)
            {
                _innerList.Add(item);
            }
        }

        public bool Remove(T item)
        {
            lock(_lock)
            {
                return _innerList.Remove(item);
            }
        }

        public int Count
        {
            get
            {
                lock(_lock)
                {
                    return _innerList.Count;
                }
            }
        }

        public T this[int index]
        {
            get
            {
                lock(_lock)
                {
                    return _innerList[index];
                }
            }
            set
            {
                lock(_lock)
                {
                    _innerList[index] = value;
                }
            }
        }

        public void Clear()
        {
            lock(_lock)
            {
                _innerList.Clear();
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock(_lock)
            {
                return new List<T>(_innerList).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}