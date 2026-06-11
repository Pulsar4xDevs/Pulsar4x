namespace Pulsar4X.Api;

/// <summary>
/// The client-side replicated galaxy model: a synchronously-readable cache the UI renders from each
/// frame. An <see cref="IGameClient"/> keeps it current from bulk snapshots and the server event
/// stream, so the immediate-mode UI never has to await the (possibly remote) server while drawing.
/// </summary>
public interface IClientGalaxy
{
    TimeState Time { get; }
    IReadOnlyCollection<SystemSummary> KnownSystems { get; }
    IClientSystem? GetSystem(string systemId);

    /// <summary>The faction's fleet command hierarchy (root fleets).</summary>
    IReadOnlyList<FleetSnapshot> Fleets { get; }

    /// <summary>The faction's ships that sit at the command-hierarchy root, outside any fleet.</summary>
    IReadOnlyList<ShipSnapshot> UnattachedShips { get; }

    /// <summary>The player's faction/corporation (identity + funds). Null until the first push.</summary>
    FactionSnapshot? Faction { get; }

    /// <summary>The faction's research state (categories, techs, scientists). Null until the first push.</summary>
    ResearchSnapshot? Research { get; }

    /// <summary>The faction's component templates and existing designs. Null until the first push.</summary>
    ComponentDesignsSnapshot? ComponentDesigns { get; }

    /// <summary>Everyone in the faction's service (officers, scientists, administrators).</summary>
    IReadOnlyList<CommanderSnapshot> Commanders { get; }
}

/// <summary>A single star system within the client galaxy model.</summary>
public interface IClientSystem
{
    string SystemId { get; }
    string Name { get; }
    DateTime DateTime { get; }
    IReadOnlyCollection<EntitySnapshot> Entities { get; }
    EntitySnapshot? GetEntity(int entityId);
}
