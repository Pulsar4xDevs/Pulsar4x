using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine.Sensors
{
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
}