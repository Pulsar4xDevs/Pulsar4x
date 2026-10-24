using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Newtonsoft.Json;
using Pulsar4X.Engine;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// Empty chair. Escrow waits until this action starts, so a queue out of range holds nothing.
/// When <see cref="_plotDestination"/> is set, a finished load inserts the destination flight
/// and an unload immediately after itself.
/// </summary>
public class LoadGoodsAction : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement | ActionLaneTypes.InteractWithExternalEntity;

    public override bool IsBlocking => true;

    public override string Name => "Load goods";

    public override string Details => _plotDestination
        ? "Load, then plot the flight to the destination."
        : "Load from the colony.";

    [JsonProperty]
    int _colonyId;

    [JsonProperty]
    int _destId;

    [JsonProperty]
    bool _plotDestination;

    [JsonProperty]
    List<MoveGoodsLine> _lines = new();

    /// <summary>The ship's half. It is not queued beside this action. Both use the same lanes.</summary>
    [JsonProperty]
    CargoTransferOrder _shipOrder;

    /// <summary>The second IsFinished in one processor pass must not insert the flight twice.</summary>
    [JsonProperty]
    bool _followOn;

    [JsonConstructor]
    LoadGoodsAction() { }

    public static LoadGoodsAction Create(
        Entity ship, int colonyId, int destId, IReadOnlyList<MoveGoodsLine> lines, bool plotDestination)
    {
        var now = ship.StarSysDateTime;
        return new LoadGoodsAction
        {
            RequestingFactionGuid = ship.FactionOwnerID,
            EntityCommandingGuid = ship.Id,
            CreatedDate = now,
            ActionOnDate = now,
            _colonyId = colonyId,
            _destId = destId,
            _plotDestination = plotDestination,
            _lines = MoveGoodsPlan.CopyLines(lines),
        };
    }

    public override void Rehydrate(Game game)
    {
        base.Rehydrate(game);
        _shipOrder?.Rehydrate(game);
    }

    internal override bool IsValidCommand(Game game)
    {
        if (!CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _, out var entity))
            return false;
        _entityCommanding = entity;
        return true;
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (IsRunning || Status == ActionStatus.Failed)
            return;
        if (_entityCommanding == null)
        {
            Fail("The colony is gone.");
            _isFinished = true;
            return;
        }

        var game = _entityCommanding.Manager.Game;
        if (!game.GlobalManager.TryGetGlobalEntityById(_colonyId, out var colony))
        {
            Fail("The colony is gone.");
            _isFinished = true;
            return;
        }
        if (!MarketExchangeAction.InRange(_entityCommanding, colony))
        {
            Fail("out of range");
            _isFinished = true;
            return;
        }

        var clamped = MoveGoodsPlan.ClampLines(colony, _entityCommanding, _lines, useLoaded: false);
        if (clamped.Count == 0)
        {
            Fail("nothing to load");
            _isFinished = true;
            return;
        }

        var (shipOrder, colonyOrder) = CargoTransferOrder.CreateLoadPair(
            _entityCommanding.FactionOwnerID, _entityCommanding, colony, clamped);
        if (!shipOrder.IsValidCommand(game))
        {
            shipOrder.Stop(atDateTime);
            Fail("nothing to load");
            _isFinished = true;
            return;
        }

        shipOrder.Execute(atDateTime);
        shipOrder.Status = ActionStatus.Running;
        if (!game.OrderHandler.HandleOrder(colonyOrder))
        {
            shipOrder.Stop(atDateTime);
            Fail("nothing to load");
            _isFinished = true;
            return;
        }

        MoveGoodsPlan.NoteMoved(shipOrder, _lines, loading: true);
        if (!_lines.Exists(line => line.Loaded > 0))
        {
            shipOrder.Stop(atDateTime);
            Fail("nothing to load");
            _isFinished = true;
            return;
        }

        _shipOrder = shipOrder;
        IsRunning = true;
        Status = ActionStatus.Running;
    }

    internal override bool IsFinished()
    {
        if (Status == ActionStatus.Failed)
            return _isFinished = true;
        if (!IsRunning || _shipOrder == null)
            return _isFinished = false;
        if (_shipOrder.Status == ActionStatus.Failed)
        {
            Fail(string.IsNullOrEmpty(_shipOrder.FailureMessage) ? "nothing to load" : _shipOrder.FailureMessage);
            return _isFinished = true;
        }
        if (!_shipOrder.IsFinished())
            return _isFinished = false;
        if (_followOn || !_plotDestination)
        {
            if (!_plotDestination && !_lines.Exists(line => line.Loaded > 0))
            {
                Fail("nothing to load");
                return _isFinished = true;
            }
            return _isFinished = true;
        }

        _followOn = true;
        if (!_lines.Exists(line => line.Loaded > 0))
        {
            Fail("nothing to load");
            return _isFinished = true;
        }
        if (!InsertDestination())
            return _isFinished = true;
        return _isFinished = true;
    }

    bool InsertDestination()
    {
        var ship = _entityCommanding;
        var game = ship.Manager.Game;
        if (!game.GlobalManager.TryGetGlobalEntityById(_destId, out var dest))
        {
            Fail("The colony is gone.");
            return false;
        }

        var moves = new List<EntityAction>();
        if (!MarketExchangeAction.InRange(ship, dest))
        {
            if (!MoveGoodsPlan.TryMoveIntoRange(ship, dest, out moves, out var reason))
            {
                Fail(string.IsNullOrEmpty(reason) ? "out of range" : reason);
                return false;
            }
        }

        if (!ship.TryGetDataBlob<ActionQueueDB>(out var queue))
        {
            Fail("The entity has no order queue.");
            return false;
        }

        var prepared = new List<EntityAction>();
        foreach (var move in moves)
        {
            if (!move.IsValidCommand(game))
            {
                Fail("out of range");
                return false;
            }
            move.ParentGoalId = ParentGoalId;
            prepared.Add(move);
        }

        var unloadLines = new List<MoveGoodsLine>();
        foreach (var line in _lines)
        {
            if (line.Loaded > 0)
                unloadLines.Add(new MoveGoodsLine { ItemId = line.ItemId, Requested = line.Loaded });
        }
        var unload = UnloadGoodsAction.Create(ship, _destId, unloadLines);
        unload.ParentGoalId = ParentGoalId;
        if (!unload.IsValidCommand(game))
        {
            Fail("The colony is gone.");
            return false;
        }
        prepared.Add(unload);

        int index = IndexOf(queue);
        if (index < 0)
        {
            Fail("The entity has no order queue.");
            return false;
        }
        int at = index + 1;
        foreach (var action in prepared)
            queue.ActionList.Insert(at++, action);
        return true;
    }

    int IndexOf(ActionQueueDB queue)
    {
        for (int i = 0; i < queue.ActionList.Count; i++)
        {
            if (ReferenceEquals(queue.ActionList[i], this))
                return i;
        }
        return -1;
    }

    public override void Stop(DateTime at)
    {
        _shipOrder?.Stop(at);
    }

    public override EntityAction Clone()
    {
        return new LoadGoodsAction
        {
            RequestingFactionGuid = RequestingFactionGuid,
            EntityCommandingGuid = EntityCommandingGuid,
            CreatedDate = CreatedDate,
            ActionOnDate = ActionOnDate,
            ParentGoalId = ParentGoalId,
            _colonyId = _colonyId,
            _destId = _destId,
            _plotDestination = _plotDestination,
            _lines = MoveGoodsPlan.CopyLines(_lines ?? new List<MoveGoodsLine>()),
        };
    }
}

