using System;
using System.Linq;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine.Orders;

public class JumpOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InstantOrder | ActionLaneTypes.Movement;

    public override bool IsBlocking => false;

    public override string Name { get; } = "Travel through jump gate";

    public override string Details { get; } = "Travel through the specified jump gate";

    Entity _factionEntity;
    Entity _entityCommanding;

    public JumpPointDB JumpGate { get; private set; }

    internal override Entity EntityCommanding { get { return _entityCommanding; } }
    bool _isFinished = false;
    string NewName;

    public static void CreateAndExecute(Game game, Entity faction, Entity fleetEntity, JumpPointDB jumpGate)
    {
        var cmd = new JumpOrder()
        {
            RequestingFactionGuid = faction.Id,
            EntityCommandingGuid = fleetEntity.Id,
            CreatedDate = fleetEntity.Manager.ManagerSubpulses.StarSysDateTime,
            JumpGate = jumpGate
        };

        game.OrderHandler.HandleOrder(cmd);
    }

    internal override void Execute(DateTime atDateTime)
    {
        if(JumpGate.OwningEntity.Manager.TryGetGlobalEntityById(JumpGate.DestinationId, out var destinationEntity))
        {
            if(!_entityCommanding.TryGetDatablob<FleetDB>(out var fleetDB)) return;

            var destinationPositionDB = destinationEntity.GetDataBlob<PositionDB>();
            var ships = fleetDB.Children.Where(c => c.HasDataBlob<ShipInfoDB>());

            // Transfer each ship in the fleet
            foreach(var ship in ships)
            {
                // Transfer to the new system
                destinationEntity.Manager.Transfer(ship);

                // Update the position
                var positionDB = ship.GetDataBlob<PositionDB>();
                positionDB.AbsolutePosition = destinationPositionDB.AbsolutePosition;
                positionDB.SetParent(destinationEntity);
            }

            // Transfer the fleet entity
            destinationEntity.Manager.Transfer(_entityCommanding);
        }

        _isFinished = true;
    }

    public override bool IsFinished()
    {
        return _isFinished;
    }

    internal override bool IsValidCommand(Game game)
    {
        if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
        {
            return true;
        }
        return false;
    }

    public override EntityCommand Clone()
    {
        throw new NotImplementedException();
    }
}
