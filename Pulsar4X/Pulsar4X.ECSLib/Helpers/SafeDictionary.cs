using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class SafeDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _innerDictionary = new Dictionary<TKey, TValue>();
        private readonly object _lock = new object();
        public delegate void DictionaryChangedHandler(TKey key, TValue value);

        public event DictionaryChangedHandler ItemAdded;
        public event DictionaryChangedHandler ItemRemoved;
        public event DictionaryChangedHandler OnChange;

        public int Count
        {
            get
            {
                lock(_lock) return _innerDictionary.Count;
            }
        }

        internal Dictionary<TKey, TValue>.KeyCollection Keys
        {
            get
            {
                lock(_lock) return _innerDictionary.Keys;
            }
        }

        internal Dictionary<TKey, TValue>.ValueCollection Values
        {
            get
            {
                lock(_lock) return _innerDictionary.Values;
            }
        }

        public SafeDictionary() { }
        public SafeDictionary(IDictionary<TKey, TValue> dictionary)
        {
            foreach(var (key, value) in dictionary)
            {
                _innerDictionary.Add(key, value);
            }
        }

        public SafeDictionary(SafeDictionary<TKey, TValue> dictionary)
        {
            foreach(var (key, value) in dictionary.Get())
            {
                _innerDictionary.Add(key, value);
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                lock(_lock) return _innerDictionary[key];
            }
            internal set
            {
                lock(_lock)
                {
                    _innerDictionary[key] = value;
                    OnChange?.Invoke(key, value);
                }
            }
        }

        internal void Add(TKey key, TValue value)
        {
            lock(_lock)
            {
                _innerDictionary.Add(key, value);
                ItemAdded?.Invoke(key , value);
                OnChange?.Invoke(key, value);
            }
        }

        internal bool Remove(TKey key)
        {
            lock(_lock)
            {
                if(_innerDictionary.TryGetValue(key, out TValue value))
                {
                    _innerDictionary.Remove(key);
                    ItemRemoved?.Invoke(key, value);
                    OnChange?.Invoke(key, value);
                    return true;
                }
            }
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            lock(_lock) return _innerDictionary.ContainsKey(key);
        }

        public IDictionary<TKey, TValue> Get()
        {
            return new Dictionary<TKey, TValue>(_innerDictionary);
        }
    }
}