using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Movement;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry;

/// <summary>
/// Mine one asteroid while this action holds the lane. First execute arms the clock.
/// Later executes, once a day has passed, pull UnitsPerDay for each mineral that fits.
/// Finishes when the rock is empty or the hold cannot take another unit of anything left.
/// </summary>
public class AsteroidMineOrder : EntityAction
{
    /// <summary>Same close-enough distance a static site uses.</summary>
    public const double OnSiteMeters = 100_000;

    public override ActionLaneTypes ActionLanes => ActionLaneTypes.Movement | ActionLaneTypes.InteractWithExternalEntity;

    public override bool IsBlocking => true;

    public override string Name => Target == null ? "Mine asteroid" : $"Mine {Target.GetOwnersName()}";

    public override string Details => _details;

    string _details = "";

    public Entity Target { get; private set; }
    public DateTime? PreviousUpdate { get; private set; }

    Entity _entityCommanding;

    internal override Entity EntityCommanding => _entityCommanding;

    public AsteroidMineOrder() { }

    public AsteroidMineOrder(Entity commandingEntity, Entity target)
    {
        _entityCommanding = commandingEntity;
        Target = target;
        RequestingFactionGuid = commandingEntity.FactionOwnerID;
        EntityCommandingGuid = commandingEntity.Id;
    }

    public override EntityAction Clone()
    {
        return new AsteroidMineOrder(EntityCommanding, Target)
        {
            UseActionLanes = UseActionLanes,
            RequestingFactionGuid = RequestingFactionGuid,
            EntityCommandingGuid = EntityCommandingGuid,
            CreatedDate = CreatedDate,
            ActionOnDate = ActionOnDate,
            ActionedOnDate = ActionedOnDate,
            IsRunning = IsRunning,
            PreviousUpdate = PreviousUpdate,
        };
    }

    internal override bool IsFinished()
    {
        if (_isFinished)
            return true;
        if (!IsRunning || _entityCommanding == null || Target == null)
            return false;
        return !CanTakeMore(_entityCommanding, Target);
    }

    internal override void Execute(DateTime atDateTime)
    {
        if (_isFinished)
            return;
        if (_entityCommanding == null || Target == null)
        {
            _isFinished = true;
            return;
        }

        if (!OnSite(_entityCommanding, Target))
        {
            _details = "Not at the asteroid";
            _isFinished = true;
            return;
        }

        if (!IsRunning)
        {
            IsRunning = true;
            PreviousUpdate = atDateTime;
            _details = "Mining";
            return;
        }

        if (PreviousUpdate == null || atDateTime - PreviousUpdate < TimeSpan.FromDays(1))
            return;

        long wholeDays = (long)Math.Floor((atDateTime - PreviousUpdate.Value).TotalDays);
        if (wholeDays < 1)
            return;

        int rate = 0;
        if (_entityCommanding.TryGetDataBlob<AsteroidMineAbilityDB>(out var ability))
            rate = ability.UnitsPerDay;

        if (rate > 0 && Target.TryGetDataBlob<MineralsDB>(out var minerals)
            && _entityCommanding.TryGetDataBlob<CargoStorageDB>(out var cargo))
        {
            long budget = rate * wholeDays;
            var library = _entityCommanding.GetFactionCargoDefinitions();
            if (library != null && budget > 0)
            {
                MineIntoHold(minerals, cargo, library, budget);
                CargoTransferProcessor.UpdateMassFuelAndDeltaV(_entityCommanding);
            }
        }

        PreviousUpdate = atDateTime;
        if (!CanTakeMore(_entityCommanding, Target))
            _isFinished = true;
    }

    internal override bool IsValidCommand(Game game)
    {
        return _entityCommanding != null && Target != null;
    }

