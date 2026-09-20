using System;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;

namespace Pulsar4X.Sensors
{
    /// <summary>
    /// This datablob goes into the sensor contact.
    /// QUESTION: I can't see this actualy getting added to an entity anywhere, maybe it does not need to be a datablob.
    /// Answer:
    /// This datablob gets added to the SensorEntity in SensorEntityFactory.
    /// SensorScan creates this datablob and passes it to the SensorEntity factory either updating or creating a new entity.
    /// the SensorEntity is what the player sees, the player doesnt get access to the actual entity. 
    /// </summary>
    public class SensorInfoDB : BaseDataBlob
    {
        [JsonProperty]
        internal int FactionId;
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
            FactionId = factionEntity.Id;
            DetectedEntity = detectedEntity;
            LastDetection = atDate;
        }

        public override object Clone()
        {
            return new SensorInfoDB(this);
        }

        internal SensorInfoDB(SensorInfoDB db)
        {
            FactionId = db.FactionId;
            DetectedEntity = db.DetectedEntity;
            SensorContact = db.SensorContact;
            LastDetection = db.LastDetection;
            LatestDetectionQuality = db.LatestDetectionQuality;
            HighestDetectionQuality = db.HighestDetectionQuality;
        }
    }
}
