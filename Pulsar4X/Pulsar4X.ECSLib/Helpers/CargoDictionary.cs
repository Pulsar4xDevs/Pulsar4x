using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Public Read, Interal Write Observible Dictionary
    /// a custom dictionary which is :
    /// Publicly Observible, with marshaled events.
    /// Public ReadOnly.
    /// Internal Writeable.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class PrIwObsDict<TKey, TValue> : INotifyCollectionChanged, ICloneable
    {
        [JsonProperty]
        private Dictionary<TKey, TValue> _dict;

        private readonly SynchronizationContext _context;

        public event NotifyCollectionChangedEventHandler CollectionChanged;


        public PrIwObsDict()
        {
            _context = AsyncOperationManager.SynchronizationContext;
            _dict = new Dictionary<TKey, TValue>();
        }

        public PrIwObsDict(Dictionary<TKey, TValue> backingDict)
        {
            _context = AsyncOperationManager.SynchronizationContext;
            _dict = backingDict;
        }

        public PrIwObsDict(PrIwObsDict<TKey, TValue> backingDict)
        {
            _context = AsyncOperationManager.SynchronizationContext;
            _dict = backingDict._dict;
        }

        #region Internal Modifications
        internal void Add(TKey key, TValue value)
        {
            _dict.Add(key, value);
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value));
            if (CollectionChanged != null && _context != null)
                _context.Post(s => CollectionChanged(this, args), null);
        }

        internal void Remove(TKey key)
        {
            _dict.Remove(key);
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, key);
            if (CollectionChanged != null && _context != null)
                _context.Post(s => CollectionChanged(this, args), null);
        }


        internal void Clear()
        {
            _dict.Clear();
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            if (CollectionChanged != null && _context != null)
                _context.Post(s => CollectionChanged(this, args), null);
        }

        internal Dictionary<TKey, TValue> ToDictionary()
        {
            return _dict;
        }


        #endregion

        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return _dict.Keys; }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return _dict.Values; }
        }

        public TValue this[TKey key]
        {
            get { return _dict[key]; }
            internal set { _dict[key] = value; }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dict.Contains(item);
        }

        public int Count
        {
            get { return _dict.Count; }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public object Clone()
        {
            return new PrIwObsDict<TKey, TValue>(_dict);
        }

    }
}
