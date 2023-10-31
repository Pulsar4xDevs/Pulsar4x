using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace Pulsar4X.DataStructures
{
    [JsonConverter(typeof(SafeListConverter))]
    public class SafeList<T> : IEnumerable<T>, IEquatable<SafeList<T>>
    {
        private readonly List<T> _innerList = new List<T>();
        private readonly object _lock = new object();

        public SafeList() { }

        public SafeList(IList<T> list)
        {
            _innerList = new List<T>(list);
        }

        public SafeList(SafeList<T> list)
        {
            _innerList = new List<T>(list);
        }

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

        public void RemoveAt(int index)
        {
            lock(_lock)
            {
                _innerList.RemoveAt(index);
            }
        }

        public void Insert(int index, T item)
        {
            lock(_lock)
            {
                _innerList.Insert(index, item);
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

        public bool Equals(SafeList<T>? other)
        {
            if(other is null) return false;
            if(ReferenceEquals(this, other)) return true;

            lock(_lock)
            {
                lock(other._lock)
                {
                    if(_innerList.Count != other._innerList.Count)
                    {
                        return false;
                    }

                    for(int i = 0; i < _innerList.Count; i++)
                    {
                        if (!EqualityComparer<T>.Default.Equals(_innerList[i], other._innerList[i]))
                            return false;
                    }

                    return true;
                }
            }
        }

        /// <summary>
        /// Needed for serialization
        /// </summary>
        [JsonIgnore]
        internal List<T> InnerList
        {
            get
            {
                lock(_lock)
                {
                    return new List<T>(_innerList);
                }
            }
        }
    }

    public class SafeListConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(SafeList<>);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            Type keyType = objectType.GetGenericArguments()[0];
            var constructedListType = typeof(List<>).MakeGenericType(keyType);

            var innerList = serializer.Deserialize(reader, constructedListType) as IList;
            var result = Activator.CreateInstance(objectType, innerList);
            return result;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var objectType = value.GetType();
            var innerListProperty = objectType.GetProperty("InnerList", BindingFlags.NonPublic | BindingFlags.Instance);
            var innerListValue = innerListProperty.GetValue(value);
            serializer.Serialize(writer, innerListValue);
        }
    }
}