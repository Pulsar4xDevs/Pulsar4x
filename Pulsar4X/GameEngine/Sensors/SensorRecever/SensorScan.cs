using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Colonies;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Movement;

namespace Pulsar4X.Sensors
{
    public class SensorScan : IInstanceProcessor
    {

        public void TriggerProcess(Entity entity, DateTime atDateTime)
        {
            ProcessEntity(entity, atDateTime);
        }
        //TODO: ReWrite this, instead of each component trying to do a scan,
        //multiple components should mix together to form a single suite and the ship itself should scan.
        //maybe the scan freqency /attribute.scanTime should just effect the chance of a detection.
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            if(entity.Manager == null) throw new NullReferenceException("entity.Manager cannot be null");

            EntityManager manager = entity.Manager;
            Entity faction = entity.Manager.Game.Factions[entity.FactionOwnerID];

            var position = entity.GetDataBlob<PositionDB>();//recever is a componentDB. not a shipDB
            if (position == null) //then it's probilby a colony
                position = entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<PositionDB>();
            
            if( entity.TryGetDataBlob<SensorAbilityDB>(out var sensorAbility))
            {
                var detectableEntitys = manager.GetAllEntitiesWithDataBlob<SensorProfileDB>();
                sensorAbility.CurrentContacts = new List<(Entity, SensorReturnValues)>();
                for(int i = 0; i < sensorAbility.InstanceStates.Count; i++)
                {
                    var sensorAbl = sensorAbility.InstanceStates[i];
                    var sensorAtb = sensorAbility.InstanceAtributes[i];
                    var sensorMgr = manager.GetSensorContacts(entity.FactionOwnerID);
                    var detections = SensorTools.GetDetectedEntites(sensorAtb, position.AbsolutePosition, detectableEntitys, atDateTime, faction.Id, true);
                    
                    SensorInfoDB sensorInfo;
                    for (int j = 0; j < detections.Length; j++)
                    {
                        var detectionValues = detections[j];
                        var detectableEntity = detectableEntitys[j];
                        sensorAbility.CurrentContacts.Add((detectableEntity, detectionValues));
                        if (detectionValues.SignalStrength_kW > 0.0)
                        {
                            
                            if (sensorAtb.IsEnergyGen)//if solar array not sensor
                            {
                                var genAbil = entity.GetDataBlob<EnergyGenAbilityDB>();
                                genAbil.LocalFuel = genAbil.TotalFuelUseAtMax.maxUse * sensorAtb.ScanTime;
                            }
                            else if (sensorMgr.SensorContactExists(detectableEntity.Id))
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
                                var contact = new SensorContact(faction, detectableEntity, atDateTime);
                                sensorMgr.AddContact(contact);
                                sensorAbl.CurrentContacts[detectableEntity.Id] = detectionValues;
                            }

                        }
                        else if (sensorMgr.SensorContactExists(detectableEntity.Id) && sensorAbl.CurrentContacts.ContainsKey(detectableEntity.Id))
                        {
                            sensorAbl.CurrentContacts.Remove(detectableEntity.Id);
                            sensorAbl.OldContacts[detectableEntity.Id] = detectionValues;
                            sensorMgr.RemoveContact(detectableEntity.Id);
                        }
                    }

                    manager.ManagerSubpulses.AddEntityInterupt(atDateTime + TimeSpan.FromSeconds(sensorAtb.ScanTime), this.TypeName, entity);
                }
            }
        }
    }
}
