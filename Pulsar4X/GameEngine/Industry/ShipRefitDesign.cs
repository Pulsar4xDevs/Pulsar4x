using System.Collections.Generic;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Storage;

namespace Pulsar4X.Industry;

/// <summary>
/// The design a shipyard refit job runs as. It is never registered with the faction, so no list offers it.
/// The job spends the yard's ship-refit points like any other job spends its industry type's points.
/// The parts move when the points are paid (see <see cref="LocalConstructionProcessor.TryApplyRefit"/>).
/// </summary>
public sealed class ShipRefitDesign : IConstructableDesign
{
    public const string TypeId = "ship-refit";

    readonly long _points;

    public ShipRefitDesign(string name, long points)
    {
        Name = name;
        _points = points;
    }

    public ConstructableGuiHints GuiHints => ConstructableGuiHints.None;
    public string UniqueID => TypeId;
    public string Name { get; }
    public bool IsValid => true;
    public Dictionary<string, long> ResourceCosts { get; } = new();
    public long IndustryPointCosts => _points;
    public double EnergyCost => 0;
    public string IndustryTypeID => TypeId;
    public ushort OutputAmount => 1;

    /// <summary>Refit jobs finish in <see cref="IndustryTools.ConstructStuff"/>, which moves the parts.</summary>
    public void OnConstructionComplete(Entity industryEntity, CargoStorageDB storage, string productionLine, IndustryJob batchJob, IConstructableDesign designInfo)
    {
    }
}
