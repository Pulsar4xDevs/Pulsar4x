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

/// <summary>
/// How one faction treats another. Stored per viewer; a missing entry means Hostile.
/// Allied and Friendly both project as <see cref="OwnerRelation.Friendly"/>.
/// </summary>
public enum FactionStance
{
    Hostile,
    Neutral,
    Friendly,
    Allied,
}

/// <summary>
/// What one faction has granted another. Stored on the granter, keyed by the receiver.
/// A missing entry means nothing is granted. The two directions may differ.
/// </summary>
[Flags]
public enum FactionGrants
{
    None = 0,
    Trade = 1,
    Positions = 2,
    Geo = 4,
    Grav = 8,
    Research = 16,
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

/// <summary>One ledger row. <see cref="Amount"/> is negative for an expense.</summary>
public sealed record LedgerEntrySnapshot(DateTime Date, string Category, string Description, decimal Amount);

/// <summary>
/// The player's faction/corporation: identity, current funds, and ledger rows newest first.
/// </summary>
public sealed record FactionSnapshot(
    string Name,
    string Abbreviation,
    decimal Funds,
    IReadOnlyList<LedgerEntrySnapshot> Transactions);

/// <summary>
/// One other faction. <see cref="Stance"/> and <see cref="GrantedByViewer"/> are this viewer's.
/// <see cref="TheirStance"/> and <see cref="GrantedToViewer"/> are theirs, shown and not editable.
/// </summary>
public sealed record FactionStanceRow(
    int FactionId,
    string Name,
    FactionStance Stance,
    FactionGrants GrantedByViewer = FactionGrants.None,
    FactionGrants GrantedToViewer = FactionGrants.None,
    FactionStance TheirStance = FactionStance.Hostile);

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
    int? ParentId) : IComponentView
{
    // The full Keplerian element set (engine units: metres, radians), so the client can propagate
    // positions itself between pushes rather than the server streaming them every tick.
    public double SemiMajorAxisM { get; init; }
    public double InclinationRad { get; init; }
    public double LongitudeOfAscendingNodeRad { get; init; }
    public double ArgumentOfPeriapsisRad { get; init; }
    public double MeanAnomalyAtEpochRad { get; init; }
    public double MeanMotionRadPerSec { get; init; }
    public DateTime Epoch { get; init; }
    /// <summary>μ = G(M+m) in m³/s².</summary>
    public double StandardGravParameter { get; init; }

    /// <summary>The orbit parent's sphere-of-influence radius (metres), for clipping hyperbolic
    /// trajectories; 0 when the orbit has no parent.</summary>
    public double ParentSoiRadiusM { get; init; }

    /// <summary>The orbiting entity's own sphere-of-influence radius (metres), for maneuver
    /// targeting and encounter prediction; 0 when not computable.</summary>
    public double SoiRadiusM { get; init; }
}

/// <summary>Newtonian-thrust movement: the current trajectory (as orbital elements around the
/// SOI parent) plus the maneuver state, for drawing the trajectory and thrust vector.</summary>
public sealed record NewtonMoveView(
    int? SoiParentId,
    double SoiRadiusM,
    Vec3 CurrentVectorMps,
    Vec3 ManeuverDeltaVMps,
    double ThrustNewtons,
    OrbitView Trajectory) : IComponentView;

/// <summary>Simplified newtonian movement: the current trajectory around the SOI parent.</summary>
public sealed record NewtonSimpleMoveView(
    int? SoiParentId,
    double SoiRadiusM,
    OrbitView CurrentTrajectory) : IComponentView;

/// <summary>Marks an in-flight projectile (missile).</summary>
public sealed record ProjectileView : IComponentView;

/// <summary>An in-flight beam: its current endpoints (metres), re-pushed each tick.</summary>
public sealed record BeamView(Vec3 StartPosition, Vec3 EndPosition) : IComponentView;

public sealed record MassVolumeView(double MassKg, double RadiusMetres, double DensityGramsPerCm3) : IComponentView
{
    /// <summary>Mass without fuel/cargo (kg).</summary>
    public double DryMassKg { get; init; }
}

public sealed record BodyView(
    string BodyType,
    double GravityMetresPerSec2,
    double SurfaceTemperatureC,
    TimeSpan DayLength,
    double AxialTiltDegrees,
    string Tectonics,
    double MagneticFieldMicroTesla,
    bool SupportsPopulations,
    double RadiationLevel = 0,
    double AtmosphericDust = 0) : IComponentView;

/// <summary>A body's atmosphere; gas names are pre-resolved from game static data.</summary>
public sealed record AtmosphereView(
    double SurfaceTemperatureC,
    double PressureAtm,
    bool Hydrosphere,
    double HydrosphereExtentPercent)
    : IComponentView
{
    public IReadOnlyList<GasAmount> Composition { get; init; } = Array.Empty<GasAmount>();
}

public sealed record GasAmount(string Name, double Percent, string Id = "", double PartialPressureAtm = 0);

