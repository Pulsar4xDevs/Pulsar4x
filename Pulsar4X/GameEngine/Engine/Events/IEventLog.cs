using Pulsar4X.DataStructures;

namespace Pulsar4X.Events;

public interface IEventLog
{
    public void Subscribe();
    public void Unsubscribe();
    public SafeList<Event> GetEvents();
}