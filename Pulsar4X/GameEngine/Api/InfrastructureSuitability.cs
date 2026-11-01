using System.Collections.Generic;
using Pulsar4X.Api;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Storage;

namespace Pulsar4X.Engine;

/// <summary>
/// The engine's reading of an infrastructure design's gravity and pressure window and of a colony's planet. The
/// choice itself is <see cref="InfrastructureFit"/>, which the client shares.
/// </summary>
internal static class InfrastructureSuitability
{
    /// <summary>The design's window, or null when it is not an infrastructure design. A missing limit is no limit.</summary>
    internal static InfrastructureWindow? WindowOf(ComponentDesign design)
    {
        if (!design.TryGetAttribute<InfrastructureCapacityAtb>(out _))
            return null;
        double minG = double.MinValue, maxG = double.MaxValue, minP = double.MinValue, maxP = double.MaxValue;
        if (design.TryGetAttribute<GravityToleranceAtb>(out var gravity))
            (minG, maxG) = (gravity.MinGravity, gravity.MaxGravity);
        if (design.TryGetAttribute<PressureToleranceAtb>(out var pressure))
            (minP, maxP) = (pressure.MinPressure, pressure.MaxPressure);
        return new InfrastructureWindow(minG, maxG, minP, maxP);
    }

    /// <summary>The colony's planet: its gravity and pressure as the infrastructure processor reads them, and its name.</summary>
    internal static (double Gravity, double Pressure) ClimateOf(Entity colony)
    {
        double gravity = 0, pressure = 0;
        if (colony.TryGetDataBlob<ColonyInfoDB>(out var info) && info.PlanetEntity.IsValid)
        {
            if (info.PlanetEntity.TryGetDataBlob<SystemBodyInfoDB>(out var body))
                gravity = body.Gravity;
            if (info.PlanetEntity.TryGetDataBlob<AtmosphereDB>(out var atmosphere))
                pressure = atmosphere.Pressure;
        }
        return (gravity, pressure);
    }

    /// <summary>The infrastructure designs the holder has crates of, with a unit or more in stock. Each design once.</summary>
    internal static List<ComponentDesign> InStock(Entity holder)
    {
        var found = new List<ComponentDesign>();
        if (!holder.TryGetDataBlob<CargoStorageDB>(out var storage))
            return found;
        foreach (var store in storage.TypeStores.Values)
        {
            foreach (var (_, cargoable) in store.GetCargoables())
            {
                if (cargoable is ComponentInstance crate
                    && crate.Design.TryGetAttribute<InfrastructureCapacityAtb>(out _)
                    && !found.Exists(d => d.UniqueID == crate.Design.UniqueID))
                    found.Add(crate.Design);
            }
        }
        return found;
    }
}
