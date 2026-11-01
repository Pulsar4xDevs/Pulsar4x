namespace Pulsar4X.Api;

/// <summary>
/// A serializable intent issued by a client to act on an entity. The server translates each
/// command into the engine's internal order (<c>EntityCommand</c>) and validates faction
/// ownership before executing. New command types are added as the write surface is ported.
/// </summary>
public abstract record GameCommand(int TargetEntityId);

/// <summary>The server's acknowledgement of a submitted command (not its eventual game effect).</summary>
public sealed record CommandResult(bool Accepted, string? CommandId = null, string? RejectionReason = null)
{
    public static CommandResult Ok(string commandId) => new(true, commandId);
    public static CommandResult Reject(string reason) => new(false, null, reason);
}

// --------------------------------------------------------------------------------------------
// Commands carry only the intent — the server supplies faction/timestamps. TargetEntityId is the
// commanded (ownership-checked) entity; secondary targets travel as extra fields the server
// resolves. The full ~45-command write surface is ported incrementally.
// --------------------------------------------------------------------------------------------

public sealed record RenameCommand(int TargetEntityId, string NewName) : GameCommand(TargetEntityId);

/// <summary>
/// Set how the commanded faction treats <see cref="OtherFactionId"/>. Targets the faction entity.
/// Writes only that faction's stance table.
/// </summary>
public sealed record SetFactionStanceCommand(int TargetEntityId, int OtherFactionId, FactionStance Stance)
    : GameCommand(TargetEntityId);

/// <summary>
/// Turn one grant on or off toward <see cref="OtherFactionId"/>. Targets the faction entity.
/// Writes only that faction's grant table. <see cref="Grant"/> is a single flag.
/// </summary>
public sealed record SetFactionGrantCommand(int TargetEntityId, int OtherFactionId, FactionGrants Grant, bool Enabled)
    : GameCommand(TargetEntityId);

/// <summary>
/// Set one cargo row of a colony's market policy. Targets the colony.
/// The book is posted later by <see cref="RunMarketCommand"/>.
/// </summary>
public sealed record SetMarketPolicyCommand(
    int TargetEntityId,
    string CargoId,
    long Min,
    long Max,
    bool AutoProduce,
    decimal Ask,
    decimal Bid) : GameCommand(TargetEntityId);

/// <summary>Remove one cargo row from a colony's market policy. Targets the colony.</summary>
public sealed record ClearMarketPolicyCommand(int TargetEntityId, string CargoId) : GameCommand(TargetEntityId);

/// <summary>Tell a colony to keep its market book posted. Targets the colony.</summary>
public sealed record RunMarketCommand(int TargetEntityId) : GameCommand(TargetEntityId);

/// <summary>How a colony supply order treats the offices its command bridge can see.</summary>
public enum SupplyMode
{
    Run,
    Balance,
    Stockpile,
}

/// <summary>Hand market work to owned colonies around this one. Targets the colony.</summary>
public sealed record SupplyLocalCommand(int TargetEntityId, SupplyMode Mode) : GameCommand(TargetEntityId);

/// <summary>Tell one cargo ship to buy in this system and sell where the bid covers the trip. Targets the ship.</summary>
public sealed record TradeCommand(int TargetEntityId) : GameCommand(TargetEntityId);

/// <summary>Tell one cargo ship to haul a posted good between this faction's colonies. Targets the ship.</summary>
public sealed record FreighterCommand(int TargetEntityId) : GameCommand(TargetEntityId);

/// <summary>Parcel trade pairs across a fleet. Targets the fleet. <see cref="BodyId"/> is the anchor body.</summary>
public sealed record FleetTradeCommand(int TargetEntityId, int BodyId) : GameCommand(TargetEntityId);

/// <summary>Parcel owned hauls across a fleet. Targets the fleet. <see cref="BodyId"/> is the anchor body.</summary>
public sealed record FleetFreighterCommand(int TargetEntityId, int BodyId) : GameCommand(TargetEntityId);

/// <summary>Split one owned haul across a fleet. Targets the fleet. The two colonies do not grow with command span.</summary>
public sealed record FleetHaulContractCommand(int TargetEntityId, int SourceId, int DestId, string CargoId)
    : GameCommand(TargetEntityId);

