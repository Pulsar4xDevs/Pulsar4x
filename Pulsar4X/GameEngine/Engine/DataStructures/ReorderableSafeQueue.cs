using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.DataStructures;

public class ReorderableSafeQueue<T>
{
    private readonly object _lockObject = new object();
    private readonly LinkedList<T> _items = new LinkedList<T>();

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

    public bool TryPeek(out T? result)
    {
        lock(_lockObject)
        {
            if(_items.Count > 0)
            {
                result = _items.First.Value;
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
}