using System;
using Newtonsoft.Json;
using static Pulsar4X.ECSLib.SensorProcessorTools;

namespace Pulsar4X.ECSLib
{
    public class SensorInfoDB : BaseDataBlob, IGetValuesHash
    {
        [JsonProperty]
        internal Entity Faction;
        [JsonProperty]
        internal Guid DetectedEntityID;
        [JsonIgnore]
        public Entity DetectedEntity; //the actual entity that we've detected. 
        [JsonProperty]
        internal Entity SensorEntity; //this is a clone of the DetectedEntity, with inacuracies in some of the data, may have datablobs which are references to the actual entitey datablobs
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
            factionEntity.GetDataBlob<FactionInfoDB>().SensorEntites.Add(detectedEntity.Guid, SensorEntityFactory.UpdateSensorContact(factionEntity, this));
        }

        public override object Clone()
        {
            return new SensorInfoDB(this);
        }

        public int GetValueCompareHash(int hash = 17)
        {
            hash = Misc.ValueHash(Faction, hash);
            hash = Misc.ValueHash(DetectedEntityID, hash);
            hash = Misc.ValueHash(SensorEntity, hash);
            hash = Misc.ValueHash(LastDetection, hash);
            hash = Misc.ValueHash(LatestDetectionQuality, hash);
            hash = Misc.ValueHash(HighestDetectionQuality, hash);

            return hash;
        }

        internal SensorInfoDB(SensorInfoDB db)
        {
            Faction = db.Faction;
            DetectedEntityID = db.DetectedEntityID;
            DetectedEntity = db.DetectedEntity;
            SensorEntity = db.SensorEntity;
            LastDetection = db.LastDetection;
            LatestDetectionQuality = db.LatestDetectionQuality;
            HighestDetectionQuality = db.HighestDetectionQuality;
        }
    }
}
