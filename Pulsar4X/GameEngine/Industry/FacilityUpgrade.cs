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
    /// to the new line. An old crate goes back to the warehouse. A site-only facility has no crate and is
    /// not refunded. Anything that fails leaves the colony as it was and returns false.
    /// </summary>
    internal static bool TrySwap(Entity colony, CargoStorageDB storage, LocalConstructionJob job)
    {
        string lineId = job.UpgradeLineId!;
        if (!TryFindFacility(colony, lineId, out var old))
            return false;
        if (!colony.TryGetDataBlob<IndustryAbilityDB>(out var industry)
            || !industry.ProductionLines.TryGetValue(lineId, out var oldLine))
            return false;

        bool returnOld = LocalConstructionJob.IsCrate(old.Design);
        if (returnOld && !RoomForOld(storage, old, job.Instance))
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

        if (!industry.ProductionLines.TryGetValue(fresh.UniqueID, out var newLine)
            || (returnOld && storage.AddCargoByUnit(old, 1) < 1))
        {
            colony.RemoveComponent(fresh);
            colony.AddComponent(old);
            industry.ProductionLines[lineId] = oldLine;
            if (job.Instance != null)
                storage.AddCargoByUnit(job.Instance, 1);
            return false;
        }

        newLine.Jobs.AddRange(oldLine.Jobs);
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

    static bool RoomForOld(CargoStorageDB storage, ComponentInstance old, ComponentInstance? leaving)
    {
        double need = old.VolumePerUnit;
        if (leaving != null && leaving.CargoTypeID == old.CargoTypeID)
            need -= leaving.VolumePerUnit;
        if (need <= 0)
            return true;
        if (!storage.TypeStores.TryGetValue(old.CargoTypeID, out var store))
            return false;
        return store.FreeVolume + 1e-9 >= need;
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
