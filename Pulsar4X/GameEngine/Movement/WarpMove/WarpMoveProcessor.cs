using System;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;
using Pulsar4X.Energy;
using Pulsar4X.Orbits;
using Pulsar4X.Galaxy;
using Pulsar4X.Engine;

namespace Pulsar4X.Movement
{
    /// <summary>
    /// Translate move processor.
    ///
    ///
    /// Non Newtonion Movement/Translation
    /// Rules:
    /// (Eventualy)
    /// An entry point and an exit point for translation is defined.
    /// Ships newtonion velocity is stored at the translation entry point.
    /// Ship enters a non newtonion translation state
    /// in this state, the ship is unaffected by it's previous newtonion vector & gravity
    /// Acceleration is instant.
    /// Speed is shown relative to the parent star.
    /// Cannot change its direction or speed untill exit.**
    /// An exit should be able to be forced prematurly, but this should come at a cost.
    /// An exit should be able to be forced by outside (enemy) forces. *
    /// Possibly the cost should be handeled by having entering the translation state
    ///     be expensive, while the travel distance/speed is ralitivly cheap.
    ///
    /// On Exit, the saved newtonion vector is given back to the ship
    ///   if the exit point and velocity does not give the required orbit
    ///   then DeltaV (normal newtonion movement) will be expended to get to that orbit.
    ///
    /// Cost of translation TBD, either special fuel and/or energy requiring reactor fuel + capacitors/batteries
    /// Exit position accuracy should be a factor of tech and skill.
    /// Max Speed should be a factor of engine power and mass of the ship. (as it is currently)
    ///   Engine Power should be a factor of engine size/design etc and tech.
    /// Cost should be a factor of tech. (& maybe skill to a small degree?)
    ///
    /// *(todo think of gameplay mechanic, anti ftl missiles?
    ///   I feel that normal combat shouldn't take place within translation state,
    ///   but this could make combat difficult to code).
    ///
    ///
    /// I considered tying the non-newtonion speed vector to actual still space,
    /// but finding how fast the sun is actualy moving proved difficult,
    /// many websites just added speeds of galaxy + solarsystem together and ignored the relative vectors.
    /// one site I found sugested 368 ± 2 km/s
    /// this might not be terrible, however if we gave max speeds of that number,
    /// we'd be able to travel 368 km/s in one direction, and none in the oposite direction.
    /// so we'd need to give max speeds of more than that, and/or force homman transfers in one direction.
    /// could provide an interesting gameplay mechanic...
    ///
    /// **
    ///NB I've alowed ships to come to zero speed warp when serveying a jump point grav anomaly, since these are still in space.
    /// this may cause some problems we will have to see how it plays out.
    /// </summary>
    public class WarpMoveProcessor : IHotloopProcessor
    {
        private static GameSettings _gameSettings;

        public TimeSpan RunFrequency => TimeSpan.FromMinutes(5);

        public TimeSpan FirstRunOffset => TimeSpan.FromMinutes(0);

        public Type GetParameterType => typeof(WarpMovingDB);

        public void Init(Game game)
        {
            _gameSettings = game.Settings;
        }



        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var datablobs = manager.GetAllDataBlobsOfType<WarpMovingDB>();
            DateTime todateTime = manager.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            foreach (var db in datablobs)
            {
                WarpMove(db.OwningEntity, db, todateTime);
            }
            MoveStateProcessor.ProcessForType(datablobs, todateTime);
            return datablobs.Count;
        }



