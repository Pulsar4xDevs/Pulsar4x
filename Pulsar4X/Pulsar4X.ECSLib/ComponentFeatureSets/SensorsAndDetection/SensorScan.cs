using System;

namespace Pulsar4X.ECSLib
{
    public class SensorScan : IInstanceProcessor
    {
        //thinking about doing this where entity is the sensor component not the ship,
        //that way, a ship can have multiple different sensors which run at different intervals. 
        //I'll need to get the parent ship... or maybe just the systemfactionInfo to store the detected ships though.
        //having the ships store what they detect could be usefull info to display though. 
        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            EntityManager manager = entity.Manager;
            DateTime atDate = manager.ManagerSubpulses.SystemLocalDateTime + TimeSpan.FromSeconds(deltaSeconds);
            var receverDB = entity.GetDataBlob<SensorReceverAtbDB>();
            foreach (var sensorProfile in manager.GetAllDataBlobsOfType<SensorProfileDB>())
            {
                if (sensorProfile.OwningEntity.HasDataBlob<PositionDB>())
                    SensorProcessorTools.DetectEntites(receverDB, sensorProfile, atDate);
            }



            manager.ManagerSubpulses.AddEntityInterupt(atDate + TimeSpan.FromSeconds(receverDB.ScanTime), this, entity);

        }
    }
}
