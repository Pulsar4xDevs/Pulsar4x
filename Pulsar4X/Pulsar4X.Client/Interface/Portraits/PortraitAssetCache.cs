using System;
using System.Collections.Generic;

namespace Pulsar4X.Client;

/// <summary>
/// Remembers successful portrait asset loads. Misses are not stored, so a later
/// <see cref="Clear"/> (Reload, or a recreated renderer) can retry the file.
/// </summary>
public sealed class PortraitAssetCache<T>
{
    readonly Dictionary<string, T> _items = new(StringComparer.OrdinalIgnoreCase);

    public int Count
    {
        get { lock (this) return _items.Count; }
    }

    public bool TryGet(string key, out T value)
    {
        lock (this)
            return _items.TryGetValue(key, out value!);
    }

    public void Set(string key, T value)
    {
        if (string.IsNullOrEmpty(key))
            return;
        lock (this)
            _items[key] = value;
    }

    public bool Contains(string key)
    {
        lock (this)
            return _items.ContainsKey(key);
    }

    public void Clear()
    {
        lock (this)
            _items.Clear();
    }

    public TValue[] SnapshotValues<TValue>(Func<T, TValue> map)
    {
        lock (this)
        {
            var copy = new TValue[_items.Count];
            int i = 0;
            foreach (var item in _items.Values)
                copy[i++] = map(item);
            return copy;
        }
    }
}
