using System;
using Newtonsoft.Json;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Extensions;

namespace Pulsar4X.Datablobs
{
    /// <summary>
    /// This datablob goes into the sensor contact.
    /// TODO: I can't see this actualy getting added to an entity anywhere, maybe it does not need to be a datablob.
    /// </summary>
    public class SensorInfoDB : BaseDataBlob, IGetValuesHash
    {
        [JsonProperty]
        internal Entity Faction;
        [JsonProperty]
        internal Guid DetectedEntityID;
        [JsonIgnore]
        public Entity DetectedEntity; //the actual entity that we've detected.
        [JsonProperty]
        internal SensorContact SensorContact;
        [JsonProperty]
        internal DateTime LastDetection; //the datetime of teh last detection
        [JsonProperty]
        internal SensorReturnValues LatestDetectionQuality; //maybe this should include a list of entites that detected it.
        [JsonProperty]
        internal SensorReturnValues HighestDetectionQuality; //this should maybe include the entity that detected it.

        //jsonconstructor
        public SensorInfoDB() { }

        internal SensorInfoDB(Entity factionEntity, Entity detectedEntity, DateTime atDate)
        {
            Faction = factionEntity;
            DetectedEntity = detectedEntity;
            LastDetection = atDate;


        }

        public override object Clone()
        {
            return new SensorInfoDB(this);
        }

        public int GetValueCompareHash(int hash = 17)
        {
            hash = ObjectExtensions.ValueHash(Faction, hash);
            hash = ObjectExtensions.ValueHash(DetectedEntityID, hash);
            hash = ObjectExtensions.ValueHash(SensorContact, hash);
            hash = ObjectExtensions.ValueHash(LastDetection, hash);
            hash = ObjectExtensions.ValueHash(LatestDetectionQuality, hash);
            hash = ObjectExtensions.ValueHash(HighestDetectionQuality, hash);

            return hash;
        }

        internal SensorInfoDB(SensorInfoDB db)
        {
            Faction = db.Faction;
            DetectedEntityID = db.DetectedEntityID;
            DetectedEntity = db.DetectedEntity;
            SensorContact = db.SensorContact;
            LastDetection = db.LastDetection;
            LatestDetectionQuality = db.LatestDetectionQuality;
            HighestDetectionQuality = db.HighestDetectionQuality;
        }
    }
}
