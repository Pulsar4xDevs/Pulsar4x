using System.Numerics;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>A portrait seed and role taken from a person or post view.</summary>
public readonly record struct PortraitRef(int Seed, string Role)
{
    public bool IsEmpty => PortraitIdentity.IsEmpty(Seed, Role);
}

/// <summary>
/// Maps view portrait fields into a <see cref="PortraitRef"/>.
/// A seed of 0 or a role of "" means the post is empty.
/// Kind and entity-id fallbacks are only for a view that has no portrait field.
/// </summary>
public static class PortraitIdentity
{
    /// <summary>Fallback faction tint until the API exposes a faction colour.</summary>
    public static readonly Vector4 DefaultFactionColour = new(0.30f, 0.48f, 0.82f, 1f);

    public static bool IsEmpty(int seed, string? role)
        => seed == 0 || string.IsNullOrEmpty(role);

    public static PortraitRef Of(CommanderSnapshot person)
        => new(person.PortraitSeed, person.PortraitRole ?? "");

    public static PortraitRef Of(CandidateView candidate)
        => new(candidate.PortraitSeed, candidate.PortraitRole ?? "");

    public static PortraitRef Administrator(ColonyView view)
        => new(view.AdministratorPortraitSeed, view.AdministratorPortraitRole ?? "");

    public static PortraitRef IndustryHead(ColonyView view)
        => new(view.IndustryHeadPortraitSeed, view.IndustryHeadPortraitRole ?? "");

    public static PortraitRef IndustryHead(IndustryView view)
        => new(view.IndustryHeadPortraitSeed, view.IndustryHeadPortraitRole ?? "");

    public static PortraitRef Commander(ShipView view)
        => new(view.CommanderPortraitSeed, view.CommanderPortraitRole ?? "");

    public static PortraitRef Commander(ShipSnapshot ship)
        => new(ship.CommanderPortraitSeed, ship.CommanderPortraitRole ?? "");

    public static PortraitRef Commander(FleetSnapshot fleet)
        => new(fleet.CommanderPortraitSeed, fleet.CommanderPortraitRole ?? "");

    public static PortraitRef Scientist(ResearcherView lab)
        => new(lab.ScientistPortraitSeed, lab.ScientistPortraitRole ?? "");

    /// <summary>Only for a view that has no portrait field.</summary>
    public static PortraitRef Fallback(int entityId, CommanderKind kind)
        => new(entityId, RoleFromKind(kind));

    public static string RoleFromKind(CommanderKind kind) => kind switch
    {
        CommanderKind.Navy => PortraitRoles.Captain,
        CommanderKind.Ground => PortraitRoles.Captain,
        CommanderKind.Scientist => PortraitRoles.Scientist,
        _ => PortraitRoles.Civilian,
    };

    public static Vector4 FactionColour(GlobalUIState? uiState)
        => DefaultFactionColour;
}
