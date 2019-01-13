using System;
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
        public SystemSensorContacts(EntityManager parentManager, Entity faction) 
        {
            ParentManager = parentManager;
            FactionEntity = faction;
            parentManager.FactionSensorManagers.Add(faction.Guid, this);
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
        }
        public List<SensorContact> GetAllContacts()
        {
            return _sensorContactsByActualGuid.Values.ToList();
        }
    }
}
