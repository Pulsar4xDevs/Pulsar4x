using System;

namespace Pulsar4X.ECSLib
{
    public class SensorScan : IInstanceProcessor
    {

        //TODO: ReWrite this, instead of each component trying to do a scan,
        //multiple components should mix together to form a single suite and the ship itself should scan. 
        //maybe the scan freqency /attribute.scanTime should just effect the chance of a detection. 
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            EntityManager manager = entity.Manager;
            Entity faction;// = entity.GetDataBlob<OwnedDB>().OwnedByFaction;
            entity.Manager.FindEntityByGuid(entity.FactionOwner, out faction);

            
            var detectableEntitys = manager.GetAllEntitiesWithDataBlob<SensorProfileDB>();

            var position = entity.GetDataBlob<PositionDB>();//recever is a componentDB. not a shipDB
            if (position == null) //then it's probilby a colony
                position = entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<PositionDB>();

            if( entity.GetDataBlob<ComponentInstancesDB>().TryGetComponentsByAttribute<SensorReceverAtbDB>(out var recevers)) 
            {
                foreach (var recever in recevers)
                {
                    var ability = recever.GetAbilityState<SensorReceverAbility>();
                    var attribute = recever.Design.GetAttribute<SensorReceverAtbDB>();

                    //SensorReceverAtbDB receverDB = designEntity.GetDataBlob<SensorReceverAtbDB>();

                    FactionInfoDB factionInfo = faction.GetDataBlob<FactionInfoDB>();


                    SystemSensorContacts sensorMgr;
                    if (!manager.FactionSensorContacts.ContainsKey(entity.FactionOwner))
                        sensorMgr = new SystemSensorContacts(manager, faction);
                    else 
                        sensorMgr = manager.FactionSensorContacts[entity.FactionOwner];

                    foreach (var detectableEntity in detectableEntitys)
                    {
                        //Entity detectableEntity = sensorProfile.OwningEntity;

                        if (detectableEntity.FactionOwner != Guid.Empty)
                        {
                            if (detectableEntity.FactionOwner != faction.Guid)                        
                            {
                                SensorProcessorTools.DetectEntites(sensorMgr, factionInfo,position, attribute, detectableEntity, atDateTime);
                            }
                            else
                            {
                                //then the sensor profile belongs to the same faction as the recever. don't bother trying to detect it. 
                            }
                        }
                        else
                        {
                            SensorProcessorTools.DetectEntites(sensorMgr, factionInfo, position, attribute, detectableEntity, atDateTime);
                        }
                    }
                    manager.ManagerSubpulses.AddEntityInterupt(atDateTime + TimeSpan.FromSeconds(attribute.ScanTime), this.TypeName, entity);
                }
            }
        }
    }
}
