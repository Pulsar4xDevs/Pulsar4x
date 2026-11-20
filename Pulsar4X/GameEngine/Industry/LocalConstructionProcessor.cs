using System;
using System.Collections.Generic;
using System.Linq;
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

        // Crew plan phase 3: a ship's construction bay short of crew builds at F² (colonies are 1).
        long pointsLeft = (long)Math.Floor(constructionDB.PointsPerDay * Pulsar4X.Ships.ShipCrew.WorkRate(entity) + 1e-9);
        int queuedBefore = constructionDB.BuildQueue.Count;
        double scrapBefore = Pulsar4X.Energy.ColonyPowerProcessor.Scrapping_kW(entity);
        while (pointsLeft > 0 && constructionDB.BuildQueue.Count > 0)
        {
            var job = constructionDB.BuildQueue.Peek();
            if (!TryResolveTarget(entity, job, out var target))
                break;
            long before = job.PointsAccumulated;
            if (!TryAdvance(entity, storage, job, ref pointsLeft))
                break;
            job.ChargeScrap(job.PointsAccumulated - before);

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

        // A finished job (an upgrade's scrapping done, the next job's starting) changes the grid
        // load: refresh demand now rather than at the next daily power pass.
        if (constructionDB.BuildQueue.Count != queuedBefore
            || Math.Abs(Pulsar4X.Energy.ColonyPowerProcessor.Scrapping_kW(entity) - scrapBefore) > 1e-9)
            Pulsar4X.Energy.ColonyPowerProcessor.RefreshDemand(entity);
    }

    /// <summary>
    /// Null when a refit can run: the ship is in range, its parts to remove are aboard and every part to
    /// mount is in the warehouse. Otherwise why not. Room never blocks a refit (a part coming off is kept over capacity); a yard with no store of a part's type waits with a reason. A part that is
    /// not in storage yet is "Waiting for X": the job claims a stored part of that design as soon as one
    /// appears (a factory job finished, a crate delivered) and then runs. Claiming is done here.
    /// </summary>
    internal static string? RefitBlocker(Entity yard, CargoStorageDB storage, ShipRefitJob job)
    {
        if (job.PointCost <= 0)
            return ShipRefitJob.NothingToRefit;
        if (yard.Manager == null || !yard.Manager.TryGetEntityById(job.ShipEntityId, out var ship)
            || ship.FactionOwnerID != yard.FactionOwnerID || !InTransferRange(yard, ship))
            return ShipRefitJob.OutOfRange;
        foreach (var part in job.Remove)
        {
            if (!ShipRefitJob.OnShip(ship, part.ID))
                return ShipRefitJob.NotOnShip;
            // Room never blocks (the part is kept even over capacity), but a yard with no store of the
            // part's cargo type at all has nowhere to put it: say so, and wait.
            if (!storage.TypeStores.ContainsKey(part.CargoTypeID))
                return ShipRefitJob.NoStorageFor + part.Name + ".";
        }

        ReleaseLostParts(storage, job);
        ClaimStoredParts(yard, storage, job);
        var waiting = new List<string>();
        foreach (var part in job.Mount)
        {
            if (storage.GetUnitsStored(part, false) < 1)
                waiting.Add(part.Name);
        }
        waiting.AddRange(WaitingDesignNames(yard, job));
        return waiting.Count == 0 ? null : ShipRefitJob.WaitingFor + string.Join(", ", waiting);
    }

    /// <summary>Names of the designs the job still needs a stored part for.</summary>
    internal static List<string> WaitingDesignNames(Entity yard, ShipRefitJob job)
    {
        var names = new List<string>();
        FactionInfoDB? info = FactionInfo(yard);
        foreach (var id in job.MountDesignIds)
            names.Add(info != null && info.ComponentDesigns.TryGetValue(id, out var design) ? design.Name : id);
        return names;
    }

    static FactionInfoDB? FactionInfo(Entity yard)
        => yard.Manager?.Game is { } game
            && game.Factions.TryGetValue(yard.FactionOwnerID, out var faction)
            && faction.TryGetDataBlob<FactionInfoDB>(out var info) ? info : null;

    /// <summary>
    /// A claimed part listed twice (an older claim did not see the job's own list), or no longer in the warehouse
    /// (sold, used, moved), goes back to "wanted" by design, so another stored part of that design can be claimed.
    /// Without this a refit held the same instance twice, passed the stock check, failed to take it a second time
    /// and waited for ever.
    /// </summary>
    internal static void ReleaseLostParts(CargoStorageDB storage, ShipRefitJob job)
    {
        var seen = new HashSet<int>();
        List<ComponentInstance>? keep = null;
        for (int i = 0; i < job.Mount.Count; i++)
        {
            var part = job.Mount[i];
            bool lost = !seen.Add(part.ID) || storage.GetUnitsStored(part, false) < 1;
            if (lost && keep == null)
                keep = job.Mount.Take(i).ToList();
            if (lost)
                job.MountDesignIds.Add(part.Design.UniqueID);
            else
                keep?.Add(part);
        }
        if (keep != null)
            job.Mount = keep;
    }

    /// <summary>
    /// Moves a stored part of each still-wanted design into the job's mount list, which reserves it.
    /// A part stored as a stack of the design becomes one instance first.
    /// </summary>
    internal static void ClaimStoredParts(Entity yard, CargoStorageDB storage, ShipRefitJob job)
    {
        if (job.MountDesignIds.Count == 0)
            return;
        var info = FactionInfo(yard);
        if (info == null)
            return;
        foreach (var id in job.MountDesignIds.ToArray())
        {
            if (!info.ComponentDesigns.TryGetValue(id, out var design))
                continue;
            var found = FindStoredPart(yard, storage, design, job);
            if (found == null)
                continue;
            job.Mount.Add(found);
            job.MountDesignIds.Remove(id);
        }
    }

    static ComponentInstance? FindStoredPart(Entity yard, CargoStorageDB storage, ComponentDesign design, ShipRefitJob job)
    {
        foreach (var (instance, _) in CargoMath.InstancesOf(storage, design))
        {
            // The job may not be on a line yet (it claims when queued), so Reserved does not see its own list.
            if (job.Mount.Any(part => part.ID == instance.ID))
                continue;
            if (!ShipRefitJob.Reserved(yard, instance.ID))
                return instance;
        }

        if (storage.GetUnitsStored(design, false) < 1)
            return null;
        var fresh = new ComponentInstance(design);
        if (storage.RemoveCargoByUnit(design, 1) < 1)
            return null;
        if (storage.AddCargoByUnitUnbounded(fresh) < 1)
        {
            storage.AddCargoByUnitUnbounded(design);
            return null;
        }
        return fresh;
    }

    /// <summary>
    /// Applies the refit: mounts the claimed parts and takes the removals off. A hold that still has
    /// passengers or cargo in it is not taken off yet: the rest of the refit is applied, the job keeps just
    /// that removal (and says <paramref name="waitReason"/>), and it completes on a later pass once the hold
    /// is empty. Returns true when the job is finished. False with no waitReason means nothing could be done.
    /// </summary>
    internal static bool TryApplyRefit(Entity yard, CargoStorageDB storage, ShipRefitJob job, out string? waitReason)
    {
        waitReason = null;
        if (!yard.Manager!.TryGetEntityById(job.ShipEntityId, out var ship))
            return false;

        var held = HeldRemovals(yard, storage, ship, job.Remove, out var heldReasons);
        var removable = job.Remove.Where(part => !held.Contains(part)).ToList();
        if (removable.Count == 0 && job.Mount.Count == 0)
        {
            waitReason = string.Join("; ", heldReasons);
            return false;
        }

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
        foreach (var part in removable)
        {
            if (!ShipRefitJob.OnShip(ship, part.ID))
            {
                Remount(ship, removed);
                RestoreCargo(storage, taken);
                return false;
            }
            ship.RemoveComponent(part);
            ShrinkShipStore(ship, part);
            removed.Add(part);
        }

        var stored = new List<ComponentInstance>();
        foreach (var part in removed)
        {
            if (storage.AddCargoByUnitUnbounded(part) < 1)
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
        // What is done is done: the job now only holds the removals still waiting on their hold.
        job.Mount = new List<ComponentInstance>();
        job.Remove = job.Remove.Where(part => held.Contains(part)).ToList();
        if (held.Count > 0)
        {
            waitReason = string.Join("; ", heldReasons);
            return false;
        }
        AdoptDesign(yard, ship, job);
        // Crew plan phase 3: crew the refitted ship cannot berth (or no longer needs) go to the yard's pool.
        Pulsar4X.Ships.ShipCrew.AfterRefit(ship, yard);
        return true;
    }

    /// <summary>
    /// The parts to take off whose hold still has more aboard than the ship's remaining holds of that type
    /// could carry once the part is gone (passengers, cargo, fuel, ordnance, minerals, anything). The overflow
    /// is first unloaded to the yard colony as far as it has room (<see cref="UnloadOverflow"/>); a part is
    /// held only for what fits nowhere. Each held part comes with why it waits.
    /// </summary>
    static HashSet<ComponentInstance> HeldRemovals(
        Entity yard, CargoStorageDB colonyStore, Entity ship, IEnumerable<ComponentInstance> remove, out List<string> reasons)
    {
        var held = new HashSet<ComponentInstance>();
        reasons = new List<string>();
        if (!ship.TryGetDataBlob<CargoStorageDB>(out var cargo))
            return held;
        var leaving = new Dictionary<string, double>();
        foreach (var part in remove)
        {
            if (!part.Design.TryGetAttribute<CargoStorageAtb>(out var hold)
                || !cargo.TypeStores.TryGetValue(hold.StoreTypeID, out var store))
                continue;
            leaving.TryGetValue(hold.StoreTypeID, out var already);
            double capacityAfter = store.MaxVolume - already - hold.MaxVolume;
            double overflow = store.MaxVolume - store.FreeVolume - capacityAfter;
            if (overflow > 1e-6)
            {
                UnloadOverflow(yard, colonyStore, ship, hold.StoreTypeID, overflow);
                overflow = store.MaxVolume - store.FreeVolume - capacityAfter;
            }
            if (overflow > 1e-6)
            {
                held.Add(part);
                reasons.Add(ShipRefitJob.EmptyHoldReason(hold.StoreTypeID, part.Name));
            }
            else
                leaving[hold.StoreTypeID] = already + hold.MaxVolume;
        }
        return held;
    }

    /// <summary>
    /// Moves up to <paramref name="volume"/> m3 of a ship's store of one type to the yard colony: cargo, fuel,
    /// ordnance, minerals and the rest into the colony's store of the same type as far as it has room; people
    /// into the colony's housing the way landing does (<see cref="PassengerAdapter.Land"/>). Partial moves are
    /// fine: what does not fit stays aboard. Logs one entry per item moved.
    /// </summary>
    static void UnloadOverflow(Entity yard, CargoStorageDB colonyStore, Entity ship, string storeType, double volume)
    {
        if (!ship.TryGetDataBlob<CargoStorageDB>(out var cargo)
            || !cargo.TypeStores.TryGetValue(storeType, out var store))
            return;
        foreach (var (id, item) in store.Cargoables.ToList())
        {
            if (volume <= 1e-6)
                return;
            if (!store.CurrentStoreInUnits.TryGetValue(id, out var aboard) || aboard <= 0 || item.VolumePerUnit <= 0)
                continue;
            long want = Math.Min(aboard, (long)Math.Ceiling(volume / item.VolumePerUnit - 1e-9));
            long moved;
            if (item is SpeciesCargo people)
                moved = PassengerAdapter.Land(yard, ship, people, want);
            else
            {
                moved = colonyStore.AddCargoByUnit(item, want);
                if (moved > 0)
                    cargo.RemoveCargoByUnit(item, moved);
            }
            if (moved <= 0)
                continue;
            volume -= moved * item.VolumePerUnit;
            PublishUnloaded(yard, ship, item, moved);
        }
    }

    static void PublishUnloaded(Entity yard, Entity ship, ICargoable item, long units)
    {
        string where = yard.GetName(yard.FactionOwnerID);
        string shipName = ship.GetName(yard.FactionOwnerID);
        EventManager.Instance.Publish(
            Event.Create(
                EventType.ShipRefitUnloaded,
                yard.StarSysDateTime,
                $"Refit of {shipName}: unloaded {units} {item.Name} to {where}",
                yard.FactionOwnerID,
                yard.Manager.ManagerID,
                ship.Id));
    }

    /// <summary>A hold taken off a ship takes its volume with it (the hold was emptied first).</summary>
    static void ShrinkShipStore(Entity ship, ComponentInstance part)
    {
        if (!part.Design.TryGetAttribute<CargoStorageAtb>(out var hold)
            || !ship.TryGetDataBlob<CargoStorageDB>(out var cargo)
            || !cargo.TypeStores.TryGetValue(hold.StoreTypeID, out var store))
            return;
        double cut = Math.Min(hold.MaxVolume, store.MaxVolume);
        store.MaxVolume -= cut;
        store.FreeVolume = Math.Max(0, store.FreeVolume - cut);
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
                storage.AddCargoByUnitUnbounded(part);
        }
    }

    internal static void PublishRefitCompleted(Entity yard, ShipRefitJob job)
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

        // An upgrade whose facility is gone spends nothing. Removing the job is the player's call.
        if (job.UpgradeLineId != null && !FacilityUpgrade.TryFindFacility(entity, job.UpgradeLineId, out _))
            return false;

        // A queued site-only mount cannot advance. The factory already spent the
        // recipe and the build points, so this follow-up is the mount cost.
        if (job.Instance != null && LocalConstructionJob.IsSiteOnly(job.Design))
        {
            storage.RemoveCargoByUnit(job.Instance, 1);
            job.Instance = null;
            job.PointCost = LocalConstructionJob.MountCost(job.Design.MassPerUnit);
            job.PointsAccumulated = 0;
            job.ResourcesRemaining.Clear();
        }

        // An older follow-up copied IndustryPointCosts after the factory had paid.
        // An empty recipe is that follow-up. A Construction-tab assemble still has its recipe.
        if (job.Instance == null
            && job.UpgradeLineId == null
            && LocalConstructionJob.IsSiteOnly(job.Design)
            && job.ResourcesRemaining.Count == 0
            && job.PointCost > LocalConstructionJob.MountCost(job.Design.MassPerUnit))
        {
            job.PointCost = LocalConstructionJob.MountCost(job.Design.MassPerUnit);
        }

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
        if (job.UpgradeLineId != null)
        {
            if (job.Instance == null && (!LocalConstructionJob.IsSiteOnly(job.Design) || !ResourcesSettled(job)))
                return false;
            if (!TryGetFaction(target, out var factionInfo))
                return false;
            return FacilityUpgrade.TrySwap(target, storage, factionInfo, job);
        }

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

    internal static bool TryResolve(FactionInfoDB factionInfo, string resourceId, out ICargoable cargoable)
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