public sealed record StarView(
    string SpectralType,
    int SpectralSubDivision,
    string SpectralClass,
    string LuminosityClass,
    double SurfaceTemperatureC,
    double Luminosity,
    double AgeYears,
    double MinHabitableRadiusAu,
    double MaxHabitableRadiusAu,
    string LuminosityClassDescription = "") : IComponentView
{
    /// <summary>Ordinal of the spectral type (O,B,A,F,G,K,M,…), for icon styling.</summary>
    public int SpectralTypeIndex { get; init; }
}

public sealed record ColonyView(long Population, int? PlanetEntityId) : IComponentView
{
    /// <summary>Per-species population breakdown (names resolved for the requesting faction).</summary>
    public IReadOnlyList<SpeciesPopulation> SpeciesPopulations { get; init; } = Array.Empty<SpeciesPopulation>();

    /// <summary>Owning faction only. How far a supply order reaches. One of <see cref="CommandSpanLabels"/>.</summary>
    public string CommandSpan { get; init; } = "";

    /// <summary>Owning faction only. The first chair at colony level or wider. -1 when that chair is empty or missing.</summary>
    public int AdministratorId { get; init; } = -1;

    /// <summary>Owning faction only. Null when the chair is empty or the colony has no chair.</summary>
    public string? AdministratorName { get; init; }

    /// <summary>Owning faction only. True when a colony-level chair exists, even if nobody sits it.</summary>
    public bool HasAdministrativePost { get; init; }
}

/// <summary>Flagship or colony bridge span, as shown to the owning faction.</summary>
public static class CommandSpanLabels
{
    public const string Body = "Covers this body";
    public const string Well = "Covers this well";
    public const string System = "Covers this system";
    public const string Neighbor = "Covers this system and one jump";
}

public sealed record SpeciesPopulation(string SpeciesName, long Population);

/// <summary>One listed good at a logistics office. Stock is the office's cargo hold.</summary>
public sealed record MarketGoodView(
    string CargoId, string Name,
    long Stock, long Reserve,
    long BuyQuantity, decimal Bid,
    long SellQuantity, decimal Ask) : IComponentView;

/// <summary>The book a faction is allowed to see. The owner, or a viewer granted Trade.</summary>
public sealed record MarketView(
    int Capacity, IReadOnlyList<MarketGoodView> Goods) : IComponentView
{
    /// <summary>True when this faction owns the office and may post listings.</summary>
    public bool CanEdit { get; init; }

    /// <summary>False when the book is visible from one grant and the other side has not granted Trade.</summary>
    public bool ExchangeOpen { get; init; }

    /// <summary>Unlocked cargo the owner can add. Empty for a friendly viewer.</summary>
    public IReadOnlyList<MarketGoodChoice> Addable { get; init; } = Array.Empty<MarketGoodChoice>();
}

/// <summary>A cargo id the owner may add to their book.</summary>
public sealed record MarketGoodChoice(string CargoId, string Name);

/// <summary>
/// One intel row a faction is allowed to see on an office.
/// A haul row (<see cref="IntelKind.Haul"/>) is a sealed shipment: <c>Ask</c> is the freight fee, <c>ClaimantId</c> is
/// the carrying ship or -1, and <c>SourceName</c> / <c>DestName</c> name the two colonies.
/// </summary>
public sealed record IntelRowView(
    IntelKind Kind,
    string Subject,
    string Name,
    string ResultTag,
    decimal Ask,
    bool ForSale,
    bool OwnedByViewer,
    string? SystemId,
    int ClaimantId = -1,
    string? SourceName = null,
    string? DestName = null) : IComponentView;

/// <summary>A finished secret the office owner may list. Empty for a friendly viewer.</summary>
public sealed record IntelCandidateView(
    IntelKind Kind,
    string Subject,
    string Name,
    string? SystemId);

/// <summary>The intel list on a logistics office. Owned and Friendly only. Separate from <see cref="MarketView"/>.</summary>
public sealed record IntelBookView(
    bool CanEdit,
    IReadOnlyList<IntelRowView> Rows,
    IReadOnlyList<IntelCandidateView> Candidates) : IComponentView;

/// <summary>A colony's infrastructure capacity (the limiter on its industrial output).</summary>
public sealed record InfrastructureView(
    long CapacityProvided,
    long CapacityRequired,
    long CapacityAvailable,
    double Efficiency,
    /// <summary>True once at least one infrastructure installation is physically present (even if
    /// disabled by environment tolerances) — the colony counts as established.</summary>
    bool HasInstalledInfrastructure = false) : IComponentView;

/// <summary>The installations on a colony (or components on a ship), grouped by design.</summary>
public sealed record InstallationsView(IReadOnlyList<InstallationGroup> Installations) : IComponentView;

public sealed record InstallationGroup(
    string DesignId,
    string Name,
    string TemplateName,
    string Description,
    int Count,
    int OperationalCount,
    /// <summary>Whether one of these can be uninstalled into the entity's cargo storage.</summary>
    bool CanStore);

