using Pulsar4X.Factions;

namespace Pulsar4X.Client;

/// <summary>
/// Client-side access to the faction's design-time state — the unlocked data store (templates,
/// techs, cargo goods), tech levels, and missile designs the interactive component designer
/// evaluates against. The designer runs client-side (per-input formula evaluation is far too chatty
/// for a server round-trip); this seam is where that data crosses the boundary: the in-process
/// adapter hands over the live engine objects zero-copy, and a network adapter will instead
/// materialise replicas synced on connect (both DataBlobs are already save-serializable).
/// </summary>
public interface IDesignDataProvider
{
    bool TryGetDesignData(out FactionInfoDB info, out FactionTechDB techs);
}
