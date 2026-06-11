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