public sealed record ShipView(
    string DesignName,
    int CrewRequired = 0,
    string? CommanderName = null,
    /// <summary>Average component health 0–1 across all installed components.</summary>
    double AverageComponentHealth = 1,
    int OperationalComponents = 0,
    int TotalComponents = 0,
    /// <summary>0 when the ship has no armor.</summary>
    double ArmorThicknessMm = 0) : IComponentView;

/// <summary>Newtonian propulsion stats; ΔV values are pre-computed server-side.</summary>
public sealed record ThrustView(
    double ThrustNewtons,
    double FuelBurnRateKgPerSec,
    double ExhaustVelocityMps,
    double DeltaVMps,
    /// <summary>ΔV at full fuel tanks; 0 when it can't be determined.</summary>
    double MaxDeltaVMps) : IComponentView
{
    /// <summary>Mass of fuel currently aboard (kg), for burn-time/fuel-cost previews.</summary>
    public double TotalFuelKg { get; init; }

    /// <summary>Display name of the fuel the drive burns.</summary>
    public string FuelName { get; init; } = "";
}

public sealed record WarpAbilityView(double MaxSpeedMps) : IComponentView;

/// <summary>Power generation/storage state (owner-only): the current load/output/demand and
/// stored energy, plus a short chronological history for plotting.</summary>
public sealed record EnergyView(
    double Load,
    double Output,
    double MaxOutput,
    double Demand,
    double Stored,
    double StoreMax) : IComponentView
{
    public IReadOnlyList<EnergyHistogramPoint> Histogram { get; init; } = Array.Empty<EnergyHistogramPoint>();
}

/// <summary>One sample of the energy history; <see cref="Seconds"/> is relative to the start of
/// the sampling window.</summary>
public sealed record EnergyHistogramPoint(int Seconds, double Output, double Demand, double Stored);

/// <summary>Present while the entity is mid-warp; carries the current warp speed.</summary>
public sealed record WarpMovingView(double SpeedMps) : IComponentView
{
    // The warp route's endpoints (metres), for drawing the travel line on the map.
    public Vec3 EntryPointAbsolute { get; init; }
    public Vec3 ExitPointAbsolute { get; init; }
    /// <summary>Exit point relative to the warp target, which may itself be moving.</summary>
    public Vec3 ExitPointRelative { get; init; }
    public int? TargetEntityId { get; init; }
}

/// <summary>Geological survey state of a body, scoped to the requesting faction.</summary>
public sealed record GeoSurveyView(
    bool IsSurveyComplete,
    bool HasSurveyStarted = false,
    /// <summary>0–100, only meaningful once the survey has started.</summary>
    double PercentComplete = 0,
    long PointsRequired = 0,
    long PointsCompleted = 0) : IComponentView;

/// <summary>Marks a body the faction could found a colony on (once geo-surveyed).</summary>
public sealed record ColonizableView : IComponentView;

/// <summary>How much the requesting faction knows about a value behind survey masking.</summary>
public enum DepositAccess
{
    None,
    Partial,
    Full,
}

/// <summary>A body's mineral deposits, masked to the requesting faction's survey knowledge
/// (amounts are pre-obscured server-side at partial access).</summary>
public sealed record MineralDepositsView(IReadOnlyList<MineralDepositRow> Deposits) : IComponentView;

public sealed record MineralDepositRow(
    int MineralId,
    string Name,
    DepositAccess Access,
    long Amount,
    double Accessibility);

/// <summary>Gravitational (jump-point) survey state of a location, scoped to the requesting faction.</summary>
public sealed record GravSurveyView(
    bool IsSurveyComplete,
    bool HasSurveyStarted = false,
    /// <summary>0–100, only meaningful once the survey has started.</summary>
    double PercentComplete = 0) : IComponentView
{
    /// <summary>The system the location's jump point leads to; null until the faction has
    /// completed the survey (revealing the destination is the survey's reward).</summary>
    public string? JumpPointToSystemId { get; init; }
}

/// <summary>Marks an entity as a usable jump point. Only projected once the requesting faction has
/// discovered it, so visibility is enforced at the boundary.</summary>
public sealed record JumpPointView : IComponentView;

/// <summary>An entity's cargo storage: capacity and contents per cargo type. Only projected for the
/// owning faction (its presence also marks refuel targets for the fleet UI).</summary>
public sealed record CargoStorageView(
    double TotalStoredMassKg,
    double TransferRateKgPerHour,
    double TransferRangeDvMps)
    : IComponentView
{
    public IReadOnlyList<CargoTypeStoreView> Stores { get; init; } = Array.Empty<CargoTypeStoreView>();

    /// <summary>Same-faction colonies in transfer range. Empty for a colony and for a ship with no rate.</summary>
    public IReadOnlyList<int> InstallColonyIds { get; init; } = Array.Empty<int>();
}

public sealed record CargoTypeStoreView(
    string TypeId,
    string TypeName,
    double MaxVolume,
    double FreeVolume)
{
    public IReadOnlyList<CargoItemView> Items { get; init; } = Array.Empty<CargoItemView>();
}

