namespace Pulsar4X.Api;

/// <summary>
/// A lightweight 3D vector for the API boundary, so the contracts assembly stays free of engine
/// math types. Values are in the same units the engine uses for the field in question.
/// </summary>
public readonly record struct Vec3(double X, double Y, double Z);

/// <summary>How a snapshot entity relates to the requesting faction (drives colouring/visibility on the client).</summary>
public enum OwnerRelation
{
    Owned,
    Friendly,
    Neutral,
    Hostile,
    Unknown,
}

/// <summary>Broad classification of an entity for list grouping and icons. The client maps this to its
/// own display enum/short-names.</summary>
public enum BodyKind
{
    Unknown,
    Star,
    Planet,
    DwarfPlanet,
    Moon,
    Asteroid,
    Comet,
    Colony,
    Ship,
}

/// <summary>
/// Marker for a bespoke, serializable, faction-scoped projection of one aspect of an entity — the
/// read-model counterpart of an engine DataBlob. The server only populates the views a faction is
/// allowed to observe. Concrete views are added incrementally as the read surface is ported.
/// </summary>
public interface IComponentView { }

/// <summary>
/// A faction-scoped snapshot of a single entity: identity plus whichever aspect views the
/// requesting faction may see. Deliberately mirrors the ergonomics of the engine's
/// <c>Entity.GetDataBlob&lt;T&gt;()</c> so porting client read-sites stays mechanical:
/// <c>entity.GetDataBlob&lt;PositionDB&gt;()</c> becomes <c>snapshot.GetView&lt;PositionView&gt;()</c>.
/// </summary>
public sealed class EntitySnapshot
{
    public int Id { get; init; }
    public int FactionId { get; init; }
    public OwnerRelation Relation { get; init; }
    public BodyKind Kind { get; init; }
    public IReadOnlyList<IComponentView> Views { get; init; } = Array.Empty<IComponentView>();

    public T? GetView<T>() where T : class, IComponentView => Views.OfType<T>().FirstOrDefault();
    public bool HasView<T>() where T : class, IComponentView => Views.Any(static v => v is T);
}

/// <summary>Minimal identity of a star system for lists/maps (cheap to enumerate).</summary>
public sealed record SystemSummary(string SystemId, string Name);

/// <summary>The player's faction/corporation: identity and current funds.</summary>
public sealed record FactionSnapshot(string Name, string Abbreviation, decimal Funds);

/// <summary>A bulk, faction-scoped snapshot of one star system at a point in time.</summary>
public sealed class SystemSnapshot
{
    public string SystemId { get; init; } = "";
    public string Name { get; init; } = "";
    public DateTime DateTime { get; init; }
    public IReadOnlyList<EntitySnapshot> Entities { get; init; } = Array.Empty<EntitySnapshot>();
}

// --------------------------------------------------------------------------------------------
// Example views. These establish the pattern; the full ~55-view read surface is ported area by
// area in a later phase. Each view is a small serializable record carrying only display-ready data.
// --------------------------------------------------------------------------------------------

public sealed record NameView(string Name) : IComponentView;

public sealed record PositionView(Vec3 AbsolutePosition, Vec3 RelativePosition, int? ParentId) : IComponentView;

public sealed record OrbitView(
    double SemiMajorAxisKm,
    double Eccentricity,
    double OrbitalPeriodSeconds,
    int? ParentId) : IComponentView;

public sealed record MassVolumeView(double MassKg, double RadiusMetres, double DensityGramsPerCm3) : IComponentView;

public sealed record BodyView(
    string BodyType,
    double GravityMetresPerSec2,
    double SurfaceTemperatureC,
    TimeSpan DayLength,
    double AxialTiltDegrees,
    string Tectonics,
    double MagneticFieldMicroTesla,
    bool SupportsPopulations) : IComponentView;

public sealed record StarView(
    string SpectralType,
    int SpectralSubDivision,
    string SpectralClass,
    string LuminosityClass,
    double SurfaceTemperatureC,
    double Luminosity,
    double AgeYears,
    double MinHabitableRadiusAu,
    double MaxHabitableRadiusAu) : IComponentView;

public sealed record ColonyView(long Population, int? PlanetEntityId) : IComponentView;

public sealed record ShipView(string DesignName) : IComponentView;

/// <summary>Geological survey state of a body, scoped to the requesting faction.</summary>
public sealed record GeoSurveyView(bool IsSurveyComplete) : IComponentView;

/// <summary>Gravitational (jump-point) survey state of a location, scoped to the requesting faction.</summary>
public sealed record GravSurveyView(bool IsSurveyComplete) : IComponentView;

/// <summary>Marks an entity as a usable jump point. Only projected once the requesting faction has
/// discovered it, so visibility is enforced at the boundary.</summary>
public sealed record JumpPointView : IComponentView;

/// <summary>Marks an entity as having cargo storage (e.g. a colony a fleet can refuel at).</summary>
public sealed record CargoStorageView : IComponentView;

/// <summary>One named contribution to a modified stat (for breakdown tooltips).</summary>
public sealed record ValueModifier(string Name, double Delta);

