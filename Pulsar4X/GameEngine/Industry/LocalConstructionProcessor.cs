using System;
using System.Collections.Generic;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Events;
using Pulsar4X.Extensions;
using Pulsar4X.Datablobs;
using Pulsar4X.Factions;
using Pulsar4X.Interfaces;
using Pulsar4X.Names;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry;

/// <summary>
/// Spends an entity's installation points on the head of its construction queue.
/// A colony mounts on itself. A ship mounts on the colony named by the job.
/// Cargo and resources come from the actor. The colony does not need a warehouse or its own rate.
/// </summary>
public class LocalConstructionProcessor : IHotloopProcessor
{
    public TimeSpan RunFrequency
    {
        get { return TimeSpan.FromDays(1); }
    }

    public TimeSpan FirstRunOffset => TimeSpan.FromHours(6);

    public Type GetParameterType => typeof(LocalConstructionDB);

    public void Init(Game game)
    {
    }

    public void ProcessEntity(Entity entity, int deltaSeconds)
    {
        if (!entity.TryGetDataBlob<LocalConstructionDB>(out var constructionDB))
            return;
        if (!entity.TryGetDataBlob<CargoStorageDB>(out var storage))
            return;

        long pointsLeft = constructionDB.PointsPerDay;
        while (pointsLeft > 0 && constructionDB.BuildQueue.Count > 0)
        {
            var job = constructionDB.BuildQueue.Peek();
            if (!TryResolveTarget(entity, job, out var target))
                break;
            if (!TryAdvance(entity, storage, job, ref pointsLeft))
                break;

            if (!job.IsComplete)
                continue;

            if (!TryFinish(storage, target, job))
                break;

            constructionDB.BuildQueue.Dequeue();
            EventManager.Instance.Publish(
                Event.Create(
                    EventType.ProductionCompleted,
                    entity.StarSysDateTime,
                    $"Completed construction of {job.Design.Name}",
                    entity.FactionOwnerID,
                    entity.Manager.ManagerID,
                    entity.Id));
        }

        SpendRefit(entity, storage, constructionDB);
    }

    /// <summary>
    /// Spends ship-refit points on the head refit. Installation points are not used.
    /// Parts move when the job finishes. A ship that is out of range waits at the head.
    /// </summary>
    static void SpendRefit(Entity yard, CargoStorageDB storage, LocalConstructionDB construction)
    {
        long pointsLeft = construction.RefitPointsPerDay;
        while (pointsLeft > 0 && construction.RefitQueue.Count > 0)
        {
            var job = construction.RefitQueue.Peek();
            if (!RefitReady(yard, storage, job))
                break;

            long needed = job.PointCost - job.PointsAccumulated;
            if (needed <= 0)
            {
                if (!TryApplyRefit(yard, storage, job))
                    break;
                construction.RefitQueue.Dequeue();
                PublishRefitCompleted(yard, job);
                continue;
            }

            long spend = Math.Min(pointsLeft, needed);
            if (spend < needed)
            {
                job.PointsAccumulated += spend;
                break;
            }

            if (!TryApplyRefit(yard, storage, job))
                break;

            job.PointsAccumulated += spend;
            pointsLeft -= spend;
            construction.RefitQueue.Dequeue();
            PublishRefitCompleted(yard, job);
        }
    }

    static bool RefitReady(Entity yard, CargoStorageDB storage, ShipRefitJob job)
    {
        if (job.PointCost <= 0)
            return false;
        if (yard.Manager == null || !yard.Manager.TryGetEntityById(job.ShipEntityId, out var ship))
            return false;
        if (ship.FactionOwnerID != yard.FactionOwnerID || !InTransferRange(yard, ship))
            return false;
        foreach (var part in job.Remove)
        {
            if (!ShipRefitJob.OnShip(ship, part.ID))
                return false;
        }
        foreach (var part in job.Mount)
        {
            if (storage.GetUnitsStored(part, false) < 1)
                return false;
        }
        return CargoFits(storage, job);
    }

