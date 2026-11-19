using System.Numerics;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// Client adapter for portrait seed and role until the engine exposes PortraitSeed / PortraitRole.
/// When those fields land, change the bodies of <see cref="Seed(CommanderSnapshot)"/>,
/// <see cref="Seed(CandidateView)"/> and <see cref="Role(CommanderSnapshot, string?)"/> — those
/// three methods are the swap points.
/// </summary>
public static class PortraitIdentity
{
    public const string Captain = "captain";
    public const string Administrator = "administrator";
    public const string IndustryHead = "industry-head";
    public const string Scientist = "scientist";
    public const string LabTechnician = "lab-technician";
    public const string Civilian = "civilian";
    public const string Candidate = "candidate";

    /// <summary>Fallback faction tint until the API exposes a faction colour.</summary>
    public static readonly Vector4 DefaultFactionColour = new(0.30f, 0.48f, 0.82f, 1f);

    // --- swap to person.PortraitSeed when the engine field is on the snapshot ---
    public static int Seed(CommanderSnapshot person) => person.Id;

    // --- swap to candidate.PortraitSeed when the engine field is on the snapshot ---
    public static int Seed(CandidateView candidate) => candidate.Id;

    public static int Seed(int entityId) => entityId;

    /// <summary>
    /// <paramref name="contextRole"/> is the post being shown (captain, industry-head, …).
    /// When it is null, the role is inferred from <see cref="CommanderSnapshot.Kind"/>.
    /// Swap the first line to person.PortraitRole when that field exists, then fall back as now.
    /// </summary>
    public static string Role(CommanderSnapshot person, string? contextRole = null)
        => contextRole ?? RoleFromKind(person.Kind);

    public static string Role(CandidateView candidate) => Candidate;

    public static string RoleFromKind(CommanderKind kind) => kind switch
    {
        CommanderKind.Navy => Captain,
        CommanderKind.Scientist => Scientist,
        CommanderKind.Civilian => Civilian,
        _ => Civilian,
    };

    public static Vector4 FactionColour(GlobalUIState? uiState)
        => DefaultFactionColour;
}
