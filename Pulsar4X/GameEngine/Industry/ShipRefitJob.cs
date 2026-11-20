using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Ships;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry;

/// <summary>
/// One shipyard refit. It names a ship and the instances to remove and to mount.
/// The point cost is copied from the mass formula when the job is queued.
/// </summary>
public class ShipRefitJob
{
    public const string NoRefitRate = "This yard has no refit rate.";
    public const string OverSlip = "That ship is over this slip.";
    public const string ColonyInstallation = "A colony installation cannot be refitted.";
    public const string InRefit = "That ship is in refit.";
    public const string OutOfRange = "That ship is not in transfer range.";
    public const string NotShipPart = "That part is not a ship component.";
    public const string NotOnShip = "That part is not on the ship.";
    public const string NotInWarehouse = "That part is not in the warehouse.";
    public const string NothingToRefit = "Nothing to refit.";
    public const string NotAShipyard = "That line is not a shipyard.";
    public const string AlreadyListed = "That part is already in a refit.";
    public const string UnknownPart = "That part design is not available.";
    public const string WaitingFor = "Waiting for ";
    public const string PassengersLeft = "Waiting for passengers to disembark from ";
    public const string CargoLeft = "Waiting for cargo to be unloaded from ";
    public const string FuelLeft = "Waiting for fuel to be unloaded from ";
    public const string NoStorageFor = "This yard has no storage for ";
    public const string UnknownDesign = "That design is not available.";

    public int ShipEntityId { get; set; }

    public List<ComponentInstance> Remove { get; set; } = new();

    public List<ComponentInstance> Mount { get; set; } = new();

    /// <summary>
    /// Parts to mount that are not in the warehouse yet, by component design id (one entry per part).
    /// The job claims a stored part of each design when one appears, then moves it into <see cref="Mount"/>.
    /// </summary>
    public List<string> MountDesignIds { get; set; } = new();

    /// <summary>Copied at queue time. A later edit to a part's mass does not change it. The industry job spends it.</summary>
    public long PointCost { get; set; }

    /// <summary>When set, a finished refit whose parts match this design adopts it. Null keeps the old design.</summary>
    public string? TargetDesignId { get; set; }

    public ShipRefitJob()
    {
    }

    public ShipRefitJob(
        int shipEntityId, List<ComponentInstance> remove, List<ComponentInstance> mount,
        IReadOnlyList<ComponentDesign>? wanted = null)
    {
        ShipEntityId = shipEntityId;
        Remove = remove;
        Mount = mount;
        long cost = CostOf(remove) + CostOf(mount);
        if (wanted != null)
        {
            foreach (var design in wanted)
            {
                MountDesignIds.Add(design.UniqueID);
                cost += LocalConstructionJob.MountCost(design.MassPerUnit);
            }
        }
        PointCost = Math.Max(1, cost);
    }

    /// <summary>Why a hold that still has contents cannot come off yet.</summary>
    public static string EmptyHoldReason(string storeTypeId, string partName)
        => storeTypeId == Pulsar4X.Colonies.SpeciesCargo.CargoType ? PassengersLeft + partName
            : storeTypeId == "fuel-storage" ? FuelLeft + partName
            : CargoLeft + partName;

    public static long CostOf(IEnumerable<ComponentInstance> parts)
    {
        long sum = 0;
        foreach (var part in parts)
            sum += LocalConstructionJob.MountCost(part.MassPerUnit);
        return sum;
    }

    public static bool IsShipPart(ComponentDesign design)
        => design.ComponentMountType.HasFlag(ComponentMountType.ShipComponent);

    public static bool IsColonyInstallation(ComponentDesign design)
        => design.ComponentMountType.HasFlag(ComponentMountType.PlanetInstallation);

    public static long ShipMass(Entity ship)
        => ship.GetDataBlob<ShipInfoDB>().Design.MassPerUnit;

