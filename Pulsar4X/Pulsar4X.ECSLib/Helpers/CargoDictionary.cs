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
    
    public class ReadOnlyObsDict<TKey, TValue> : IDictionary<TKey, TValue> , INotifyCollectionChanged , ICloneable
    {
        [JsonProperty]
        IDictionary<TKey, TValue> _dict;
        private readonly SynchronizationContext _context;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ReadOnlyObsDict()
        {
            _context = AsyncOperationManager.SynchronizationContext;
            _dict = new Dictionary<TKey, TValue>();
        }

        public ReadOnlyObsDict(IDictionary<TKey, TValue> backingDict)
        {
            _context = AsyncOperationManager.SynchronizationContext;
            _dict = backingDict;
        }


        /// <summary>
        /// DONOTUSE use AddNotify instead (internal)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            throw new InvalidOperationException();
        }

        internal void AddNotify(TKey key, TValue value)
        {
            if (_dict is ReadOnlyObsDict<TKey, TValue>)
            {
                ReadOnlyObsDict<TKey, TValue> dict = (ReadOnlyObsDict<TKey, TValue>)_dict;
                dict.AddNotify(key, value);
            }
            else
                _dict.Add(key, value);
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey,TValue>(key, value));          
            if (CollectionChanged != null && _context != null)
                _context.Post(s => CollectionChanged(this, args), null);
        }

        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return _dict.Keys; }
        }

        /// <summary>
        /// Do Not Use (use RemoveNotify if internal)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(TKey key)
        {
            throw new InvalidOperationException();
        }
        internal void RemoveNotify(TKey key)
        {
            _dict.Remove(key);
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, key);
            if (CollectionChanged != null && _context != null)
                _context.Post(s => CollectionChanged(this, args), null);
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
            set { _dict[key] = value; }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new InvalidOperationException();
        }

        public void Clear()
        {
            throw new InvalidOperationException();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dict.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _dict.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _dict.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new InvalidOperationException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        System.Collections.IEnumerator
               System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)_dict).GetEnumerator();
        }

        public object Clone()
        {    
            return new ReadOnlyObsDict<TKey, TValue>(_dict);
        }
    }

}
