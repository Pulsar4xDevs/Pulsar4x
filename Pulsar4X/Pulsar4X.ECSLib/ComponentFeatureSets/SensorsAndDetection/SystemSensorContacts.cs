using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public enum DataFrom
    {
        Parent,
        Sensors,
        Memory
    }

    public class SensorContact
    {
        public Guid ActualEntityGuid;
        public Entity ActualEntity;

        public SensorInfoDB SensorInfo;
        public SensorPositionDB Position;
        //public SensorOrbitDB Orbit;

        public string Name = "UnNamed";

        public SensorContact(Entity factionEntity, Entity actualEntity, DateTime atDateTime)
        {
            ActualEntity = actualEntity;
            ActualEntityGuid = actualEntity.Guid;
            SensorInfo = new SensorInfoDB(factionEntity, actualEntity, atDateTime);
            Position = new SensorPositionDB(actualEntity.GetDataBlob<PositionDB>());
            var factionInfoDB = factionEntity.GetDataBlob<FactionInfoDB>();
            if (!factionInfoDB.SensorContacts.ContainsKey(actualEntity.Guid))
                factionInfoDB.SensorContacts.Add(actualEntity.Guid, this);
            actualEntity.ChangeEvent += ActualEntity_ChangeEvent;
        }

        void ActualEntity_ChangeEvent(EntityChangeData.EntityChangeType changeType, BaseDataBlob db)
        {
            if (changeType == EntityChangeData.EntityChangeType.EntityRemoved)
            {
                Position.GetDataFrom = DataFrom.Memory;
            }
        }

    }

    /// <summary>
    /// System sensor contacts.
    /// one per faction per system
    /// </summary>
    public class SystemSensorContacts
    {
        public Entity FactionEntity;

        public EntityManager ParentManager;
        Dictionary<Guid, SensorContact> _sensorContactsByActualGuid = new Dictionary<Guid, SensorContact>();

        public XThreadData<EntityChangeData> Changes = new XThreadData<EntityChangeData>();

        public SystemSensorContacts(EntityManager parentManager, Entity faction)
        {
            ParentManager = parentManager;
            FactionEntity = faction;
            parentManager.FactionSensorContacts.Add(faction.Guid, this);
        }

        public bool SensorContactExists(Guid actualEntityGuid)
        {
            return _sensorContactsByActualGuid.ContainsKey(actualEntityGuid);
        }

        public SensorContact GetSensorContact(Guid actualEntityGuid)
        {
            return (_sensorContactsByActualGuid[actualEntityGuid]);
        }
        internal void AddContact(SensorContact sensorContact)
        {
            _sensorContactsByActualGuid.Add(sensorContact.ActualEntityGuid, sensorContact);
            Changes.Write(new EntityChangeData() 
            { 
                Entity = sensorContact.ActualEntity, 
                ChangeType = EntityChangeData.EntityChangeType.EntityAdded 
            });
        }
        internal void RemoveContact(Guid ActualEntityGuid)
        {
            if (_sensorContactsByActualGuid.ContainsKey(ActualEntityGuid))
            {
                var entity = _sensorContactsByActualGuid[ActualEntityGuid].ActualEntity;
                _sensorContactsByActualGuid.Remove(ActualEntityGuid);
                Changes.Write(new EntityChangeData() 
                { 
                    Entity = entity, 
                    ChangeType = EntityChangeData.EntityChangeType.EntityAdded 
                });
            }
        }
        public List<SensorContact> GetAllContacts()
        {
            return _sensorContactsByActualGuid.Values.ToList();
        }
        public List<Guid> GetAllContactGuids()
        {
            return _sensorContactsByActualGuid.Keys.ToList();
        }
    }

    public class XThreadData<T>
    {
        ConcurrentHashSet<ConcurrentQueue<T>> _subscribers = new ConcurrentHashSet<ConcurrentQueue<T>>();

        public void Write(T data)
        {
            foreach (ConcurrentQueue<T> sub in _subscribers)
            {
                sub.Enqueue(data);            
            }

        }

        public ConcurrentQueue<T> Subscribe()
        {
            ConcurrentQueue<T> newQueue = new ConcurrentQueue<T>();
            _subscribers.Add(newQueue);
            return newQueue; 
        }

        public void Unsubscribe(ConcurrentQueue<T> queue)
        {
            _subscribers.Remove(queue);
        }
    }
}