public sealed record CargoItemView(
    int Id,
    string Name,
    string ItemKind,
    string Description,
    long Units,
    long UnitsInEscrow,
    double MassStoredKg,
    double MassPerUnitKg,
    double VolumeStored,
    double VolumePerUnit,
    long FreeUnitSpace,
    /// <summary>Whether Install can be clicked. A ship crate is disabled when no colony is in range.</summary>
    bool CanInstall)
{
    /// <summary>Installation points to mount this crate. Zero when it cannot be mounted.</summary>
    public long MountCost { get; init; }

    /// <summary>Whether the cargo menu shows Install. A freighter stays false. A ship with the rate
    /// stays true even when <see cref="CanInstall"/> is false.</summary>
    public bool ShowInstall { get; init; }

    /// <summary>The component design's id when this row is a component instance, else empty.</summary>
    public string DesignId { get; init; } = "";

    /// <summary>A finished facility a Construction Bay can mount: the design is crate-able.</summary>
    public bool IsCrate { get; init; }
}

/// <summary>An entity's weapons and fire controls. Owner-only.</summary>
public sealed record FireControlView(IReadOnlyList<FireControlSnapshot> FireControls) : IComponentView
{
    /// <summary>Every weapon on the entity, assigned or not.</summary>
    public IReadOnlyList<WeaponSnapshot> Weapons { get; init; } = Array.Empty<WeaponSnapshot>();

    /// <summary>Faction ordnance designs held in this entity's cargo, available to load.</summary>
    public IReadOnlyList<OrdnanceStoreItem> Ordnance { get; init; } = Array.Empty<OrdnanceStoreItem>();
}

/// <summary>One fire-control component: its current target, engagement state, and assigned weapons.</summary>
public sealed record FireControlSnapshot(
    string Id,
    string Name,
    int? TargetId,
    string? TargetName,
    bool IsEngaging)
{
    public IReadOnlyList<string> AssignedWeaponIds { get; init; } = Array.Empty<string>();
}

/// <summary>One weapon component: its fire-control assignment, magazine state and loaded ordnance.</summary>
public sealed record WeaponSnapshot(
    string Id,
    string Name,
    string? FireControlId,
    int MagazineCurrent,
    int MagazineSize,
    string? OrdnanceId,
    string? OrdnanceName,
    long OrdnanceStored);

/// <summary>An ordnance design with how many units the entity holds in cargo.</summary>
public sealed record OrdnanceStoreItem(string Id, string Name, long Stored);

/// <summary>A colony's mining operation joined with its planet's deposits and stockpile — one row per
/// known mineral. Amounts are masked to what the requesting faction has surveyed.</summary>
public sealed record ColonyMiningView(int NumberOfMines)
    : IComponentView
{
    public IReadOnlyList<MineralMiningRow> Minerals { get; init; } = Array.Empty<MineralMiningRow>();
}

public sealed record MineralMiningRow(
    int MineralId,
    string Name,
    string Description,
    long? Stockpile,           // null when the colony has no storage at all
    long? AvailableToMine,     // null when the faction's survey data can't resolve the amount
    double Accessibility,
    long AnnualProduction,     // 0 when the colony can't mine this mineral
    bool CanMine);

/// <summary>A cohort's kind. A cohort is never idle. Idle remains so an old snapshot still reads.</summary>
public enum AcademyCourseKind
{
    Idle,
    Class,
    Command,
}

/// <summary>Every mounted academy on a colony, plus the people a recruit cohort would draw on.</summary>
public sealed record NavalAcademyView(
    IReadOnlyList<NavalAcademyClassView> Academies,
    long Population,
    int PeoplePerEnsign) : IComponentView;

/// <summary>One mounted academy. Class size is how many students can be enrolled at once.</summary>
public sealed record NavalAcademyClassView(
    int ComponentId,
    int ClassSize,
    int TrainingPeriodMonths,
    int SeatsInUse,
    int MeanCap,
    IReadOnlyList<AcademyCohortView> Cohorts);

/// <summary>One run. A class enrolls <see cref="Count"/> recruits. A command course is one officer.</summary>
public sealed record AcademyCohortView(
    int CohortId,
    AcademyCourseKind Course,
    int Count,
    DateTime Graduation,
    int OfficerId,
    int PopulationTaken);

// --------------------------------------------------------------------------------------------
// Industry (production lines) and local construction. Owner-only.
// --------------------------------------------------------------------------------------------

/// <summary>An entity's industrial capability: its production lines, their job queues, and what
/// each line can build (with cost previews against the local stockpile).</summary>
public sealed record IndustryView(IReadOnlyList<ProductionLineView> ProductionLines) : IComponentView
{
    /// <summary>Launch pads on this colony. Empty when the colony has no launch complex.</summary>
    public IReadOnlyList<LaunchPadStateView> Pads { get; init; } = Array.Empty<LaunchPadStateView>();
}

public sealed record LaunchPadStateView(long MaxTonnage, bool Free);

