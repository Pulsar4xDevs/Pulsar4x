using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.Client;

/// <summary>Builds a <see cref="NewGameRequest"/> from a catalog's effective DefaultStart.</summary>
public static class NewGameRequests
{
    /// <summary>
    /// Species, colony, system, body, theme, funds, galaxy size and the end-of-life setting
    /// come from <paramref name="catalog"/>'s DefaultStart. The enabled systems are the
    /// catalog's included packs. Throws when DefaultStart is missing a value the new game needs.
    /// </summary>
    public static NewGameRequest FromDefaultStart(
        NewGameCatalog catalog,
        IReadOnlyList<string> modManifestPaths,
        string factionName,
        string factionAbbreviation,
        int masterSeed,
        string? scenarioManifestPath = null)
    {
        var start = catalog.DefaultStart;
        if (start == null
            || string.IsNullOrEmpty(start.SpeciesId)
            || string.IsNullOrEmpty(start.ColonyId)
            || string.IsNullOrEmpty(start.SystemId)
            || (string.IsNullOrEmpty(start.BodyId) && start.SystemId != "random")
            || start.StartingFunds == null
            || start.MaxSystems == null
            || start.EleStart == null)
        {
            throw new InvalidOperationException(
                "This scenario has no complete default start. It needs a species, a colony, a system, a body (unless the system is random), starting funds, a galaxy size, and an end-of-life setting.");
        }

        return new NewGameRequest(
            ModManifestPaths: modManifestPaths,
            FactionName: factionName,
            FactionAbbreviation: factionAbbreviation,
            SpeciesId: start.SpeciesId,
            ColonyId: start.ColonyId,
            SystemId: start.SystemId,
            BodyId: start.BodyId ?? "",
            EnabledSystems: catalog.IncludedSystemIds.ToList(),
            MaxSystems: start.MaxSystems.Value,
            MasterSeed: masterSeed,
            StartingFunds: start.StartingFunds.Value,
            EleStart: start.EleStart.Value,
            SMPassword: "",
            PlayerPassword: "",
            ScenarioId: catalog.ScenarioId,
            ScenarioManifestPath: scenarioManifestPath,
            ThemeId: string.IsNullOrEmpty(start.ThemeId) ? null : start.ThemeId);
    }
}
