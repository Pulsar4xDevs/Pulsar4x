using System;
using System.Collections.Generic;
using GameEngine.Movement;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Industry;

namespace Pulsar4X.Engine
{

    public class NewtonionMovementProcessor : IHotloopProcessor
    {
        public NewtonionMovementProcessor()
        {
        }

        public TimeSpan RunFrequency => TimeSpan.FromSeconds(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromSeconds(0);

        public Type GetParameterType => typeof(NewtonMoveDB);

        public void Init(Game game)
        {

        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            var nmdb = entity.GetDataBlob<NewtonMoveDB>();
            NewtonMove(nmdb, deltaSeconds);
            DateTime todateTime = entity.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            MoveStateProcessor.ProcessForType(nmdb, todateTime);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            //List<Entity> entites = manager.GetAllEntitiesWithDataBlob<NewtonMoveDB>(_nmDBIdx);
            var nmdb = manager.GetAllDataBlobsOfType<NewtonMoveDB>();
            foreach (var db in nmdb)
            {
                NewtonMove(db, deltaSeconds);
            }
            DateTime todateTime = manager.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            MoveStateProcessor.ProcessForType(nmdb, todateTime);

            return nmdb.Count;
        }

        /// <summary>
        /// This was designed so that fast moving objects will get interpolated a lot more than slow moving objects
        /// so fast moving objects shouldn't loose positional acuracy when close to a planet,
        /// and slow moving objects won't have processor time wasted on them by calulcating too often.
        /// However this seems to be unstable and looses energy, unsure why. currently set it to just itterate/interpolate every second.
        /// so currently will be using more time to get through this than neccisary.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="deltaSeconds">Delta seconds.</param>
        public static void NewtonMove(NewtonMoveDB newtonMoveDB, int deltaSeconds)
        {
            var entity = newtonMoveDB.OwningEntity;
            //NewtonMoveDB newtonMoveDB = entity.GetDataBlob<NewtonMoveDB>();
            var factionDataStore = entity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data;
            NewtonThrustAbilityDB newtonThrust = entity.GetDataBlob<NewtonThrustAbilityDB>();
            PositionDB positionDB = entity.GetDataBlob<PositionDB>();
            double massTotal_Kg = entity.GetDataBlob<MassVolumeDB>().MassTotal;
            double parentMass_kg = newtonMoveDB.ParentMass;

            var manager = entity.Manager;
            DateTime dateTimeFrom = newtonMoveDB.LastProcessDateTime;
            DateTime dateTimeNow = manager.ManagerSubpulses.StarSysDateTime;
            DateTime dateTimeFuture = dateTimeNow + TimeSpan.FromSeconds(deltaSeconds);
            double deltaT = (dateTimeFuture - dateTimeFrom).TotalSeconds;

            double sgp = GeneralMath.StandardGravitationalParameter(massTotal_Kg + parentMass_kg);


            double secondsToItterate = deltaT;
            while (secondsToItterate > 0)
            {


                //double timeStep = Math.Max(secondsToItterate / speed_kms, 1);
                //timeStep = Math.Min(timeStep, secondsToItterate);
                double timeStepInSeconds = 1;//because the above seems unstable and looses energy.
                double distanceToParent_m = positionDB.GetDistanceTo_m(newtonMoveDB.SOIParent.GetDataBlob<PositionDB>());

                distanceToParent_m = Math.Max(distanceToParent_m, 0.1); //don't let the distance be 0 (once collision is in this will likely never happen anyway)

                double gravForce = UniversalConstants.Science.GravitationalConstant * (massTotal_Kg * parentMass_kg / Math.Pow(distanceToParent_m, 2));
                Vector3 gravForceVector = gravForce * -Vector3.Normalise(positionDB.RelativePosition);

                Vector3 totalDVFromGrav = (gravForceVector / massTotal_Kg) * timeStepInSeconds;

                //double maxAccelFromThrust1 = newtonThrust.ExhaustVelocity * Math.Log(mass_Kg / (mass_Kg - newtonThrust.FuelBurnRate));//per second
                //double maxAccelFromThrust = newtonThrust.ThrustInNewtons / mass_Kg; //per second


                Vector3 manuverDV = newtonMoveDB.ManuverDeltaV; //how much dv needed to complete the manuver.
                Vector3 totalDVFromThrust = new Vector3(0,0,0);



                if(manuverDV.Length() > 0)
                {
                    double dryMass = massTotal_Kg - newtonThrust.FuelBurnRate * timeStepInSeconds; //how much our ship weighs after a timestep of fuel is used.
                    //how much dv can we get in this timestep.
                    double deltaVThisStep = OrbitMath.TsiolkovskyRocketEquation(massTotal_Kg, dryMass, newtonThrust.ExhaustVelocity);
                    deltaVThisStep = Math.Min(manuverDV.Length(), deltaVThisStep); //don't use more Dv than what is called for.
                    deltaVThisStep = Math.Min(newtonThrust.DeltaV, deltaVThisStep); //check we've got the deltaV to spend.

                    totalDVFromThrust = Vector3.Normalise(manuverDV) * deltaVThisStep;

                    //remove the fuel from the ship.
                    double kgOfFuel = OrbitMath.TsiolkovskyFuelUse(massTotal_Kg, newtonThrust.ExhaustVelocity, deltaVThisStep);
                    var fuelTypeID = newtonThrust.FuelType;
                    var fuelType = entity.GetFactionCargoDefinitions().GetAny(fuelTypeID);
                    var massRemoved = CargoTransferProcessor.AddRemoveCargoMass(entity, fuelType, -kgOfFuel);

                    
                    
                    //convert prograde to global frame of reference for thrust direction
                    //Vector3 globalCoordDVFromThrust = OrbitMath.ProgradeToStateVector(sgp, totalDVFromThrust,
                    //    positionDB.RelativePosition_m,
                    //    newtonMoveDB.CurrentVector_ms);

                    //remove the vectorDV from the amount needed to fully complete the manuver.
                    newtonMoveDB.ManuverDeltaV -= totalDVFromThrust;
                    newtonMoveDB.UpdateKeplerElements();
                    //newtonMoveDB.DeltaVForManuver_FoRO_m -= totalDVFromThrust;



                }






                Vector3 totalDV = totalDVFromGrav + totalDVFromThrust;
                Vector3 newVelocity = totalDV + newtonMoveDB.CurrentVector_ms;

                newtonMoveDB.CurrentVector_ms = newVelocity;
                Vector3 deltaPos = (newtonMoveDB.CurrentVector_ms + newVelocity) / 2 * timeStepInSeconds;

                positionDB.RelativePosition += deltaPos;

                double sOIRadius = newtonMoveDB.SOIParent.GetSOI_m();
                var kE = newtonMoveDB.GetElements();

                if (positionDB.RelativePosition.Length() >= sOIRadius)
                {
                    Entity? newParent;
                    Vector3 parentrelativeVector;
                    //if our parent is a regular kepler object (normaly this is the case)
                    if (newtonMoveDB.SOIParent.HasDataBlob<OrbitDB>())
                    {
                        var orbitDB = newtonMoveDB.SOIParent.GetDataBlob<OrbitDB>();
                        newParent = orbitDB.Parent;
                        if(newParent == null) throw new NullReferenceException("newParent cannot be null");
                        var parentVelocity = orbitDB.InstantaneousOrbitalVelocityVector_m(entity.StarSysDateTime);
                        parentrelativeVector = newtonMoveDB.CurrentVector_ms + parentVelocity;

                    }
                    else //if (newtonMoveDB.SOIParent.HasDataBlob<NewtonMoveDB>())
                    {   //this will pretty much never happen.
                        newParent = newtonMoveDB.SOIParent.GetDataBlob<NewtonMoveDB>().SOIParent;
                        var parentVelocity = newtonMoveDB.SOIParent.GetDataBlob<NewtonMoveDB>().CurrentVector_ms;
                        parentrelativeVector = newtonMoveDB.CurrentVector_ms + parentVelocity;
                    }
                    parentMass_kg = newParent.GetDataBlob<MassVolumeDB>().MassDry;

                    Vector3 posrelativeToNewParent = positionDB.AbsolutePosition - newParent.GetDataBlob<PositionDB>().AbsolutePosition;


                    var dateTime = dateTimeNow + TimeSpan.FromSeconds(deltaSeconds - secondsToItterate);
                    //double sgp = GMath.StandardGravitationalParameter(parentMass_kg + mass_Kg);


                    kE = OrbitMath.KeplerFromPositionAndVelocity(sgp, posrelativeToNewParent, parentrelativeVector, dateTime);
                    positionDB.SetParent(newParent);
                    newtonMoveDB.ParentMass = parentMass_kg;
                    newtonMoveDB.SOIParent = newParent;
                    newtonMoveDB.CurrentVector_ms = parentrelativeVector;
                    newtonMoveDB.UpdateKeplerElements(kE);

                }

                if (newtonMoveDB.ManuverDeltaV.Length() <= 0) //if we've completed the manuver.
                {
                    var dateTime = dateTimeNow + TimeSpan.FromSeconds(deltaSeconds - secondsToItterate);
                    //double sgp = GMath.StandardGravitationalParameter(parentMass_kg + mass_Kg);

                    //something funky with the below. though it may be just float differences, not sure.
                    /*
#if DEBUG
                    var kE2 = OrbitMath.KeplerFromPositionAndVelocity(sgp, positionDB.RelativePosition_m, newtonMoveDB.CurrentVector_ms, dateTime);
                    if (kE.Eccentricity != kE2.Eccentricity)
                        throw new Exception("Old Elements Exception - eccentricity has changed and newtonMoveDB.UpdateKeplerElements() has not been called");
#endif
                    */
                    var parentEntity = entity.GetSOIParentEntity(positionDB);
                    if(parentEntity == null) throw new NullReferenceException("parentEntity cannot be null");

                    if (kE.Eccentricity < 1) //if we're going to end up in a regular orbit around our new parent
                    {
                        if (entity.HasDataBlob<ProjectileInfoDB>()) //this feels a bit hacky.
                        {
                            var newOrbit = OrbitDB.FromKeplerElements(parentEntity, massTotal_Kg, kE, dateTime);
                            var fastOrbit = new OrbitUpdateOftenDB(newOrbit);
                            positionDB.SetParent(parentEntity);
                            entity.SetDataBlob(fastOrbit);
                            var newPos = fastOrbit.GetPosition(dateTime);
                            positionDB.RelativePosition = newPos;
                        }
                        else
                        {
                            var newOrbit = OrbitDB.FromKeplerElements(parentEntity, massTotal_Kg, kE, dateTime);
                            positionDB.SetParent(parentEntity);
                            entity.SetDataBlob(newOrbit);
                            var newPos = newOrbit.GetPosition(dateTime);
                            positionDB.RelativePosition = newPos;
                        }

                    }
                    break;

                }

                secondsToItterate -= timeStepInSeconds;
            }
            newtonMoveDB.LastProcessDateTime = dateTimeFuture;
        }

        /// <summary>
        /// Gets the relative(To SOI parent) position and velocity for a given datetime.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="newtonMoveDB"></param>
        /// <param name="atDateTime"></param>
        /// <returns>Positional and Velocity states</returns>
        public static (Vector3 pos, Vector3 vel)GetRelativeState(Entity entity, NewtonMoveDB newtonMoveDB, DateTime atDateTime)
        {
            PositionDB positionDB = entity.GetDataBlob<PositionDB>();
            NewtonThrustAbilityDB newtonThrust = entity.GetDataBlob<NewtonThrustAbilityDB>();
            DateTime dateTimeNow = entity.StarSysDateTime;
            TimeSpan timeDelta = atDateTime - dateTimeNow;
            double mass_Kg = entity.GetDataBlob<MassVolumeDB>().MassDry;
            double parentMass_kg = newtonMoveDB.ParentMass;

            Vector3 newrelative = positionDB.RelativePosition;
            Vector3 velocity = newtonMoveDB.CurrentVector_ms;

            double secondsToItterate = timeDelta.TotalSeconds;
            while (secondsToItterate > 0)
            {
                //double timeStep = Math.Max(secondsToItterate / speed_kms, 1);
                //timeStep = Math.Min(timeStep, secondsToItterate);
                double timeStep = 1;//because the above seems unstable and looses energy.
                double distanceToParent_m = positionDB.GetDistanceTo_m(newtonMoveDB.SOIParent.GetDataBlob<PositionDB>());

                distanceToParent_m = Math.Max(distanceToParent_m, 0.1); //don't let the distance be 0 (once collision is in this will likely never happen anyway)

                double gravForce = UniversalConstants.Science.GravitationalConstant * (mass_Kg * parentMass_kg / Math.Pow(distanceToParent_m, 2));
                Vector3 gravForceVector = gravForce * -Vector3.Normalise(positionDB.RelativePosition);

                Vector3 acceleratonFromGrav = gravForceVector / mass_Kg;

                double maxAccelFromThrust1 = newtonThrust.ExhaustVelocity * Math.Log(mass_Kg / (mass_Kg - newtonThrust.FuelBurnRate));//per second
                double maxAccelFromThrust = newtonThrust.ThrustInNewtons / mass_Kg; //per second

                //ohhh was this wrong before? which frame of reference should we be in here? parent ralitive or prograde ralitive?
                Vector3 accelerationFromThrust = newtonMoveDB.ManuverDeltaV / maxAccelFromThrust; //per second

                Vector3 accelerationTotal = acceleratonFromGrav + accelerationFromThrust;

                Vector3 newVelocity = (accelerationTotal * timeStep) + velocity;


                velocity = newVelocity;
                Vector3 deltaPos = (velocity + newVelocity) / 2 * timeStep; //we calculate the position using the average velocity between the start and end of the delta time.

                 newrelative += deltaPos;

                secondsToItterate -= timeStep;
            }

            return (newrelative, velocity);
        }

        /// <summary>
        /// Gets the absolute(global) position and velocity for a given datetime
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="newtonMoveDB"></param>
        /// <param name="atDateTime"></param>
        /// <returns>Positional and Velocity states</returns>
        public static (Vector3 pos, Vector3 vel) GetAbsoluteState(Entity entity, NewtonMoveDB newtonMoveDB, DateTime atDateTime)
        {
            PositionDB positionDB = entity.GetDataBlob<PositionDB>();
            NewtonThrustAbilityDB newtonThrust = entity.GetDataBlob<NewtonThrustAbilityDB>();
            DateTime dateTimeNow = entity.StarSysDateTime;
            TimeSpan timeDelta = atDateTime - dateTimeNow;
            double mass_Kg = entity.GetDataBlob<MassVolumeDB>().MassDry;
            double parentMass_kg = newtonMoveDB.ParentMass;

            Vector3 newAbsolute = positionDB.AbsolutePosition;
            Vector3 velocity = newtonMoveDB.CurrentVector_ms;

            double secondsToItterate = timeDelta.TotalSeconds;
            while (secondsToItterate > 0)
            {
                //double timeStep = Math.Max(secondsToItterate / speed_kms, 1);
                //timeStep = Math.Min(timeStep, secondsToItterate);
                double timeStep = 1;//because the above seems unstable and looses energy.
                double distanceToParent_m = positionDB.GetDistanceTo_m(newtonMoveDB.SOIParent.GetDataBlob<PositionDB>());

                distanceToParent_m = Math.Max(distanceToParent_m, 0.1); //don't let the distance be 0 (once collision is in this will likely never happen anyway)

                double gravForce = UniversalConstants.Science.GravitationalConstant * (mass_Kg * parentMass_kg / Math.Pow(distanceToParent_m, 2));
                Vector3 gravForceVector = gravForce * -Vector3.Normalise(positionDB.RelativePosition);

                Vector3 acceleratonFromGrav = gravForceVector / mass_Kg;

                double maxAccelFromThrust1 = newtonThrust.ExhaustVelocity * Math.Log(mass_Kg / (mass_Kg - newtonThrust.FuelBurnRate));//per second
                double maxAccelFromThrust = newtonThrust.ThrustInNewtons / mass_Kg; //per second
                Vector3 accelerationFromThrust = newtonMoveDB.ManuverDeltaV / maxAccelFromThrust; //per second

                Vector3 accelerationTotal = acceleratonFromGrav + accelerationFromThrust;

                Vector3 newVelocity = (accelerationTotal * timeStep) + velocity;


                velocity = newVelocity;
                Vector3 deltaPos = (velocity + newVelocity) / 2 * timeStep;

                 newAbsolute += deltaPos;

                secondsToItterate -= timeStep;
            }

            return (newAbsolute, velocity);
        }

        /// <summary>
        /// calculates, sets and returns DV.
        /// </summary>
        /// <param name="parentEntity"></param>
        /// <returns></returns>
        public static void UpdateNewtonThrustAbilityDB(Entity parentEntity)
        {
            var factionDataStore = parentEntity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data;
            var db = parentEntity.GetDataBlob<NewtonThrustAbilityDB>();
            var ft = db.FuelType;
            var ev = db.ExhaustVelocity;
            var totalMass = parentEntity.GetDataBlob<MassVolumeDB>().MassTotal;
            ProcessedMaterial fuel = factionDataStore.CargoGoods.GetMaterial(ft);

            double fuelMass = 0;
            if(parentEntity.HasDataBlob<VolumeStorageDB>())
            {
                var cargo = parentEntity.GetDataBlob<VolumeStorageDB>();
                fuelMass = cargo.GetMassStored(fuel);
            }
            db.SetFuel(fuelMass, totalMass);
        }
    }
}
