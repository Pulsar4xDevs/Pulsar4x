using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Missiles
{
    public class MissileProcessor
    {
        public static void LaunchMissile(Entity launchingEntity, Entity targetEntity, double launchForce, OrdnanceDesign missileDesign)
        {

            var atDatetime = launchingEntity.Manager.StarSysDateTime;
            var parentPositionDB = launchingEntity.GetDataBlob<PositionDB>();
            Vector3 parentPosition = parentPositionDB.AbsolutePosition_m;
            var parentPosRal = parentPositionDB.RelativePosition_m;
            var tgtEntityOrbit = targetEntity.GetDataBlob<OrbitDB>();
            
            //MissileLauncherAtb launcherAtb;
            CargoStorageDB cargo = launchingEntity.GetDataBlob<CargoStorageDB>();
            int numMis = (int)StorageSpaceProcessor.GetAmount(cargo, missileDesign);
            if (numMis < 1)
                return;
            double launchSpeed = launchForce / missileDesign.Mass;
            
            //missileDesign.

            double burnTime = ((missileDesign.WetMass - missileDesign.DryMass) / missileDesign.BurnRate) * 0.8; //use 80% of fuel.
            double drymass = (missileDesign.WetMass - missileDesign.DryMass) * 0.8;  //use 80% of fuel.
            double launchManuverDv = OrbitMath.TsiolkovskyRocketEquation(missileDesign.WetMass, drymass, missileDesign.ExaustVelocity);
            double totalDV = OrbitMath.TsiolkovskyRocketEquation(missileDesign.WetMass, missileDesign.DryMass, missileDesign.ExaustVelocity);
            double speed = launchSpeed + launchManuverDv;
            
            var tgtintercept = OrbitMath.GetInterceptPosition_m(parentPosition, speed, tgtEntityOrbit, atDatetime);
            var tgtEstPos = tgtintercept.position + targetEntity.GetDataBlob<PositionDB>().RelativePosition_m;
            var vectorToTgt = Vector3.Normalise(tgtEstPos - parentPosRal);
            var launcherVector = vectorToTgt * launchSpeed;
            
            Vector3 parentVelocity = Entity.GetVelocity_m(launchingEntity, launchingEntity.Manager.StarSysDateTime);

            var launchVelocity = parentVelocity + launcherVector;
            

            
            var manuverDV = vectorToTgt * launchManuverDv;


            
            var misslPositionDB = (PositionDB)parentPositionDB.Clone();
            var newtmovedb = new NewtonMoveDB(misslPositionDB.Parent, launchVelocity);
            newtmovedb.ActionOnDateTime = atDatetime;
            
            
            newtmovedb.DeltaVForManuver_m = manuverDV; 
            
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
            dataBlobs.Add(new ProjectileInfoDB(launchingEntity.Guid));
            dataBlobs.Add(new ComponentInstancesDB());
            dataBlobs.Add(misslPositionDB);
            dataBlobs.Add(MassVolumeDB.NewFromMassAndVolume(missileDesign.WetMass, missileDesign.WetMass));
            dataBlobs.Add(new NameDB("Missile", launchingEntity.FactionOwner, missileDesign.Name ));
            dataBlobs.Add(newtmovedb);
            var newMissile = Entity.Create(launchingEntity.Manager, launchingEntity.FactionOwner, dataBlobs);
            
            foreach (var tuple in missileDesign.Components)
            {
                EntityManipulation.AddComponentToEntity(newMissile, tuple.design, tuple.count);
            }

            newMissile.GetDataBlob<NewtonThrustAbilityDB>().DeltaV = totalDV;
            
            StorageSpaceProcessor.RemoveCargo(cargo, missileDesign, 1); //remove missile from parent.
        }
    }

    public class ProjectileInfoDB : BaseDataBlob
    {
        public Guid LaunchedBy = new Guid();


        [JsonConstructor]
        private ProjectileInfoDB()
        {
        }

        public ProjectileInfoDB(Guid launchedBy)
        {
            LaunchedBy = launchedBy;
        }

        public override object Clone()
        {
            throw new System.NotImplementedException();
        }
    }
}