    static bool CargoFits(CargoStorageDB storage, ShipRefitJob job)
    {
        var need = new Dictionary<string, double>();
        foreach (var part in job.Remove)
            AddVolume(need, part.CargoTypeID, part.VolumePerUnit);
        foreach (var part in job.Mount)
            AddVolume(need, part.CargoTypeID, -part.VolumePerUnit);

        foreach (var (cargoType, volume) in need)
        {
            if (volume <= 0)
                continue;
            if (!storage.TypeStores.TryGetValue(cargoType, out var store))
                return false;
            if (store.FreeVolume + 1e-9 < volume)
                return false;
        }
        return true;
    }

    static void AddVolume(Dictionary<string, double> need, string cargoType, double volume)
    {
        need.TryGetValue(cargoType, out var current);
        need[cargoType] = current + volume;
    }

    static bool TryApplyRefit(Entity yard, CargoStorageDB storage, ShipRefitJob job)
    {
        if (!yard.Manager!.TryGetEntityById(job.ShipEntityId, out var ship))
            return false;

        var taken = new List<ComponentInstance>();
        foreach (var part in job.Mount)
        {
            if (storage.RemoveCargoByUnit(part, 1) < 1)
            {
                RestoreCargo(storage, taken);
                return false;
            }
            taken.Add(part);
        }

        var removed = new List<ComponentInstance>();
        foreach (var part in job.Remove)
        {
            if (!ShipRefitJob.OnShip(ship, part.ID))
            {
                Remount(ship, removed);
                RestoreCargo(storage, taken);
                return false;
            }
            ship.RemoveComponent(part);
            removed.Add(part);
        }

        var stored = new List<ComponentInstance>();
        foreach (var part in removed)
        {
            if (storage.AddCargoByUnit(part, 1) < 1)
            {
                foreach (var back in stored)
                    storage.RemoveCargoByUnit(back, 1);
                Remount(ship, removed);
                RestoreCargo(storage, taken);
                return false;
            }
            stored.Add(part);
        }

        foreach (var part in taken)
            ship.AddComponent(part);
        AdoptDesign(yard, ship, job);
        return true;
    }

    static void AdoptDesign(Entity yard, Entity ship, ShipRefitJob job)
    {
        if (string.IsNullOrEmpty(job.TargetDesignId))
            return;
        var game = yard.Manager?.Game;
        if (game == null || !game.Factions.TryGetValue(yard.FactionOwnerID, out var faction))
            return;
        if (!faction.TryGetDataBlob<FactionInfoDB>(out var info))
            return;
        if (!info.ShipDesigns.TryGetValue(job.TargetDesignId, out var design))
            return;
        if (!CountsMatch(ship, design))
            return;
        ship.GetDataBlob<ShipInfoDB>().SetDesign(design);
    }

    static bool CountsMatch(Entity ship, ShipDesign design)
    {
        var expected = new Dictionary<string, int>();
        if (design.Components != null)
        {
            foreach (var (part, count) in design.Components)
            {
                if (part == null || count <= 0)
                    continue;
                expected.TryGetValue(part.UniqueID, out var have);
                expected[part.UniqueID] = have + count;
            }
        }

        var actual = new Dictionary<string, int>();
        if (ship.TryGetDataBlob<ComponentInstancesDB>(out var components))
        {
            foreach (var instance in components.AllComponents.Values)
            {
                var id = instance.Design?.UniqueID;
                if (string.IsNullOrEmpty(id))
                    continue;
                actual.TryGetValue(id, out var have);
                actual[id] = have + 1;
            }
        }

        if (actual.Count != expected.Count)
            return false;
        foreach (var pair in expected)
        {
            if (!actual.TryGetValue(pair.Key, out var have) || have != pair.Value)
                return false;
        }
        return true;
    }

    static void Remount(Entity ship, List<ComponentInstance> removed)
    {
        foreach (var part in removed)
        {
            if (!ShipRefitJob.OnShip(ship, part.ID))
                ship.AddComponent(part);
        }
    }

    static void RestoreCargo(CargoStorageDB storage, List<ComponentInstance> taken)
    {
        foreach (var part in taken)
        {
            if (storage.GetUnitsStored(part, false) < 1)
                storage.AddCargoByUnit(part, 1);
        }
    }

