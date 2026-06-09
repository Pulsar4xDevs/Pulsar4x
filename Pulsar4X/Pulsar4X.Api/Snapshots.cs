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
