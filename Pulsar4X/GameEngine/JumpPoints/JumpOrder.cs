using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Ships;

namespace Pulsar4X.JumpPoints;

/// <summary>
/// Fleet-level jump order. Creates per-ship warp + jump commands so each ship
/// independently warps to the gate and jumps through when it arrives.
/// </summary>
public class JumpOrder : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;

    public override bool IsBlocking => true;

    public override string Name { get; } = "Jump fleet through gate";

    public override string Details { get; } = "Warp fleet to jump gate and transit";

    Entity _factionEntity;
    Entity _entityCommanding;

    public JumpPointDB JumpGate { get; private set; }

    internal override Entity EntityCommanding { get { return _entityCommanding; } }

    List<ShipJumpCommand> _shipJumpCommands = new List<ShipJumpCommand>();

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
        if (IsRunning) return;
        if (!_entityCommanding.TryGetDataBlob<FleetDB>(out var fleetDB)) return;
        if (JumpGate.OwningEntity == null) return;

        var gateEntity = JumpGate.OwningEntity;
        var ships = fleetDB.Children.Where(c => c.HasDataBlob<ShipInfoDB>());

        foreach (var ship in ships)
        {
            // Queue a warp command if the ship isn't already at the gate
            var shipParent = ship.GetDataBlob<PositionDB>().Parent;
            if (shipParent != gateEntity)
            {
                if (!ship.HasDataBlob<WarpAbilityDB>()) continue;

                var warpCmd = WarpMoveCommand.CreateCommandEZ(ship, gateEntity, atDateTime);
                ship.Manager.Game.OrderHandler.HandleOrder(warpCmd);
            }

            // Queue a per-ship jump command (will execute after warp completes)
            var jumpCmd = ShipJumpCommand.Create(ship, JumpGate);
            ship.Manager.Game.OrderHandler.HandleOrder(jumpCmd);
            _shipJumpCommands.Add(jumpCmd);
        }

        IsRunning = true;
    }

    internal override bool IsFinished()
    {
        if (!IsRunning)
            return _isFinished = false;

        foreach (var cmd in _shipJumpCommands)
        {
            if (!cmd.IsFinished())
                return _isFinished = false;
        }

        // All ships have jumped — transfer the fleet entity to the destination system
        if (!_isFinished)
        {
            if (JumpGate.OwningEntity.Manager.TryGetGlobalEntityById(JumpGate.DestinationId, out var destinationEntity))
            {
                destinationEntity.Manager.Transfer(_entityCommanding);
            }
            _isFinished = true;
        }

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

/// <summary>
/// Per-ship jump command. Transfers a single ship through a jump gate
/// when executed (typically after a warp command completes).
/// </summary>
public class ShipJumpCommand : EntityCommand
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement;

    public override bool IsBlocking => false;

    public override string Name { get; } = "Transit jump gate";

    public override string Details { get; } = "Transit through the jump gate";

    Entity _factionEntity;
    Entity _entityCommanding;
    JumpPointDB _jumpGate;

    internal override Entity EntityCommanding => _entityCommanding;

    public static ShipJumpCommand Create(Entity ship, JumpPointDB jumpGate)
    {
        return new ShipJumpCommand()
        {
            RequestingFactionGuid = ship.FactionOwnerID,
            EntityCommandingGuid = ship.Id,
            CreatedDate = ship.Manager.ManagerSubpulses.StarSysDateTime,
            _jumpGate = jumpGate,
        };
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (_jumpGate.OwningEntity == null) { _isFinished = true; return; }

        if (_entityCommanding.Manager.TryGetGlobalEntityById(_jumpGate.DestinationId, out var destinationEntity))
        {
            var destinationPositionDB = destinationEntity.GetDataBlob<PositionDB>();

            // Transfer this ship to the destination system
            destinationEntity.Manager.Transfer(_entityCommanding);

            // Update position to the destination gate
            var positionDB = _entityCommanding.GetDataBlob<PositionDB>();
            positionDB.AbsolutePosition = destinationPositionDB.AbsolutePosition;
            positionDB.SetParent(destinationEntity);
        }

        _isFinished = true;
    }

    internal override bool IsFinished()
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
