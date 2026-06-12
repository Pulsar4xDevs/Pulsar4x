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
}

public sealed record SpeciesPopulation(string SpeciesName, long Population);

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
    double PercentComplete = 0) : IComponentView;

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
    /// <summary>Whether this item is a component instance that can be installed on the holding entity.</summary>
    bool CanInstall);

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

public sealed record NavalAcademyView(IReadOnlyList<NavalAcademyClassView> Academies) : IComponentView;

public sealed record NavalAcademyClassView(int ClassSize, int TrainingPeriodMonths, DateTime GraduationDate);

// --------------------------------------------------------------------------------------------
// Industry (production lines) and local construction. Owner-only.
// --------------------------------------------------------------------------------------------

/// <summary>An entity's industrial capability: its production lines, their job queues, and what
/// each line can build (with cost previews against the local stockpile).</summary>
public sealed record IndustryView(IReadOnlyList<ProductionLineView> ProductionLines) : IComponentView;

public sealed record ProductionLineView(
    string Id,
    string Name,
    /// <summary>Industry points the line applies per day to its current (head) job; 0 when idle.</summary>
    double CurrentRatePerDay)
{
    public IReadOnlyList<IndustryJobView> Jobs { get; init; } = Array.Empty<IndustryJobView>();
    public IReadOnlyList<ConstructibleItemView> Constructibles { get; init; } = Array.Empty<ConstructibleItemView>();
}

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
    bool CanAutoInstall)
{
    public IReadOnlyList<IndustryCostItem> Costs { get; init; } = Array.Empty<IndustryCostItem>();
}

public sealed record IndustryCostItem(
    string Name,
    long PerUnit,
    /// <summary>Units currently in the entity's stockpile.</summary>
    long Available,
    /// <summary>Whether the faction could produce/mine more of this input itself.</summary>
    bool CanProduce);

/// <summary>An entity's local construction capability: build rate, FIFO queue, and the designs the
/// faction can queue here.</summary>
public sealed record ConstructionView(long PointsPerDay) : IComponentView
{
    public IReadOnlyList<ConstructionJobView> BuildQueue { get; init; } = Array.Empty<ConstructionJobView>();
    public IReadOnlyList<ConstructibleDesignView> AvailableDesigns { get; init; } = Array.Empty<ConstructibleDesignView>();
}

public sealed record ConstructionJobView(
    string Name,
    string ComponentType,
    long IndustryPointCosts,
    long PointsAccumulated,
    /// <summary>Progress on the current item, 0.0–1.0.</summary>
    double Progress);

public sealed record ConstructibleDesignView(string DesignId, string Name, string ComponentType, long IndustryPointCosts);

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
}

/// <summary>A component template the faction can design from.</summary>
public sealed record ComponentTemplateSummary(string Id, string Name, string ComponentType, string Description);

/// <summary>An existing component design; <see cref="PropertyValues"/> carries the inputs it was
/// created with so the client can reload it into the designer.</summary>
public sealed record ComponentDesignSummary(string Id, string Name, string TemplateId, string TemplateName)
{
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

/// <summary>One queued order on a fleet or ship, with its execution state (for lists/tooltips).</summary>
public sealed record OrderSnapshot(
    string Name,
    bool IsRunning,
    bool IsFinished,
    string Details = "",
    /// <summary>True for a not-yet-running thrust maneuver the player can still edit/delete.</summary>
    bool IsEditableManeuver = false)
{
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
public sealed record OrdersView(IReadOnlyList<OrderSnapshot> Orders) : IComponentView;

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
    public IReadOnlyList<StandingOrder> StandingOrders { get; init; } = Array.Empty<StandingOrder>();
    public IReadOnlyList<FleetSnapshot> SubFleets { get; init; } = Array.Empty<FleetSnapshot>();
    public IReadOnlyList<ShipSnapshot> Ships { get; init; } = Array.Empty<ShipSnapshot>();
}
