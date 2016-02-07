using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public struct LogEvent
    {
        [CanBeNull]
        public Entity Faction { get; }
        public bool IsSMOnly { get; }

        public DateTime Time { get; }
        [CanBeNull]
        public StarSystem System { get; }
        public string Message { get; }
        
        // ReSharper disable InconsistentNaming (Conforming to JSON parameterized consturctor)
        public LogEvent(Entity Faction, DateTime Time, StarSystem System, string Message, bool IsSMOnly = false)
        // ReSharper restore InconsistentNaming
        {
            this.Faction = Faction;
            this.IsSMOnly = IsSMOnly;
            this.Time = Time;
            this.System = System;
            this.Message = Message;
        }
    }
}
