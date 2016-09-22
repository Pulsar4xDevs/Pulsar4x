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
        private Dictionary<TKey, TValue> _dict = new Dictionary<TKey, TValue>();

        private readonly SynchronizationContext _context;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #region Constructors
        public PrIwObsDict()
        {
            _context = AsyncOperationManager.SynchronizationContext;
        }

        public PrIwObsDict(Dictionary<TKey, TValue> backingDict) : this()
        {
            _dict = backingDict;
        }

        public PrIwObsDict(PrIwObsDict<TKey, TValue> backingDict) : this()
        {
            _dict = backingDict._dict;
        }

        #endregion

        #region Internal Modifications
        internal void Add(TKey key, TValue value)
        {
            _dict.Add(key, value);
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value));
            if (CollectionChanged != null && _context != null)
                _context.Post(s => CollectionChanged(this, args), null);
        }

        //internal void Remove(TKey key)
        //{
        //    _dict.Remove(key);
        //    NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, key);
        //    if (CollectionChanged != null && _context != null)
        //        _context.Post(s => CollectionChanged(this, args), null);
        //}

        internal bool Remove(TKey key)
        {
            bool isSuccess = false;
            if(_dict.Remove(key))
            {
                isSuccess = true;
                NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, key);
                if (CollectionChanged != null && _context != null)
                    _context.Post(s => CollectionChanged(this, args), null);
            }
            return isSuccess;
        }

        internal void Clear()
        {
            _dict.Clear();
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            if (CollectionChanged != null && _context != null)
                _context.Post(s => CollectionChanged(this, args), null);
        }

        internal Dictionary<TKey, TValue> GetInternalDictionary()
        {
            return _dict;
        }
        #endregion

        #region public read methods
        
        public Dictionary<TKey, TValue> ToDictionary()
        {
            return new Dictionary<TKey, TValue>(_dict);
        }

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
        #endregion

        public object Clone()
        {
            return new PrIwObsDict<TKey, TValue>(_dict);
        }

    }


    public class PrIwObsList<T> : INotifyCollectionChanged, ICloneable
    {

        [JsonProperty]
        private List<T> _list = new List<T>();

        private readonly SynchronizationContext _context;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #region Constructors
        public PrIwObsList()
        {
            _context = AsyncOperationManager.SynchronizationContext;
        }

        public PrIwObsList(List<T> backingList) : this()
        {
            _list = backingList;
        }

        public PrIwObsList(PrIwObsList<T> cloning) : this()
        {
            _list = cloning._list;
        }
        #endregion

        #region Internal Modifications
        /// <summary>
        /// Adds an item to the end of the list
        /// </summary>
        /// <param name="item"></param>
        internal void Add(T item)
        {
            _list.Add(item);
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item);
            if (CollectionChanged != null && _context != null)
                _context.Post(s => CollectionChanged(this, args), null);
        }

        internal bool Remove(T item)
        {
            bool isSuccess = false;
            if (_list.Remove(item))
            {
                isSuccess = true;
                NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item);
                if (CollectionChanged != null && _context != null)
                    _context.Post(s => CollectionChanged(this, args), null);
            }
            return isSuccess;
        }

        internal void Clear()
        {
            _list.Clear();
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            if (CollectionChanged != null && _context != null)
                _context.Post(s => CollectionChanged(this, args), null);
        }

        internal List<T> GetInternalList()
        {
            return _list;
        }

        #endregion

        #region public read methods
        public bool Contains(T item)
        {
            return _list.Contains(item);
        }
        public List<T> ToList()
        {
            return _list.ToList();
        }
        public T this[int index]
        {
            get { return _list[index]; }
            internal set { _list[index] = value; }
        }

        public int Count
        {
            get { return _list.Count; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();           
        }
        #endregion



        #region IClonable Implemenation
        public object Clone()
        {
            return new PrIwObsList<T>(this);
        }
        #endregion
    }

    public static class EntityStoreHelpers
    {
        public static List<KeyValuePair<Entity, List<Entity>>> GetComponentsOfType<T>(PrIwObsDict<Entity, PrIwObsList<Entity>> fromCollection) where T : BaseDataBlob
        {
            var returnlist = new List<KeyValuePair<Entity, List<Entity>>>();
            foreach (KeyValuePair<Entity, PrIwObsList<Entity>> EntityListKVP in fromCollection)
            {
                if (EntityListKVP.Key.HasDataBlob<T>())
                {
                    KeyValuePair<Entity, List<Entity>> newkvp = new KeyValuePair<Entity, List<Entity>>(EntityListKVP.Key, EntityListKVP.Value.ToList());
                    returnlist.Add(newkvp);
                }
            }
            return returnlist;
        }
    }

}
