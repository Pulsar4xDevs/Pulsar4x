using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine.Engine.Orders;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Interfaces;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry;

/// <summary>What a recycle job takes apart.</summary>
public enum RecycleKind
{
    /// <summary>A ship in transfer range of a colony with a shipyard, on a shipyard line.</summary>
    Ship,
    /// <summary>Components or installations in the colony's warehouse, on a factory line that builds them.</summary>
    Component,
}

/// <summary>
/// One recycle order, held on a normal industry job (<see cref="IndustryJob.Recycle"/>). It spends the line's points of
/// the item's industry type: <see cref="GameSettings.RecycleReturnFraction"/> of the build points per item. When an item's
/// points are paid it is taken apart and the same fraction of its build resources goes into the colony's warehouse.
/// </summary>
public class RecycleJob
{
    public RecycleKind Kind { get; set; }

    /// <summary>The ship to scrap (<see cref="RecycleKind.Ship"/>); -1 otherwise.</summary>
    public int ShipEntityId { get; set; } = -1;

    /// <summary>The ship design id, or the component / installation design id.</summary>
    public string DesignId { get; set; } = "";

    /// <summary>Resources one item returns, by cargo id, copied when queued.</summary>
    public Dictionary<string, long> Returns { get; set; } = new();
}

/// <summary>The design a recycle job runs as: never registered with the faction, so no list offers it.</summary>
public sealed class RecycleDesign : IConstructableDesign
{
    public const string TypeId = "recycle";

    readonly long _points;

    public RecycleDesign(string name, string industryTypeId, long points)
    {
        Name = name;
        IndustryTypeID = industryTypeId;
        _points = points;
    }

    public ConstructableGuiHints GuiHints => ConstructableGuiHints.None;
    public string UniqueID => TypeId;
    public string Name { get; }
    public bool IsValid => true;
    public Dictionary<string, long> ResourceCosts { get; } = new();
    public long IndustryPointCosts => _points;
    public double EnergyCost => 0;
    public string IndustryTypeID { get; }
    public ushort OutputAmount => 1;

    /// <summary>Recycle jobs finish in <see cref="IndustryTools.ConstructStuff"/> (<see cref="Recycling.Advance"/>).</summary>
    public void OnConstructionComplete(Entity industryEntity, CargoStorageDB storage, string productionLine, IndustryJob batchJob, IConstructableDesign designInfo)
    {
    }
}

/// <summary>Rules, costs and the daily step for recycling ships, components and installations.</summary>
public static class Recycling
{
    public const string NoShipyard = "That line is not a shipyard.";
    public const string NotThisLine = "That line cannot build it, so it cannot take it apart.";
    public const string OutOfRange = "That ship is not in transfer range.";
    public const string InRefit = "That ship is in refit.";
    public const string AlreadyQueued = "That ship is already queued for recycling.";
    public const string QueuedForRecycling = "That ship is queued for recycling.";
    public const string NotInWarehouse = "Not enough of that in the warehouse.";
    public const string UnknownDesign = "That design is not available.";
    public const string LineNotFound = "That production line was not found.";
    public const string NothingToRecycle = "Recycle at least one.";
    public const string NotAShip = "That is not a ship of this faction.";
    public const string WaitingFor = "Waiting for ";

    /// <summary>Why this ship cannot be recycled on the line, or null.</summary>
    public static string? ShipRejection(Entity colony, string lineId, Entity ship)
    {
        if (!colony.HasDataBlob<ColonyInfoDB>() || !colony.TryGetDataBlob<IndustryAbilityDB>(out var industry)
            || !industry.ProductionLines.TryGetValue(lineId, out var line))
            return LineNotFound;
        if (!IndustryTools.IsShipyardLine(line))
            return NoShipyard;
        if (ship.FactionOwnerID != colony.FactionOwnerID || !ship.HasDataBlob<ShipInfoDB>())
            return NotAShip;
        if (!LocalConstructionProcessor.InTransferRange(colony, ship))
            return OutOfRange;
        if (ShipRefitJob.TryFind(ship, out _, out _))
            return InRefit;
        if (TryFindShip(ship, out _, out _))
            return AlreadyQueued;
        if (ShipDesignOf(ship) == null)
            return UnknownDesign;
        return null;
    }

