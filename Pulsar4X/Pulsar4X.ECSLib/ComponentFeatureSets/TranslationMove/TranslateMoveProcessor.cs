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
        static StaticDataStore staticData;//maybe shouldnt do this, however I can't currently see a reason we'd ever want to run with two different static data sets.


        public TimeSpan RunFrequency => TimeSpan.FromMinutes(10);

        public TimeSpan FirstRunOffset => TimeSpan.FromMinutes(10);

        public Type GetParameterType => typeof(TranslateMoveDB);

        public void Init(Game game)
        {
            staticData = game.StaticData; 
        }

        public static void StartNonNewtTranslation(Entity entity)
        {
            var moveDB = entity.GetDataBlob<TranslateMoveDB>();
            var propulsionDB = entity.GetDataBlob<PropulsionDB>();
            var positionDB = entity.GetDataBlob<PositionDB>();
            var maxSpeedMS = propulsionDB.MaximumSpeed_MS;

            Vector4 targetPosMt = Distance.AuToMt(moveDB.TranslationExitPoint_AU);
            Vector4 currentPositionMt = Distance.AuToMt(positionDB.AbsolutePosition_AU);

            Vector4 postionDelta = currentPositionMt - targetPosMt;
            double totalDistance = postionDelta.Length();

            double maxKMeters = ShipMovementProcessor.CalcMaxFuelDistance_KM(entity);
            double fuelMaxDistanceMt = maxKMeters * 1000;

            if (fuelMaxDistanceMt >= totalDistance)
            {
                var currentVelocityMS = Vector4.Normalise(targetPosMt - currentPositionMt) * maxSpeedMS;
                propulsionDB.CurrentVectorMS = currentVelocityMS;
                moveDB.CurrentNonNewtonionVectorMS = currentVelocityMS;
                moveDB.TranslateEntryPoint_AU = positionDB.AbsolutePosition_AU;
                CargoStorageDB storedResources = entity.GetDataBlob<CargoStorageDB>();

                foreach (var item in propulsionDB.FuelUsePerKM)
                {
                    var fuel = staticData.GetICargoable(item.Key);
                    StorageSpaceProcessor.RemoveCargo(storedResources, fuel, (long)(item.Value * totalDistance));
                }
            }

        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            var manager = entity.Manager;
            var moveDB = entity.GetDataBlob<TranslateMoveDB>();
            var propulsionDB = entity.GetDataBlob<PropulsionDB>();
            var currentVelocityMS = moveDB.CurrentNonNewtonionVectorMS;

            var positionDB = entity.GetDataBlob<PositionDB>();
            var currentPositionAU = positionDB.AbsolutePosition_AU;
            Vector4 currentPositionMt = Distance.AuToMt(positionDB.AbsolutePosition_AU);

            Vector4 targetPosMt;

            targetPosMt = Distance.AuToMt(moveDB.TranslationExitPoint_AU);

            var deltaVecToTargetMt = currentPositionMt - targetPosMt;

            var newPositionMt = currentPositionMt + currentVelocityMS * deltaSeconds;

            //var distanceToTargetAU = deltaVecToTargetAU.Length();  //in au
            var distanceToTargetMt = deltaVecToTargetMt.Length();

            //var deltaVecToTimeAU = currentPositionAU - TimePosAU;
            var positionDelta = currentPositionMt - newPositionMt;

            double distanceToMove = positionDelta.Length();
            var distnaceToMove2 = Vector4.Magnitude(positionDelta);



            if (distanceToTargetMt <= distanceToMove) // moving would overtake target, just go directly to target
            {
                //distanceToMoveAU = distanceToTargetAU;
                distanceToMove = distanceToTargetMt;
                propulsionDB.CurrentVectorMS = new Vector4(0, 0, 0, 0);
                //newPosAU = targetPosAU;
                newPositionMt = targetPosMt;
                moveDB.IsAtTarget = true;
                entity.RemoveDataBlob<TranslateMoveDB>();
            }

            positionDB.AbsolutePosition_AU = Distance.MtToAu( newPositionMt);

            double metersMoved = distanceToMove;

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