/// <summary>One trip. A captained ship carries these warehouse piles between two owned colonies.</summary>
public sealed record MoveGoodsCommand(
    int TargetEntityId,
    int SourceId,
    int DestId,
    IReadOnlyList<CargoTransferItem> Items) : GameCommand(TargetEntityId);

/// <summary>This many finished crates of one component design.</summary>
public sealed record BootstrapCrateRequest(string DesignId, int Count);

/// <summary>
/// One captained ship with a Construction Bay. It loads the crates at the source colony, flies to the
/// destination colony and its bay mounts them there from its own hold.
/// </summary>
public sealed record BootstrapColonyCommand(
    int TargetEntityId,
    int SourceId,
    int DestId,
    IReadOnlyList<BootstrapCrateRequest> Crates) : GameCommand(TargetEntityId);

/// <summary>
/// The fleet Bootstrap order. Crates are listed in priority order: the fleet's ships, sub-fleets included,
/// take them first to last, over as many trips as it takes. The same design listed twice counts once, at its
/// first place. Targets the fleet.
/// <para>
/// <c>SiteBuilds</c> are site-only facilities the player wants built at the destination. They are not shipped. The
/// resources they cost (cost x count) are derived by the server and hauled once every crate has arrived.
/// </para>
/// </summary>
public sealed record FleetBootstrapCommand(
    int TargetEntityId,
    int SourceId,
    int DestId,
    IReadOnlyList<BootstrapCrateRequest> Crates,
    IReadOnlyList<BootstrapCrateRequest>? SiteBuilds = null) : GameCommand(TargetEntityId);

/// <summary>Empty chair. Plot the flight to the source and a load. The load plots the destination when it finishes.</summary>
public sealed record QueueMoveGoodsCommand(
    int TargetEntityId,
    int SourceId,
    int DestId,
    IReadOnlyList<CargoTransferItem> Items) : GameCommand(TargetEntityId);

/// <summary>Empty chair, in cargo range. Load these piles from the colony. Escrow waits until the action starts.</summary>
public sealed record LoadGoodsCommand(
    int TargetEntityId,
    int ColonyId,
    IReadOnlyList<CargoTransferItem> Items) : GameCommand(TargetEntityId);

/// <summary>Empty chair, in cargo range. Unload these piles from the ship onto the colony.</summary>
public sealed record UnloadGoodsCommand(
    int TargetEntityId,
    int ColonyId,
    IReadOnlyList<CargoTransferItem> Items) : GameCommand(TargetEntityId);

/// <summary>One trip for one captained cargo ship in the fleet. The lines are not split.</summary>
public sealed record FleetMoveGoodsCommand(
    int TargetEntityId,
    int SourceId,
    int DestId,
    IReadOnlyList<CargoTransferItem> Items) : GameCommand(TargetEntityId);

/// <summary>
/// Post or replace one cargo row on a colony's market book, and keep the matching policy row
/// in step so a running market wake uses these prices and reserve. Targets the colony.
/// </summary>
public sealed record SetMarketListingCommand(
    int TargetEntityId,
    string CargoId,
    long SellQuantity,
    decimal Ask,
    long BuyQuantity,
    decimal Bid,
    long Reserve) : GameCommand(TargetEntityId);

/// <summary>Remove one cargo row from the book and from market policy. Targets the colony.</summary>
public sealed record ClearMarketListingCommand(int TargetEntityId, string CargoId) : GameCommand(TargetEntityId);

/// <summary>A secret a logistics office can list. Geo, Pin, and Jump are the survey kinds.</summary>
public enum IntelKind
{
    Geo,
    Pin,
    Jump,
}

/// <summary>
/// Post or replace one intel row on a colony's office. Targets the colony.
/// <see cref="Subject"/> is the secret's id. Survey kinds store an entity id in invariant decimal form.
/// </summary>
public sealed record SetIntelListingCommand(
    int TargetEntityId,
    string Subject,
    IntelKind Kind,
    decimal Ask,
    bool ForSale) : GameCommand(TargetEntityId);

/// <summary>Drop one intel row. A missing row succeeds. Targets the colony.</summary>
public sealed record ClearIntelListingCommand(
    int TargetEntityId,
    string Subject,
    IntelKind Kind) : GameCommand(TargetEntityId);