    /// <summary>Why <paramref name="quantity"/> of this design cannot be recycled on the line, or null.</summary>
    public static string? ComponentRejection(Entity colony, string lineId, string designId, long quantity)
    {
        if (!colony.HasDataBlob<ColonyInfoDB>() || !colony.TryGetDataBlob<IndustryAbilityDB>(out var industry)
            || !industry.ProductionLines.TryGetValue(lineId, out var line))
            return LineNotFound;
        if (quantity < 1 || quantity > ushort.MaxValue)
            return NothingToRecycle;
        var design = ComponentDesignOf(colony, designId);
        if (design == null)
            return UnknownDesign;
        if (!IndustryTools.LineAccepts(line, design))
            return NotThisLine;
        if (!colony.TryGetDataBlob<CargoStorageDB>(out var storage) || Available(colony, storage, design) < quantity)
            return NotInWarehouse;
        return null;
    }

    /// <summary>A factory line of the colony that builds the design (fastest first), or null.</summary>
    public static string? LineFor(Entity colony, IConstructableDesign design)
        => colony.TryGetDataBlob<IndustryAbilityDB>(out var industry) ? IndustryTools.FastestLineId(industry, design.IndustryTypeID) : null;

    /// <summary>The colony's first shipyard line, or null.</summary>
    public static string? ShipyardLine(Entity colony)
        => colony.TryGetDataBlob<IndustryAbilityDB>(out var industry)
            ? industry.ProductionLines.Where(l => IndustryTools.IsShipyardLine(l.Value)).Select(l => l.Key).OrderBy(k => k, StringComparer.Ordinal).FirstOrDefault()
            : null;

    /// <summary>
    /// Resources one item returns: its build resources with every component in them broken down into that component's
    /// own resources, times <see cref="GameSettings.RecycleReturnFraction"/>, rounded down. Ids are cargo goods.
    /// </summary>
    public static Dictionary<string, long> ReturnsOf(FactionInfoDB info, IConstructableDesign design)
    {
        var raw = new Dictionary<string, double>();
        Expand(info, design, 1, raw, 0);
        var returns = new Dictionary<string, long>();
        foreach (var (id, amount) in raw.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            long back = (long)Math.Floor(amount * GameSettings.RecycleReturnFraction + 1e-9);
            if (back > 0)
                returns[id] = back;
        }
        return returns;
    }

    static void Expand(FactionInfoDB info, IConstructableDesign design, double count, Dictionary<string, double> into, int depth)
    {
        if (design.ResourceCosts == null)
            return;
        foreach (var (id, amount) in design.ResourceCosts)
        {
            if (depth < 8 && info.ComponentDesigns.TryGetValue(id, out var part) && part != null)
                Expand(info, part, count * amount, into, depth + 1);
            else
                into[id] = into.GetValueOrDefault(id) + count * amount;
        }
    }

    /// <summary>Industry points to take one item apart: <see cref="GameSettings.RecycleReturnFraction"/> of its build points (at least 1).</summary>
    public static long PointsOf(IConstructableDesign design)
        => Math.Max(1, (long)Math.Ceiling(design.IndustryPointCosts * GameSettings.RecycleReturnFraction));

    /// <summary>Units of the design in the colony's warehouse that no refit has claimed.</summary>
    public static long Available(Entity colony, CargoStorageDB storage, ComponentDesign design)
    {
        long units = storage.GetUnitsStored(design, false);
        foreach (var (instance, count) in CargoMath.InstancesOf(storage, design))
        {
            if (!ShipRefitJob.Reserved(colony, instance.ID))
                units += count;
        }
        return units;
    }

    /// <summary>Every recycle job on the colony's lines.</summary>
    public static IEnumerable<(string LineId, IndustryJob Job)> JobsOf(Entity colony)
    {
        if (!colony.TryGetDataBlob<IndustryAbilityDB>(out var industry))
            yield break;
        foreach (var (lineId, line) in industry.ProductionLines)
            foreach (var job in line.Jobs.ToArray())
                if (job.Recycle != null)
                    yield return (lineId, job);
    }

    /// <summary>The recycle job that will scrap this ship, if any.</summary>
    public static bool TryFindShip(Entity ship, out Entity colony, out IndustryJob job)
    {
        colony = null!;
        job = null!;
        if (ship.Manager == null)
            return false;
        foreach (var holder in ship.Manager.GetAllEntitiesWithDataBlob<IndustryAbilityDB>())
        {
            foreach (var (_, queued) in JobsOf(holder))
            {
                if (queued.Recycle!.Kind != RecycleKind.Ship || queued.Recycle.ShipEntityId != ship.Id)
                    continue;
                colony = holder;
                job = queued;
                return true;
            }
        }
        return false;
    }

