using System;

namespace Pulsar4X.ECSLib
{
    public class SensorScan : IInstanceProcessor
    {
        //thinking about doing this where entity is the sensor component not the ship,
        //that way, a ship can have multiple different sensors which run at different intervals. 
        //I'll need to get the parent ship... or maybe just the systemfactionInfo to store the detected ships though.
        //having the ships      what they detect could be usefull info to display though. 
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            EntityManager manager = entity.Manager;
            Entity faction;// = entity.GetDataBlob<OwnedDB>().OwnedByFaction;
            entity.Manager.FindEntityByGuid(entity.FactionOwner, out faction);

            var designEntity = entity.GetDataBlob<ComponentInstanceInfoDB>().DesignEntity;
            SensorReceverAtbDB receverDB = designEntity.GetDataBlob<SensorReceverAtbDB>();

            FactionInfoDB factionInfo = faction.GetDataBlob<FactionInfoDB>();

            var detectableEntitys = manager.GetAllEntitiesWithDataBlob<SensorProfileDB>();

            SystemSensorContacts sensorMgr;
            if (!manager.FactionSensorManagers.ContainsKey(entity.FactionOwner))
                sensorMgr = new SystemSensorContacts(manager, faction);
            else 
                sensorMgr = manager.FactionSensorManagers[entity.FactionOwner];

            foreach (var detectableEntity in detectableEntitys)
            {
                //Entity detectableEntity = sensorProfile.OwningEntity;

                if (detectableEntity.FactionOwner != Guid.Empty)
                {
                    if (detectableEntity.FactionOwner != faction.Guid)                        
                    {
                        var position = entity.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<PositionDB>();//recever is a componentDB. not a shipDB
                        if (position == null) //then it's probilby a colony
                            position = entity.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<PositionDB>();

                        SensorProcessorTools.DetectEntites(sensorMgr, factionInfo,position, receverDB, detectableEntity, atDateTime);
                    }
                    else
                    {
                        //then the sensor profile belongs to the same faction as the recever. don't bother trying to detect it. 
                    }
                }
                else
                {

                    var position = entity.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<PositionDB>();//recever is a componentDB. not a shipDB
                    if (position == null) //then it's probilby a colony
                        position = entity.GetDataBlob<ComponentInstanceInfoDB>().ParentEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<PositionDB>();

                    SensorProcessorTools.DetectEntites(sensorMgr, factionInfo, position, receverDB, detectableEntity, atDateTime);
                }
            }



            manager.ManagerSubpulses.AddEntityInterupt(atDateTime + TimeSpan.FromSeconds(receverDB.ScanTime), this.TypeName, entity);

        }
    }
}