    static void MineIntoHold(MineralsDB minerals, CargoStorageDB cargo, CargoDefinitionsLibrary library, long budget)
    {
        foreach (var (id, deposit) in minerals.Minerals)
        {
            if (deposit == null || deposit.Amount.Actual <= 0)
                continue;

            if (!library.GetMinerals().TryGetValue(id, out var mineral) || mineral == null)
                continue;
            if (!cargo.TypeStores.ContainsKey(mineral.CargoTypeID))
                continue;

            long free = CargoMath.GetFreeUnitSpace(cargo, mineral);
            long wanted = budget;
            if (wanted > deposit.Amount.Actual)
                wanted = deposit.Amount.Actual;
            if (wanted > free)
                wanted = free;
            if (wanted <= 0)
                continue;

            long stored = cargo.AddCargoByUnit(mineral, wanted);
            MiningHelper.Deplete(deposit, stored);
        }
    }

    /// <summary>True when some mineral still on the rock will fit at least one more unit.</summary>
    public static bool CanTakeMore(Entity ship, Entity rock)
    {
        if (!rock.TryGetDataBlob<MineralsDB>(out var minerals))
            return false;
        if (!ship.TryGetDataBlob<CargoStorageDB>(out var cargo))
            return false;
        var library = ship.GetFactionCargoDefinitions();
        if (library == null)
            return false;

        foreach (var (id, deposit) in minerals.Minerals)
        {
            if (deposit == null || deposit.Amount.Actual <= 0)
                continue;

            if (!library.GetMinerals().TryGetValue(id, out var mineral) || mineral == null)
                continue;
            if (!cargo.TypeStores.ContainsKey(mineral.CargoTypeID))
                continue;
            if (CargoMath.GetFreeUnitSpace(cargo, mineral) >= 1)
                return true;
        }

        return false;
    }

    public static bool OnSite(Entity ship, Entity rock)
    {
        if (ship.TryGetDataBlob<PositionDB>(out var shipPos) && shipPos.Parent == rock)
            return true;
        if (ship.TryGetDataBlob<PositionDB>(out shipPos) && rock.TryGetDataBlob<PositionDB>(out var rockPos))
        {
            double sep = shipPos.GetDistanceTo_m(rockPos);
            return double.IsFinite(sep) && sep <= OnSiteMeters;
        }
        return false;
    }
}

/// <summary>
/// One day of waiting. The ship stays on the goal, and the next boundary plans again.
/// An empty queue would mark the goal completed.
/// </summary>
public class UnloadWaitOrder : EntityAction
{
    public override ActionLaneTypes ActionLanes => ActionLaneTypes.InteractWithExternalEntity;

    public override bool IsBlocking => true;

    public override string Name => "Waiting to unload";

    public override string Details => "No colony can take this cargo yet";

    DateTime? _started;
    Entity _entityCommanding;

    internal override Entity EntityCommanding => _entityCommanding;

    public UnloadWaitOrder() { }

    public UnloadWaitOrder(Entity commandingEntity)
    {
        _entityCommanding = commandingEntity;
        RequestingFactionGuid = commandingEntity.FactionOwnerID;
        EntityCommandingGuid = commandingEntity.Id;
    }

    public override EntityAction Clone()
    {
        return new UnloadWaitOrder(_entityCommanding)
        {
            UseActionLanes = UseActionLanes,
            RequestingFactionGuid = RequestingFactionGuid,
            EntityCommandingGuid = EntityCommandingGuid,
            CreatedDate = CreatedDate,
            ActionOnDate = ActionOnDate,
            ActionedOnDate = ActionedOnDate,
            IsRunning = IsRunning,
        };
    }

    internal override bool IsFinished() => _isFinished;

    internal override void Execute(DateTime atDateTime)
    {
        if (_isFinished)
            return;
        if (!IsRunning)
        {
            IsRunning = true;
            _started = atDateTime;
            return;
        }

        if (_started != null && atDateTime - _started >= TimeSpan.FromDays(1))
            _isFinished = true;
    }

    internal override bool IsValidCommand(Game game)
    {
        return _entityCommanding != null;
    }
}