    internal static ShipDesign? ShipDesignOf(Entity ship)
        => ship.TryGetDataBlob<ShipInfoDB>(out var info) ? info.Design : null;

    internal static ComponentDesign? ComponentDesignOf(Entity colony, string designId)
        => Info(colony) is { } info && info.ComponentDesigns.TryGetValue(designId, out var design) ? design : null;

    /// <summary>A ship recycle job: one item, the ship design's type points on the yard's line.</summary>
    internal static IndustryJob ShipJob(Entity colony, Entity ship)
    {
        var design = ShipDesignOf(ship)!;
        string shipName = ship.GetName(colony.FactionOwnerID);
        var recycle = new RecycleJob
        {
            Kind = RecycleKind.Ship,
            ShipEntityId = ship.Id,
            DesignId = design.UniqueID,
            Returns = ReturnsOf(Info(colony)!, design),
        };
        var job = IndustryJob.ForRecycle(recycle, new RecycleDesign("Scrap " + shipName, design.IndustryTypeID, PointsOf(design)));
        job.NumberOrdered = 1;
        return job;
    }

    /// <summary>A component / installation recycle job: <paramref name="quantity"/> items on a factory line.</summary>
    internal static IndustryJob ComponentJob(Entity colony, ComponentDesign design, ushort quantity)
    {
        var recycle = new RecycleJob
        {
            Kind = RecycleKind.Component,
            DesignId = design.UniqueID,
            Returns = ReturnsOf(Info(colony)!, design),
        };
        var job = IndustryJob.ForRecycle(recycle, new RecycleDesign("Recycle " + design.Name, design.IndustryTypeID, PointsOf(design)));
        job.NumberOrdered = quantity;
        return job;
    }

    /// <summary>
    /// One day of a recycle job: spends the line's points of its type; each item whose points are paid is taken apart
    /// (<see cref="TryTakeApart"/>). An item that cannot be taken apart yet (ship out of range, cargo aboard with no
    /// room ashore, part gone from the warehouse) waits with a reason and spends nothing more.
    /// </summary>
    internal static void Advance(Entity colony, CargoStorageDB storage, IndustryAbilityDB.ProductionLine line, IndustryJob job,
        Dictionary<string, int> pointsLeft)
    {
        var recycle = job.Recycle!;
        string? blocker = Blocker(colony, storage, recycle);
        job.WaitReason = blocker ?? "";
        if (blocker != null)
        {
            job.Status = IndustryJobStatus.MissingResources;
            return;
        }
        job.Status = IndustryJobStatus.Queued;
        while (job.NumberCompleted < job.NumberOrdered)
        {
            int pool = pointsLeft.GetValueOrDefault(job.TypeID);
            long spend = Math.Min(pool, job.ProductionPointsLeft);
            if (spend > 0)
            {
                job.ProductionPointsLeft -= spend;
                pointsLeft[job.TypeID] = pool - (int)spend;
                job.Status = IndustryJobStatus.Processing;
            }
            if (job.ProductionPointsLeft > 0)
                return;
            if (!TryTakeApart(colony, storage, recycle, out var held))
            {
                job.WaitReason = held ?? "";
                job.Status = IndustryJobStatus.MissingResources;
                return;
            }
            job.NumberCompleted++;
            job.ProductionPointsLeft = job.ProductionPointsCost;
            if (job.NumberCompleted < job.NumberOrdered && (blocker = Blocker(colony, storage, recycle)) != null)
            {
                job.WaitReason = blocker;
                job.Status = IndustryJobStatus.MissingResources;
                return;
            }
        }
        job.Status = IndustryJobStatus.Completed;
        line.Jobs.Remove(job);
    }

    /// <summary>Why the next item cannot be worked on, or null.</summary>
    internal static string? Blocker(Entity colony, CargoStorageDB storage, RecycleJob recycle)
    {
        if (recycle.Kind == RecycleKind.Ship)
        {
            if (colony.Manager == null || !colony.Manager.TryGetEntityById(recycle.ShipEntityId, out var ship)
                || ship.FactionOwnerID != colony.FactionOwnerID || !LocalConstructionProcessor.InTransferRange(colony, ship))
                return OutOfRange;
            return null;
        }
        var design = ComponentDesignOf(colony, recycle.DesignId);
        if (design == null)
            return UnknownDesign;
        return Available(colony, storage, design) < 1 ? WaitingFor + design.Name : null;
    }

