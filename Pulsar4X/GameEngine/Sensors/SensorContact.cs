using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Pulsar4X.Datablobs;
using Pulsar4X.Messaging;

namespace Pulsar4X.Engine.Sensors
{
    public enum DataFrom
    {
        Parent,
        Sensors,
        Memory
    }

    public class SensorContact
    {
        public int ActualEntityId;
        public Entity ActualEntity;

        public SensorInfoDB SensorInfo;
        public SensorPositionDB Position;
        //public SensorOrbitDB Orbit;

        public string Name = "UnNamed";

        [JsonConstructor]
        public SensorContact() { }

        public SensorContact(Entity factionEntity, Entity actualEntity, DateTime atDateTime)
        {
            ActualEntity = actualEntity;
            ActualEntityId = actualEntity.Id;
            SensorInfo = new SensorInfoDB(factionEntity, actualEntity, atDateTime);
            Position = new SensorPositionDB(actualEntity.GetDataBlob<PositionDB>());
            var factionInfoDB = factionEntity.GetDataBlob<FactionInfoDB>();
            if (!factionInfoDB.SensorContacts.ContainsKey(actualEntity.Id))
                factionInfoDB.SensorContacts.Add(actualEntity.Id, this);
            Name = actualEntity.GetDataBlob<NameDB>().GetName(factionEntity);

            MessagePublisher.Instance.Subscribe(MessageTypes.EntityRemoved, EntityRemoved, msg => msg.EntityId != null && msg.EntityId.Value == actualEntity.Id);
        }

        async Task EntityRemoved(Message message)
        {
            await Task.Run(() => Position.GetDataFrom = DataFrom.Memory);
        }

    }
}