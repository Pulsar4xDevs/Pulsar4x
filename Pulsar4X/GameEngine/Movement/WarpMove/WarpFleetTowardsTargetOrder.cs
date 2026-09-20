using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Fleets;
using Pulsar4X.Ships;

namespace Pulsar4X.Movement;

public class WarpFleetTowardsTargetOrder : EntityAction
{
    public override EntityAction.ActionLaneTypes ActionLanes => EntityAction.ActionLaneTypes.Movement;

    public override bool IsBlocking => true;

    public override string Name => "Move Fleet Towards Target";

    public override string Details => "";

    private Entity _entityCommanding;

    internal override Entity EntityCommanding => _entityCommanding;

    public Entity Target { get; set; }

    List<EntityAction> _shipCommands = new List<EntityAction>();

    public override EntityAction Clone()
    {
        throw new NotImplementedException();
    }

    internal override bool IsFinished()
    {
        if(!IsRunning)
            _isFinished = false;
        else
        {
            foreach (var command in _shipCommands)
            {
                if (!command.IsFinished())
                    return _isFinished = false;
            }
            _isFinished = true;
        }
        return _isFinished;
    }

    internal override void Execute(DateTime atDateTime)
    {
        if(IsRunning) return;
        if(!_entityCommanding.TryGetDataBlob<FleetDB>(out var fleetDB)) return;
        // Get all the ships we need to add the movement command to
        var ships = fleetDB.Children.Where(c => c.HasDataBlob<ShipInfoDB>());


        foreach(var ship in ships)
        {
            //don't give move order if ship is already at location.
            var shipParent = ship.GetDataBlob<PositionDB>().Parent;
            if(shipParent == Target)
                continue;
            if (Target.TryGetDataBlob<ColonyInfoDB>(out var colonyDB) && colonyDB.PlanetEntity == shipParent)
                continue;
            if(!ship.HasDataBlob<WarpAbilityDB>()) continue;
                
            var shipCommand = WarpMoveAction.CreateCommandEZ(ship, Target, atDateTime);

            _shipCommands.Add(shipCommand);
            ship.Manager.Game.OrderHandler.HandleOrder(shipCommand);
        }
        IsRunning = true;
    }

    public static WarpFleetTowardsTargetOrder CreateCommand(Entity fleet, Entity target)
    {
        var order = new WarpFleetTowardsTargetOrder()
        {
            RequestingFactionGuid = fleet.FactionOwnerID,
            EntityCommandingGuid = fleet.Id,
            _entityCommanding = fleet,
            Target = target,
        };

        return order;
    }

    internal override bool IsValidCommand(Game game)
    {
        return true;
    }
}