/// <summary>
/// Buy one intel row from a colony's office. Targets the colony. The commanding faction is the buyer.
/// </summary>
public sealed record BuyIntelCommand(
    int TargetEntityId,
    string Subject,
    IntelKind Kind) : GameCommand(TargetEntityId);

// ----- fleet organisation (commanded entity: the faction for create, otherwise the fleet/ship) -----
#region Fleet Organization Commands

/// <summary>Create a new (empty, server-named) fleet in the given system. Targets the faction itself.</summary>
public sealed record CreateFleetCommand(int TargetEntityId, string SystemId) : GameCommand(TargetEntityId);

/// <summary>Found a colony on a body (the faction's first species settles it). Targets the faction itself.</summary>
public sealed record CreateColonyCommand(int TargetEntityId, int BodyId) : GameCommand(TargetEntityId);

public sealed record DisbandFleetCommand(int TargetEntityId) : GameCommand(TargetEntityId);

/// <summary>Re-parent a fleet under another fleet, or under the faction root when
/// <see cref="NewParentId"/> is the faction's entity id.</summary>
public sealed record ChangeFleetParentCommand(int TargetEntityId, int NewParentId) : GameCommand(TargetEntityId);

/// <summary>Move a ship (the commanded entity) to <see cref="ToFleetId"/>; the server detaches it
/// from whichever fleet (or the faction root) currently holds it.</summary>
public sealed record ReassignShipCommand(int TargetEntityId, int ToFleetId) : GameCommand(TargetEntityId);

/// <summary>Take a ship out of its fleet. It stays owned, and shows with the unassigned ships.
/// Targets the ship.</summary>
public sealed record DetachShipCommand(int TargetEntityId) : GameCommand(TargetEntityId);

public sealed record SetFlagshipCommand(int TargetEntityId, int ShipId) : GameCommand(TargetEntityId);

/// <summary>
/// Seat a navy officer as this ship's captain, or clear the chair when <see cref="CommanderId"/>
/// is negative. Targets the ship. The fleet commander is the flagship's captain, so the fleet
/// UI sends this against the flagship. One officer, one ship: a person already captaining
/// another ship leaves that chair.
/// </summary>
public sealed record AssignCaptainCommand(int TargetEntityId, int CommanderId) : GameCommand(TargetEntityId);

/// <summary>
/// Seat a civilian as this colony's administrator, or clear the chair when <see cref="CommanderId"/>
/// is negative. Targets the colony. The chair is the first post at colony level or wider.
/// A civilian already posted elsewhere leaves that post first. A navy officer or a scientist is rejected.
/// </summary>
public sealed record AssignAdministratorCommand(int TargetEntityId, int CommanderId) : GameCommand(TargetEntityId);

/// <summary>Start a recruit cohort at one mounted academy. Targets the colony. Count is how many ensigns this run enrolls.</summary>
public sealed record TrainAcademyClassCommand(int TargetEntityId, int ComponentId, int Count) : GameCommand(TargetEntityId);

/// <summary>Send one unassigned officer through one academy's command course. Targets the colony.</summary>
public sealed record AcademyCommandCourseCommand(int TargetEntityId, int ComponentId, int CommanderId) : GameCommand(TargetEntityId);

/// <summary>Stop one cohort and return the people that cohort took. Targets the colony.</summary>
public sealed record CancelAcademyCourseCommand(int TargetEntityId, int ComponentId, int CohortId) : GameCommand(TargetEntityId);

/// <summary>One posted haul between two colonies. Targets the ship. Both ends must sit inside its freight reach.</summary>
public sealed record HaulPairCommand(int TargetEntityId, int SourceId, int DestId, string CargoId) : GameCommand(TargetEntityId);

/// <summary>One buy and one sell. Targets the ship. Both ends must sit inside its freight reach.</summary>
public sealed record TradePairCommand(int TargetEntityId, int SourceId, int DestId, string CargoId) : GameCommand(TargetEntityId);

/// <summary>Replace a fleet's standing (conditional) orders with the given list. The editor runs
/// client-side; this is the single authoritative write — the server rebuilds engine conditions and
/// actions from the <see cref="StandingOrderTypes"/> ids, rejecting unknown ones.</summary>
public sealed record SetStandingOrdersCommand(
    int TargetEntityId,
    IReadOnlyList<StandingOrder> Orders) : GameCommand(TargetEntityId);

