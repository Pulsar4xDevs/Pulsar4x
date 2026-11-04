using System;
using System.Collections.Generic;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// One preview per ship, body, game day, and option set. Holds a handful and drops the oldest.
    /// </summary>
    public sealed class MovePreviewSession
    {
        public const int Capacity = 8;

        readonly Dictionary<Key, TripPreview> _cache = new();
        readonly Queue<Key> _oldest = new();

        public TripPreview Get(int shipId, int bodyId, int day, MoveOrderOptions? options, Func<MoveOrderOptions, TripPreview> load)
        {
            var key = new Key(shipId, bodyId, day, options ?? new MoveOrderOptions());
            if (_cache.TryGetValue(key, out TripPreview? hit) && hit != null)
                return hit;
            TripPreview made = load(key.Options);
            _cache[key] = made;
            _oldest.Enqueue(key);
            while (_oldest.Count > Capacity)
                _cache.Remove(_oldest.Dequeue());
            return made;
        }

        readonly record struct Key(int ShipId, int BodyId, int Day, MoveOrderOptions Options);
    }
}
