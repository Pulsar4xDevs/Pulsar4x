using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Pulsar4X.DataStructures;

[JsonConverter(typeof(ReorderableSafeQueueConverter))]
public class ReorderableSafeQueue<T> : IEnumerable<T>
{
    private readonly object _lockObject = new object();
    private readonly LinkedList<T> _items = new LinkedList<T>();

    public ReorderableSafeQueue() { }

    public ReorderableSafeQueue(IList<T> list)
    {
        _items = new LinkedList<T>(list);
    }

    public ReorderableSafeQueue(ReorderableSafeQueue<T> list)
    {
        _items = new LinkedList<T>(list.ToList());
    }

    public int Count
    {
        get
        {
            lock(_lockObject)
            {
                return _items.Count;
            }
        }
    }

    public void Enqueue(T item)
    {
        lock (_lockObject)
        {
            _items.AddLast(item);
        }
    }

    public bool TryDequeue(out T result)
    {
        lock (_lockObject)
        {
            if (_items.Count > 0)
            {
                result = _items.First.Value;
                _items.RemoveFirst();
                return true;
            }

            result = default;
            return false;
        }
    }

    public bool TryPeek([NotNullWhen(true)] out T? result)
    {
        lock(_lockObject)
        {
            if(_items.First != null)
            {
                result = _items.First.Value!;
                return true;
            }
            result = default(T);
            return false;
        }
    }

    public bool TryMoveUp(T item)
    {
        lock (_lockObject)
        {
            var node = _items.Find(item);
            if (node == null || node.Previous == null)
                return false;

            var previous = node.Previous;
            _items.Remove(node);
            _items.AddBefore(previous, item);
            return true;
        }
    }

    public bool TryMoveDown(T item)
    {
        lock (_lockObject)
        {
            var node = _items.Find(item);
            if (node == null || node.Next == null)
                return false;

            var next = node.Next;
            _items.Remove(node);
            _items.AddAfter(next, item);
            return true;
        }
    }

    public bool TryRemoveItem(T item)
    {
        lock (_lockObject)
        {
            var node = _items.Find(item);
            if(node == null)
                return false;

            _items.Remove(node);
            return true;
        }
    }

    public IEnumerable<T> ToList()
    {
        lock (_lockObject)
        {
            return _items.ToList();
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        lock(_lockObject)
        {
            return new LinkedList<T>(_items).GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Needed for serialization
    /// </summary>
    [JsonIgnore]
    internal LinkedList<T> InnerList
    {
        get
        {
            lock(_lockObject)
            {
                return new LinkedList<T>(_items);
            }
        }
    }
}

public class ReorderableSafeQueueConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(ReorderableSafeQueue<>);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        // Get the generic type parameter T from ReorderableSafeQueue<T>
        Type itemType = objectType.GetGenericArguments()[0];

        // Create a type for List<T> to deserialize the JSON array into
        Type listType = typeof(List<>).MakeGenericType(itemType);

        // Deserialize the JSON into a List<T>
        var list = serializer.Deserialize(reader, listType);

        if (list == null)
            return Activator.CreateInstance(objectType);

        // Create a new instance of ReorderableSafeQueue<T> using the constructor that takes IList<T>
        var constructor = objectType.GetConstructor(new[] { typeof(IList<>).MakeGenericType(itemType) });

        if (constructor != null)
            return constructor.Invoke(new[] { list });

        // Fallback: create an empty queue and add items manually
        var result = Activator.CreateInstance(objectType);
        var enqueueMethod = objectType.GetMethod("Enqueue");

        // Get the enumerator and add each item
        var enumerator = list.GetType().GetMethod("GetEnumerator").Invoke(list, null);
        var moveNextMethod = enumerator.GetType().GetMethod("MoveNext");
        var currentProperty = enumerator.GetType().GetProperty("Current");

        while (moveNextMethod.Invoke(enumerator, null) is bool moveNext && moveNext)
        {
            var item = currentProperty.GetValue(enumerator);
            enqueueMethod.Invoke(result, new[] { item });
        }

        return result;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var objectType = value.GetType();
        var innerProperty = objectType.GetProperty("InnerList", BindingFlags.NonPublic | BindingFlags.Instance);
        var innerValue = innerProperty.GetValue(value);
        serializer.Serialize(writer, innerValue);
    }
}