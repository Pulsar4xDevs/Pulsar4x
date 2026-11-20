using System.Numerics;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

public readonly record struct PortraitRef(int Seed, string Role, string Species = "human")
{
    public bool IsEmpty => PortraitIdentity.IsEmpty(Seed, Role);
}

public static class PortraitIdentity
{
    public const string DefaultSpecies = "human";

    public static readonly Vector4 DefaultFactionColour = new(0.30f, 0.48f, 0.82f, 1f);

    public static bool IsEmpty(int seed, string? role)
        => seed == 0 || string.IsNullOrEmpty(role);

    /// <summary>
    /// Temporary species for every portrait until engine PortraitSpecies fields land.
    /// Change only this method when those fields are merged.
    /// </summary>
    public static string SpeciesFromView() => DefaultSpecies;

    public static string ResolveSpecies(string? species)
        => string.IsNullOrWhiteSpace(species) ? SpeciesFromView() : species.Trim();

    static PortraitRef From(int seed, string? role, string? species = null)
        => new(seed, role ?? "", ResolveSpecies(species));

    public static PortraitRef Of(CommanderSnapshot person)
        => From(person.PortraitSeed, person.PortraitRole);

    public static PortraitRef Of(CandidateView candidate)
        => From(candidate.PortraitSeed, candidate.PortraitRole);

    public static PortraitRef Administrator(ColonyView view)
        => From(view.AdministratorPortraitSeed, view.AdministratorPortraitRole);

    public static PortraitRef IndustryHead(ColonyView view)
        => From(view.IndustryHeadPortraitSeed, view.IndustryHeadPortraitRole);

    public static PortraitRef IndustryHead(IndustryView view)
        => From(view.IndustryHeadPortraitSeed, view.IndustryHeadPortraitRole);

    public static PortraitRef Commander(ShipView view)
        => From(view.CommanderPortraitSeed, view.CommanderPortraitRole);

    public static PortraitRef Commander(ShipSnapshot ship)
        => From(ship.CommanderPortraitSeed, ship.CommanderPortraitRole);

    public static PortraitRef Commander(FleetSnapshot fleet)
        => From(fleet.CommanderPortraitSeed, fleet.CommanderPortraitRole);

    public static PortraitRef Scientist(ResearcherView lab)
        => From(lab.ScientistPortraitSeed, lab.ScientistPortraitRole);

    /// <summary>Only for a view that has no portrait field.</summary>
    public static PortraitRef Fallback(int entityId, CommanderKind kind)
        => From(entityId, RoleFromKind(kind));

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
