using System.Collections.Generic;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Missiles
{
    public class MissileProcessor
    {
        public static void LaunchMissile(Entity launchingEntity, Entity targetEntity, double launchForce, OrdnanceDesign missileDesign)
        {

            var atDatetime = launchingEntity.Manager.StarSysDateTime;
            var parentPositionDB = launchingEntity.GetDataBlob<PositionDB>();
            Vector3 parentPosition = parentPositionDB.AbsolutePosition_m;
            
            var tgtEntityOrbit = targetEntity.GetDataBlob<OrbitDB>();
            
            //MissileLauncherAtb launcherAtb;
            CargoStorageDB cargo = launchingEntity.GetDataBlob<CargoStorageDB>();
            int numMis = (int)StorageSpaceProcessor.GetAmount(cargo, missileDesign);
            if (numMis < 1)
                return;
            double launchSpeed = launchForce / missileDesign.Mass;
            
            //missileDesign.

            double burnTime = (missileDesign.WetMass - missileDesign.DryMass) / missileDesign.BurnRate;
            double dv = OrbitMath.TsiolkovskyRocketEquation(missileDesign.WetMass, missileDesign.DryMass, missileDesign.ExaustVelocity);
            double avgSpd = launchSpeed + dv * 0.5;
            
            var tgtEstPos = OrbitMath.GetInterceptPosition_m(parentPosition, avgSpd, tgtEntityOrbit, atDatetime);
            
            Vector3 parentVelocity = Entity.GetVelocity_m(launchingEntity, launchingEntity.Manager.StarSysDateTime);

            Vector3 tgtEstVector = Vector3.Normalise( tgtEstPos.position - parentPosition); //normalised vector to predicted position
            
            Vector3 launchVelocity = parentVelocity + (tgtEstVector * launchSpeed);
            
            
            var misslPositionDB = (PositionDB)parentPositionDB.Clone();
            var newtmovedb = new NewtonMoveDB(misslPositionDB.Parent, launchVelocity);
            newtmovedb.ActionOnDateTime = atDatetime;
            newtmovedb.DeltaVForManuver_m = Vector3.Normalise(tgtEstPos.position) * dv;
            
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();
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

            newMissile.GetDataBlob<NewtonThrustAbilityDB>().DeltaV = dv;
            
            StorageSpaceProcessor.RemoveCargo(cargo, missileDesign, 1); //remove missile from parent.
        }
    }
}