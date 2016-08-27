using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Pulsar4X.ECSLib
{
    public class Event : ISerializable
    {
        public DateTime Time { get; internal set; }
        
        public string Message { get; internal set; }
        
        [CanBeNull]
        public Entity Faction { get; internal set; }
        
        [CanBeNull]
        public Guid SystemGuid { get; internal set; }
        
        [CanBeNull]
        public Entity Entity { get; internal set; }

        public EventType EventType { get; internal set; }
        
        internal List<Guid> ConcernedPlayers { get; set; }

        public Event(SerializationInfo info, StreamingContext context)
        {
            Time = (DateTime)info.GetValue(nameof(Time), typeof(DateTime));
            Message = (string)info.GetValue(nameof(Message), typeof(string));
            Faction = (Entity)info.GetValue(nameof(Faction), typeof(Entity));
            SystemGuid = (Guid)info.GetValue(nameof(SystemGuid), typeof(Guid));
            Entity = (Entity)info.GetValue(nameof(Entity), typeof(Entity));

            if ((context.State & StreamingContextStates.Persistence) != 0)
            {
                ConcernedPlayers = (List<Guid>)info.GetValue(nameof(ConcernedPlayers), typeof(List<Guid>));
            }
        }

        public Event(DateTime time, string message, Entity faction= null, Entity entity = null, List<Guid> concernedPlayers = null) : this(time, message, Guid.Empty, faction, entity, concernedPlayers)
        { }

        public Event(DateTime time, string message, Guid systemGuid, Entity faction= null, Entity entity = null, List<Guid> concernedPlayers = null)
        {
            Time = time;
            Message = message;
            Faction = faction;
            SystemGuid = systemGuid;
            Entity = entity;
            if (concernedPlayers == null)
            {
                concernedPlayers = new List<Guid>();
            }
            ConcernedPlayers = concernedPlayers;
        }


        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Time), Time);
            info.AddValue(nameof(Message), Message);
            info.AddValue(nameof(Faction), Faction);
            info.AddValue(nameof(SystemGuid), SystemGuid);
            info.AddValue(nameof(Entity), Entity);

            if ((context.State & StreamingContextStates.Persistence) != 0)
            {
                info.AddValue(nameof(ConcernedPlayers), ConcernedPlayers);
            }
        }
    }
}
