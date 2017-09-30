using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{

    public class TranslateMoveCommand : EntityCommand
    {
        public Guid RequestingFactionGuid { get; set; }

        public Guid EntityCommandingGuid { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ActionedOnDate { get; set; }

        public int ActionLanes => 1;

        public bool IsBlocking => true;
        public bool IsRunning { get; private set; } = false;
        public Guid TargetEntityGuid { get; set; }
        public Vector4 TargetPosition { get; set; }

        private Entity _entityCommanding;
        public Entity EntityCommanding { get { return _entityCommanding; } }

        [JsonIgnore]
        Entity _factionEntity;
        TranslateMoveDB _db;

        public bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                return true;
            }
            return false;
        }

        public void ActionCommand(Game game)
        {
           _db = new TranslateMoveDB() {TargetPosition = this.TargetPosition };
            _entityCommanding.SetDataBlob(_db);
            IsRunning = true;
        }

        public bool IsFinished()
        {
            if(_db != null)
                return _db.IsAtTarget;
            return false;
        }
    }


    public class TranslateMoveDB : BaseDataBlob
    {
        internal Vector4 TargetPosition;
        internal bool IsAtTarget = false;


        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }

    public class TranslateMoveProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromMinutes(30);

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            var manager = entity.Manager;
            var moveDB = entity.GetDataBlob<TranslateMoveDB>();
            var propulsionDB = entity.GetDataBlob<PropulsionDB>();
            var currentVector = propulsionDB.CurrentSpeed;
            var maxSpeed = propulsionDB.MaximumSpeed;
            var positionDB = entity.GetDataBlob<PositionDB>();
            var currentPosition = positionDB.AbsolutePosition;
            var targetPos = moveDB.TargetPosition;
            var deltaVecToTarget = currentPosition - targetPos;
            var currentSpeed = currentVector.Length();

            StaticDataStore staticData = entity.Manager.Game.StaticData;
            CargoStorageDB storedResources = entity.GetDataBlob<CargoStorageDB>();
            Dictionary<Guid, double> fuelUsePerMeter = propulsionDB.FuelUsePerKM;
            double maxKMeters = ShipMovementProcessor.CalcMaxFuelDistance(entity);

            var nextTPos = currentPosition + (currentVector * deltaSeconds);
      

            var distanceToTarget = deltaVecToTarget.Length();  //in au


            var deltaVecToNextT = currentPosition - nextTPos;
            var fuelMaxDistanceAU = GameConstants.Units.KmPerAu * maxKMeters;

            Vector4 newPos = currentPosition;

            var distanceToNextTPos = deltaVecToNextT.Length();
            double newDistanceDelta;
            if (fuelMaxDistanceAU < distanceToNextTPos)
            {
                newDistanceDelta = fuelMaxDistanceAU;
                double percent = fuelMaxDistanceAU / distanceToNextTPos;
                newPos = nextTPos + deltaVecToNextT * percent;
                Event usedAllFuel = new Event(manager.ManagerSubpulses.SystemLocalDateTime, "Used all Fuel", entity.GetDataBlob<OwnedDB>().ObjectOwner, entity);
                usedAllFuel.EventType = EventType.FuelExhausted;
                manager.Game.EventLog.AddEvent(usedAllFuel);
            }
            else
            {
                newDistanceDelta = distanceToNextTPos;
                newPos = nextTPos;
            }



            if (distanceToTarget < newDistanceDelta) // moving would overtake target, just go directly to target
            {
                newDistanceDelta = distanceToTarget;
                propulsionDB.CurrentSpeed = new Vector4(0, 0, 0, 0);
                newPos = targetPos;
                entity.RemoveDataBlob<TranslateMoveDB>();



            }
            positionDB.AbsolutePosition = newPos;
            int kMetersMoved = (int)(newDistanceDelta * GameConstants.Units.KmPerAu);
            foreach (var item in propulsionDB.FuelUsePerKM)
            {
                var fuel = staticData.GetICargoable(item.Key);
                StorageSpaceProcessor.RemoveCargo(storedResources, fuel, (long)(item.Value * kMetersMoved));
            }
        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            throw new NotImplementedException();
        }
    }



    public class OrbitTransferCommand : EntityCommand
    {
        public OrbitTransferCommand()
        {
        }

        public DateTime ActionedOnDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public Guid RequestingFactionGuid { get; set; }

        public Guid EntityCommandingGuid { get; set; }

        public Guid MoveToOrbitParentGuid { get; set; }

        public int ActionLanes => 1;

        public bool IsBlocking => true;
        public bool IsRunning { get; private set; } = false;
        private Entity _entityCommanding;
        public Entity EntityCommanding { get { return _entityCommanding; } }

        [JsonIgnore]
        Entity _factionEntity;
        [JsonIgnore]
        Entity _moveToOrbitParent;

        public void ActionCommand(Game game)
        {
            OrbitTransferDB newTransferDB = new OrbitTransferDB();

            newTransferDB.OrbitParent = _moveToOrbitParent;

            _entityCommanding.Manager.SetDataBlob(_entityCommanding.ID, newTransferDB);
            IsRunning = true;
        }


        public bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                if (game.GlobalManager.TryGetEntityByGuid(MoveToOrbitParentGuid, out _moveToOrbitParent))
                {
                    return true;
                }
            }
            return false;
        }


        public bool IsFinished()
        {
            throw new NotImplementedException();
        }
    }

    class OrbitTransferDB:BaseDataBlob
    {

        internal Entity OrbitParent { get; set; }
        internal double TargetOrbitRange { get; set; }
        //internal double TargetOrbitEcentricity { get; set; }


        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }

    class OrbitTransferProcessor:IHotloopProcessor
    {
        public TimeSpan RunFrequency {
            get {
                return TimeSpan.FromDays(1);
            }
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            OrbitTransferDB translationDB = entity.GetDataBlob<OrbitTransferDB>();
            PositionDB positionDB = entity.GetDataBlob<PositionDB>();

            OrbitDB orbitDB = entity.GetDataBlob<OrbitDB>();
            //OrbitProcessor.

        }

        public void ProcessManager(EntityManager manager, int deltaSeconds)
        {
            throw new NotImplementedException();
        }



    }
}
