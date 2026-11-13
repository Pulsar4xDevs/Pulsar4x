using System;
using System.Collections.Generic;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry;

/// <summary>
/// Replaces an installed facility with another design of the same template.
/// The new design is an ordinary mount or assemble on the colony's installation queue.
/// The old facility keeps working until the job finishes, then comes off in the same step.
/// A production line id is its facility's instance id, so the jobs move to the new line.
/// </summary>
public static class FacilityUpgrade
{
    public const string NotAColony = "Only a colony can upgrade its facilities.";
    public const string NoPoints = "This colony has no installation points.";
    public const string NoFacility = "That production line has no installed facility.";
    public const string SameDesign = "That is the current design.";
    public const string NotSameType = "That design is not the same facility type.";
    public const string NoOtherDesign = "No other design of this facility type.";
    public const string InProgress = "An upgrade is already queued for this line.";
    public const string NoCrate = "No crate of that design in the warehouse.";
    public const string CrateQueued = "That crate is already in the construction queue.";
    public const string NotInstallation = "That design is not a colony installation.";

    /// <summary>
    /// Scrapping a site-built facility costs this share of its build energy, as a grid load while
    /// the upgrade job runs (Jon, decision 5). Cutting, unbolting and sorting for the half recipe that
    /// comes back is far less work than forming and machining the parts, and recycling scrap metal
    /// takes roughly a fifth of making it new, so a fifth. Build energy itself is never refunded.
    /// </summary>
    public const double ScrapEnergyFraction = 0.2;

    /// <summary>
    /// Energy to scrap the facility behind <paramref name="lineId"/>, kWh. A crate comes off whole
    /// and goes back to the warehouse, so only a site-only facility is scrapped. 0 when there is none.
    /// </summary>
    public static double ScrapEnergy(Entity colony, string lineId)
    {
        if (!TryFindFacility(colony, lineId, out var old) || !LocalConstructionJob.IsSiteOnly(old.Design))
            return 0;
        return ScrapEnergyFraction * Math.Max(0, old.Design.EnergyCost);
    }

    public static bool TryFindFacility(Entity colony, string lineId, out ComponentInstance facility)
    {
        facility = null!;
        if (!colony.TryGetDataBlob<ComponentInstancesDB>(out var components))
            return false;
        if (!components.AllComponents.TryGetValue(lineId, out var found) || found?.Design == null)
            return false;
        facility = found;
        return true;
    }

    /// <summary>
    /// Valid faction installation designs with the facility's TemplateID, not its own design.
    /// Ordered by name, then id.
    /// </summary>
    public static List<ComponentDesign> Candidates(FactionInfoDB factionInfo, ComponentDesign current)
    {
        var found = new List<ComponentDesign>();
        if (string.IsNullOrEmpty(current.TemplateID))
            return found;
        foreach (var design in factionInfo.ComponentDesigns.Values)
        {
            if (design.UniqueID == current.UniqueID || design.TemplateID != current.TemplateID)
                continue;
            if (!design.IsValid || !design.ComponentMountType.HasFlag(ComponentMountType.PlanetInstallation))
                continue;
            found.Add(design);
        }
        found.Sort((a, b) =>
        {
            int byName = string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            return byName != 0 ? byName : string.CompareOrdinal(a.UniqueID, b.UniqueID);
        });
        return found;
    }

    public static bool IsQueued(LocalConstructionDB construction, string lineId)
    {
        foreach (var job in construction.BuildQueue)
        {
            if (job.UpgradeLineId == lineId)
                return true;
        }
        return false;
    }

    /// <summary>A stored crate of the design that no queued job names. Null when there is none.</summary>
    public static ComponentInstance? FreeCrate(Entity colony, ComponentDesign design)
    {
        if (!colony.TryGetDataBlob<CargoStorageDB>(out var storage))
            return null;
        colony.TryGetDataBlob<LocalConstructionDB>(out var construction);
        foreach (var pair in storage.TypeStores)
        {
            foreach (var cargoable in pair.Value.GetCargoables().Values)
            {
                if (cargoable is not ComponentInstance instance || instance.Design?.UniqueID != design.UniqueID)
                    continue;
                if (construction != null && CrateInQueue(construction, instance))
                    continue;
                return instance;
            }
        }
        return null;
    }

    /// <summary>Why this upgrade cannot be queued. Null when it can.</summary>
    public static string? Rejection(Entity colony, string lineId, ComponentDesign design, ComponentInstance? crate)
    {
        if (!colony.HasDataBlob<ColonyInfoDB>())
            return NotAColony;
        if (!colony.TryGetDataBlob<LocalConstructionDB>(out var construction) || construction.PointsPerDay <= 0)
            return NoPoints;
        if (!TryFindFacility(colony, lineId, out var facility)
            || !colony.TryGetDataBlob<IndustryAbilityDB>(out var industry)
            || !industry.ProductionLines.ContainsKey(lineId))
            return NoFacility;
        if (design.UniqueID == facility.Design.UniqueID)
            return SameDesign;
        if (string.IsNullOrEmpty(design.TemplateID)
            || design.TemplateID != facility.Design.TemplateID
            || !design.AttributesByType.ContainsKey(typeof(IndustryAtb)))
            return NotSameType;
        if (!design.IsValid || !design.ComponentMountType.HasFlag(ComponentMountType.PlanetInstallation))
            return NotInstallation;
        if (IsQueued(construction, lineId))
            return InProgress;

        if (LocalConstructionJob.IsCrate(design))
        {
            if (crate == null || crate.Design?.UniqueID != design.UniqueID)
                return NoCrate;
            if (!colony.TryGetDataBlob<CargoStorageDB>(out var storage) || storage.GetUnitsStored(crate, false) < 1)
                return NoCrate;
            if (CrateInQueue(construction, crate))
                return CrateQueued;
            return null;
        }

        if (!LocalConstructionJob.IsSiteOnly(design) || crate != null)
            return NotInstallation;
        return null;
    }

