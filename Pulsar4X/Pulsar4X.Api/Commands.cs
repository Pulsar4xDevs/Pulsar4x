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
// Example command. Establishes the pattern; the full ~45-command write surface is ported
// incrementally. Commands carry only the intent — the server supplies faction/timestamps.
// --------------------------------------------------------------------------------------------

public sealed record RenameCommand(int TargetEntityId, string NewName) : GameCommand(TargetEntityId);