    static void PublishRefitCompleted(Entity yard, ShipRefitJob job)
    {
        EventManager.Instance.Publish(
            Event.Create(
                EventType.ShipRefitCompleted,
                yard.StarSysDateTime,
                "Refit completed",
                yard.FactionOwnerID,
                yard.Manager.ManagerID,
                job.ShipEntityId));
    }

    public int ProcessManager(EntityManager manager, int deltaSeconds)
    {
        var entities = manager.GetAllEntitiesWithDataBlob<LocalConstructionDB>();
        foreach (var entity in entities)
            ProcessEntity(entity, deltaSeconds);

        return entities.Count;
    }

    static bool TryAdvance(Entity entity, CargoStorageDB storage, LocalConstructionJob job, ref long pointsLeft)
    {
        if (job.PointCost <= 0)
            return false;

        if (job.Instance != null)
            return TryAdvanceMount(storage, job, ref pointsLeft);

        if (LocalConstructionJob.IsSiteOnly(job.Design))
            return TryAdvanceAssemble(entity, storage, job, ref pointsLeft);

        return false;
    }

    static bool TryAdvanceMount(CargoStorageDB storage, LocalConstructionJob job, ref long pointsLeft)
    {
        var instance = job.Instance!;
        if (!LocalConstructionJob.IsCrate(instance.Design))
            return false;
        if (storage.GetUnitsStored(instance, false) < 1)
            return false;

        long needed = job.PointCost - job.PointsAccumulated;
        if (needed <= 0)
            return true;

        long spend = Math.Min(pointsLeft, needed);
        if (spend <= 0)
            return false;

        job.PointsAccumulated += spend;
        pointsLeft -= spend;
        return true;
    }

    static bool TryAdvanceAssemble(Entity entity, CargoStorageDB storage, LocalConstructionJob job, ref long pointsLeft)
    {
        if (!TryGetFaction(entity, out var factionInfo))
            return false;
        if (!ResourcesOnHand(storage, factionInfo, job))
            return false;

        long needed = job.PointCost - job.PointsAccumulated;
        if (needed <= 0)
            return ResourcesSettled(job);

        long spend = Math.Min(pointsLeft, needed);
        if (spend <= 0)
            return false;

        if (!ConsumeShare(storage, factionInfo, job, spend, needed, spend == needed))
            return false;

        job.PointsAccumulated += spend;
        pointsLeft -= spend;
        return true;
    }

