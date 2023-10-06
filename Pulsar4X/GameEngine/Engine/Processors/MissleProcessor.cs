using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Orbital;
using Pulsar4X.Engine.Designs;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Engine
{
    public class MissileProcessor
    {
        public static void LaunchMissile(Entity launchingEntity, Entity targetEntity, double launchForce, OrdnanceDesign missileDesign, int count)
        {

            var atDatetime = launchingEntity.Manager.StarSysDateTime;
            var parentPositionDB = launchingEntity.GetDataBlob<PositionDB>();
            Vector3 parentPosition = parentPositionDB.AbsolutePosition;
            var parentPosRal = parentPositionDB.RelativePosition;
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
            dataBlobs.Add(new ProjectileInfoDB(launchingEntity.Id, count));
            dataBlobs.Add(new ComponentInstancesDB());
            dataBlobs.Add(misslPositionDB);
            dataBlobs.Add(MassVolumeDB.NewFromMassAndVolume(missileDesign.WetMass, missileDesign.WetMass));
            dataBlobs.Add(new NameDB(defaultName, launchingEntity.FactionOwnerID,  factionsName));
            dataBlobs.Add(newtmovedb);
            dataBlobs.Add(orderabledb);
            var newMissile = Entity.Create();
            newMissile.FactionOwnerID = launchingEntity.FactionOwnerID;
            launchingEntity.Manager.AddEntity(newMissile, dataBlobs);

            foreach (var tuple in missileDesign.Components)
            {
                newMissile.AddComponent(tuple.design, tuple.count);
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
                var cargoLibrary = newMissile.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
                NewtonThrustCommand.CreateCommands(cargoLibrary, newMissile, manuvers);
            }
            CargoTransferProcessor.RemoveCargoItems(launchingEntity, missileDesign, 1);//remove missile from parent.
        }
    }
}