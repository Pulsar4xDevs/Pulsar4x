using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class LogEvent
    {
        public DateTime Time { get; }
        public string Message { get; }
        public bool IsSMOnly { get; }

        [CanBeNull]
        public Entity Faction { get; }
        public Guid SystemGuid { get; }
        [CanBeNull]
        public Entity Entity { get; }

        [JsonProperty]
        internal List<Guid> ConcernedPlayers { get; set; }
        
        // ReSharper disable InconsistentNaming (Conforming to JSON parameterized consturctor)
        public LogEvent(DateTime Time, string Message, Entity Faction, Guid SystemGuid, Entity Entity, bool IsSMOnly = false, List<Guid> ConcernedPlayers = null)
        // ReSharper restore InconsistentNaming
        {
            this.Time = Time;
            this.Message = Message;
            this.Faction = Faction;
            this.SystemGuid = SystemGuid;
            this.Entity = Entity;
            this.IsSMOnly = IsSMOnly;
            if (ConcernedPlayers == null)
            {
                ConcernedPlayers = new List<Guid>();
            }
            this.ConcernedPlayers = ConcernedPlayers;
        }
    }
}