public sealed record ProductionLineView(
    string Id,
    string Name,
    /// <summary>Industry points the line applies per day to its current (head) job; 0 when idle.</summary>
    double CurrentRatePerDay)
{
    public IReadOnlyList<IndustryJobView> Jobs { get; init; } = Array.Empty<IndustryJobView>();
    public IReadOnlyList<ConstructibleItemView> Constructibles { get; init; } = Array.Empty<ConstructibleItemView>();

    /// <summary>True when this line builds hulls. Slip size is gross tonnage, compared to ship mass.</summary>
    public bool IsShipyard { get; init; }

    public double SlipSize { get; init; }

    public IReadOnlyList<YardHullView> Hulls { get; init; } = Array.Empty<YardHullView>();

    /// <summary>Ship-refit points per day on this colony. Zero when the yard has no refit rate.</summary>
    public long RefitPointsPerDay { get; init; }

    public IReadOnlyList<RefitShipView> RefitShips { get; init; } = Array.Empty<RefitShipView>();

    public IReadOnlyList<RefitPartView> RefitCargo { get; init; } = Array.Empty<RefitPartView>();

    public IReadOnlyList<RefitJobView> RefitJobs { get; init; } = Array.Empty<RefitJobView>();

    /// <summary>Faction ship designs the refit pane can target. Empty when this line is not a yard.</summary>
    public IReadOnlyList<RefitTargetView> RefitTargets { get; init; } = Array.Empty<RefitTargetView>();
}

/// <summary>A ship in transfer range of a shipyard line.</summary>
public sealed record RefitShipView(int EntityId, string Name, long Mass, bool OverSlip)
{
    public string SlipReason { get; init; } = "";

    public IReadOnlyList<RefitPartView> Parts { get; init; } = Array.Empty<RefitPartView>();
}

/// <summary>A ship part on the ship or in the warehouse. Cost is the mass formula.</summary>
public sealed record RefitPartView(int ComponentId, string Name, long Mass, long Cost)
{
    /// <summary>Component design id. The refit diff matches on this, not on the instance.</summary>
    public string DesignId { get; init; } = "";
}

/// <summary>A ship design the refit pane can turn the selected ship toward.</summary>
public sealed record RefitTargetView(string DesignId, string Name)
{
    public IReadOnlyList<RefitTargetPartView> Parts { get; init; } = Array.Empty<RefitTargetPartView>();
}

/// <summary>One component design and how many copies a target ship design carries.</summary>
public sealed record RefitTargetPartView(string DesignId, string Name, int Count);

/// <summary>A component design the target needs that is not on the ship or in the warehouse.</summary>
public sealed record RefitMissingPartView(string DesignId, string Name, int Count);

/// <summary>Suggested checks and the parts still to build, for one target design.</summary>
public sealed record RefitDesignDiffResult(
    IReadOnlyList<int> RemoveInstanceIds,
    IReadOnlyList<int> MountInstanceIds,
    IReadOnlyList<RefitMissingPartView> Missing);

/// <summary>A refit job queued on this line.</summary>
public sealed record RefitJobView(string ShipName, long PointCost, long PointsAccumulated, double Progress);

/// <summary>Shown on the ship while a refit names it. Not an editor.</summary>
public sealed record ShipRefitView(string YardName, long PointCost, long PointsAccumulated, double Progress) : IComponentView
{
    public IReadOnlyList<string> Removing { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Mounting { get; init; } = Array.Empty<string>();
}

/// <summary>A finished hull waiting on a shipyard line.</summary>
public sealed record YardHullView(string DesignId, string DesignName, string Name, long Mass, string Reason);

public sealed record IndustryJobView(
    string JobId,
    string Name,
    int NumberCompleted,
    int NumberOrdered,
    bool Repeat,
    string Status,
    bool MissingResources,
    double PercentComplete,
    long ProductionPointsLeft)
{
    /// <summary>What the job still needs (names pre-resolved), for status tooltips.</summary>
    public IReadOnlyList<ResourceRequirement> RemainingRequirements { get; init; } = Array.Empty<ResourceRequirement>();
}

public sealed record ResourceRequirement(string Name, long Amount);

/// <summary>Something a production line can build, with per-unit costs and current local availability
/// so the client can preview a job without engine math.</summary>
public sealed record ConstructibleItemView(
    string DesignId,
    string Name,
    long IndustryPointsPerUnit,
    int OutputAmount,
    /// <summary>Whether the production checkbox can queue a construction mount.</summary>
    bool CanAutoInstall)
{
    public IReadOnlyList<IndustryCostItem> Costs { get; init; } = Array.Empty<IndustryCostItem>();

    /// <summary>Why the checkbox is disabled. Empty when it can queue a mount.</summary>
    public string AutoInstallReason { get; init; } = "";

    /// <summary>Design mass in the same unit as slip size and pad tonnage.</summary>
    public long Mass { get; init; }

    /// <summary>True when this ship design is heavier than the line's slip.</summary>
    public bool OverSlip { get; init; }

    /// <summary>Why the design is disabled in the new-job combo. Empty when it fits the slip.</summary>
    public string SlipReason { get; init; } = "";
}

public sealed record IndustryCostItem(
    string Name,
    long PerUnit,
    /// <summary>Units currently in the entity's stockpile.</summary>
    long Available,
    /// <summary>Whether the faction could produce/mine more of this input itself.</summary>
    bool CanProduce);

/// <summary>An entity's installation queue: the daily rate, the jobs, stored crates, and site-only designs.
/// A ship also lists same-faction colonies in transfer range.</summary>
public sealed record ConstructionView(long PointsPerDay) : IComponentView
{
    public IReadOnlyList<ConstructionJobView> BuildQueue { get; init; } = Array.Empty<ConstructionJobView>();
    public IReadOnlyList<ConstructionCrateView> Crates { get; init; } = Array.Empty<ConstructionCrateView>();
    public IReadOnlyList<ConstructionSiteView> SiteDesigns { get; init; } = Array.Empty<ConstructionSiteView>();