/// <summary>
/// Empty chair, in cargo range. Escrow waits until this action starts.
/// </summary>
public class UnloadGoodsAction : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement | ActionLaneTypes.InteractWithExternalEntity;

    public override bool IsBlocking => true;

    public override string Name => "Unload goods";

    public override string Details => "Unload onto the colony.";

    [JsonProperty]
    int _colonyId;

    [JsonProperty]
    List<MoveGoodsLine> _lines = new();

    [JsonProperty]
    CargoTransferOrder _shipOrder;

    [JsonConstructor]
    UnloadGoodsAction() { }

    public static UnloadGoodsAction Create(Entity ship, int colonyId, IReadOnlyList<MoveGoodsLine> lines)
    {
        var now = ship.StarSysDateTime;
        return new UnloadGoodsAction
        {
            RequestingFactionGuid = ship.FactionOwnerID,
            EntityCommandingGuid = ship.Id,
            CreatedDate = now,
            ActionOnDate = now,
            _colonyId = colonyId,
            _lines = MoveGoodsPlan.CopyLines(lines),
        };
    }

    public override void Rehydrate(Game game)
    {
        base.Rehydrate(game);
        _shipOrder?.Rehydrate(game);
    }

    internal override bool IsValidCommand(Game game)
    {
        if (!CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _, out var entity))
            return false;
        _entityCommanding = entity;
        return true;
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (IsRunning || Status == ActionStatus.Failed)
            return;
        if (_entityCommanding == null)
        {
            Fail("The colony is gone.");
            _isFinished = true;
            return;
        }

        var game = _entityCommanding.Manager.Game;
        if (!game.GlobalManager.TryGetGlobalEntityById(_colonyId, out var colony))
        {
            Fail("The colony is gone.");
            _isFinished = true;
            return;
        }
        if (!MarketExchangeAction.InRange(_entityCommanding, colony))
        {
            Fail("out of range");
            _isFinished = true;
            return;
        }

        var clamped = MoveGoodsPlan.ClampLines(_entityCommanding, colony, _lines, useLoaded: false);
        if (clamped.Count == 0)
        {
            Fail("no place to unload");
            _isFinished = true;
            return;
        }

        var signed = new List<(ICargoable item, long amount)>(clamped.Count);
        foreach (var (item, amount) in clamped)
            signed.Add((item, -amount));

        var (shipOrder, colonyOrder) = CargoTransferOrder.CreateUnloadPair(
            _entityCommanding.FactionOwnerID, _entityCommanding, colony, signed);
        if (!shipOrder.IsValidCommand(game))
        {
            shipOrder.Stop(atDateTime);
            Fail("no place to unload");
            _isFinished = true;
            return;
        }

        shipOrder.Execute(atDateTime);
        shipOrder.Status = ActionStatus.Running;
        if (!game.OrderHandler.HandleOrder(colonyOrder))
        {
            shipOrder.Stop(atDateTime);
            Fail("no place to unload");
            _isFinished = true;
            return;
        }

        MoveGoodsPlan.NoteMoved(shipOrder, _lines, loading: false);
        if (!_lines.Exists(line => line.Delivered > 0))
        {
            shipOrder.Stop(atDateTime);
            Fail("no place to unload");
            _isFinished = true;
            return;
        }

        _shipOrder = shipOrder;
        IsRunning = true;
        Status = ActionStatus.Running;
    }

    internal override bool IsFinished()
    {
        if (Status == ActionStatus.Failed)
            return _isFinished = true;
        if (!IsRunning || _shipOrder == null)
            return _isFinished = false;
        if (_shipOrder.Status == ActionStatus.Failed)
        {
            Fail(string.IsNullOrEmpty(_shipOrder.FailureMessage) ? "no place to unload" : _shipOrder.FailureMessage);
            return _isFinished = true;
        }
        if (!_shipOrder.IsFinished())
            return _isFinished = false;
        return _isFinished = true;
    }

    public override void Stop(DateTime at)
    {
        _shipOrder?.Stop(at);
    }

    public override EntityAction Clone()
    {
        return new UnloadGoodsAction
        {
            RequestingFactionGuid = RequestingFactionGuid,
            EntityCommandingGuid = EntityCommandingGuid,
            CreatedDate = CreatedDate,
            ActionOnDate = ActionOnDate,
            ParentGoalId = ParentGoalId,
            _colonyId = _colonyId,
            _lines = MoveGoodsPlan.CopyLines(_lines ?? new List<MoveGoodsLine>()),
        };
    }
}
