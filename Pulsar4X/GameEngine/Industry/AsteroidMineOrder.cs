using System;
using System.Collections.Generic;
using GameEngine.Engine.Orders;
using Pulsar4X.Energy;
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
            // Crew plan phase 3: a miner short of crew digs at F².
            long budget = (long)Math.Floor(rate * wholeDays * Pulsar4X.Ships.ShipCrew.WorkRate(_entityCommanding) + 1e-9);
            var library = _entityCommanding.GetFactionCargoDefinitions();
            if (library != null && budget > 0)
            {
                // Energy phase 6: digging takes each mineral's extraction energy from the ship's plant.
                var plan = PlanMining(minerals, cargo, library, budget);
                double needed = 0;
                foreach (var (mineral, _, wanted) in plan)
                    needed += MineKJ(mineral, wanted);
                double share = 1;
                if (needed > 0)
                {
                    double got = ShipPower.DrawEnergy(_entityCommanding, needed, wholeDays * 86400.0, atDateTime);
                    share = Math.Clamp(got / needed, 0, 1);
                }
                long total = 0;
                foreach (var (mineral, deposit, wanted) in plan)
                {
                    long units = (long)Math.Floor(wanted * share + 1e-9);
                    if (units <= 0)
                        continue;
                    long stored = cargo.AddCargoByUnit(mineral, units);
                    MiningHelper.Deplete(deposit, stored);
                    total += stored;
                }
                if (needed > 0 && share < 1)
                    _details = total > 0 ? "Mining, short of power" : "No power for the miner";
                else
                    _details = "Mining";
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

    /// <summary>kJ to dig this many units: mass x the mineral's extraction energy (kWh/kg) x 3600.</summary>
    public static double MineKJ(Mineral mineral, long units)
        => units > 0 ? units * (double)mineral.MassPerUnit * EmbodiedEnergy.ExtractionOf(mineral) * 3600.0 : 0;

    /// <summary>What a day's budget would dig of each mineral, before power is counted.</summary>
    static List<(Mineral mineral, MineralDeposit deposit, long wanted)> PlanMining(MineralsDB minerals, CargoStorageDB cargo, CargoDefinitionsLibrary library, long budget)
    {
        var plan = new List<(Mineral, MineralDeposit, long)>();
        foreach (var (id, deposit) in minerals.Minerals)
        {
            if (deposit == null || deposit.Amount.Actual <= 0)
                continue;
            if (!library.GetMinerals().TryGetValue(id, out var mineral) || mineral == null)
                continue;
            if (!cargo.TypeStores.ContainsKey(mineral.CargoTypeID))
                continue;
            long free = CargoMath.GetFreeUnitSpace(cargo, mineral);
            long wanted = Math.Min(budget, Math.Min(deposit.Amount.Actual, free));
            if (wanted > 0)
                plan.Add((mineral, deposit, wanted));
        }
        return plan;
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
