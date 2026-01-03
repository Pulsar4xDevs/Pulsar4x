using System;
using System.Collections.Generic;

namespace Pulsar4X.Engine
{
    // Represents an item on the queue.
    public class TimeQueueItem<T>
    {
        public DateTime Time { get; private set; }
        public T Item { get; private set; }

        public TimeQueueItem(DateTime time, T item)
        {
            Time = time;
            Item = item;
        }
    }

    // TimeQueueItem comparer. Uses DateTime.Compare.
    public class TimeQueueItemComparer<T> : IComparer<TimeQueueItem<T>>
    {
        public int Compare(TimeQueueItem<T> x, TimeQueueItem<T> y) =>
            DateTime.Compare(x.Time, y.Time);
    }

    public class TimeQueue<T> : IEnumerable<TimeQueueItem<T>>
    {
        private static TimeQueueItemComparer<T> comparer = new TimeQueueItemComparer<T>();
        private List<TimeQueueItem<T>> queue = new();

        public TimeQueue()
        {
            queue = new();
        }

        public TimeQueue(IEnumerable<TimeQueueItem<T>> collection)
        {
            queue = new List<TimeQueueItem<T>>(collection);
            queue.Sort(comparer);
        }

        public TimeQueue(IEnumerable<(DateTime, T)> collection)
        {
            queue = new List<TimeQueueItem<T>>();
            foreach (var (time, item) in collection)
                queue.Add(new TimeQueueItem<T>(time, item));
            queue.Sort(comparer);
        }

        // Add and sort the queue.
        public void Add(DateTime time, T item)
        {
            var i = new TimeQueueItem<T>(time, item);

            if (queue.Count == 0)
            {
                queue.Add(i);
            }
            else
            {
                var idx = queue.BinarySearch(i, comparer);
                queue.Insert(idx < 0 ? ~idx : idx, i);
            }
        }

        // List RemoveAt. No need to sort. Exception if invalid idx.
        public void RemoveAt(int idx)
        {
            queue.RemoveAt(idx);
        }

        // Splits off all items before the specified time from the queue.
        public TimeQueueItem<T>[] Split(DateTime time)
        {
            List<TimeQueueItem<T>> ret = new();

            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].Time <= time)
                    ret.Add(queue[i]);
                else
                    break;
            }

            for (int i = 0; i < ret.Count; i++)
                RemoveAt(0);

            return ret.ToArray();
        }

        // Implement IEnumerator.
        public IEnumerator<TimeQueueItem<T>> GetEnumerator()
        {
            return queue.GetEnumerator();
        }

        // Implement IEnumerator.
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