#endregion

// ----- fleet movement/activity orders (commanded entity: the fleet) -----
#region Fleet Movement Commands

public sealed record MoveToBodyCommand(int TargetEntityId, int BodyId) : GameCommand(TargetEntityId);

/// <summary>Warp to the body and geo-survey it.</summary>
public sealed record GeoSurveyCommand(int TargetEntityId, int BodyId) : GameCommand(TargetEntityId);

/// <summary>Mine surveyed asteroids in the current system and haul the ore home. No body is named.</summary>
public sealed record MineAsteroidsCommand(int TargetEntityId) : GameCommand(TargetEntityId);

/// <summary>Warp to the location and gravitationally survey it for jump points.</summary>
public sealed record GravSurveyCommand(int TargetEntityId, int LocationId) : GameCommand(TargetEntityId);

/// <summary>Warp to the jump point and transit through it.</summary>
public sealed record JumpCommand(int TargetEntityId, int JumpPointId) : GameCommand(TargetEntityId);

/// <summary>
/// Refuel from this colony. A fleet warps there and fills its ships. A captained ship is given
/// the Refuel goal, and the captain plots the flight. An empty chair uses a load instead.
/// </summary>
public sealed record RefuelAtCommand(int TargetEntityId, int ColonyId) : GameCommand(TargetEntityId);

#endregion

// ----- research (commanded entity: the lab) -----
#region Faction Research Commands

public sealed record AssignScientistCommand(int TargetEntityId, int ScientistId) : GameCommand(TargetEntityId);

public sealed record UnassignScientistCommand(int TargetEntityId, int ScientistId) : GameCommand(TargetEntityId);

/// <summary>Set a lab's funding level (0–5; scales both output and cost).</summary>
public sealed record SetResearchFundingCommand(int TargetEntityId, int FundingLevel) : GameCommand(TargetEntityId);

public sealed record AddTechToQueueCommand(int TargetEntityId, string TechId) : GameCommand(TargetEntityId);

public sealed record RemoveTechFromQueueCommand(int TargetEntityId, string TechId) : GameCommand(TargetEntityId);

/// <summary>Move a queued tech one slot up (towards active) or down.</summary>
public sealed record MoveTechInQueueCommand(int TargetEntityId, string TechId, bool MoveUp) : GameCommand(TargetEntityId);

#endregion

// ----- installations/components (commanded entity: the colony or ship holding them) -----
#region Faction Installation / Component Commands

/// <summary>Uninstall one installed component of the given design and move it into cargo storage.</summary>
public sealed record UninstallComponentCommand(int TargetEntityId, string DesignId) : GameCommand(TargetEntityId);

/// <summary>Queue a mount of a crate in the commanded entity's cargo (<see cref="CargoItemView.Id"/>).
/// A colony mounts it on itself. A ship passes <paramref name="ColonyId"/> and mounts it on that colony.
/// Does not mount the part and does not remove it from cargo until the mount finishes.</summary>
public sealed record InstallComponentCommand(int TargetEntityId, int ComponentId, int? ColonyId = null) : GameCommand(TargetEntityId);

// ----- cargo transfer (commanded entity: the source, which must hold the items) -----

/// <summary>One line of a transfer: a cargo item (<see cref="CargoItemView.Id"/>) and how many
/// units to move from the commanded entity to the partner.</summary>
public sealed record CargoTransferItem(int CargoItemId, long Units);

/// <summary>Order a cargo transfer from the commanded entity to a partner. Both need cargo storage
/// and both must belong to the faction (the engine validates each side's order). The transfer
/// itself runs over time, rate- and range-limited by the engine.</summary>
public sealed record TransferCargoCommand(
    int TargetEntityId,
    int PartnerEntityId,
    IReadOnlyList<CargoTransferItem> Items) : GameCommand(TargetEntityId);

#endregion

// ----- order queue (commanded entity: the entity holding the order) -----
#region Entity Order Queue Commands

/// <summary>Set whether the simulation pauses when the given queued order actions
/// (<see cref="OrderSnapshot.OrderId"/>).</summary>
public sealed record SetOrderPauseCommand(int TargetEntityId, string OrderId, bool Pause) : GameCommand(TargetEntityId);

