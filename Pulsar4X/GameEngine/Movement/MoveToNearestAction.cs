using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.WarpMove;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine.Orders
{
    public class MoveToNearestAction : EntityCommand
    {
        public override string Name => "Move to Nearest";
        public override string Details => "Moves the fleet to the nearest X by filter.";

        public override ActionLaneTypes ActionLanes { get; } = ActionLaneTypes.IneteractWithSelf | ActionLaneTypes.InteractWithEntitySameFleet | ActionLaneTypes.Movement;

        public override bool IsBlocking => true;

        /// <summary>
        /// Entity commanding is a fleet in this action
        /// </summary>
        protected Entity _entityCommanding;
        internal override Entity EntityCommanding
        {
            get { return _entityCommanding; }
        }

        public EntityManager.FilterEntities Filter { get; protected set; }

        public delegate Entity EntitySelector(Entity entity);
        public EntitySelector? TargetSelector { get; protected set; }
        public EntityFilter EntityFactionFilter { get; protected set; } = EntityFilter.Friendly | EntityFilter.Neutral | EntityFilter.Hostile;

        private List<EntityCommand> _shipCommands = new List<EntityCommand>();

        public override bool IsFinished()
        {
            return ShipsFinishedWarping();
        }

        internal override void Execute(DateTime atDateTime)
        {
            if(!IsRunning) FindNearestAndSetupWarpCommands();
        }

        private void FindNearestAndSetupWarpCommands()
        {
            if(!EntityCommanding.TryGetDatablob<FleetDB>(out var fleetDB)) return;
            if(fleetDB.FlagShipID == -1) return;
            if(!EntityCommanding.Manager.TryGetEntityById(fleetDB.FlagShipID, out var flagship)) return;
            if(!flagship.TryGetDatablob<PositionDB>(out var flagshipPositionDB)) return;

            // Get all entites based on the filter
            List<Entity> filteredEntities = EntityCommanding.Manager.GetFilteredEntities(
                EntityFactionFilter,
                RequestingFactionGuid,
                Filter);

            Entity? closestValidEntity = null;
            double closestDistance = double.MaxValue;

            // Find the closest colony
            foreach(var entity in filteredEntities)
            {
                if(!entity.TryGetDatablob<PositionDB>(out var positionDB))
                {
                    continue;
                }

                var distance = positionDB.GetDistanceTo_m(flagshipPositionDB);
                if(distance < closestDistance)
                {
                    closestDistance = distance;
                    closestValidEntity = entity;
                }
            }

            if(closestValidEntity == null) return;

            var targetEntity = TargetSelector == null ? closestValidEntity : TargetSelector(closestValidEntity);

            if(!targetEntity.TryGetDatablob<PositionDB>(out var targetEntityPositionDB))
            {
                return;
            }

            if(targetEntityPositionDB.Parent == null) return;

            double targetSMA = OrbitMath.LowOrbitRadius(targetEntityPositionDB.Parent);

            // Get all the ships we need to add the movement command to
            var ships = fleetDB.Children.Where(c => c.HasDataBlob<ShipInfoDB>());

            foreach(var ship in ships)
            {
                if(!ship.HasDataBlob<WarpAbilityDB>()) continue;
                if(!ship.TryGetDatablob<PositionDB>(out var shipPositionDB)) continue;
                if(shipPositionDB.Parent == targetEntityPositionDB.OwningEntity) continue;

                var shipMass = ship.GetDataBlob<MassVolumeDB>().MassTotal;

                (Vector3 position, DateTime _) = WarpMath.GetInterceptPosition
                (
                    ship,
                    targetEntity.GetDataBlob<OrbitDB>(),
                    EntityCommanding.StarSysDateTime
                );

                Vector3 targetPos = Vector3.Normalise(position) * targetSMA;

                // Create the movement order
                var cargoLibrary = EntityCommanding.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoGoods;
                (WarpMoveCommand warpCommand, NewtonThrustCommand? thrustCommand) = WarpMoveCommand.CreateCommand(cargoLibrary, RequestingFactionGuid, ship, targetEntity, targetPos, EntityCommanding.StarSysDateTime, new Vector3(), shipMass);
                _shipCommands.Add(warpCommand);
            }

            IsRunning = true;
        }

        private bool ShipsFinishedWarping()
        {
            if(!IsRunning) return false;

            foreach(var command in _shipCommands)
            {
                if(!command.IsFinished())
                    return false;
            }
            return true;
        }

        internal override bool IsValidCommand(Game game)
        {
            return true;
        }

        protected MoveToNearestAction() { }

        protected static T CreateCommand<T>(int factionId, Entity commandingEntity) where T : MoveToNearestAction, new()
        {
            var command = new T()
            {
                _entityCommanding = commandingEntity,
                UseActionLanes = true,
                RequestingFactionGuid = factionId,
                EntityCommandingGuid = commandingEntity.Id,
            };

            return command;
        }

        public override EntityCommand Clone()
        {
            var command = new MoveToNearestAction()
            {
                _entityCommanding = this._entityCommanding,
                Filter = this.Filter,
                UseActionLanes = this.UseActionLanes,
                RequestingFactionGuid = this.RequestingFactionGuid,
                EntityCommandingGuid = this.EntityCommandingGuid,
                CreatedDate = this.CreatedDate,
                ActionOnDate = this.ActionOnDate,
                ActionedOnDate = this.ActionedOnDate,
                IsRunning = this.IsRunning,
                TargetSelector = this.TargetSelector
            };

            return command;
        }
    }
}