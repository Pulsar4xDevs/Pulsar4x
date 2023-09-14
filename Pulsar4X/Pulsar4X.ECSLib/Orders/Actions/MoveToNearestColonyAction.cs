using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    public class MoveToNearestColonyAction : EntityCommand
    {
        public override string Name => "Move to Nearest Colony";
        public override string Details => "Moves the fleet to the nearest colony.";

        public override ActionLaneTypes ActionLanes { get; } = ActionLaneTypes.IneteractWithSelf | ActionLaneTypes.InteractWithEntitySameFleet | ActionLaneTypes.Movement;

        public override bool IsBlocking => true;

        private Entity _entityCommanding;
        internal override Entity EntityCommanding
        {
            get { return _entityCommanding; }
        }

        private List<EntityCommand> _shipCommands = new List<EntityCommand>();

        public override bool IsFinished()
        {
            return ShipsFinishedWarping();
        }

        internal override void Execute(DateTime atDateTime)
        {
            if(!IsRunning) FindColonyAndSetupWarpCommands();
        }

        private void FindColonyAndSetupWarpCommands()
        {
            if(!EntityCommanding.TryGetDatablob<FleetDB>(out var fleetDB)) return;
            if(fleetDB.FlagShipID == Guid.Empty) return;
            if(!EntityCommanding.Manager.TryGetEntityByGuid(fleetDB.FlagShipID, out var flagship)) return;
            if(!flagship.TryGetDatablob<PositionDB>(out var flagshipPositionDB)) return;

            // Get all colonies in the system
            var colonies = EntityCommanding.Manager.GetAllEntitiesWithDataBlob<ColonyInfoDB>();
            Entity closestColony = null;
            double closestDistance = double.MaxValue;

            // Find the closest colony
            foreach(var colony in colonies)
            {
                if(!colony.TryGetDatablob<PositionDB>(out var positionDB))
                {
                    continue;
                }

                var distance = positionDB.GetDistanceTo_m(flagshipPositionDB);
                if(distance < closestDistance)
                {
                    closestDistance = distance;
                    closestColony = colony;
                }
            }

            if(closestColony == null) return;

            // Get the colonies parent radius
            closestColony.TryGetDatablob<PositionDB>(out var closestColonyPositionDB);
            double targetSMA = OrbitMath.LowOrbitRadius(closestColonyPositionDB.Parent);

            // Get all the ships we need to add the movement command to
            var ships = fleetDB.Children.Where(c => c.HasDataBlob<ShipInfoDB>());

            foreach(var ship in ships)
            {
                if(!ship.HasDataBlob<WarpAbilityDB>()) continue;
                if(!ship.TryGetDatablob<PositionDB>(out var shipPositionDB)) continue;
                if(shipPositionDB.Parent == closestColonyPositionDB.Parent) continue;

                var shipMass = ship.GetDataBlob<MassVolumeDB>().MassTotal;

                (Vector3 position, DateTime _) = OrbitProcessor.GetInterceptPosition
                (
                    ship,
                    closestColonyPositionDB.Parent.GetDataBlob<OrbitDB>(),
                    EntityCommanding.StarSysDateTime
                );

                Vector3 targetPos = Vector3.Normalise(position) * targetSMA;

                // Create the movement order
                (WarpMoveCommand warpCommand, NewtonThrustCommand thrustCommand) = WarpMoveCommand.CreateCommand(RequestingFactionGuid, ship, closestColonyPositionDB.Parent, targetPos, EntityCommanding.StarSysDateTime, new Vector3() , shipMass);
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

        public MoveToNearestColonyAction() { }
        public MoveToNearestColonyAction(Entity commandingEntity)
        {
            _entityCommanding = commandingEntity;
        }

        public static MoveToNearestColonyAction CreateCommand(Guid factionId, Entity commandingEntity)
        {
            var command = new MoveToNearestColonyAction(commandingEntity)
            {
                UseActionLanes = true,
                RequestingFactionGuid = factionId,
                EntityCommandingGuid = commandingEntity.Guid,
            };

            return command;
        }

        public override EntityCommand Clone()
        {
            var command = new MoveToNearestColonyAction(EntityCommanding)
            {
                UseActionLanes = this.UseActionLanes,
                RequestingFactionGuid = this.RequestingFactionGuid,
                EntityCommandingGuid = this.EntityCommandingGuid,
                CreatedDate = this.CreatedDate,
                ActionOnDate = this.ActionOnDate,
                ActionedOnDate = this.ActionedOnDate,
                IsRunning = this.IsRunning
            };

            return command;
        }
    }
}