/// <summary>A stat with its effective value, base value, and the modifiers between them.</summary>
public sealed record ModifiedValue(double Value, double BaseValue, IReadOnlyList<ValueModifier> Modifiers);

/// <summary>A research lab: its design, where it is, who runs it, its economics, and its tech queue
/// (ids into <see cref="ResearchSnapshot.Techs"/>). Only projected for the owning faction.</summary>
public sealed record ResearcherView(
    string DesignName,
    string DesignTemplateName,
    string DesignDescription,
    int LocationId,
    string LocationName,
    int? ScientistId,
    string? ScientistName,
    ModifiedValue CostPerDay,
    ModifiedValue PointsPerDay,
    int FundingLevel,
    IReadOnlyList<string> TechQueue) : IComponentView;

// --------------------------------------------------------------------------------------------
// Faction research state. Tech progress lives in faction data (not on lab entities), so it's
// pushed as its own faction-scoped snapshot rather than as entity views.
// --------------------------------------------------------------------------------------------

public sealed record TechCategorySnapshot(string Id, string Name);

/// <summary>A technology as the faction sees it: identity, classification, progress, and what the
/// next level unlocks (names pre-resolved server-side).</summary>
public sealed record TechSnapshot(
    string Id,
    string Name,
    string DisplayName,
    string MaxLevelName,
    string Description,
    string CategoryId,
    string CategoryName,
    int Level,
    int MaxLevel,
    int ResearchCost,
    int ResearchProgress,
    bool IsResearchable)
{
    public IReadOnlyList<string> NextLevelUnlocks { get; init; } = Array.Empty<string>();
}

/// <summary>Mirrors the engine's commander classification.</summary>
public enum CommanderKind
{
    Navy,
    Ground,
    Scientist,
    Civilian,
}

/// <summary>A commander's bonus for chooser tooltips; the filter target is pre-resolved to a display name.</summary>
public sealed record CommanderBonusSnapshot(string Name, double Value, bool IsPercentage, string? FilterName);

/// <summary>A faction commander (scientist, officer, …) for assignment UIs.</summary>
public sealed record CommanderSnapshot(
    int Id,
    string Name,
    CommanderKind Kind,
    bool IsAssigned,
    int Experience,
    int ExperienceCap,
    DateTime CommissionedOn)
{
    public IReadOnlyList<CommanderBonusSnapshot> Bonuses { get; init; } = Array.Empty<CommanderBonusSnapshot>();
}

/// <summary>The faction's research state: tech categories (game-static), every unlocked tech with
/// its progress, and the faction's scientists.</summary>
public sealed class ResearchSnapshot
{
    public IReadOnlyList<TechCategorySnapshot> Categories { get; init; } = Array.Empty<TechCategorySnapshot>();
    public IReadOnlyList<TechSnapshot> Techs { get; init; } = Array.Empty<TechSnapshot>();
    public IReadOnlyList<CommanderSnapshot> Scientists { get; init; } = Array.Empty<CommanderSnapshot>();
}

// --------------------------------------------------------------------------------------------
// Fleet command hierarchy (faction-scoped). The galaxy is otherwise organised by system, but a
// faction's fleets form their own tree of sub-fleets and member ships, so they're modelled
// separately. Per-entity details (position for centring, etc.) come from the system EntitySnapshots.
// --------------------------------------------------------------------------------------------

/// <summary>One queued order on a fleet or ship, with its execution state (for lists/tooltips).</summary>
public sealed record OrderSnapshot(string Name, bool IsRunning, bool IsFinished);

/// <summary>A ship as a fleet member: identity, the system it currently resides in, and the
/// display details the fleet UI shows (design, commander, queued orders).</summary>
public sealed record ShipSnapshot(
    int Id,
    string Name,
    string SystemId,
    string DesignName = "",
    string? CommanderName = null)
{
    public IReadOnlyList<OrderSnapshot> Orders { get; init; } = Array.Empty<OrderSnapshot>();
}

/// <summary>A fleet node in the command hierarchy.</summary>
public sealed class FleetSnapshot
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int? FlagshipId { get; init; }
    public string? FlagshipName { get; init; }
    public string? CommanderName { get; init; }

    /// <summary>The system the fleet currently resides in (null when unknown/in transit).</summary>
    public string? SystemId { get; init; }
    public string? SystemName { get; init; }

    /// <summary>The nearest faction-visible body the flagship is orbiting (hidden ancestors such as
    /// undiscovered anomalies are skipped server-side).</summary>
    public int? OrbitingEntityId { get; init; }
    public string? OrbitingName { get; init; }

    public bool InheritOrders { get; init; }
    public bool CanGeoSurvey { get; init; }
    public bool CanGravSurvey { get; init; }
    public IReadOnlyList<OrderSnapshot> Orders { get; init; } = Array.Empty<OrderSnapshot>();
    public IReadOnlyList<FleetSnapshot> SubFleets { get; init; } = Array.Empty<FleetSnapshot>();
    public IReadOnlyList<ShipSnapshot> Ships { get; init; } = Array.Empty<ShipSnapshot>();
}
