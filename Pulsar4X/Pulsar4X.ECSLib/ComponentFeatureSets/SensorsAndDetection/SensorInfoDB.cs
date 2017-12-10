using System;
using static Pulsar4X.ECSLib.SensorProcessorTools;

namespace Pulsar4X.ECSLib
{
    public class SensorInfoDB : BaseDataBlob
    {
        internal Entity Faction;
        internal Entity DetectedEntity; //the actual entity that we've detected. 

        internal Entity SensorEntity; //this is a clone of the DetectedEntity, with inacuracies in some of the data, may have datablobs which are references to the actual entitey datablobs
        internal DateTime LastDetection; //the datetime of teh last detection
        internal sensorReturnValues LatestDetectionQuality; //maybe this should include a list of entites that detected it. 
        internal sensorReturnValues HighestDetectionQuality; //this should maybe include the entity that detected it. 

        //jsonconstructor
        internal SensorInfoDB() { }

        internal SensorInfoDB(Entity factionEntity)
        {
            Faction = factionEntity;
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
        internal SensorInfoDB(SensorInfoDB db)
        {
            Faction = db.Faction;
            DetectedEntity = db.DetectedEntity;
            SensorEntity = db.SensorEntity;
            LastDetection = db.LastDetection;
            LatestDetectionQuality = db.LatestDetectionQuality;
            HighestDetectionQuality = db.HighestDetectionQuality;
        }
    }
}
