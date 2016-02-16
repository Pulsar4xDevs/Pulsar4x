﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pulsar4X.ViewModel
{
    public enum DisplayMode
    {
        Key,
        Value
    }
    /// <summary>
    /// An attempt at creating a class to make binding dictionaries to UI more streamlined.
    /// To Use in the View:
    ///     myComboBox.DataContext = viewModel.myDictionaryVM;
    ///     myComboBox.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string> m) => m.DisplayList);
    ///     myComboBox.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string> m) => m.SelectedIndex);
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class DictionaryVM<TKey,TValue> :IDictionary<TKey,TValue>
    {
        private Dictionary<TKey, TValue> _dictionary;
        private Dictionary<int, KeyValuePair<TKey, TValue>> _index = new Dictionary<int, KeyValuePair<TKey, TValue>>(); 
        private Dictionary<KeyValuePair<TKey,TValue>,int> _reverseIndex = new Dictionary<KeyValuePair<TKey, TValue>, int>(); 
        public ObservableCollection<string> DisplayList { get; set; }
        public int SelectedIndex { get; set; }
        public DisplayMode DisplayMode { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="displayMode">Should the DisplayList be made up of the Key or the Value</param>
        public DictionaryVM(DisplayMode displayMode = DisplayMode.Value)
        {
            DisplayMode = displayMode;
            DisplayList = new ObservableCollection<string>();
            _dictionary = new Dictionary<TKey, TValue>();
            SelectedIndex = -1;
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <returns>The Selected Value</returns>
        public TValue GetValue()
        {
            return _index[SelectedIndex].Value;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The value at the given index</returns>
        public TValue GetValue(int index)
        {
            return _index[index].Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>The Selected Key</returns>
        public TKey GetKey()
        {
            return _index[SelectedIndex].Key;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The Key at the given Index</returns>
        public TKey GetKey(int index)
        {
            return _index[index].Key;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>The Selected KeyValuePair</returns>
        public KeyValuePair<TKey, TValue> GetKeyValuePair()
        {
            return _index[SelectedIndex];
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns>The KeyValuePair at the given Index</returns>
        public KeyValuePair<TKey, TValue> GetKeyValuePair(int index)
        {
            return _index[index];
        }

        public int GetIndex(KeyValuePair<TKey,TValue> kvp)
        {
            return _reverseIndex[kvp];
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            int i = _index.Count;
            _dictionary.Add(item.Key, item.Value);
            _index.Add(i, item);
            _reverseIndex.Add(item, i);
            if (DisplayMode == DisplayMode.Key)
                DisplayList.Add(_index[i].Key.ToString());
            else
                DisplayList.Add(_index[i].Value.ToString());           
        }

        public void Clear()
        {
            _dictionary.Clear();
            _index.Clear();
            _reverseIndex.Clear();
            DisplayList.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (_reverseIndex.ContainsKey(item))
            {
                int i = _reverseIndex[item];
                _dictionary.Remove(item.Key);
                _index.Remove(i);
                _reverseIndex.Remove(item);
                DisplayList.RemoveAt(i);
                return true;
            }
            return false;
        }

        public int Count
        {
            get { return _index.Count; }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            Add(new KeyValuePair<TKey, TValue>(key,value));   
        }

        public bool Remove(TKey key)
        {
            if (_dictionary.ContainsKey(key))
            {
                KeyValuePair<TKey, TValue> item = new KeyValuePair<TKey, TValue>(key, _dictionary[key]);
                return Remove(item); 
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get { return _dictionary[key]; }
            set
            {
                KeyValuePair<TKey, TValue> olditem = new KeyValuePair<TKey, TValue>(key, _dictionary[key]);
                int i = _reverseIndex[olditem];
                _dictionary[key] = value;
                KeyValuePair<TKey, TValue> newitem = new KeyValuePair<TKey, TValue>(key, _dictionary[key]);
                _index[i] = newitem;
                _reverseIndex.Remove(olditem);
                _reverseIndex.Add(newitem,i);
                if (this.DisplayMode == DisplayMode.Value)
                    DisplayList[i] = value.ToString();
            }
        }

        public ICollection<TKey> Keys
        {
            get { return _dictionary.Keys; }
        }

        public ICollection<TValue> Values
        {
            get { return _dictionary.Values; }
        }
    }
}
