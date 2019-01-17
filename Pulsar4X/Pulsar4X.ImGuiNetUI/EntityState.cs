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
        public List<EntityChangeData> Changes = new List<EntityChangeData>();
        public List<EntityChangeData> _changesNextFrame = new List<EntityChangeData>();
        public CommandReferences CmdRef;

        public EntityState(Entity entity)
        {
            Entity = entity;
            foreach (var db in entity.DataBlobs)
            {
                DataBlobs.Add(db.GetType(), db);
            }
            Position = entity.GetDataBlob<PositionDB>();

            //Name = entity.GetDataBlob<NameDB>().GetName(_state.Faction);

            entity.ChangeEvent += On_entityChangeEvent;
        }

        public EntityState(SensorContact sensorContact)
        {
            Entity = sensorContact.ActualEntity;
            Position = sensorContact.Position;

            //Name = sensorContact.GetDataBlob<NameDB>().GetName(_state.Faction);

            sensorContact.ActualEntity.ChangeEvent += On_entityChangeEvent;

        }


        //maybe this should be done in the SystemState?
        void On_entityChangeEvent(EntityChangeData.EntityChangeType changeType, BaseDataBlob db)
        {
            _changesNextFrame.Add(new EntityChangeData() { ChangeType = changeType, Datablob = db, Entity = Entity });
            switch (changeType)
            {
                case EntityChangeData.EntityChangeType.DBAdded:
                    DataBlobs[db.GetType()] = db;
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

        public void PostFrameCleanup()
        {
            Changes = _changesNextFrame;
            _changesNextFrame = new List<EntityChangeData>();
        }

    }
}
