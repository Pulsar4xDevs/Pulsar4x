using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine.Sensors
{
    /// <summary>
    /// System sensor contacts.
    /// one per faction per system
    /// </summary>
    public class SystemSensorContacts
    {
        [JsonProperty]
        public Entity FactionEntity;

        [JsonProperty]
        public EntityManager ParentManager;

        [JsonProperty]
        Dictionary<int, SensorContact> _sensorContactsByActualGuid = new ();

        [JsonProperty]
        public XThreadData<EntityChangeData> Changes = new XThreadData<EntityChangeData>();

        [JsonConstructor]
        public SystemSensorContacts() { }

        public SystemSensorContacts(EntityManager parentManager, Entity faction)
        {
            ParentManager = parentManager;
            FactionEntity = faction;
            parentManager.FactionSensorContacts.Add(faction.Id, this);
        }

        public bool SensorContactExists(int actualEntityGuid)
        {
            return _sensorContactsByActualGuid.ContainsKey(actualEntityGuid);
        }

        public SensorContact GetSensorContact(int actualEntityId)
        {
            return (_sensorContactsByActualGuid[actualEntityId]);
        }
        internal void AddContact(SensorContact sensorContact)
        {
            _sensorContactsByActualGuid.Add(sensorContact.ActualEntityId, sensorContact);
            Changes.Write(new EntityChangeData()
            {
                Entity = sensorContact.ActualEntity,
                ChangeType = EntityChangeData.EntityChangeType.EntityAdded
            });
        }
        internal void RemoveContact(int ActualEntityId)
        {
            if (_sensorContactsByActualGuid.ContainsKey(ActualEntityId))
            {
                var entity = _sensorContactsByActualGuid[ActualEntityId].ActualEntity;
                _sensorContactsByActualGuid.Remove(ActualEntityId);
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
        public List<int> GetAllContactGuids()
        {
            return _sensorContactsByActualGuid.Keys.ToList();
        }
    }
}