    /// <summary>True when the actor is a ship. Colony Management does not list colonies for a colony.</summary>
    public bool IsShip { get; init; }

    public IReadOnlyList<ConstructionColonyView> ColoniesInRange { get; init; } = Array.Empty<ConstructionColonyView>();
}

/// <summary>A same-faction colony in cargo-transfer range of a construction ship.</summary>
public sealed record ConstructionColonyView(int EntityId, string Name);

public sealed record ConstructionJobView(
    string Name,
    /// <summary>Mount or Assemble.</summary>
    string Kind,
    /// <summary>Point cost copied onto the job when it was queued.</summary>
    long PointCost,
    long PointsAccumulated,
    /// <summary>Progress on the current item, 0.0–1.0.</summary>
    double Progress)
{
    /// <summary>Colony a ship job mounts on. Empty when the actor mounts on itself.</summary>
    public string TargetName { get; init; } = "";
}

/// <summary>A warehouse crate that can be mounted. The cost is the mass formula, not the recipe.</summary>
public sealed record ConstructionCrateView(int ComponentId, string DesignId, string Name, long MountCost);

/// <summary>A design with no cargo flag. Listed even when its recipe is short.</summary>
public sealed record ConstructionSiteView(string DesignId, string Name, long IndustryPointCosts)
{
    public IReadOnlyList<string> MissingResources { get; init; } = Array.Empty<string>();
}

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

/// <summary>A named skill the commander already knows. The line is the player-facing description.</summary>
public sealed record CommanderSkillSnapshot(string Name, string Line);

/// <summary>
/// One skill domain. Points rise by one when a finished order maps here.
/// Quality is 1 + points / 100. Cap is the commander's experience cap, or 100 when that cap is 0.
/// </summary>
public sealed record CommanderDomainSnapshot(string Name, int Points, int Cap, float Quality, string Line);

/// <summary>A faction commander (scientist, officer, …) for the personnel roster and assignment UIs.</summary>
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

    /// <summary>Named skills this commander knows. Empty when they know none.</summary>
    public IReadOnlyList<CommanderSkillSnapshot> Skills { get; init; } = Array.Empty<CommanderSkillSnapshot>();

    /// <summary>Command, Nav, and Survey. Present even when every value is 0.</summary>
    public IReadOnlyList<CommanderDomainSnapshot> Domains { get; init; } = Array.Empty<CommanderDomainSnapshot>();

    /// <summary>Numeric rank within the commander's career track.</summary>
    public int Rank { get; init; }

    /// <summary>Rank title resolved server-side from the game theme (e.g. "Captain"); null when the
    /// theme has no title for this rank/track.</summary>
    public string? RankName { get; init; }

    /// <summary>When the commander reached their current rank.</summary>
    public DateTime RankedOn { get; init; }

