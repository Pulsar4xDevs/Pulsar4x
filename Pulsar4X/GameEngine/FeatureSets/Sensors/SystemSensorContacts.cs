using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Messaging;

namespace Pulsar4X.Engine.Sensors
{
    /// <summary>
    /// System sensor contacts.
    /// one per faction per system
    /// </summary>
    public class SystemSensorContacts
    {
        [JsonProperty]
        public int FactionId;

        [JsonProperty]
        Dictionary<int, SensorContact> _sensorContactsByEntityId = new ();

        [JsonProperty]
        public XThreadData<Message> Changes = new XThreadData<Message>();

        [JsonConstructor]
        public SystemSensorContacts() { }

        public SystemSensorContacts(Entity faction)
        {
            FactionId = faction.Id;
        }

        public bool SensorContactExists(int actualEntityGuid)
        {
            return _sensorContactsByEntityId.ContainsKey(actualEntityGuid);
        }

        public SensorContact GetSensorContact(int actualEntityId)
        {
            return _sensorContactsByEntityId[actualEntityId];
        }

        public bool TryGetSensorContact(int entityId, out SensorContact? sensorContact)
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
            Changes.Write(Message.Create(MessageTypes.EntityAdded, sensorContact.ActualEntity.Id));
        }
        internal void RemoveContact(int ActualEntityId)
        {
            if (_sensorContactsByEntityId.ContainsKey(ActualEntityId))
            {
                var entity = _sensorContactsByEntityId[ActualEntityId].ActualEntity;
                _sensorContactsByEntityId.Remove(ActualEntityId);
                Changes.Write(Message.Create(MessageTypes.EntityRemoved, ActualEntityId));
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