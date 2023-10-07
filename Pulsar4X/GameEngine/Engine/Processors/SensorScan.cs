using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine.Sensors;

namespace Pulsar4X.Engine
{
    public class SensorScan : IInstanceProcessor
    {

        //TODO: ReWrite this, instead of each component trying to do a scan,
        //multiple components should mix together to form a single suite and the ship itself should scan.
        //maybe the scan freqency /attribute.scanTime should just effect the chance of a detection.
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            EntityManager manager = entity.Manager;
            Entity faction = entity.Manager.Game.Factions[entity.FactionOwnerID];

            var detectableEntitys = manager.GetAllEntitiesWithDataBlob<SensorProfileDB>();

            var position = entity.GetDataBlob<PositionDB>();//recever is a componentDB. not a shipDB
            if (position == null) //then it's probilby a colony
                position = entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<PositionDB>();

            if( entity.GetDataBlob<ComponentInstancesDB>().TryGetComponentsByAttribute<SensorReceiverAtbDB>(out var recevers))
            {
                foreach (var recever in recevers)
                {
                    var sensorAbl = recever.GetAbilityState<SensorReceiverAbility>();
                    var sensorAtb = recever.Design.GetAttribute<SensorReceiverAtbDB>();

                    FactionInfoDB factionInfo = faction.GetDataBlob<FactionInfoDB>();


                    SystemSensorContacts sensorMgr = manager.GetFactionSensorContacts(entity.FactionOwnerID);

                    var detections = SensorTools.GetDetectedEntites(sensorAtb, position.AbsolutePosition, detectableEntitys, atDateTime, faction.Id, true);
                    SensorInfoDB sensorInfo;
                    for (int i = 0; i < detections.Length; i++)
                    {
                        SensorReturnValues detectionValues;
                        detectionValues = detections[i];
                        var detectableEntity = detectableEntitys[i];
                        if (detectionValues.SignalStrength_kW > 0.0)
                        {
                            if (sensorMgr.SensorContactExists(detectableEntity.Id))
                            {
                                //sensorInfo = knownContacts[detectableEntity.ID].GetDataBlob<SensorInfoDB>();
                                sensorInfo = sensorMgr.GetSensorContact(detectableEntity.Id).SensorInfo;
                                sensorInfo.LatestDetectionQuality = detectionValues;
                                sensorInfo.LastDetection = atDateTime;
                                if (sensorInfo.HighestDetectionQuality.SignalQuality < detectionValues.SignalQuality)
                                    sensorInfo.HighestDetectionQuality.SignalQuality = detectionValues.SignalQuality;

                                if (sensorInfo.HighestDetectionQuality.SignalStrength_kW < detectionValues.SignalStrength_kW)
                                    sensorInfo.HighestDetectionQuality.SignalStrength_kW = detectionValues.SignalStrength_kW;
                                SensorEntityFactory.UpdateSensorContact(faction, sensorInfo);
                            }
                            else
                            {
                                SensorContact contact = new SensorContact(faction, detectableEntity, atDateTime);
                                sensorMgr.AddContact(contact);
                                sensorAbl.CurrentContacts[detectableEntity.Id] = detectionValues;

                                //knownContacts.Add(detectableEntity.ID, SensorEntityFactory.UpdateSensorContact(receverFaction, sensorInfo)); moved this line to the SensorInfoDB constructor
                            }

                        }
                        else if (sensorMgr.SensorContactExists(detectableEntity.Id) && sensorAbl.CurrentContacts.ContainsKey(detectableEntity.Id))
                        {
                            sensorAbl.CurrentContacts.Remove(detectableEntity.Id);
                            sensorAbl.OldContacts[detectableEntity.Id] = detectionValues;
                        }
                    }

                    manager.ManagerSubpulses.AddEntityInterupt(atDateTime + TimeSpan.FromSeconds(sensorAtb.ScanTime), this.TypeName, entity);
                }
            }
        }
    }
}
