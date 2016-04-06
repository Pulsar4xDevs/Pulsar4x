using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{

    public class RangeEnabledObservableCollection<T> : ObservableCollection<T>
    {
        public void AddRange(IEnumerable<T> items)
        {
            this.CheckReentrancy();
            foreach (var item in items)
                this.Items.Add(item);
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public class ItemPair<T1, T2> : INotifyPropertyChanged
    {
        private T1 _item1;
        public T1 Item1
        {
            get { return _item1; }
            set { _item1 = value; OnPropertyChanged(); }
        }

        private T2 _item2;
        public T2 Item2
        {
            get { return _item2; }
            set { _item2 = value; OnPropertyChanged(); }
        }

        public ItemPair(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));           
        }
    }

    public enum DisplayMode
    {
        Key,
        Value
    }

    public delegate void SelectionChangedEventHandler(int oldSelection, int newSelection);

    /// <summary>
    /// An attempt at creating a class to make binding dictionaries to UI more streamlined.
    /// To Use in the View:
    /// Bind the combobox to the dictionaryVM type:
    ///     myComboBox.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
    ///     myComboBox.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
    ///     
    /// Bind the combobox datacontext in the .cs:
    ///     myComboBox.DataContext = viewModel.myDictionaryVM;
    /// Or bind it in the xaml:
    ///     <ComboBox ID="myComboBox" DataContext="{Binding myDictionaryVM}" />
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class DictionaryVM<TKey,TValue> : IDictionary<TKey,TValue>, INotifyPropertyChanged
    {
        private Dictionary<TKey, TValue> _dictionary;
        private List<KeyValuePair<TKey, TValue>> _index = new List<KeyValuePair<TKey, TValue>>(); 
        private Dictionary<KeyValuePair<TKey,TValue>,int> _reverseIndex = new Dictionary<KeyValuePair<TKey, TValue>, int>(); 
        public ObservableCollection<string> DisplayList { get; set; }
        private int _selectedIndex;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                int old = _selectedIndex;
                _selectedIndex = value;
                if(SelectionChangedEvent != null) SelectionChangedEvent(old, _selectedIndex);
                OnPropertyChanged();
            }
        }
        public DisplayMode DisplayMode { get; set; }

        public event SelectionChangedEventHandler SelectionChangedEvent;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

        public TKey SelectedKey { get { return GetKey(); } }
        public TValue SelectedValue { get { return GetValue(); } }
        /// <summary>
        /// 
        /// </summary>
        /// <returns>The Selected Value</returns>
        public TValue GetValue()
        {
            if (SelectedIndex >= 0 && SelectedIndex < _index.Count)
            {
                return _index[SelectedIndex].Value;
            }
            return default(TValue);
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
            if (SelectedIndex >= 0 && SelectedIndex < _index.Count)
            {
                return _index[SelectedIndex].Key;
            }
            return default(TKey);
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
            if (SelectedIndex >= 0 && SelectedIndex < _index.Count)
            {
                return _index[SelectedIndex];
            }
            return new KeyValuePair<TKey, TValue>(default(TKey), default(TValue));
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
            _index.Add(item);
            _reverseIndex.Add(item, i);
            if (DisplayMode == DisplayMode.Key)
            {
                DisplayList.Add(_index[i].Key.ToString());
            }
            else
            {
                DisplayList.Add(_index[i].Value.ToString());
            }
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
                _index.RemoveAt(i);
                _reverseIndex.Remove(item);
                DisplayList.RemoveAt(i);
                //_reverseIndex = new Dictionary<KeyValuePair<TKey, TValue>, int>();
                int i2 = 0;
                foreach (var thing in _dictionary)
                {
                    
                    _reverseIndex[thing] = i2;
                    i2++;
                }
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
