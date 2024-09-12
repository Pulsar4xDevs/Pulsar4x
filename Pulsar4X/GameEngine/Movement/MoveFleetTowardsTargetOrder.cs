using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine.Orders;

public class MoveFleetTowardsTargetOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;

    public override bool IsBlocking => true;

    public override string Name => "Move Fleet Towards Target";

    public override string Details => "";

    private Entity _entityCommanding;

    internal override Entity EntityCommanding => _entityCommanding;

    public Entity Target { get; set; }

    List<EntityCommand> _shipCommands = new List<EntityCommand>();

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }

    public override bool IsFinished()
    {
        if(!IsRunning) return false;

        foreach(var command in _shipCommands)
        {
            if(!command.IsFinished())
                return false;
        }
        return true;
    }

    internal override void Execute(DateTime atDateTime)
    {
        if(IsRunning) return;
        if(!_entityCommanding.TryGetDatablob<FleetDB>(out var fleetDB)) return;
        // Get all the ships we need to add the movement command to
        var ships = fleetDB.Children.Where(c => c.HasDataBlob<ShipInfoDB>());

        foreach(var ship in ships)
        {
            var shipCommand = MoveTowardsTargetOrder.CreateCommand(ship, Target);
            ship.Manager.Game.OrderHandler.HandleOrder(shipCommand);
            _shipCommands.Add(shipCommand);
        }
        IsRunning = true;
    }

    public static MoveFleetTowardsTargetOrder CreateCommand(Entity entity, Entity target)
    {
        var order = new MoveFleetTowardsTargetOrder()
        {
            RequestingFactionGuid = entity.FactionOwnerID,
            EntityCommandingGuid = entity.Id,
            _entityCommanding = entity,
            Target = target,
        };

        return order;
    }

    internal override bool IsValidCommand(Game game)
    {
        return true;
    }
}