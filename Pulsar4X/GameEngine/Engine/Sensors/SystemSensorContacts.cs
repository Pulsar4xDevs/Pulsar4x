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
        Dictionary<int, SensorContact> _sensorContactsByEntityId = new ();

        [JsonProperty]
        public XThreadData<EntityChangeData> Changes = new XThreadData<EntityChangeData>();

        [JsonConstructor]
        public SystemSensorContacts() { }

        public SystemSensorContacts(EntityManager parentManager, Entity faction)
        {
            ParentManager = parentManager;
            FactionEntity = faction;
        }

        public bool SensorContactExists(int actualEntityGuid)
        {
            return _sensorContactsByEntityId.ContainsKey(actualEntityGuid);
        }

        public SensorContact GetSensorContact(int actualEntityId)
        {
            return _sensorContactsByEntityId[actualEntityId];
        }

        public bool TryGetSensorContact(int entityId, out SensorContact sensorContact)
        {
            if(_sensorContactsByEntityId.ContainsKey(entityId))
            {
                sensorContact = _sensorContactsByEntityId[entityId];
                return true;
            }

            sensorContact = null;
            return false;
        }

        internal void AddContact(SensorContact sensorContact)
        {
            _sensorContactsByEntityId.Add(sensorContact.ActualEntityId, sensorContact);
            Changes.Write(new EntityChangeData()
            {
                Entity = sensorContact.ActualEntity,
                ChangeType = EntityChangeData.EntityChangeType.EntityAdded
            });
        }
        internal void RemoveContact(int ActualEntityId)
        {
            if (_sensorContactsByEntityId.ContainsKey(ActualEntityId))
            {
                var entity = _sensorContactsByEntityId[ActualEntityId].ActualEntity;
                _sensorContactsByEntityId.Remove(ActualEntityId);
                Changes.Write(new EntityChangeData()
                {
                    Entity = entity,
                    ChangeType = EntityChangeData.EntityChangeType.EntityAdded
                });
            }
        }
        public List<SensorContact> GetAllContacts()
        {
            return _sensorContactsByEntityId.Values.ToList();
        }
        public List<int> GetAllContactIds()
        {
            return _sensorContactsByEntityId.Keys.ToList();
        }
    }
}