/// <summary>Remove one order (<see cref="OrderSnapshot.OrderId"/>). A running line is stopped.
/// A warp line also drops the circularise that was waiting behind it.</summary>
public sealed record CancelOrderCommand(int TargetEntityId, string OrderId) : GameCommand(TargetEntityId);

/// <summary>Stop now, or let the step that has already started finish and do not start the next.</summary>
public enum CancelTiming
{
    Now,
    WhenConvenient,
}

/// <summary>Cancel the commanded ship or fleet's current goal.</summary>
public sealed record CancelGoalCommand(int TargetEntityId, CancelTiming Timing) : GameCommand(TargetEntityId);

#endregion

#region Ship Commands

// ----- ship movement orders (commanded entity: the ship) -----

/// <summary>Queue a newtonian burn of <see cref="DeltaVMps"/> (orbit-relative: X = radial,
/// Y = prograde, Z = normal) centred on <see cref="NodeTime"/>. The server computes the burn
/// duration from the ship's thrust and fuel state.</summary>
public sealed record NewtonThrustCommand(int TargetEntityId, DateTime NodeTime, Vec3 DeltaVMps) : GameCommand(TargetEntityId);

/// <summary>Warp the ship to <see cref="DestinationId"/>. With an
/// <see cref="InsertionPointRelative"/> (metres, relative to the destination) the ship exits warp
/// there; otherwise the server picks a low circular orbit. Either way the server computes the
/// intercept and the post-warp orbit — the interactive orbit shaping in the warp window is
/// client-side preview only.</summary>
public sealed record WarpMoveCommand(
    int TargetEntityId,
    int DestinationId,
    Vec3? InsertionPointRelative = null) : GameCommand(TargetEntityId);

/// <summary>Empty chair: queue the same plot <c>MovePlanner</c> would build for this body.
/// No goal, no replan. A seated captain uses <see cref="MoveToBodyCommand"/> instead.</summary>
public sealed record GoToBodyCommand(int TargetEntityId, int BodyId) : GameCommand(TargetEntityId);

/// <summary>Empty chair: warp to the body with the planner drop-in and circularise.
/// Forces warp even when a newtonian transfer would also work.</summary>
public sealed record WarpToBodyCommand(int TargetEntityId, int BodyId) : GameCommand(TargetEntityId);

/// <summary>Empty chair: circularise around the current SOI parent at current radius.</summary>
public sealed record CirculariseCommand(int TargetEntityId) : GameCommand(TargetEntityId);

/// <summary>Empty chair: Hohmann to a circular orbit of <see cref="RadiusMetres"/>
/// around the current SOI parent. Circularise first if leftover is eccentric.</summary>
public sealed record ChangeAltitudeCommand(int TargetEntityId, double RadiusMetres) : GameCommand(TargetEntityId);

/// <summary>Empty chair: rendezvous onto <see cref="BodyId"/>'s orbit around the shared parent.</summary>
public sealed record MatchOrbitCommand(int TargetEntityId, int BodyId) : GameCommand(TargetEntityId);

// ----- fire control (commanded entity: the ship) -----

/// <summary>Replace a fire control's assigned weapon set (ids from <see cref="WeaponSnapshot.Id"/>).</summary>
public sealed record SetFireControlWeaponsCommand(
    int TargetEntityId,
    string FireControlId,
    IReadOnlyList<string> WeaponIds) : GameCommand(TargetEntityId);

/// <summary>Point a fire control at a target entity.</summary>
public sealed record SetFireControlTargetCommand(
    int TargetEntityId,
    string FireControlId,
    int TargetId) : GameCommand(TargetEntityId);

/// <summary>Load an ordnance design (<see cref="OrdnanceStoreItem.Id"/>) into a weapon.</summary>
public sealed record AssignOrdnanceCommand(
    int TargetEntityId,
    string WeaponId,
    string OrdnanceDesignId) : GameCommand(TargetEntityId);

/// <summary>Open or cease fire on a fire control's current target.</summary>
public sealed record SetFireModeCommand(
    int TargetEntityId,
    string FireControlId,
    bool OpenFire) : GameCommand(TargetEntityId);

#endregion

#region Faction Design Commands

