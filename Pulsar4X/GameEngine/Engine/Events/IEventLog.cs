using Pulsar4X.DataStructures;

namespace Pulsar4X.Events;

public interface IEventLog
{
    public void Subscribe();
    public void Unsubscribe();
    public SafeList<Event> GetEvents();

    public void ToggleHaltsOn(EventType eventType);
    public void SetHaltsOn(EventType eventType, bool halt);
    public bool HaltsOn(EventType eventType);
}