    /// <summary>
    /// Mounts the new facility, takes the old one off, and moves the old line's jobs, hulls and refits
    /// to the new line. An old crate goes back to the warehouse whole. A site-only facility has no crate,
    /// so half its recipe goes back instead (<see cref="SiteRefund"/>). Room for whatever comes back is
    /// checked before anything moves. Anything that fails leaves the colony as it was and returns false.
    /// </summary>
    internal static bool TrySwap(Entity colony, CargoStorageDB storage, FactionInfoDB factionInfo, LocalConstructionJob job)
    {
        string lineId = job.UpgradeLineId!;
        if (!TryFindFacility(colony, lineId, out var old))
            return false;
        if (!colony.TryGetDataBlob<IndustryAbilityDB>(out var industry)
            || !industry.ProductionLines.TryGetValue(lineId, out var oldLine))
            return false;

        bool returnOld = LocalConstructionJob.IsCrate(old.Design);
        var refund = returnOld ? new List<(ICargoable Item, long Units)>() : SiteRefund(factionInfo, old.Design);
        if (!RoomForReturns(storage, returnOld ? old : null, refund, job.Instance))
            return false;

        ComponentInstance fresh;
        if (job.Instance != null)
        {
            if (storage.RemoveCargoByUnit(job.Instance, 1) < 1)
                return false;
            fresh = job.Instance;
        }
        else
        {
            fresh = new ComponentInstance(job.Design);
        }

        // Mount first so a shared blob such as the construction queue never drops to empty mid-swap.
        colony.AddComponent(fresh);
        colony.RemoveComponent(old);

        var added = new List<(ICargoable Item, long Units)>();
        bool ok = industry.ProductionLines.TryGetValue(fresh.UniqueID, out var newLine);
        if (ok && returnOld)
        {
            long back = storage.AddCargoByUnit(old, 1);
            if (back > 0)
                added.Add((old, back));
            ok = back == 1;
        }
        foreach (var (item, units) in refund)
        {
            if (!ok)
                break;
            long back = storage.AddCargoByUnit(item, units);
            if (back > 0)
                added.Add((item, back));
            ok = back == units;
        }

        if (!ok)
        {
            foreach (var (item, units) in added)
                storage.RemoveCargoByUnit(item, units);
            colony.RemoveComponent(fresh);
            colony.AddComponent(old);
            industry.ProductionLines[lineId] = oldLine;
            if (job.Instance != null)
                storage.AddCargoByUnit(job.Instance, 1);
            return false;
        }

        newLine!.Jobs.AddRange(oldLine.Jobs);
        newLine.Hulls.AddRange(oldLine.Hulls);
        if (colony.TryGetDataBlob<LocalConstructionDB>(out var construction))
        {
            foreach (var refit in construction.RefitQueue)
            {
                if (refit.ProductionLineId == lineId)
                    refit.ProductionLineId = fresh.UniqueID;
            }
        }
        return true;
    }

    /// <summary>
    /// What a decommissioned site-only facility gives back: half of each recipe amount, rounded down.
    /// No industry points come back. A stand-in until there is a real scrap feature. An id the faction
    /// cannot resolve is skipped. Empty for a crate design, which goes back to the warehouse whole.
    /// </summary>
    public static List<(ICargoable Item, long Units)> SiteRefund(FactionInfoDB factionInfo, ComponentDesign design)
    {
        var refund = new List<(ICargoable Item, long Units)>();
        if (!LocalConstructionJob.IsSiteOnly(design))
            return refund;
        foreach (var (resourceId, amount) in design.ResourceCosts)
        {
            long units = amount / 2;
            if (units <= 0)
                continue;
            if (!LocalConstructionProcessor.TryResolve(factionInfo, resourceId, out var cargoable))
                continue;
            refund.Add((cargoable, units));
        }
        return refund;
    }

    /// <summary>
    /// Free volume per cargo type for the returned crate and the refund, after the new crate leaves.
    /// </summary>
    static bool RoomForReturns(
        CargoStorageDB storage, ComponentInstance? oldCrate,
        List<(ICargoable Item, long Units)> refund, ComponentInstance? leaving)
    {
        var need = new Dictionary<string, double>();
        void Add(string cargoType, double volume)
        {
            need.TryGetValue(cargoType, out var current);
            need[cargoType] = current + volume;
        }

        if (oldCrate != null)
            Add(oldCrate.CargoTypeID, oldCrate.VolumePerUnit);
        foreach (var (item, units) in refund)
            Add(item.CargoTypeID, item.VolumePerUnit * units);
        if (leaving != null)
            Add(leaving.CargoTypeID, -leaving.VolumePerUnit);

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

    static bool CrateInQueue(LocalConstructionDB construction, ComponentInstance crate)
    {
        foreach (var job in construction.BuildQueue)
        {
            if (job.Instance != null && job.Instance.ID == crate.ID)
                return true;
        }
        return false;
    }
}
