using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class TranslateMoveProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromMinutes(10);

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            var manager = entity.Manager;
            var moveDB = entity.GetDataBlob<TranslateMoveDB>();
            var propulsionDB = entity.GetDataBlob<PropulsionDB>();
            //var currentVector = propulsionDB.CurrentSpeed;
            var maxSpeed = propulsionDB.MaximumSpeed;
            var positionDB = entity.GetDataBlob<PositionDB>();
            var currentPosition = positionDB.AbsolutePosition;
            //targetPosition taking the range (how close we want to get) into account. 
            var targetPos = moveDB.TargetPosition * (1 - (moveDB.MoveRangeInKM / GameConstants.Units.KmPerAu) / moveDB.TargetPosition.Length());

            var deltaVecToTarget = currentPosition - targetPos;


            var currentSpeed = GMath.GetVector(currentPosition, targetPos, maxSpeed);
            propulsionDB.CurrentVector = currentSpeed;
            moveDB.CurrentVector = currentSpeed;
            StaticDataStore staticData = entity.Manager.Game.StaticData;
            CargoStorageDB storedResources = entity.GetDataBlob<CargoStorageDB>();
            Dictionary<Guid, double> fuelUsePerMeter = propulsionDB.FuelUsePerKM;
            double maxKMeters = ShipMovementProcessor.CalcMaxFuelDistance(entity);

            var nextTPos = currentPosition + (currentSpeed * deltaSeconds);


            var distanceToTargetAU = deltaVecToTarget.Length();  //in au

            var deltaVecToNextT = currentPosition - nextTPos;
            var fuelMaxDistanceAU = maxKMeters / GameConstants.Units.KmPerAu;



            Vector4 newPos = currentPosition;

            double distanceToNextTPos = deltaVecToNextT.Length();
            double distanceToMove;
            if (fuelMaxDistanceAU < distanceToNextTPos)
            {
                distanceToMove = fuelMaxDistanceAU;
                double percent = fuelMaxDistanceAU / distanceToNextTPos;
                newPos = nextTPos + deltaVecToNextT * percent;
                Event usedAllFuel = new Event(manager.ManagerSubpulses.SystemLocalDateTime, "Used all Fuel", entity.GetDataBlob<OwnedDB>().ObjectOwner, entity);
                usedAllFuel.EventType = EventType.FuelExhausted;
                manager.Game.EventLog.AddEvent(usedAllFuel);
            }
            else
            {
                distanceToMove = distanceToNextTPos;
                newPos = nextTPos;
            }



            if (distanceToTargetAU < distanceToMove) // moving would overtake target, just go directly to target
            {
                distanceToMove = distanceToTargetAU;
                propulsionDB.CurrentVector = new Vector4(0, 0, 0, 0);
                newPos = targetPos;
                moveDB.IsAtTarget = true;
                entity.RemoveDataBlob<TranslateMoveDB>();
            }

            positionDB.AbsolutePosition = newPos;
            int kMetersMoved = (int)(distanceToMove * GameConstants.Units.KmPerAu);
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
