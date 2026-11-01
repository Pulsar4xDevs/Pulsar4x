using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Newtonsoft.Json;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Industry;
using Pulsar4X.Industry.Orders;
using Pulsar4X.Storage;

namespace Pulsar4X.Logistics;

/// <summary>
/// The ship's Construction Bay mounts crates from the ship's own hold onto a colony in transfer range.
/// The mount order is instant and the bay works once a day, so this action stays running until every
/// crate is on the colony. It fails when the colony is gone or out of range, or when a crate is neither
/// mounted, queued nor in the hold. It holds the ship in place while it works.
/// </summary>
public class MountFromHoldAction : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement | ActionLaneTypes.InteractWithExternalEntity;

    public override bool IsBlocking => true;

    public override string Name => "Mount crates";

    public override string Details => "Mount " + _crateIds.Count + (_crateIds.Count == 1 ? " crate" : " crates") + ".";

    [JsonProperty]
    int _colonyId;

    [JsonProperty]
    List<int> _crateIds = new();

    [JsonConstructor]
    MountFromHoldAction() { }

    public static MountFromHoldAction Create(Entity ship, int colonyId, IReadOnlyList<int> crateIds)
    {
        var now = ship.StarSysDateTime;
        return new MountFromHoldAction
        {
            RequestingFactionGuid = ship.FactionOwnerID,
            EntityCommandingGuid = ship.Id,
            CreatedDate = now,
            ActionOnDate = now,
            _colonyId = colonyId,
            _crateIds = new List<int>(crateIds),
        };
    }

    public override EntityAction Clone()
    {
        return new MountFromHoldAction
        {
            RequestingFactionGuid = RequestingFactionGuid,
            EntityCommandingGuid = EntityCommandingGuid,
            CreatedDate = CreatedDate,
            ActionOnDate = ActionOnDate,
            ParentGoalId = ParentGoalId,
            _colonyId = _colonyId,
            _crateIds = new List<int>(_crateIds),
        };
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
        if (IsRunning || Status == ActionStatus.Failed || _isFinished)
            return;
        var ship = _entityCommanding;
        if (ship == null || !TryColony(ship, out var colony))
        {
            FailAndFinish("The colony is gone.");
            return;
        }
        if (!LocalConstructionProcessor.InTransferRange(ship, colony))
        {
            FailAndFinish("The colony is out of transfer range.");
            return;
        }

        var bay = ship.GetDataBlob<LocalConstructionDB>();
        foreach (int id in _crateIds)
        {
            if (Mounted(colony, id) || Queued(bay, id))
                continue;
            if (!TryAboard(ship, id, out var crate))
            {
                FailAndFinish("A crate is missing from the hold.");
                return;
            }
            if (!ship.Manager.Game.OrderHandler.HandleOrder(AddToConstructionQueueOrder.CreateMount(ship, crate, colony)))
            {
                FailAndFinish("The Construction Bay cannot mount " + crate.Design.Name + ".");
                return;
            }
        }

        IsRunning = true;
        Status = ActionStatus.Running;
    }

    internal override bool IsFinished()
    {
        if (_isFinished)
            return true;
        if (Status == ActionStatus.Failed)
            return _isFinished = true;
        if (!IsRunning)
            return false;

        var ship = _entityCommanding;
        if (ship == null || !TryColony(ship, out var colony))
        {
            FailAndFinish("The colony is gone.");
            return true;
        }

        var bay = ship.GetDataBlob<LocalConstructionDB>();
        bool pending = false;
        foreach (int id in _crateIds)
        {
            if (Mounted(colony, id))
                continue;
            if (!Queued(bay, id) || !TryAboard(ship, id, out _))
            {
                FailAndFinish("A crate is missing from the hold.");
                return true;
            }
            pending = true;
        }

        if (!pending)
        {
            _isFinished = true;
            return true;
        }

        // The bay only waits at the head of its queue. Say so rather than stall.
        if (!LocalConstructionProcessor.InTransferRange(ship, colony))
        {
            FailAndFinish("The colony is out of transfer range.");
            return true;
        }
        return false;
    }

    bool TryColony(Entity ship, out Entity colony)
    {
        colony = null!;
        return ship.Manager != null
            && ship.Manager.TryGetEntityById(_colonyId, out colony)
            && colony.IsValid
            && colony.HasDataBlob<ColonyInfoDB>()
            && colony.FactionOwnerID == ship.FactionOwnerID;
    }

    /// <summary>Drops this action's own jobs so a failed goal leaves nothing half queued.</summary>
    void FailAndFinish(string message)
    {
        var ship = _entityCommanding;
        if (ship != null && ship.TryGetDataBlob<LocalConstructionDB>(out var bay))
        {
            var keep = new Queue<LocalConstructionJob>();
            foreach (var job in bay.BuildQueue)
            {
                if (job.Instance == null || !_crateIds.Contains(job.Instance.ID))
                    keep.Enqueue(job);
            }
            bay.BuildQueue = keep;
        }
        Fail(message);
        _isFinished = true;
    }

    internal static bool Mounted(Entity colony, int crateId)
    {
        if (!colony.TryGetDataBlob<ComponentInstancesDB>(out var instances))
            return false;
        foreach (var instance in instances.AllComponents.Values)
        {
            if (instance.ID == crateId)
                return true;
        }
        return false;
    }

    internal static bool Queued(LocalConstructionDB bay, int crateId)
    {
        foreach (var job in bay.BuildQueue)
        {
            if (job.Instance != null && job.Instance.ID == crateId)
                return true;
        }
        return false;
    }

    internal static bool TryAboard(Entity entity, int crateId, out ComponentInstance crate)
    {
        crate = null!;
        if (!MoveGoodsPlan.TryCargo(entity, crateId, out var cargo) || cargo is not ComponentInstance found)
            return false;
        crate = found;
        return entity.TryGetDataBlob<CargoStorageDB>(out var storage) && storage.GetUnitsStored(found, false) >= 1;
    }
}