    /// <summary>
    /// Takes one item apart. A ship: everything aboard (cargo, fuel, ordnance, passengers) is unloaded to the colony as
    /// far as it has room, else the ship waits; its crew go to the colony's pool (as <see cref="ShipCrew.ToPool"/>), its
    /// captain stays in the faction unassigned, and the ship is removed. A component: one unit leaves the warehouse.
    /// Then the returns go into the warehouse (kept over capacity, like refit parts; a resource with no store of its
    /// type is lost and logged).
    /// </summary>
    internal static bool TryTakeApart(Entity colony, CargoStorageDB storage, RecycleJob recycle, out string? held)
    {
        held = null;
        string itemName;
        if (recycle.Kind == RecycleKind.Ship)
        {
            if (colony.Manager == null || !colony.Manager.TryGetEntityById(recycle.ShipEntityId, out var ship))
            {
                held = OutOfRange;
                return false;
            }
            if (!UnloadAll(colony, storage, ship, out held))
                return false;
            itemName = ship.GetName(colony.FactionOwnerID);
            ReleaseCaptain(ship);
            int crew = ShipCrew.Scrap(ship, colony);
            ShipLogBook.NoteIndustryChange(colony, $"{itemName} was scrapped; {crew} crew went to the pool");
        }
        else
        {
            var design = ComponentDesignOf(colony, recycle.DesignId);
            if (design == null || !TakeOne(colony, storage, design))
            {
                held = WaitingFor + (design?.Name ?? recycle.DesignId);
                return false;
            }
            itemName = design.Name;
        }

        var info = Info(colony);
        foreach (var (id, amount) in recycle.Returns)
        {
            var good = info?.Data.CargoGoods.GetAny(id);
            if (good == null || storage.AddCargoByUnitUnbounded(good, amount) < amount)
                ShipLogBook.NoteIndustryChange(colony, $"recycling {itemName}: no store for {good?.Name ?? id}, {amount} lost");
        }
        return true;
    }

    static bool UnloadAll(Entity colony, CargoStorageDB storage, Entity ship, out string? held)
    {
        held = null;
        if (!ship.TryGetDataBlob<CargoStorageDB>(out var cargo))
            return true;
        foreach (var (typeId, store) in cargo.TypeStores.ToList())
        {
            double used = store.MaxVolume - store.FreeVolume;
            if (used > 1e-6)
                LocalConstructionProcessor.UnloadOverflow(colony, storage, ship, typeId, used + 1);
            if (store.CurrentStoreInUnits.Values.Any(units => units > 0))
            {
                held = ShipRefitJob.EmptyHoldReason(typeId, ship.GetName(colony.FactionOwnerID));
                return false;
            }
        }
        return true;
    }

    /// <summary>The captain leaves the ship before it goes (<see cref="ShipFactory.DestroyShip"/> would remove them).</summary>
    static void ReleaseCaptain(Entity ship)
    {
        if (!ship.TryGetDataBlob<ShipInfoDB>(out var info) || info.CommanderID < 0)
            return;
        if (ship.Manager != null && ship.Manager.TryGetGlobalEntityById(info.CommanderID, out var captain)
            && captain.TryGetDataBlob<Pulsar4X.People.CommanderDB>(out var db) && db.AssignedTo == ship.Id)
            db.AssignedTo = -1;
        info.CommanderID = -1;
    }

    static bool TakeOne(Entity colony, CargoStorageDB storage, ComponentDesign design)
    {
        if (storage.GetUnitsStored(design, false) >= 1 && storage.RemoveCargoByUnit(design, 1) >= 1)
            return true;
        foreach (var (instance, _) in CargoMath.InstancesOf(storage, design))
        {
            if (!ShipRefitJob.Reserved(colony, instance.ID) && storage.RemoveCargoByUnit(instance, 1) >= 1)
                return true;
        }
        return false;
    }

    static FactionInfoDB? Info(Entity colony)
        => colony.Manager?.Game is { } game && game.Factions.TryGetValue(colony.FactionOwnerID, out var faction)
            && faction.TryGetDataBlob<FactionInfoDB>(out var info) ? info : null;
}
