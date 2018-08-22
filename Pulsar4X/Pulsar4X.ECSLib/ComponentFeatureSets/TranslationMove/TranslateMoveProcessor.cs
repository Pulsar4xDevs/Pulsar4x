using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
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
    /// Speed is shown ralitive to the parent star.  
    /// Cannot change its direction or speed untill exit.  
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
    /// many websites just added speeds of galaxy + solarsystem together and ignored the ralitive vectors. 
    /// one site I found sugested 368 ± 2 km/s 
    /// this might not be terrible, however if we gave max speeds of that number, 
    /// we'd be able to travel 368 km/s in one direction, and none in the oposite direction.
    /// so we'd need to give max speeds of more than that, and/or force homman transfers in one direction. 
    /// could provide an interesting gameplay mechanic...
    /// 
    /// 
    /// </summary>
    public class TranslateMoveProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromMinutes(10);

        public TimeSpan FirstRunOffset => TimeSpan.FromMinutes(10);

        public Type GetParameterType => typeof(TranslateMoveDB);

        public void Init(Game game)
        {
            //nothing needed for this one. 
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            var manager = entity.Manager;
            var moveDB = entity.GetDataBlob<TranslateMoveDB>();
            var propulsionDB = entity.GetDataBlob<PropulsionDB>();
            //var currentVector = propulsionDB.CurrentSpeed;
            var maxSpeedMS = propulsionDB.MaximumSpeed_MS;
            var positionDB = entity.GetDataBlob<PositionDB>();
            var currentPositionAU = positionDB.AbsolutePosition_AU;
            //targetPosition taking the range (how close we want to get) into account. 
            Vector4 targetPosAU;
            if (moveDB.TargetPositionDB != null)
            {
                moveDB.TargetPosition_AU = moveDB.TargetPositionDB.AbsolutePosition_AU;
                targetPosAU = moveDB.TargetPosition_AU * (1 - (moveDB.MoveRange_KM / GameConstants.Units.KmPerAu) / moveDB.TargetPosition_AU.Length());
            }
            else
                targetPosAU = moveDB.TargetPosition_AU;
            
            var deltaVecToTargetAU = currentPositionAU - targetPosAU;


            var currentVelocityMS = Distance.AuToKm( GMath.GetVector(currentPositionAU, targetPosAU, Distance.MToAU( maxSpeedMS) )) * 1000;

            propulsionDB.CurrentVectorMS = currentVelocityMS;
            moveDB.CurrentVectorMS = currentVelocityMS;
            StaticDataStore staticData = entity.Manager.Game.StaticData;
            CargoStorageDB storedResources = entity.GetDataBlob<CargoStorageDB>();
            Dictionary<Guid, double> fuelUsePerMeter = propulsionDB.FuelUsePerKM;
            double maxKMeters = ShipMovementProcessor.CalcMaxFuelDistance_KM(entity);

            var TimePosAU = currentPositionAU +  Distance.MToAU(currentVelocityMS) * deltaSeconds; ;


            var distanceToTargetAU = deltaVecToTargetAU.Length();  //in au

            var deltaVecToTimeAU = currentPositionAU - TimePosAU;
            var fuelMaxDistanceAU = maxKMeters / GameConstants.Units.KmPerAu;



            Vector4 newPosAU = currentPositionAU;

            double distanceToNextTPosAU = deltaVecToTimeAU.Length();
            double distanceToMoveAU;
            if (fuelMaxDistanceAU < distanceToNextTPosAU)
            {
                distanceToMoveAU = fuelMaxDistanceAU;
                double percent = fuelMaxDistanceAU / distanceToNextTPosAU;
                newPosAU = TimePosAU + deltaVecToTimeAU * percent;
                //Event usedAllFuel = new Event(manager.ManagerSubpulses.SystemLocalDateTime, "Used all Fuel", entity.GetDataBlob<OwnedDB>().OwnedByFaction, entity);
                //usedAllFuel.EventType = EventType.FuelExhausted;
                //manager.Game.EventLog.AddEvent(usedAllFuel);
            }
            else
            {
                distanceToMoveAU = distanceToNextTPosAU;
                newPosAU = TimePosAU;
            }



            if (distanceToTargetAU < distanceToMoveAU) // moving would overtake target, just go directly to target
            {
                distanceToMoveAU = distanceToTargetAU;
                propulsionDB.CurrentVectorMS = new Vector4(0, 0, 0, 0);
                newPosAU = targetPosAU;
                moveDB.IsAtTarget = true;
                entity.RemoveDataBlob<TranslateMoveDB>();
            }

            positionDB.AbsolutePosition_AU = newPosAU;

            double kMetersMoved = Distance.AuToKm(distanceToMoveAU);
            foreach (var item in propulsionDB.FuelUsePerKM)
            {
                var fuel = staticData.GetICargoable(item.Key);
                StorageSpaceProcessor.RemoveCargo(storedResources, fuel, (long)(item.Value * kMetersMoved));
            }
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            List<Entity> entitysWithTranslateMove = manager.GetAllEntitiesWithDataBlob<TranslateMoveDB>();
            foreach (var entity in entitysWithTranslateMove)
            {
                ProcessEntity(entity, deltaSeconds);
            }
        }
    }


}
