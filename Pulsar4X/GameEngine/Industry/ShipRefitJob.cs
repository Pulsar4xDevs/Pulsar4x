using System.Collections.Generic;
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
    public const string UnknownDesign = "That design is not available.";

    public int ShipEntityId { get; set; }

    public string ProductionLineId { get; set; } = "";

    public List<ComponentInstance> Remove { get; set; } = new();

    public List<ComponentInstance> Mount { get; set; } = new();

    public long PointsAccumulated { get; set; }

    /// <summary>Copied at queue time. A later edit to a part's mass does not change it.</summary>
    public long PointCost { get; set; }

    /// <summary>When set, a finished refit whose parts match this design adopts it. Null keeps the old design.</summary>
    public string? TargetDesignId { get; set; }

    public bool IsComplete => PointCost > 0 && PointsAccumulated >= PointCost;

    public double Progress => PointCost <= 0 ? 0 : (double)PointsAccumulated / PointCost;

    public ShipRefitJob(int shipEntityId, string productionLineId, List<ComponentInstance> remove, List<ComponentInstance> mount)
    {
        ShipEntityId = shipEntityId;
        ProductionLineId = productionLineId;
        Remove = remove;
        Mount = mount;
        PointCost = CostOf(remove) + CostOf(mount);
    }

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
        IReadOnlyList<ComponentInstance> remove, IReadOnlyList<ComponentInstance> mount)
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
        if (remove.Count == 0 && mount.Count == 0)
            return NothingToRefit;

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

    public static bool TryFind(Entity ship, out Entity yard, out ShipRefitJob job)
    {
        yard = null!;
        job = null!;
        if (ship.Manager == null)
            return false;

        foreach (var holder in ship.Manager.GetAllEntitiesWithDataBlob<LocalConstructionDB>())
        {
            foreach (var queued in holder.GetDataBlob<LocalConstructionDB>().RefitQueue)
            {
                if (queued.ShipEntityId != ship.Id)
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

    static bool Reserved(Entity yard, int componentId)
    {
        if (yard.Manager == null)
            return false;
        foreach (var holder in yard.Manager.GetAllEntitiesWithDataBlob<LocalConstructionDB>())
        {
            foreach (var job in holder.GetDataBlob<LocalConstructionDB>().RefitQueue)
            {
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
