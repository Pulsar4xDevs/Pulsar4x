namespace Pulsar4X.Api;

/// <summary>
/// Identifies an authenticated player and the faction they are bound to. Issued by the server on
/// <see cref="IGameServer.Connect"/> and presented on every subsequent request so the server can
/// scope reads and commands to what that faction is allowed to see and do.
/// </summary>
public readonly record struct PlayerSession(Guid SessionId, int FactionId)
{
    public static readonly PlayerSession None = default;
}

/// <summary>A request to join a running game and bind to a faction.</summary>
public sealed record ConnectRequest
{
    /// <summary>Well-known <see cref="Credential"/> that authorises binding to the all-seeing
    /// GameMaster faction. Placeholder until real auth lands with networking.</summary>
    public const string SpaceMasterCredential = "sm:in-process-host";

    /// <summary>Display name of the connecting player.</summary>
    public string PlayerName { get; init; } = "Player";

    /// <summary>
    /// Faction to bind to. Used by the in-process host (which already knows the player's faction) to
    /// scope the session. Null lets the server choose per policy. Network play will gate this behind
    /// <see cref="Credential"/>.
    /// </summary>
    public int? FactionId { get; init; }

    /// <summary>
    /// Optional credential for controlling/rejoining a specific faction (e.g. an SM password or a
    /// saved session token). Null requests a default/observer binding per server policy.
    /// </summary>
    public string? Credential { get; init; }

    /// <summary>
    /// Hash of the client's loaded mod set. The server rejects mismatches so both sides share an
    /// identical data definition (blueprints, components, etc.).
    /// </summary>
    public string? ModManifestHash { get; init; }
}

/// <summary>The outcome of a <see cref="ConnectRequest"/>.</summary>
public sealed record ConnectResult
{
    public bool Success { get; init; }
    public PlayerSession Session { get; init; }
    public string? FailureReason { get; init; }
    public GameInfo? Game { get; init; }

    public static ConnectResult Ok(PlayerSession session, GameInfo game)
        => new() { Success = true, Session = session, Game = game };

    public static ConnectResult Fail(string reason)
        => new() { Success = false, FailureReason = reason };
}

/// <summary>Static, faction-agnostic facts about the running game session.</summary>
public sealed record GameInfo(string Name, string GitHash)
{
    /// <summary>Movement-rule settings the movement-order UI adapts to (they change which
    /// maneuver inputs make sense, not what the player may do).</summary>
    public bool StrictNewtonian { get; init; }

    public bool UseRelativeVelocity { get; init; }
}
