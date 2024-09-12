using System.Collections.Concurrent;

namespace Pulsar4X.DataStructures
{
    public class XThreadData<T>
    {
        ConcurrentHashSet<ConcurrentQueue<T>> _subscribers = new ConcurrentHashSet<ConcurrentQueue<T>>();

        public void Write(T data)
        {
            foreach (ConcurrentQueue<T> sub in _subscribers)
            {
                sub.Enqueue(data);
            }

        }

        public ConcurrentQueue<T> Subscribe()
        {
            ConcurrentQueue<T> newQueue = new ConcurrentQueue<T>();
            _subscribers.Add(newQueue);
            return newQueue;
        }

        public void Unsubscribe(ConcurrentQueue<T> queue)
        {
            _subscribers.Remove(queue);
        }
    }
}