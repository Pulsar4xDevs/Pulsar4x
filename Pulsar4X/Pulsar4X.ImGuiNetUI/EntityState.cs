using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class EntityState
    {
        public Entity Entity;
        public string Name;

        public IPosition Position;
        public NameIcon NameIcon;
        public OrbitIcon OrbitIcon;
        public OrbitOrderWiget DebugOrbitOrder;
        public bool IsDestroyed = false; //currently IsDestroyed = true if moved from one system to another, may need to revisit this. 
        public Dictionary<Type, BaseDataBlob> DataBlobs = new Dictionary<Type, BaseDataBlob>();

        public CommandReferences CmdRef;

        public EntityState(Entity entity)
        {
            Entity = entity;
            foreach (var db in entity.DataBlobs)
            {
                DataBlobs.Add(db.GetType(), db);
            }
            Position = entity.GetDataBlob<PositionDB>();
            entity.ChangeEvent += On_entityChangeEvent;
        }

        public EntityState(SensorContact sensorContact)
        {
            Entity = sensorContact.ActualEntity;
            Position = sensorContact.Position;
            sensorContact.ActualEntity.ChangeEvent += On_entityChangeEvent;
        }


        //maybe this should be done in the SystemState?
        void On_entityChangeEvent(EntityChangeData.EntityChangeType changeType, BaseDataBlob db)
        {
            switch (changeType)
            {
                case EntityChangeData.EntityChangeType.DBAdded:
                    DataBlobs.Add(db.GetType(), db);
                    break;
                case EntityChangeData.EntityChangeType.DBRemoved:
                    DataBlobs.Remove(db.GetType());
                    break;
                case EntityChangeData.EntityChangeType.EntityRemoved:
                    DataBlobs.Clear();
                    IsDestroyed = true;
                    break;
                default:
                    break;
            }
        }

    }
}
