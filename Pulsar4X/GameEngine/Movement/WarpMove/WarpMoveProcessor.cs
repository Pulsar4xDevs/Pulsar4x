using System;
using GameEngine.Engine.Orders;
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
    ///
    /// In-transit interpolation is a hotloop (regular position updates while warping).
    /// Arrival is an instance interrupt at <see cref="WarpMovingDB.PredictedExitTime"/> —
    /// same pattern as SOI enter/exit — so drop-in is not quantized to <see cref="RunFrequency"/>.
    /// </summary>
    public class WarpMoveProcessor : IInstanceProcessor, IHotloopProcessor
    {
        private static GameSettings _gameSettings;

        /// <summary>
        /// Test observation point for drop-in. Production does not subscribe.
        /// Fired once per <see cref="SetOrbitHereSimpleNewt"/> / NoNewt call.
        /// </summary>
        internal static Action<Entity, DateTime>? TestDropIn;

        public TimeSpan RunFrequency => TimeSpan.FromMinutes(5);

        public TimeSpan FirstRunOffset => TimeSpan.FromMinutes(0);

        public Type GetParameterType => typeof(WarpMovingDB);

        public void Init(Game game)
        {
            _gameSettings = game.Settings;
        }

        /// <summary>
        /// Arrival interrupt at <see cref="WarpMovingDB.PredictedExitTime"/>.
        /// Interpolate this tick first (covers the fractional second the hotloop
        /// truncated), then drop in even if overshoot missed by a FP residual.
        /// No-ops if the hotloop overshoot-fallback already ended the bubble.
        /// </summary>
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            if (!entity.TryGetDataBlob<WarpMovingDB>(out var db) || db.IsAtTarget)
                return;

            WarpMove(entity, db, atDateTime);

            if (!entity.TryGetDataBlob(out db) || db.IsAtTarget)
                return;

            if (db.HasStarted
                && db.PredictedExitTime != DateTime.MinValue
                && atDateTime >= db.PredictedExitTime)
            {
                FinishArrival(entity, db, atDateTime);
                return;
            }

            if (db.OwningEntity != null)
                MoveStateProcessor.ProcessForType(db, atDateTime);
        }

        /// <summary>
        /// Pin arrival to <paramref name="moveDB"/>.PredictedExitTime so ManagerSubPulse
        /// subdivides the pulse to that instant instead of the next 5-minute hotloop.
        /// </summary>
        internal static void ScheduleArrival(Entity entity, WarpMovingDB moveDB)
        {
            if (!moveDB.HasStarted)
                return;
            DateTime when = moveDB.PredictedExitTime;
            if (when > entity.StarSysDateTime)
                entity.Manager.ManagerSubpulses.AddEntityInterupt(when, nameof(WarpMoveProcessor), entity);
        }

        /// <summary>
        /// Run the action queue at this exact instant. AddEntityInterupt(now) is not safe
        /// here: ProcessToNextInterupt has already Split() the instance queue, and it
        /// breaks when StarSysDateTime == _processToDateTime, so a same-time interrupt
        /// would wait until the next master pulse. Same "run now" pattern as HandleOrder.
        /// </summary>
        static void WakeActionQueue(Entity entity, DateTime atDateTime)
        {
            if (entity.Manager?.Game == null)
                return;
            entity.Manager.Game.ProcessorManager
                .GetInstanceProcessor(nameof(ActionQueueProcessor))
                .ProcessEntity(entity, atDateTime);
        }


        /// <summary>
        /// Efficent processes all entities in the system for the hotloop process. 
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="deltaSeconds"></param>
        /// <returns></returns>
        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var datablobs = manager.GetAllDataBlobsOfType<WarpMovingDB>();
            DateTime todateTime = manager.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            foreach (var db in datablobs)
            {
                if (db.OwningEntity is null || db.IsAtTarget)
                    continue;
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

        /// <summary>
        /// Hotloop-style advance to an explicit datetime. Named apart from the instance
        /// <see cref="ProcessEntity(Entity, DateTime)"/> override (arrival interrupt).
        /// </summary>
        public static void ProcessToDate(Entity entity, DateTime toDateTime)
        {
            if (!entity.TryGetDataBlob<WarpMovingDB>(out var db) || db.IsAtTarget)
                return;
            WarpMove(entity, db, toDateTime);
            if (db.OwningEntity != null)
                MoveStateProcessor.ProcessForType(db, toDateTime);
        }

        public static void WarpMove(Entity entity, WarpMovingDB moveDB,  DateTime toDateTime)
        {
            if (moveDB.IsAtTarget)
                return;

            if (moveDB.HasStarted || TryStartWarp(entity, moveDB, toDateTime))
            {
                var currentVelocityMS = moveDB.CurrentNonNewtonionVectorMS;
                DateTime dateTimeFrom = moveDB.LastProcessDateTime;

                double deltaT = (toDateTime - dateTimeFrom).TotalSeconds;

                Vector3 targetPosMt = moveDB.ExitPointAbsolute;

                var newPositionMt = moveDB._position + (Vector2)currentVelocityMS * deltaT;

                double distanceToMove = ( moveDB._position - newPositionMt).Length();
                double distanceToTargetMt = (moveDB._position - (Vector2)targetPosMt).Length();

                if (distanceToTargetMt <= distanceToMove) // moving would overtake target, just go directly to target
                {
                    SnapBubbleToExit(moveDB);
                    // Hotloop ticks can overshoot PredictedExitTime; snap the bubble here but
                    // only drop in once the arrival interrupt is due. Otherwise the 5-minute
                    // hotloop and the instance interrupt both call EndWarpMove.
                    // The instance processor still FinishArrival if this tick undershoots
                    // by a FP residual.
                    if (ArrivalDue(moveDB, toDateTime))
                        FinishArrival(entity, moveDB, toDateTime);
                }
                else
                {
                    moveDB._position = newPositionMt;
                }


                moveDB.LastProcessDateTime = toDateTime;
            }
        }

        public static bool TryStartWarp(Entity entity, WarpMovingDB moveDB, DateTime toDateTime)
        {
            var powerDB = entity.GetDataBlob<EnergyGenAbilityDB>();
            var warpDB = entity.GetDataBlob<WarpAbilityDB>();
            
            double estored = powerDB.EnergyStored[warpDB.EnergyType];
            bool canStart = false;
            var creationCost = warpDB.BubbleCreationCost;
            if (creationCost <= estored)
            {
                var positionDB = entity.GetDataBlob<PositionDB>();
                var maxSpeedMS = warpDB.MaxSpeed;

                EnergyGenProcessor.EnergyGen(entity, toDateTime);

                // Check to make sure we don't set the position parent to itself
                if(positionDB.Parent != positionDB.Root)
                    positionDB.SetParent(positionDB.Root);

                // Intercept from the same inertial start the bubble will fly from.
                // GetInterceptPosition(entity, ...) uses GetAbsoluteFuturePosition, which
                // is the origin once OnSetToEntity has stripped OrbitDB.
                Vector3 currentPositionMt = positionDB.AbsolutePosition;
                var intercept = WarpMath.GetInterceptPosition(
                    currentPositionMt, maxSpeedMS, moveDB.TargetEntity, toDateTime, moveDB.ExitPointrelative);
                moveDB.ExitPointAbsolute = intercept.position;
                moveDB.PredictedExitTime = intercept.etiDateTime;
                moveDB.EntryPointAbsolute = currentPositionMt;
                moveDB.EntryDateTime = toDateTime;

                moveDB._position = (Vector2)currentPositionMt;
                Vector3 targetPosMt = moveDB.ExitPointAbsolute;

                var currentVelocityMS = Vector3.Normalise(targetPosMt - currentPositionMt) * maxSpeedMS;

                moveDB.CurrentNonNewtonionVectorMS = currentVelocityMS;
                moveDB.LastProcessDateTime = toDateTime;
                
                EnergyGenProcessor.EnergyGen(entity, toDateTime - TimeSpan.FromSeconds(1));
                powerDB.AddDemand(creationCost, toDateTime - TimeSpan.FromSeconds(1));
                EnergyGenProcessor.EnergyGen(entity, toDateTime);
                powerDB.AddDemand(-creationCost, toDateTime);
                powerDB.AddDemand(warpDB.BubbleSustainCost, toDateTime);

                moveDB.HasStarted = true;
                canStart = true;
                ScheduleArrival(entity, moveDB);
            }

            return canStart;
        }


        static bool ArrivalDue(WarpMovingDB moveDB, DateTime atDateTime)
        {
            return moveDB.PredictedExitTime == DateTime.MinValue
                   || atDateTime >= moveDB.PredictedExitTime;
        }

        static void SnapBubbleToExit(WarpMovingDB moveDB)
        {
            moveDB._parentEnitity = moveDB.TargetEntity;
            moveDB._position = (Vector2)moveDB.ExitPointrelative;
        }

        /// <summary>
        /// Snap to the planned exit and drop in. Instance interrupt calls this when
        /// interpolation left a residual; hotloop calls it only after an overshoot
        /// that is already due.
        /// </summary>
        static void FinishArrival(Entity entity, WarpMovingDB moveDB, DateTime atDateTime)
        {
            if (moveDB.IsAtTarget)
                return;
            if (moveDB.TargetEntity == null)
                return;

            var warpDB = entity.GetDataBlob<WarpAbilityDB>();
            SnapBubbleToExit(moveDB);
            var destinationMoveType = moveDB.TargetEntity.GetDataBlob<PositionDB>().MoveType;
            if (destinationMoveType == PositionDB.MoveTypes.None)
            {
                moveDB.CurrentNonNewtonionVectorMS = Vector3.Zero;
                moveDB.IsAtTarget = true;
            }
            else
            {
                moveDB.IsAtTarget = true;
                EndWarpMove(entity, warpDB, moveDB, atDateTime);
            }
        }

        static void EndWarpMove(Entity entity, WarpAbilityDB warpDB, WarpMovingDB moveDB,  DateTime toDateTime)
        {
            if (!entity.HasDataBlob<WarpMovingDB>())
                return;

            var powerDB = entity.GetDataBlob<EnergyGenAbilityDB>();


            EnergyGenProcessor.EnergyGen(entity, toDateTime - TimeSpan.FromSeconds(1));
            powerDB.AddDemand(warpDB.BubbleCollapseCost, toDateTime - TimeSpan.FromSeconds(1));
            EnergyGenProcessor.EnergyGen(entity, toDateTime);
            powerDB.AddDemand(-warpDB.BubbleSustainCost, toDateTime);
            powerDB.AddDemand(-warpDB.BubbleCollapseCost, toDateTime);

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
                    WakeActionQueue(entity, toDateTime);
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

            TestDropIn?.Invoke(entity, atDateTime);

            //just chuck it in a circular orbit.
            OrbitDB newOrbit = OrbitDB.FromPosition(targetEntity, entity, atDateTime);
            entity.SetDataBlob(newOrbit);
            moveStatedb.SetParent(targetEntity);
            moveDB.IsAtTarget = true;

        }

        static void SetOrbitHereSimpleNewt(Entity entity, WarpMovingDB moveDB, DateTime atDateTime)
        {
            TestDropIn?.Invoke(entity, atDateTime);

            entity.TryGetDataBlob<PositionDB>(out var posdb);
            Vector3 pos1 = posdb.RelativePosition;
            var combinedMass = entity.GetDataBlob<MassVolumeDB>().MassTotal;
            combinedMass += moveDB.TargetEntity.GetDataBlob<MassVolumeDB>().MassTotal;
            var sgp = GeneralMath.StandardGravitationalParameter(combinedMass);
            var targetOrbit = moveDB.TargetEntity.GetDataBlob<OrbitDB>();
            var soi = OrbitMath.GetSOIRadius(targetOrbit);
            KeplerElements currentOrbit;
            Entity orbitalParent = moveDB.TargetEntity;

            Vector3 pos2a = moveDB.ExitPointrelative;
            
            if(soi > moveDB.ExitPointrelative.Length())
                currentOrbit = OrbitMath.KeplerFromPositionAndVelocity(sgp, moveDB.ExitPointrelative, moveDB.SavedNewtonionVector, atDateTime);
            else//if we're outside the soi, then we create an orbit around the parent instead. 
            {
                orbitalParent = moveDB.TargetEntity.GetSOIParentEntity();
                combinedMass = entity.GetDataBlob<MassVolumeDB>().MassTotal;
                combinedMass += orbitalParent.GetDataBlob<MassVolumeDB>().MassTotal;
                sgp = GeneralMath.StandardGravitationalParameter(combinedMass);
                var parentAbs = (Vector3)MoveMath.GetAbsoluteFuturePosition(orbitalParent, atDateTime);
                var parentRelitivePos = moveDB.ExitPointAbsolute - parentAbs;
                currentOrbit = OrbitMath.KeplerFromPositionAndVelocity(sgp, parentRelitivePos, moveDB.SavedNewtonionVector, atDateTime);
            }
            //todo: check current orbit is valid. (eg within soi)

            //check if the orbit is actualy valid and not just default values
            //if it is default values, then we just drop it in a trajectory from it's position and velocity.
            //this should be the correct we will remove EndPointTargetOrbit from WarpMovingDB and let it be handled seperatly
            if (moveDB.EndpointTargetOrbit.StandardGravParameter == 0)
            {
                
                OrbitDB newOrbitdb = OrbitDB.FromKeplerElements(orbitalParent, combinedMass, currentOrbit, atDateTime);
                entity.SetDataBlob(newOrbitdb);
                OrbitProcessor.ProcessEntity(entity, atDateTime);
                Vector3 pos2 = posdb.RelativePosition;
                entity.Manager.Game.TimePulse.PauseTime();
                return;
            }
            
            NewtonSimpleMoveDB newtMove = new NewtonSimpleMoveDB(orbitalParent, currentOrbit, moveDB.EndpointTargetOrbit, atDateTime);
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

                NewtonThrustAction.CreateCommand(entity.FactionOwnerID, entity, manuverNodeTime, moveDB.EndpointTargetExpendDeltaV, secondsBurn);
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

                NewtonThrustAction.CreateCommand(entity.FactionOwnerID, entity, manuverNodeTime, circularizationBurn, secondsBurn);
            }
*/
        }


    }


}