    /// <summary>Display name of the current posting (commanded ship, lab, admin post), pre-resolved
    /// server-side; null when unassigned.</summary>
    public string? AssignmentName { get; init; }
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
// Component design (faction-scoped). The interactive designer runs CLIENT-side (formula evaluation
// is too chatty for a per-input round-trip) against the mod data both sides already share plus the
// faction's synced design-time state; the server only validates: CreateComponentDesignCommand
// replays the submitted inputs onto a fresh engine designer and registers the result.
// --------------------------------------------------------------------------------------------

/// <summary>The faction's component-design surface: the templates it can design from and the
/// designs it has already created.</summary>
public sealed class ComponentDesignsSnapshot
{
    public IReadOnlyList<ComponentTemplateSummary> Templates { get; init; } = Array.Empty<ComponentTemplateSummary>();
    public IReadOnlyList<ComponentDesignSummary> Designs { get; init; } = Array.Empty<ComponentDesignSummary>();
    /// <summary>
    /// The crate list a fleet Bootstrap order starts from, in priority order, from mod data. Only designs this
    /// faction has and that can be crated are listed.
    /// </summary>
    public IReadOnlyList<BootstrapCrateRequest> BootstrapDefaults { get; init; } = Array.Empty<BootstrapCrateRequest>();
}

/// <summary>A component template the faction can design from.</summary>
public sealed record ComponentTemplateSummary(string Id, string Name, string ComponentType, string Description);

/// <summary>An amount of one resource, by cargo id, with its display name.</summary>
public sealed record ResourceCostLine(string CargoId, string Name, long Amount);

/// <summary>An existing component design; <see cref="PropertyValues"/> carries the inputs it was
/// created with so the client can reload it into the designer.</summary>
public sealed record ComponentDesignSummary(string Id, string Name, string TemplateId, string TemplateName)
{
    /// <summary>Can be mounted on a planet and carried in a ship's hold. A fleet Bootstrap order can ship these.</summary>
    public bool IsCrate { get; init; }
    /// <summary>Mounts on a planet but cannot ride in a hold. It is built on site, from hauled resources.</summary>
    public bool IsSiteOnly { get; init; }
    /// <summary>What one unit costs to build, per resource.</summary>
    public IReadOnlyList<ResourceCostLine> ResourceCosts { get; init; } = Array.Empty<ResourceCostLine>();
    public IReadOnlyList<DesignerInput> PropertyValues { get; init; } = Array.Empty<DesignerInput>();
}

/// <summary>One player-set designer property value: numeric for sliders/ranges/enums, a string id
/// (tech/fuel/ordnance/category id or formula key) for selection lists. The serializable form of a
/// designer's player-settable state — sent with <c>CreateComponentDesignCommand</c> so the server
/// can replay and validate the design, and carried on <see cref="ComponentDesignSummary"/> so the
/// client can reload an existing design into its designer.</summary>
public sealed record DesignerInput(string PropertyName, double? NumericValue = null, string? StringValue = null);

// --------------------------------------------------------------------------------------------
// Fleet command hierarchy (faction-scoped). The galaxy is otherwise organised by system, but a
// faction's fleets form their own tree of sub-fleets and member ships, so they're modelled
// separately. Per-entity details (position for centring, etc.) come from the system EntitySnapshots.
// --------------------------------------------------------------------------------------------

public sealed record GoalSnapshot(
    string Name,
    string Status,
    string Message = "")
{
    /// <summary>Set once the player has cancelled. The buttons hide, and a transit may still be finishing.</summary>
    public bool PlayerCancelled { get; init; }
}


/// <summary>One queued order on a fleet or ship, with its execution state (for lists/tooltips).</summary>
public sealed record OrderSnapshot(
    string Name,
    bool IsRunning,
    bool IsFinished,
    string Details = "",
    /// <summary>True for a not-yet-running thrust maneuver the player can still edit/delete.</summary>
    bool IsEditableManeuver = false)
{
    public string Status = "";
    /// <summary>The order's id, for order-addressed commands (e.g. pause).</summary>
    public string OrderId { get; init; } = "";

    public bool IsBlocking { get; init; }

    /// <summary>Which action lanes the order occupies (movement / external / self).</summary>
    public bool UsesMovementLane { get; init; }
    public bool UsesExternalLane { get; init; }
    public bool UsesSelfLane { get; init; }

    /// <summary>Pause the simulation when this order actions.</summary>
    public bool PauseOnAction { get; init; }

    /// <summary>The burn-centre time of an editable maneuver (null otherwise), so the maneuver
    /// panel can edit the order from the snapshot alone.</summary>
    public DateTime? ManeuverNodeTime { get; init; }

    /// <summary>The orbit-relative ΔV (X = radial, Y = prograde, Z = normal) of an editable
    /// maneuver; null otherwise.</summary>
    public Vec3? ManeuverDeltaVMps { get; init; }
}

/// <summary>The entity's own order queue (owner-only; fleets/ships also carry orders in the
/// fleet-hierarchy snapshots, this view serves per-entity UI like the entity window).</summary>
public sealed record OrdersView(GoalSnapshot goal, IReadOnlyList<OrderSnapshot> Orders) : IComponentView;

/// <summary>How a ship's-log line sits on the page. To, From, and Action are indented.</summary>
public enum ShipLogKind
{
    Own,
    To,
    From,
    Action,
}

/// <summary>One saved line. The text already names the other party.</summary>
public sealed record ShipLogLine(DateTime At, ShipLogKind Kind, string Text);

/// <summary>The owner's book for one ship, fleet, or colony. Another faction does not get this view.</summary>
public sealed record ShipLogView(IReadOnlyList<ShipLogLine> Lines) : IComponentView;


/// <summary>A ship as a fleet member: identity, the system it currently resides in, and the
/// display details the fleet UI shows (design, commander, queued orders).</summary>
public sealed record ShipSnapshot(
    int Id,
    string Name,
    string SystemId,
    string DesignName = "",
    string? CommanderName = null)
{
    public GoalSnapshot? Goal { get; init; }
    public IReadOnlyList<OrderSnapshot> Orders { get; init; } = Array.Empty<OrderSnapshot>();

    /// <summary>This hull's own geo-survey kit. A fleet flag is "any child can".</summary>
    public bool CanGeoSurvey { get; init; }

    /// <summary>This hull's own grav-survey kit. A fleet flag is "any child can".</summary>
    public bool CanGravSurvey { get; init; }

    /// <summary>This hull's own asteroid miner. A fleet flag is "any child can".</summary>
    public bool CanMineAsteroids { get; init; }

    /// <summary>The seated captain, or null when the chair is empty and the player plots actions.</summary>
    public int? CommanderId { get; init; }

    /// <summary>This hull has a Construction Bay that can mount crates.</summary>
    public bool HasConstructionBay { get; init; }

    /// <summary>Owned colonies currently inside this ship's cargo-transfer range.</summary>
    public IReadOnlyList<int> ColoniesInCargoRange { get; init; } = Array.Empty<int>();

    /// <summary>Effective span, the narrower of the bridge and the seated rank. One of <see cref="CommandSpanLabels"/>.</summary>
    public string CommandSpan { get; init; } = "";

    /// <summary>One line under the captain. Names the effective span, the bridge, the officer, and which of them limits reach.</summary>
    public string SpanLine { get; init; } = "";

    /// <summary>What to change to reach a well: fit a bridge, seat or promote an officer. Empty when the ship already does.</summary>
    public string ReachFixWell { get; init; } = "";

    /// <summary>What to change to reach a system. Empty when the ship already does.</summary>
    public string ReachFixSystem { get; init; } = "";

    /// <summary>Systems a haul or trade from this ship may use.</summary>
    public IReadOnlyList<string> LogisticsSystemIds { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Well-known ids for the standing-order condition and action types. The set is defined by engine
/// code (not mod data), so the ids are part of the contract both sides compile against: the client
/// builds its pick-lists from them, the server maps them back to engine conditions/actions.
/// </summary>
public static class StandingOrderTypes
{
    // conditions
    public const string FuelCondition = "condition:fuel";

    // actions
    public const string MoveToNearestColony = "action:move-to-nearest-colony";
    public const string MoveToNearestGeoSurvey = "action:move-to-nearest-geo-survey";
    public const string MoveToNearestAnomaly = "action:move-to-nearest-anomaly";
    public const string Refuel = "action:refuel";
    public const string Resupply = "action:resupply";
}

public enum StandingOrderComparison
{
    LessThan,
    LessThanOrEqual,
    EqualTo,
    GreaterThan,
    GreaterThanOrEqual,
}

/// <summary>How a condition combines with the next condition in the list.</summary>
public enum StandingOrderLogic
{
    And,
    Or,
}

/// <summary>One threshold condition of a standing order (e.g. "fleet average fuel &lt; 30%").</summary>
public sealed record StandingOrderCondition(
    string ConditionType,
    StandingOrderComparison Comparison,
    float Threshold,
    /// <summary>Combines this condition with the next one; null on the last condition.</summary>
    StandingOrderLogic? Logic = null);

/// <summary>
/// One standing (conditional) order: when the conditions hold and the fleet is otherwise idle, the
/// actions are queued in order. Serializable, so it doubles as the read model on
/// <see cref="FleetSnapshot.StandingOrders"/> and the write payload of <c>SetStandingOrdersCommand</c>.
/// </summary>
public sealed record StandingOrder(
    string Name,
    IReadOnlyList<StandingOrderCondition> Conditions,
    IReadOnlyList<string> Actions);

/// <summary>A fleet node in the command hierarchy.</summary>
public sealed class FleetSnapshot
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int? FlagshipId { get; init; }
    public string? FlagshipName { get; init; }
    public string? CommanderName { get; init; }

    /// <summary>
    /// The flagship's captain. Null when the fleet has no flagship or that chair is empty.
    /// Seating the fleet commander writes this person onto the flagship.
    /// </summary>
    public int? CommanderId { get; init; }

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
    public bool CanMineAsteroids { get; init; }

    /// <summary>Effective span of the flagship. One of <see cref="CommandSpanLabels"/>.</summary>
    public string CommandSpan { get; init; } = "";

    /// <summary>One line under the fleet commander. Names the effective span, the bridge, and the officer.</summary>
    public string SpanLine { get; init; } = "";

    /// <summary>How far trade, freight, and a haul contract reach. One step wider than <see cref="CommandSpan"/>.</summary>
    public string LogisticsSpan { get; init; } = "";

    /// <summary>Systems a trade or haul order may use. The fleet's system, plus one jump when <see cref="LogisticsSpan"/> says so.</summary>
    public IReadOnlyList<string> LogisticsSystemIds { get; init; } = Array.Empty<string>();
    public GoalSnapshot? Goal { get; init; }
    public IReadOnlyList<OrderSnapshot> Orders { get; init; } = Array.Empty<OrderSnapshot>();
    public IReadOnlyList<StandingOrder> StandingOrders { get; init; } = Array.Empty<StandingOrder>();
    public IReadOnlyList<FleetSnapshot> SubFleets { get; init; } = Array.Empty<FleetSnapshot>();
    public IReadOnlyList<ShipSnapshot> Ships { get; init; } = Array.Empty<ShipSnapshot>();
}