// ----- component design (commanded entity: the faction itself) -----

/// <summary>Create (save) a component design. The interactive designer runs client-side; this is
/// the single authoritative write: the server replays <see cref="Inputs"/> onto a fresh designer
/// (validating template, bounds and formulas) and registers the result, then re-pushes the
/// faction's designs (and research — a new design adds a researchable tech).</summary>
public sealed record CreateComponentDesignCommand(
    int TargetEntityId,
    string TemplateId,
    string Name,
    IReadOnlyList<DesignerInput> Inputs) : GameCommand(TargetEntityId);

// ----- ship design (commanded entity: the faction itself) -----

/// <summary>One component stack in a ship design, in hull order (front to back).</summary>
public sealed record ShipComponentCount(string ComponentDesignId, int Count);

/// <summary>Create or update a ship design. The interactive designer (component layout, armor,
/// live stats) runs client-side; this is the single authoritative write: the server resolves the
/// referenced component/armor ids against the faction's own designs, recalculates the derived
/// values, computes validity, and registers the design. <see cref="DesignId"/> null/empty creates a
/// new design (server-generated id); otherwise the existing design is updated in place.</summary>
public sealed record SaveShipDesignCommand(
    int TargetEntityId,
    string? DesignId,
    string Name,
    IReadOnlyList<ShipComponentCount> Components,
    string ArmorId,
    float ArmorThickness,
    bool IsObsolete,
    bool Tanker = false) : GameCommand(TargetEntityId);

public sealed record DeleteShipDesignCommand(int TargetEntityId, string DesignId) : GameCommand(TargetEntityId);

/// <summary>Mark a ship design obsolete: it disappears from the designer's list and can no longer
/// be produced, but ships already built from it are unaffected.</summary>
public sealed record SetShipDesignObsoleteCommand(int TargetEntityId, string DesignId) : GameCommand(TargetEntityId);

#endregion

// ----- industry / local construction (commanded entity: the colony) -----
#region Colony Development Commands

/// <summary>Queue a batch job on one of the entity's production lines. When <see cref="AutoInstall"/>
/// is set, the design is a colony installation, and the colony has installation points, completion
/// queues a construction mount of that instance. It does not mount the part.</summary>
public sealed record QueueIndustryJobCommand(
    int TargetEntityId,
    string ProductionLineId,
    string DesignId,
    int Quantity,
    bool Repeat,
    bool AutoInstall) : GameCommand(TargetEntityId);

/// <summary>Move a queued industry job up (negative delta) or down within its production line.</summary>
public sealed record ChangeIndustryJobPriorityCommand(
    int TargetEntityId,
    string ProductionLineId,
    string JobId,
    int Delta) : GameCommand(TargetEntityId);

public sealed record CancelIndustryJobCommand(
    int TargetEntityId,
    string ProductionLineId,
    string JobId) : GameCommand(TargetEntityId);

/// <summary>Queue a site-only assemble, or a mount when <paramref name="ComponentId"/> names a stored crate.
/// A colony mounts on itself. A ship passes <paramref name="ColonyId"/> and mounts on that colony.</summary>
public sealed record AddToConstructionQueueCommand(int TargetEntityId, string DesignId, int? ComponentId = null, int? ColonyId = null) : GameCommand(TargetEntityId);

/// <summary>Queue a shipyard refit. The yard spends ship-refit points. The ship is one in transfer range.
/// Removed instances return to the warehouse when the job finishes. Mounted instances leave it.</summary>
public sealed record QueueShipRefitCommand(
    int TargetEntityId,
    string ProductionLineId,
    int ShipEntityId,
    IReadOnlyList<int> RemoveInstanceIds,
    IReadOnlyList<int> MountInstanceIds,
    /// <summary>When set, a finished refit whose parts match this design adopts it. Null keeps the old design.</summary>
    string? TargetDesignId = null) : GameCommand(TargetEntityId);

/// <summary>Move a job in the local-construction queue by its current position.</summary>
public sealed record MoveConstructionJobCommand(int TargetEntityId, int QueueIndex, bool MoveUp) : GameCommand(TargetEntityId);

public sealed record RemoveConstructionJobCommand(int TargetEntityId, int QueueIndex) : GameCommand(TargetEntityId);

#endregion