    /// <summary>Null when the refit can be queued.</summary>
    public static string? Rejection(
        Entity yard, string lineId, Entity ship,
        IReadOnlyList<ComponentInstance> remove, IReadOnlyList<ComponentInstance> mount,
        IReadOnlyList<ComponentDesign>? wanted = null)
    {
        if (!yard.TryGetDataBlob<LocalConstructionDB>(out var construction) || construction.RefitPointsPerDay <= 0)
            return NoRefitRate;
        if (!yard.TryGetDataBlob<IndustryAbilityDB>(out var industry)
            || !industry.ProductionLines.TryGetValue(lineId, out var line))
            return "That production line was not found.";
        if (!line.IndustryTypeRates.ContainsKey("ship-assembly"))
            return NotAShipyard;
        if (ship.FactionOwnerID != yard.FactionOwnerID
            || !ship.HasDataBlob<ShipInfoDB>()
            || !LocalConstructionProcessor.InTransferRange(yard, ship))
            return OutOfRange;
        if (ShipMass(ship) > line.MaxVolume)
            return OverSlip;
        if (TryFind(ship, out _, out _))
            return InRefit;
        if (Recycling.TryFindShip(ship, out _, out _))
            return Recycling.QueuedForRecycling;
        if (remove.Count == 0 && mount.Count == 0 && (wanted == null || wanted.Count == 0))
            return NothingToRefit;
        foreach (var design in wanted ?? Array.Empty<ComponentDesign>())
        {
            if (!IsShipPart(design))
                return IsColonyInstallation(design) ? ColonyInstallation : NotShipPart;
        }

        var seen = new HashSet<int>();
        foreach (var part in remove)
        {
            if (!seen.Add(part.ID))
                return AlreadyListed;
            if (!IsShipPart(part.Design))
                return IsColonyInstallation(part.Design) ? ColonyInstallation : NotShipPart;
            if (!OnShip(ship, part.ID))
                return NotOnShip;
            if (Reserved(yard, part.ID))
                return AlreadyListed;
        }

        if (!yard.TryGetDataBlob<CargoStorageDB>(out var storage))
            return NotInWarehouse;
        foreach (var part in mount)
        {
            if (!seen.Add(part.ID))
                return AlreadyListed;
            if (!IsShipPart(part.Design))
                return IsColonyInstallation(part.Design) ? ColonyInstallation : NotShipPart;
            if (storage.GetUnitsStored(part, false) < 1)
                return NotInWarehouse;
            if (Reserved(yard, part.ID))
                return AlreadyListed;
        }

        return null;
    }

    /// <summary>Every refit job on a yard's lines, with the line that holds it.</summary>
    public static IEnumerable<(string LineId, IndustryJob Job)> JobsOf(Entity yard)
    {
        if (!yard.TryGetDataBlob<IndustryAbilityDB>(out var industry))
            yield break;
        foreach (var (lineId, line) in industry.ProductionLines)
        {
            foreach (var job in line.Jobs.ToArray())
            {
                if (job.Refit != null)
                    yield return (lineId, job);
            }
        }
    }

    public static bool TryFind(Entity ship, out Entity yard, out IndustryJob job)
    {
        yard = null!;
        job = null!;
        if (ship.Manager == null)
            return false;

        foreach (var holder in ship.Manager.GetAllEntitiesWithDataBlob<IndustryAbilityDB>())
        {
            foreach (var (_, queued) in JobsOf(holder))
            {
                if (queued.Refit!.ShipEntityId != ship.Id)
                    continue;
                yard = holder;
                job = queued;
                return true;
            }
        }

        return false;
    }

    public static bool OnShip(Entity ship, int componentId)
    {
        if (!ship.TryGetDataBlob<ComponentInstancesDB>(out var components))
            return false;
        foreach (var part in components.AllComponents.Values)
        {
            if (part.ID == componentId)
                return true;
        }
        return false;
    }

    internal static bool Reserved(Entity yard, int componentId)
    {
        if (yard.Manager == null)
            return false;
        foreach (var holder in yard.Manager.GetAllEntitiesWithDataBlob<IndustryAbilityDB>())
        {
            foreach (var (_, queued) in JobsOf(holder))
            {
                var job = queued.Refit!;
                foreach (var part in job.Remove)
                    if (part.ID == componentId)
                        return true;
                foreach (var part in job.Mount)
                    if (part.ID == componentId)
                        return true;
            }
        }
        return false;
    }
}