    /// <summary>
    /// Every remaining input must be in the warehouse. A short input spends nothing and changes no stock.
    /// </summary>
    static bool ResourcesOnHand(CargoStorageDB storage, FactionInfoDB factionInfo, LocalConstructionJob job)
    {
        foreach (var (resourceId, remaining) in job.ResourcesRemaining)
        {
            if (remaining <= 0)
                continue;
            if (!TryResolve(factionInfo, resourceId, out var cargoable))
                return false;
            if (storage.GetUnitsStored(cargoable, false) < remaining)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Takes this tick's share of the recipe. The finishing tick takes whatever is left.
    /// </summary>
    static bool ConsumeShare(
        CargoStorageDB storage, FactionInfoDB factionInfo, LocalConstructionJob job,
        long spend, long needed, bool finishing)
    {
        foreach (var (resourceId, remaining) in new Dictionary<string, long>(job.ResourcesRemaining))
        {
            if (remaining <= 0)
                continue;

            long take = finishing ? remaining : remaining * spend / needed;
            if (take <= 0)
                continue;
            if (!TryResolve(factionInfo, resourceId, out var cargoable))
                return false;

            long removed = storage.RemoveCargoByUnit(cargoable, take);
            job.ResourcesRemaining[resourceId] = remaining - removed;
            if (removed != take)
                return false;
        }
        return true;
    }

    /// <summary>
    /// A zero target mounts on the actor. A ship job names a same-faction colony that is still in
    /// cargo-transfer range. Anything else waits at the head and spends nothing.
    /// </summary>
    static bool TryResolveTarget(Entity actor, LocalConstructionJob job, out Entity target)
    {
        if (job.TargetEntityId == 0)
        {
            target = actor;
            return true;
        }

        target = null!;
        if (actor.Manager == null || !actor.Manager.TryGetEntityById(job.TargetEntityId, out var colony))
            return false;
        if (!colony.HasDataBlob<ColonyInfoDB>() || colony.FactionOwnerID != actor.FactionOwnerID)
            return false;
        if (!InTransferRange(actor, colony))
            return false;
        target = colony;
        return true;
    }

    static bool TryFinish(CargoStorageDB storage, Entity target, LocalConstructionJob job)
    {
        if (job.Instance != null)
        {
            if (storage.RemoveCargoByUnit(job.Instance, 1) < 1)
                return false;
            target.AddComponent(job.Instance);
            return true;
        }

        if (!LocalConstructionJob.IsSiteOnly(job.Design) || !ResourcesSettled(job))
            return false;

        target.AddComponent(job.Design);
        return true;
    }

    /// <summary>
    /// Cargo-transfer range: both sides have cargo storage, they share a parent with a mass, and
    /// <see cref="CargoTransferProcessor.CalcTransferRate"/> is above zero.
    /// </summary>
    public static bool InTransferRange(Entity from, Entity to)
    {
        if (!from.TryGetDataBlob<CargoStorageDB>(out var fromCargo))
            return false;
        if (!to.TryGetDataBlob<CargoStorageDB>(out var toCargo))
            return false;

        var parent = from.GetSOIParentEntity();
        if (parent == null || parent != to.GetSOIParentEntity())
            return false;
        if (!parent.TryGetDataBlob<MassVolumeDB>(out _))
            return false;

        double dv = CargoTransferProcessor.CalcDVDifference_m(from, to);
        if (double.IsNaN(dv) || double.IsInfinity(dv))
            return false;
        return CargoTransferProcessor.CalcTransferRate(dv, fromCargo, toCargo) > 0;
    }

    /// <summary>
    /// Same-faction colonies in cargo-transfer range, ordered by name then id.
    /// </summary>
    public static List<Entity> ColoniesInRange(Entity ship)
    {
        var found = new List<Entity>();
        if (ship.Manager == null)
            return found;

        foreach (var colony in ship.Manager.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
        {
            if (colony.FactionOwnerID != ship.FactionOwnerID)
                continue;
            if (!InTransferRange(ship, colony))
                continue;
            found.Add(colony);
        }

        int factionId = ship.FactionOwnerID;
        found.Sort((a, b) =>
        {
            int byName = string.Compare(NameOf(a, factionId), NameOf(b, factionId), StringComparison.Ordinal);
            return byName != 0 ? byName : a.Id.CompareTo(b.Id);
        });
        return found;
    }

    static string NameOf(Entity entity, int factionId)
        => entity.TryGetDataBlob<NameDB>(out var name) ? name.GetName(factionId) : "";

    static bool ResourcesSettled(LocalConstructionJob job)
    {
        foreach (var remaining in job.ResourcesRemaining.Values)
            if (remaining > 0)
                return false;
        return true;
    }

    static bool TryGetFaction(Entity entity, out FactionInfoDB factionInfo)
    {
        factionInfo = null!;
        var game = entity.Manager?.Game;
        if (game == null || !game.Factions.TryGetValue(entity.FactionOwnerID, out var faction))
            return false;
        return faction.TryGetDataBlob(out factionInfo);
    }

    static bool TryResolve(FactionInfoDB factionInfo, string resourceId, out ICargoable cargoable)
    {
        if (factionInfo.Data.CargoGoods.Contains(resourceId))
        {
            var good = factionInfo.Data.CargoGoods.GetAny(resourceId);
            if (good != null)
            {
                cargoable = good;
                return true;
            }
        }
        if (factionInfo.ComponentDesigns.TryGetValue(resourceId, out var design))
        {
            cargoable = design;
            return true;
        }
        cargoable = null!;
        return false;
    }
}
