using System;

namespace Pulsar4X.ECSLib
{
    public class SensorScan : IInstanceProcessor
    {
        //thinking about doing this where entity is the sensor component not the ship,
        //that way, a ship can have multiple different sensors which run at different intervals. 
        //I'll need to get the parent ship... or maybe just the systemfactionInfo to store the detected ships though.
        //having the ships      what they detect could be usefull info to display though. 
        internal override void ProcessEntity(Entity entity, int deltaSeconds)
        {
            EntityManager manager = entity.Manager;
            Entity faction;// = entity.GetDataBlob<OwnedDB>().OwnedByFaction;
            entity.Manager.FindEntityByGuid(entity.FactionOwner, out faction);
            DateTime atDate = manager.ManagerSubpulses.SystemLocalDateTime + TimeSpan.FromSeconds(deltaSeconds);

            SensorReceverAtbDB receverDB = entity.GetDataBlob<SensorReceverAtbDB>();
            FactionInfoDB factionInfo = faction.GetDataBlob<FactionInfoDB>();

            var detectableEntitys = manager.GetAllEntitiesWithDataBlob<SensorProfileDB>();
            foreach (var detectableEntity in detectableEntitys)
            {
                //Entity detectableEntity = sensorProfile.OwningEntity;

                if (detectableEntity.FactionOwner != Guid.Empty)
                {
                    if (detectableEntity.FactionOwner != faction.FactionOwner)
                    {
                        SensorProcessorTools.DetectEntites(faction, factionInfo, receverDB, detectableEntity, atDate);
                    }
                    else
                    {
                        //then the sensor profile belongs to the same faction as the recever. don't bother trying to detect it. 
                    }
                }
                else
                {
                    SensorProcessorTools.DetectEntites(faction, factionInfo, receverDB, detectableEntity, atDate);
                }
            }



            manager.ManagerSubpulses.AddEntityInterupt(atDate + TimeSpan.FromSeconds(receverDB.ScanTime), this.TypeName, entity);

        }
    }
}
