using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Missiles
{
    public class MissileProcessor
    {
        public static void LaunchMissile(Entity launchingEntity, Entity targetEntity, double launchForce, OrdnanceDesign missileDesign, int count)
        {

            var atDatetime = launchingEntity.Manager.StarSysDateTime;
            var parentPositionDB = launchingEntity.GetDataBlob<PositionDB>();
            Vector3 parentPosition = parentPositionDB.AbsolutePosition_m;
            var parentPosRal = parentPositionDB.RelativePosition_m;
            var targetEntityOrbit = targetEntity.GetDataBlob<OrbitDB>();
            if (targetEntity.HasDataBlob<OrbitUpdateOftenDB>())
                targetEntityOrbit = targetEntity.GetDataBlob<OrbitUpdateOftenDB>();
            
            //MissileLauncherAtb launcherAtb;
            VolumeStorageDB cargo = launchingEntity.GetDataBlob<VolumeStorageDB>();
            
            long numMis = cargo.TypeStores[missileDesign.CargoTypeID].CurrentStoreInUnits[missileDesign.ID];
            if (numMis < 1)
                return;
            
            
            
            double launchSpeed = launchForce / missileDesign.MassPerUnit;
            
            double burnTime = ((missileDesign.WetMass - missileDesign.DryMass) / missileDesign.BurnRate) * 0.8; //use 80% of fuel.
            double drymass = (missileDesign.WetMass - missileDesign.DryMass) * 0.8;  //use 80% of fuel.
            double launchManuverDv = OrbitMath.TsiolkovskyRocketEquation(missileDesign.WetMass, drymass, missileDesign.ExaustVelocity);
            double totalDV = OrbitMath.TsiolkovskyRocketEquation(missileDesign.WetMass, missileDesign.DryMass, missileDesign.ExaustVelocity);
            double speed = launchSpeed + launchManuverDv;
            var misslPositionDB = (PositionDB)parentPositionDB.Clone();
            Vector3 parentVelocity = launchingEntity.GetRelativeFutureVelocity(launchingEntity.StarSysDateTime);
            

            
            var orderabledb = new OrderableDB();
            var newtmovedb = new NewtonMoveDB(misslPositionDB.Parent, parentVelocity);

            string defaultName = "Missile";
            string factionsName = missileDesign.Name;
            if (count > 1)
            {
                defaultName += " x" + count;
                factionsName += " x" + count;
            }

            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
            dataBlobs.Add(new ProjectileInfoDB(launchingEntity.Guid, count));
            dataBlobs.Add(new ComponentInstancesDB());
            dataBlobs.Add(misslPositionDB);
            dataBlobs.Add(MassVolumeDB.NewFromMassAndVolume(missileDesign.WetMass, missileDesign.WetMass));
            dataBlobs.Add(new NameDB(defaultName, launchingEntity.FactionOwnerID,  factionsName));
            dataBlobs.Add(newtmovedb);
            dataBlobs.Add(orderabledb);
            var newMissile = Entity.Create(launchingEntity.Manager, launchingEntity.FactionOwnerID, dataBlobs);
            
            foreach (var tuple in missileDesign.Components)
            {
                EntityManipulation.AddComponentToEntity(newMissile, tuple.design, tuple.count);
            }

            var newtdb = newMissile.GetDataBlob<NewtonThrustAbilityDB>();
            //newtdb.DryMass_kg = missileDesign.MassPerUnit;
            newtdb.SetFuel(missileDesign.WetMass - missileDesign.DryMass, missileDesign.WetMass);
            

            bool directAttack = false;
            
            
            if(directAttack)
            {
                /*
                var tgtintercept = OrbitMath.GetInterceptPosition_m(parentPosition, speed, tgtEntityOrbit, atDatetime);
                var tgtEstPos = tgtintercept.position + targetEntity.GetDataBlob<PositionDB>().RelativePosition_m;
                 
                var tgtCurPos = Entity.GetPosition_m(targetEntity, atDatetime);
                   
                var vectorToTgt = Vector3.Normalise(tgtCurPos - parentPosRal);
                 
                //var vectorToTgt = Vector3.Normalise(tgtEstPos - parentPosRal);
                var launcherVector = vectorToTgt * launchSpeed;
                 
                 
                var launchVelocity = parentVelocity + launcherVector;
                var manuverDV = vectorToTgt * launchManuverDv;
                 
                launchVelocity = parentVelocity + launcherVector;
                */
                ThrustToTargetCmd.CreateCommand(launchingEntity.FactionOwnerID, newMissile, launchingEntity.StarSysDateTime, targetEntity);
                
            }
            else
            {
                var launchOrbit = launchingEntity.GetDataBlob<OrbitDB>();
                if (launchingEntity.HasDataBlob<OrbitUpdateOftenDB>())
                    launchOrbit = launchingEntity.GetDataBlob<OrbitUpdateOftenDB>();
                
                var launchTrueAnomaly = launchOrbit.GetTrueAnomaly(atDatetime);
                var targetTrueAnomaly = targetEntityOrbit.GetTrueAnomaly(atDatetime);
                var phaseAngle = targetTrueAnomaly - launchTrueAnomaly;
                var manuvers = InterceptCalcs.OrbitPhasingManuvers(launchOrbit, atDatetime, phaseAngle);
                
                
                var manuverDV = manuvers[0].deltaV;
                //newtmovedb.ActionOnDateTime = atDatetime;
                //newtmovedb.DeltaVForManuver_FoRO_m = manuverDV;   
                //NewtonThrustCommand.CreateCommand(launchingEntity.FactionOwner, newMissile, atDatetime, manuverDV);
                //NewtonThrustCommand.CreateCommand(newMissile, (manuver))
                //DateTime futureDate = atDatetime + TimeSpan.FromSeconds(manuvers[1].timeInSeconds);
                //Vector3 futureDV = manuvers[1].deltaV;
                //NewtonThrustCommand.CreateCommand(launchingEntity.FactionOwner, newMissile, futureDate, futureDV);
                //ThrustToTargetCmd.CreateCommand(launchingEntity.FactionOwner, newMissile, futureDate + TimeSpan.FromSeconds(1), targetEntity);
                NewtonThrustCommand.CreateCommands(newMissile, manuvers);
            }
            CargoTransferProcessor.RemoveCargoItems(launchingEntity, missileDesign, 1);//remove missile from parent.
        }
    }



    public class ProjectileInfoDB : BaseDataBlob
    {
        public Guid LaunchedBy = new Guid();
        public int Count = 1;

        [JsonConstructor]
        private ProjectileInfoDB()
        {
        }

        public ProjectileInfoDB(Guid launchedBy, int count)
        {
            LaunchedBy = launchedBy;
            Count = count;
        }

        public override object Clone()
        {
            throw new System.NotImplementedException();
        }
    }
}