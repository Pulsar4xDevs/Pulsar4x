using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;

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

        public SensorContact(Entity factionEntity, Entity actualEntity, DateTime atDateTime)
        {
            ActualEntity = actualEntity;
            ActualEntityId = actualEntity.Id;
            SensorInfo = new SensorInfoDB(factionEntity, actualEntity, atDateTime);
            Position = new SensorPositionDB(actualEntity.GetDataBlob<PositionDB>());
            var factionInfoDB = factionEntity.GetDataBlob<FactionInfoDB>();
            if (!factionInfoDB.SensorContacts.ContainsKey(actualEntity.Id))
                factionInfoDB.SensorContacts.Add(actualEntity.Id, this);
            actualEntity.ChangeEvent += ActualEntity_ChangeEvent;
            Name = actualEntity.GetDataBlob<NameDB>().GetName(factionEntity);
        }

        void ActualEntity_ChangeEvent(EntityChangeData.EntityChangeType changeType, BaseDataBlob db)
        {
            if (changeType == EntityChangeData.EntityChangeType.EntityRemoved)
            {
                Position.GetDataFrom = DataFrom.Memory;
            }
        }

    }
}