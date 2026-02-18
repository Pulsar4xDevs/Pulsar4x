using System;
using Pulsar4X.Orbital;
using Pulsar4X.Interfaces;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Industry;
using Pulsar4X.Factions;
using Pulsar4X.Orbits;
using Pulsar4X.Storage;
using Pulsar4X.Weapons;
using Pulsar4X.Galaxy;
using Pulsar4X.Engine;

namespace Pulsar4X.Movement
{

    public class NewtonionMovementProcessor : IHotloopProcessor
    {
        public struct IntegrationState
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector3 ManuverDeltaV;
            public double Mass;
            public double FuelBurned;
        }

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
            DateTime toDateTime = entity.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            NewtonMove(nmdb, toDateTime);
            MoveStateProcessor.ProcessForType(nmdb, toDateTime);
        }

        public static void ProcessEntity(Entity entity, DateTime toDateTime)
        {
            var nmdb = entity.GetDataBlob<NewtonMoveDB>();
            NewtonMove(nmdb, toDateTime);
            MoveStateProcessor.ProcessForType(nmdb, toDateTime);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            //List<Entity> entites = manager.GetAllEntitiesWithDataBlob<NewtonMoveDB>(_nmDBIdx);
            var nmdb = manager.GetAllDataBlobsOfType<NewtonMoveDB>();

            DateTime toDateTime = manager.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            foreach (var db in nmdb)
            {
                NewtonMove(db, toDateTime);
            }

            MoveStateProcessor.ProcessForType(nmdb, toDateTime);

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
        public static void NewtonMove(NewtonMoveDB newtonMoveDB, DateTime toDateTime)
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
            //DateTime toDateTime = dateTimeNow + TimeSpan.FromSeconds(deltaSeconds);
            double deltaT = (toDateTime - dateTimeFrom).TotalSeconds;

            double sgp = GeneralMath.StandardGravitationalParameter(massTotal_Kg + parentMass_kg);


            double dryMass_Kg = massTotal_Kg - newtonThrust.TotalFuel_kg;

            double secondsToItterate = deltaT;
            while (secondsToItterate > 0)
            {
                double timeStepInSeconds = 1;

                var result = IntegrateOneStep(
                    positionDB.RelativePosition, newtonMoveDB.CurrentVector_ms, newtonMoveDB.ManuverDeltaV,
                    massTotal_Kg, parentMass_kg,
                    newtonThrust.ExhaustVelocity, newtonThrust.FuelBurnRate, dryMass_Kg,
                    timeStepInSeconds);

                positionDB.RelativePosition = result.Position;
                newtonMoveDB.CurrentVector_ms = result.Velocity;
                newtonMoveDB.ManuverDeltaV = result.ManuverDeltaV;

                if (result.FuelBurned > 0)
                {
                    var fuelTypeID = newtonThrust.FuelType;
                    var fuelType = entity.GetFactionCargoDefinitions().GetAny(fuelTypeID);
                    CargoTransferProcessor.AddRemoveCargoMass(entity, fuelType, -result.FuelBurned);
                    massTotal_Kg = entity.GetDataBlob<MassVolumeDB>().MassTotal;
                }

                //update kepler elements from current state after velocity and position are updated
                newtonMoveDB.UpdateKeplerElements();

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
                    else //if (newtonMoveDB.Parent.HasDataBlob<NewtonMoveDB>())
                    {   //this will pretty much never happen.
                        newParent = newtonMoveDB.SOIParent.GetDataBlob<NewtonMoveDB>().SOIParent;
                        var parentVelocity = newtonMoveDB.SOIParent.GetDataBlob<NewtonMoveDB>().CurrentVector_ms;
                        parentrelativeVector = newtonMoveDB.CurrentVector_ms + parentVelocity;
                    }
                    parentMass_kg = newParent.GetDataBlob<MassVolumeDB>().MassDry;

                    Vector3 posrelativeToNewParent = positionDB.AbsolutePosition - newParent.GetDataBlob<PositionDB>().AbsolutePosition;


                    var dateTime = dateTimeNow + TimeSpan.FromSeconds(deltaT - secondsToItterate);
                    //double sgp = GMath.StandardGravitationalParameter(parentMass_kg + mass_Kg);


                    sgp = GeneralMath.StandardGravitationalParameter(massTotal_Kg + parentMass_kg);
                    kE = OrbitMath.KeplerFromPositionAndVelocity(sgp, posrelativeToNewParent, parentrelativeVector, dateTime);
                    positionDB.SetParent(newParent);
                    newtonMoveDB.ParentMass = parentMass_kg;
                    newtonMoveDB.SOIParent = newParent;
                    newtonMoveDB.CurrentVector_ms = parentrelativeVector;
                    newtonMoveDB.UpdateKeplerElements(kE);

                }
                // Check child bodies for SOI entry
                else if (newtonMoveDB.SOIParent.HasDataBlob<OrbitDB>())
                {
                    var currentTime = dateTimeNow + TimeSpan.FromSeconds(deltaT - secondsToItterate);
                    var parentOrbit = newtonMoveDB.SOIParent.GetDataBlob<OrbitDB>();
                    foreach (var child in parentOrbit.Children)
                    {
                        if (child == entity) continue;
                        if (!child.HasDataBlob<OrbitDB>() || !child.HasDataBlob<MassVolumeDB>()) continue;

                        var childSOI = child.GetSOI_m();
                        if (childSOI <= 0 || double.IsInfinity(childSOI)) continue;

                        var childOrbit = child.GetDataBlob<OrbitDB>();
                        var childPos = childOrbit.GetPosition(currentTime);
                        var dist = (positionDB.RelativePosition - childPos).Length();

                        if (dist < childSOI)
                        {
                            // SOI Entry transition
                            var childMass = child.GetDataBlob<MassVolumeDB>().MassDry;
                            var childVel = OrbitMath.InstantaneousOrbitalVelocityVector_m(childOrbit, currentTime);
                            var relPos = positionDB.RelativePosition - childPos;
                            var relVel = newtonMoveDB.CurrentVector_ms - childVel;

                            sgp = GeneralMath.StandardGravitationalParameter(massTotal_Kg + childMass);
                            kE = OrbitMath.KeplerFromPositionAndVelocity(sgp, relPos, relVel, currentTime);

                            positionDB.SetParent(child);
                            newtonMoveDB.SOIParent = child;
                            newtonMoveDB.ParentMass = childMass;
                            newtonMoveDB.CurrentVector_ms = relVel;
                            positionDB.RelativePosition = relPos;
                            newtonMoveDB.UpdateKeplerElements(kE);
                            parentMass_kg = childMass;
                            break;
                        }
                    }
                }

                if (newtonMoveDB.ManuverDeltaV.Length() <= 0) //if we've completed the manuver.
                {
                    var dateTime = dateTimeNow + TimeSpan.FromSeconds(deltaT - secondsToItterate);

                    var parentEntity = positionDB.Parent;
                    if(parentEntity == null) throw new NullReferenceException("parentEntity cannot be null");

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
                    // OrbitDB now handles the trajectory.
                    // For hyperbolic orbits (e >= 1), OrbitDB.OnSetToEntity schedules
                    // a ChangeSOIProcessor interrupt at the exact SOI exit time.
                    break;
                }

                secondsToItterate -= timeStepInSeconds;
            }
            newtonMoveDB.LastProcessDateTime = toDateTime;
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
            double massTotal_Kg = entity.GetDataBlob<MassVolumeDB>().MassTotal;
            double dryMass_Kg = massTotal_Kg - newtonThrust.TotalFuel_kg;
            double parentMass_kg = newtonMoveDB.ParentMass;

            Vector3 position = positionDB.RelativePosition;
            Vector3 velocity = newtonMoveDB.CurrentVector_ms;
            Vector3 manuverDeltaV = newtonMoveDB.ManuverDeltaV;
            double mass = massTotal_Kg;

            double secondsToItterate = (atDateTime - entity.StarSysDateTime).TotalSeconds;
            while (secondsToItterate > 0)
            {
                double timeStep = 1;

                var result = IntegrateOneStep(
                    position, velocity, manuverDeltaV,
                    mass, parentMass_kg,
                    newtonThrust.ExhaustVelocity, newtonThrust.FuelBurnRate, dryMass_Kg,
                    timeStep);

                position = result.Position;
                velocity = result.Velocity;
                manuverDeltaV = result.ManuverDeltaV;
                mass = result.Mass;

                secondsToItterate -= timeStep;
            }

            return (position, velocity);
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
            double massTotal_Kg = entity.GetDataBlob<MassVolumeDB>().MassTotal;
            double dryMass_Kg = massTotal_Kg - newtonThrust.TotalFuel_kg;
            double parentMass_kg = newtonMoveDB.ParentMass;

            Vector3 newAbsolute = positionDB.AbsolutePosition;
            Vector3 position = positionDB.RelativePosition;
            Vector3 velocity = newtonMoveDB.CurrentVector_ms;
            Vector3 manuverDeltaV = newtonMoveDB.ManuverDeltaV;
            double mass = massTotal_Kg;

            double secondsToItterate = (atDateTime - entity.StarSysDateTime).TotalSeconds;
            while (secondsToItterate > 0)
            {
                double timeStep = 1;

                Vector3 oldPosition = position;

                var result = IntegrateOneStep(
                    position, velocity, manuverDeltaV,
                    mass, parentMass_kg,
                    newtonThrust.ExhaustVelocity, newtonThrust.FuelBurnRate, dryMass_Kg,
                    timeStep);

                Vector3 deltaPos = result.Position - oldPosition;
                newAbsolute += deltaPos;

                position = result.Position;
                velocity = result.Velocity;
                manuverDeltaV = result.ManuverDeltaV;
                mass = result.Mass;

                secondsToItterate -= timeStep;
            }

            return (newAbsolute, velocity);
        }

        /// <summary>
        /// Pure-function integration step shared by NewtonMove and prediction functions.
        /// Computes gravity, thrust (with Tsiolkovsky fuel model), and trapezoidal position integration.
        /// </summary>
        public static IntegrationState IntegrateOneStep(
            Vector3 position, Vector3 velocity, Vector3 manuverDeltaV,
            double mass, double parentMass,
            double exhaustVelocity, double fuelBurnRate, double dryMass,
            double timeStep)
        {
            double distanceToParent_m = position.Length();
            distanceToParent_m = Math.Max(distanceToParent_m, 0.1);

            double gravForce = UniversalConstants.Science.GravitationalConstant * (mass * parentMass / Math.Pow(distanceToParent_m, 2));
            Vector3 gravForceVector = gravForce * -Vector3.Normalise(position);
            Vector3 totalDVFromGrav = (gravForceVector / mass) * timeStep;

            Vector3 totalDVFromThrust = new Vector3(0, 0, 0);
            double fuelBurned = 0;

            if (manuverDeltaV.Length() > 0)
            {
                double afterBurnMass = mass - fuelBurnRate * timeStep;
                double dvThisStep = OrbitMath.TsiolkovskyRocketEquation(mass, afterBurnMass, exhaustVelocity);
                dvThisStep = Math.Min(manuverDeltaV.Length(), dvThisStep);

                double availableDV = OrbitMath.TsiolkovskyRocketEquation(mass, dryMass, exhaustVelocity);
                dvThisStep = Math.Min(availableDV, dvThisStep);

                totalDVFromThrust = Vector3.Normalise(manuverDeltaV) * dvThisStep;

                fuelBurned = OrbitMath.TsiolkovskyFuelUse(mass, exhaustVelocity, dvThisStep);
                manuverDeltaV -= totalDVFromThrust;
                mass -= fuelBurned;
            }

            Vector3 totalDV = totalDVFromGrav + totalDVFromThrust;
            Vector3 newVelocity = totalDV + velocity;
            Vector3 deltaPos = (velocity + newVelocity) / 2 * timeStep;

            return new IntegrationState
            {
                Position = position + deltaPos,
                Velocity = newVelocity,
                ManuverDeltaV = manuverDeltaV,
                Mass = mass,
                FuelBurned = fuelBurned,
            };
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
            if(parentEntity.HasDataBlob<CargoStorageDB>())
            {
                var cargo = parentEntity.GetDataBlob<CargoStorageDB>();
                fuelMass = cargo.GetMassStored(fuel, false);
            }
            db.SetFuel(fuelMass, totalMass);
        }
    }
}
