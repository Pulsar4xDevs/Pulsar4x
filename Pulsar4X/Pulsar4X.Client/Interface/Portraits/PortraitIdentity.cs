using System.Numerics;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

public readonly record struct PortraitRef(int Seed, string Role, string Species = "human")
{
    public bool IsEmpty => PortraitIdentity.IsEmpty(Seed, Role, Species);
}

public static class PortraitIdentity
{
    public const string DefaultSpecies = "human";

    public static readonly Vector4 DefaultFactionColour = new(0.30f, 0.48f, 0.82f, 1f);

    /// <summary>
    /// A seed of 0, a blank role, or a blank species means the post is empty.
    /// <paramref name="species"/> is ignored when omitted so older two-argument checks still
    /// follow the seed and role only.
    /// </summary>
    public static bool IsEmpty(int seed, string? role, string? species = null)
        => seed == 0
            || string.IsNullOrEmpty(role)
            || (species != null && string.IsNullOrWhiteSpace(species));

    /// <summary>
    /// The view's <c>PortraitSpecies</c> (or prefixed post field). An empty string is an empty post.
    /// Unknown pack ids are left as written; the catalog falls back to human at pick time.
    /// </summary>
    public static string SpeciesFromView(string? species)
        => string.IsNullOrWhiteSpace(species) ? "" : species.Trim();

    public static string ResolveSpecies(string? species)
        => SpeciesFromView(species);

    static PortraitRef From(int seed, string? role, string? species)
        => new(seed, role ?? "", SpeciesFromView(species));

    public static PortraitRef Of(CommanderSnapshot person)
        => From(person.PortraitSeed, person.PortraitRole, person.PortraitSpecies);

    public static PortraitRef Of(CandidateView candidate)
        => From(candidate.PortraitSeed, candidate.PortraitRole, candidate.PortraitSpecies);

    public static PortraitRef Administrator(ColonyView view)
        => From(view.AdministratorPortraitSeed, view.AdministratorPortraitRole, view.AdministratorPortraitSpecies);

    public static PortraitRef IndustryHead(ColonyView view)
        => From(view.IndustryHeadPortraitSeed, view.IndustryHeadPortraitRole, view.IndustryHeadPortraitSpecies);

    public static PortraitRef IndustryHead(IndustryView view)
        => From(view.IndustryHeadPortraitSeed, view.IndustryHeadPortraitRole, view.IndustryHeadPortraitSpecies);

    public static PortraitRef Commander(ShipView view)
        => From(view.CommanderPortraitSeed, view.CommanderPortraitRole, view.CommanderPortraitSpecies);

    public static PortraitRef Commander(ShipSnapshot ship)
        => From(ship.CommanderPortraitSeed, ship.CommanderPortraitRole, ship.CommanderPortraitSpecies);

    public static PortraitRef Commander(FleetSnapshot fleet)
        => From(fleet.CommanderPortraitSeed, fleet.CommanderPortraitRole, fleet.CommanderPortraitSpecies);

    public static PortraitRef Scientist(ResearcherView lab)
        => From(lab.ScientistPortraitSeed, lab.ScientistPortraitRole, lab.ScientistPortraitSpecies);

    /// <summary>Only for a view that has no portrait field.</summary>
    public static PortraitRef Fallback(int entityId, CommanderKind kind)
        => From(entityId, RoleFromKind(kind), DefaultSpecies);

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
