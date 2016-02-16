using System;

namespace Pulsar4X.ECSLib
{
    public class LogEvent
    {
        public DateTime Time { get; }
        public string Message { get; }
        public bool IsSMOnly { get; }

        [CanBeNull]
        public Entity Faction { get; }
        [CanBeNull]
        public StarSystem System { get; }
        [CanBeNull]
        public Entity Entity { get; }
        
        // ReSharper disable InconsistentNaming (Conforming to JSON parameterized consturctor)
        public LogEvent(DateTime Time, string Message, Entity Faction, StarSystem System, Entity Entity, bool IsSMOnly = false)
        // ReSharper restore InconsistentNaming
        {
            this.Time = Time;
            this.Message = Message;
            this.Faction = Faction;
            this.System = System;
            this.Entity = Entity;
            this.IsSMOnly = IsSMOnly;
        }
    }
}