        /// <summary>
        /// Moves an entity while it's in a non newtonion translation state.
        /// </summary>
        /// <param name="entity">Entity.</param>
        /// <param name="deltaSeconds">Unused</param>
        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            var db = entity.GetDataBlob<WarpMovingDB>();
            DateTime toDateTime = entity.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            WarpMove(entity, db, toDateTime);
            MoveStateProcessor.ProcessForType(db, toDateTime);
        }

        public static void ProcessEntity(Entity entity, DateTime toDateTime)
        {
            var db = entity.GetDataBlob<WarpMovingDB>();
            WarpMove(entity, db, toDateTime);
            MoveStateProcessor.ProcessForType(db, toDateTime);
        }

        public static void WarpMove(Entity entity, WarpMovingDB moveDB,  DateTime toDateTime)
        {
            if (moveDB.HasStarted || StartNonNewtTranslation(entity))
            {
                var warpDB = entity.GetDataBlob<WarpAbilityDB>();

                var currentVelocityMS = moveDB.CurrentNonNewtonionVectorMS;
                DateTime dateTimeFrom = moveDB.LastProcessDateTime;

                double deltaT = (toDateTime - dateTimeFrom).TotalSeconds;

                Vector3 targetPosMt = moveDB.ExitPointAbsolute;

                var newPositionMt = moveDB._position + (Vector2)currentVelocityMS * deltaT;

                double distanceToMove = ( moveDB._position - newPositionMt).Length();
                double distanceToTargetMt = (moveDB._position - (Vector2)targetPosMt).Length();

                if (distanceToTargetMt <= distanceToMove) // moving would overtake target, just go directly to target
                {
                    moveDB._parentEnitity = moveDB.TargetEntity;
                    moveDB._position = (Vector2)moveDB.ExitPointrelative;
                    var destinationMoveType = moveDB.TargetEntity.GetDataBlob<PositionDB>().MoveType;
                    moveDB.IsAtTarget = true;
                    //if our destination is a non moving object eg a grav anomaly or jump point.
                    if(destinationMoveType == PositionDB.MoveTypes.None)
                        moveDB.CurrentNonNewtonionVectorMS = Vector3.Zero;
                    else
                        EndWarpMove(entity, warpDB, moveDB, toDateTime);
                }
                else
                {
                    moveDB._position = newPositionMt;
                }


                moveDB.LastProcessDateTime = toDateTime;
            }
        }

        public static bool StartNonNewtTranslation(Entity entity)
        {

            var warpDB = entity.GetDataBlob<WarpAbilityDB>();
            var positionDB = entity.GetDataBlob<PositionDB>();
            var maxSpeedMS = warpDB.MaxSpeed;
            var powerDB = entity.GetDataBlob<EnergyGenAbilityDB>();
            EnergyGenProcessor.EnergyGen(entity, entity.StarSysDateTime);
            
            // Check to make sure we don't set the position parent to itself
            if(positionDB.Parent != positionDB.Root)
                positionDB.SetParent(positionDB.Root);

            Vector3 currentPositionMt = positionDB.AbsolutePosition;


            var moveDB = entity.GetDataBlob<WarpMovingDB>();
            var tgt = moveDB.TargetEntity.GetDataBlob<PositionDB>();
            var tgtpos = tgt.AbsolutePosition;
            moveDB._position = (Vector2)positionDB.AbsolutePosition;
            Vector3 targetPosMt = moveDB.ExitPointAbsolute;
            Vector3 postionDelta = currentPositionMt - targetPosMt;
            double totalDistance = postionDelta.Length();

            var creationCost = warpDB.BubbleCreationCost;
            var t = totalDistance / warpDB.MaxSpeed;
            var tcost = t * warpDB.BubbleSustainCost;
            double estored = powerDB.EnergyStored[warpDB.EnergyType];
            bool canStart = false;
            if (creationCost <= estored)
            {

                var currentVelocityMS = Vector3.Normalise(targetPosMt - currentPositionMt) * maxSpeedMS;
                var speed = currentVelocityMS.Length();
                moveDB.CurrentNonNewtonionVectorMS = currentVelocityMS;
                moveDB.LastProcessDateTime = entity.StarSysDateTime;

                //estore = (estore.stored - creationCost, estore.maxStore);
                powerDB.AddDemand(creationCost, entity.StarSysDateTime);
                powerDB.AddDemand(-creationCost, entity.StarSysDateTime + TimeSpan.FromSeconds(1));
                powerDB.AddDemand(warpDB.BubbleSustainCost, entity.StarSysDateTime + TimeSpan.FromSeconds(1));
                //powerDB.EnergyStore[warpDB.EnergyType] = estore;
                moveDB.HasStarted = true;
                canStart = true;
            }

            return canStart;
        }


        static void EndWarpMove(Entity entity, WarpAbilityDB warpDB, WarpMovingDB moveDB,  DateTime toDateTime)
        {
            var powerDB = entity.GetDataBlob<EnergyGenAbilityDB>();



            powerDB.AddDemand(warpDB.BubbleCollapseCost, entity.StarSysDateTime);
            powerDB.AddDemand(-warpDB.BubbleSustainCost, entity.StarSysDateTime);
            powerDB.AddDemand(-warpDB.BubbleCollapseCost, entity.StarSysDateTime + TimeSpan.FromSeconds(1));

            var destinationMoveType = moveDB.TargetEntity.GetDataBlob<PositionDB>().MoveType;

            switch (destinationMoveType)
            {
                case PositionDB.MoveTypes.None:
                {
                    //if our destination is a non moving object eg a grav anomaly or jump point.
                    //this case should be handled prior to this.
                    throw new Exception("shouldn't get here");
                    break;
                }
                case PositionDB.MoveTypes.Orbit:
                {
                    entity.RemoveDataBlob<WarpMovingDB>();
                    if (_gameSettings.StrictNewtonion)
                        SetOrbitHereSimpleNewt(entity, moveDB, toDateTime);
                    else
                        SetOrbitHereNoNewt(entity, moveDB, toDateTime);
                    break;
                }
                case PositionDB.MoveTypes.NewtonSimple:
                {
                    throw new NotImplementedException();
                    break;
                }
                case PositionDB.MoveTypes.NewtonComplex:
                {
                    throw new NotImplementedException();
                    break;
                }
                case PositionDB.MoveTypes.Warp:
                {
                    var targetSpeed = moveDB.TargetEntity.GetDataBlob<WarpMovingDB>().CurrentNonNewtonionVectorMS;
                    var newspeed = Math.Min(targetSpeed.Length(), warpDB.MaxSpeed);
                    moveDB.CurrentNonNewtonionVectorMS = Vector3.Normalise(targetSpeed) * newspeed;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }


        /// <summary>
        /// Sets a circular orbit without newtonion movement or fuel use.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="positionDB"></param>
        /// <param name="moveDB"></param>
        /// <param name="atDateTime"></param>
        /// <exception cref="NullReferenceException"></exception>
        static void SetOrbitHereNoNewt(Entity entity, WarpMovingDB moveDB, DateTime atDateTime)
        {
            if(moveDB.TargetEntity == null) throw new NullReferenceException("moveDB.TargetEntity cannot be null");

            PositionDB moveStatedb = entity.GetDataBlob<PositionDB>();

            double targetSOI = moveDB.TargetEntity.GetSOI_m();

            Entity? targetEntity;

            if (moveDB.TargetEntity.GetDataBlob<PositionDB>().GetDistanceTo_m(moveStatedb) > targetSOI)
            {
                targetEntity = moveDB.TargetEntity.GetDataBlob<OrbitDB>().Parent; //TODO: it's concevable we could be in another SOI not the parent (ie we could be in a target's moon's SOI)
            }
            else
            {
                targetEntity = moveDB.TargetEntity;
            }

            if(targetEntity == null) throw new NullReferenceException("targetEntity cannot be null");

            //just chuck it in a circular orbit.
            OrbitDB newOrbit = OrbitDB.FromPosition(targetEntity, entity, atDateTime);
            entity.SetDataBlob(newOrbit);
            moveStatedb.SetParent(targetEntity);
            moveDB.IsAtTarget = true;

        }

        static void SetOrbitHereSimpleNewt(Entity entity, WarpMovingDB moveDB, DateTime atDateTime)
        {
            var newOrbit = moveDB.EndpointTargetOrbit;
            var mass = moveDB.TargetEntity.GetDataBlob<MassVolumeDB>().MassTotal;
            mass += entity.GetDataBlob<MassVolumeDB>().MassTotal;
            var sgp = GeneralMath.StandardGravitationalParameter(mass);

            var currentOrbit = OrbitMath.KeplerFromPositionAndVelocity(sgp, moveDB.ExitPointrelative, moveDB.SavedNewtonionVector, atDateTime);

            var target = moveDB.TargetEntity;
            NewtonSimpleMoveDB newtMove = new NewtonSimpleMoveDB(target, currentOrbit, newOrbit, atDateTime);
            entity.SetDataBlob(newtMove);
            NewtonSimpleProcessor.ProcessEntity(entity, atDateTime);

        }

        /// <summary>
        /// Sets an orbit using full newtonion movement and fuel use.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="positionDB"></param>
        /// <param name="moveDB"></param>
        /// <param name="atDateTime"></param>
        /// <exception cref="NullReferenceException"></exception>
        static void SetOrbitHereFullNewt(Entity entity, WarpMovingDB moveDB, DateTime atDateTime)
        {
            if(moveDB.TargetEntity == null) throw new NullReferenceException("moveDB.TargetEntity cannot be null");
            //propulsionDB.CurrentVectorMS = new Vector3(0, 0, 0);
            var moveStatedb = entity.GetDataBlob<PositionDB>();
            double targetSOI = moveDB.TargetEntity.GetSOI_m();

            Entity? targetEntity;

            if (moveDB.TargetEntity.GetDataBlob<PositionDB>().GetDistanceTo_m(moveStatedb) > targetSOI)
            {
                targetEntity = moveDB.TargetEntity.GetDataBlob<OrbitDB>().Parent; //TODO: it's concevable we could be in another SOI not the parent (ie we could be in a target's moon's SOI)
            }
            else
            {
                targetEntity = moveDB.TargetEntity;
            }

            if(targetEntity == null) throw new NullReferenceException("targetEntity cannot be null");
            OrbitDB targetPlanetsOrbit = targetEntity.GetDataBlob<OrbitDB>();
            Vector3 insertionVector_m = OrbitProcessor.GetOrbitalInsertionVector(moveDB.SavedNewtonionVector, targetPlanetsOrbit, atDateTime);
            moveStatedb.SetParent(targetEntity);
            moveDB.IsAtTarget = true;

            OrbitDB newOrbit = OrbitDB.FromVelocity(targetEntity, entity, insertionVector_m, atDateTime);
            entity.SetDataBlob(newOrbit);

            var burnRate = entity.GetDataBlob<NewtonThrustAbilityDB>().FuelBurnRate;
            var exhaustVelocity = entity.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
            var mass = entity.GetDataBlob<MassVolumeDB>().MassTotal;

            /*
            if (moveDB.EndpointTargetExpendDeltaV.Length() != 0)
            {
                double fuelBurned = OrbitMath.TsiolkovskyFuelUse(mass, exhaustVelocity, moveDB.EndpointTargetExpendDeltaV.Length());
                double secondsBurn = fuelBurned / burnRate;
                var manuverNodeTime = entity.StarSysDateTime + TimeSpan.FromSeconds(secondsBurn * 0.5);

                NewtonThrustCommand.CreateCommand(entity.FactionOwnerID, entity, manuverNodeTime, moveDB.EndpointTargetExpendDeltaV, secondsBurn);
            }
            else if (moveDB.AutoCirculariseAfterWarp)
            {
                var sgp = GeneralMath.StandardGravitationalParameter(mass + targetEntity.GetDataBlob<MassVolumeDB>().MassTotal);
                var pos = positionDB.RelativePosition;
                double curSpeed = insertionVector_m.Length();
                double circSpeed = OrbitalMath.InstantaneousOrbitalSpeed(sgp, pos.Length(), pos.Length());
                double speediff = circSpeed - curSpeed;
                Vector3 circularizationBurn = speediff * Vector3.Normalise(insertionVector_m);

                double fuelBurned = OrbitMath.TsiolkovskyFuelUse(mass, exhaustVelocity, circularizationBurn.Length());
                double secondsBurn = fuelBurned / burnRate;
                var manuverNodeTime = entity.StarSysDateTime + TimeSpan.FromSeconds(secondsBurn * 0.5);

                NewtonThrustCommand.CreateCommand(entity.FactionOwnerID, entity, manuverNodeTime, circularizationBurn, secondsBurn);
            }
*/
        }


    }


}
