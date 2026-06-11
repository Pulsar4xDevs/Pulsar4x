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

// ----- fleet organisation (commanded entity: the faction for create, otherwise the fleet/ship) -----

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

public sealed record SetFlagshipCommand(int TargetEntityId, int ShipId) : GameCommand(TargetEntityId);

// ----- fleet movement/activity orders (commanded entity: the fleet) -----

public sealed record MoveToBodyCommand(int TargetEntityId, int BodyId) : GameCommand(TargetEntityId);

/// <summary>Warp to the body and geo-survey it.</summary>
public sealed record GeoSurveyCommand(int TargetEntityId, int BodyId) : GameCommand(TargetEntityId);

/// <summary>Warp to the location and gravitationally survey it for jump points.</summary>
public sealed record GravSurveyCommand(int TargetEntityId, int LocationId) : GameCommand(TargetEntityId);

/// <summary>Warp to the jump point and transit through it.</summary>
public sealed record JumpCommand(int TargetEntityId, int JumpPointId) : GameCommand(TargetEntityId);

/// <summary>Warp to the colony and refuel the fleet's ships from its stores.</summary>
public sealed record RefuelAtCommand(int TargetEntityId, int ColonyId) : GameCommand(TargetEntityId);

// ----- research (commanded entity: the lab) -----

public sealed record AssignScientistCommand(int TargetEntityId, int ScientistId) : GameCommand(TargetEntityId);

public sealed record UnassignScientistCommand(int TargetEntityId, int ScientistId) : GameCommand(TargetEntityId);

/// <summary>Set a lab's funding level (0–5; scales both output and cost).</summary>
public sealed record SetResearchFundingCommand(int TargetEntityId, int FundingLevel) : GameCommand(TargetEntityId);

public sealed record AddTechToQueueCommand(int TargetEntityId, string TechId) : GameCommand(TargetEntityId);

public sealed record RemoveTechFromQueueCommand(int TargetEntityId, string TechId) : GameCommand(TargetEntityId);

/// <summary>Move a queued tech one slot up (towards active) or down.</summary>
public sealed record MoveTechInQueueCommand(int TargetEntityId, string TechId, bool MoveUp) : GameCommand(TargetEntityId);

// ----- installations/components (commanded entity: the colony or ship holding them) -----

/// <summary>Uninstall one installed component of the given design and move it into cargo storage.</summary>
public sealed record UninstallComponentCommand(int TargetEntityId, string DesignId) : GameCommand(TargetEntityId);

/// <summary>Install a component instance (<see cref="CargoItemView.Id"/>) out of cargo storage.</summary>
public sealed record InstallComponentCommand(int TargetEntityId, int ComponentId) : GameCommand(TargetEntityId);

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

// ----- order queue (commanded entity: the entity holding the order) -----

/// <summary>Set whether the simulation pauses when the given queued order actions
/// (<see cref="OrderSnapshot.OrderId"/>).</summary>
public sealed record SetOrderPauseCommand(int TargetEntityId, string OrderId, bool Pause) : GameCommand(TargetEntityId);

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
    bool IsObsolete) : GameCommand(TargetEntityId);

public sealed record DeleteShipDesignCommand(int TargetEntityId, string DesignId) : GameCommand(TargetEntityId);

/// <summary>Mark a ship design obsolete: it disappears from the designer's list and can no longer
/// be produced, but ships already built from it are unaffected.</summary>
public sealed record SetShipDesignObsoleteCommand(int TargetEntityId, string DesignId) : GameCommand(TargetEntityId);

// ----- industry / local construction (commanded entity: the colony) -----

/// <summary>Queue a batch job on one of the entity's production lines. When <see cref="AutoInstall"/>
/// is set and the design is a colony installation, completed output installs on the entity itself.</summary>
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

public sealed record AddToConstructionQueueCommand(int TargetEntityId, string DesignId) : GameCommand(TargetEntityId);

/// <summary>Move a job in the local-construction queue by its current position.</summary>
public sealed record MoveConstructionJobCommand(int TargetEntityId, int QueueIndex, bool MoveUp) : GameCommand(TargetEntityId);

public sealed record RemoveConstructionJobCommand(int TargetEntityId, int QueueIndex) : GameCommand(TargetEntityId);
