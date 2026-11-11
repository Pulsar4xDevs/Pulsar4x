using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Factions;
using Pulsar4X.Modding;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

/// <summary>
/// The new-game wizard's scenario page: which required mods are not installed, which mods a
/// scenario requires, how picking one ticks those mods, and that an explicit scenario id is
/// what gets loaded.
/// </summary>
[TestFixture]
public class ScenarioWizardTests
{
    private const string BaseMod = "Pulsar4x";
    private const string EarlyMod = "Early Tech Start";
    private const string SolDefault = "Pulsar4x.Scenario.SolDefault";
    private const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";
    private const string EarlyModNamespace = "Pulsar4x.EarlyTechStart";

    private static ModsState.ModMetaData Mod(string name)
        => ModsState.AvailableMods.Single(m => m.Mod.ModName == name);

    private static List<string> TickedPaths(params string[] names)
        => names.Select(n => Mod(n).Path).ToList();

    private static Dictionary<string, ScenarioOption> DiscoverScenarios()
    {
        ModsState.RefreshModsList("Data");
        var roots = GameLifecycle.ScenarioRoots(ModsState.AvailableMods.Select(m => m.Path));
        var found = new ModLoader().DiscoverScenarios(roots);
        var options = new Dictionary<string, ScenarioOption>();
        foreach (var scenario in found.Values)
        {
            options[scenario.Namespace] = new ScenarioOption(
                scenario.Namespace,
                scenario.ScenarioName,
                scenario.Description,
                scenario.DefaultEnabled,
                scenario.RequiredMods ?? new List<string>(),
                Path.Combine(scenario.ScenarioDirectory, "scenarioInfo.json"));
        }
        return options;
    }

    private static List<ModOption> InstalledMods()
    {
        return ModsState.AvailableMods.Select(m => new ModOption(
            m.Mod.ModName,
            m.Mod.Namespace,
            m.Mod.Version,
            m.ManifestHash,
            m.Path,
            m.Mod.DefaultEnabled)).ToList();
    }

    [Test]
    public void MissingMods_EarlyTech_ListsModsThatAreOffOrNotInstalled()
    {
        var early = DiscoverScenarios()[EarlyTech];

        var onlyBase = new Dictionary<string, bool>
        {
            [BaseMod] = true,
            [EarlyModNamespace] = false,
        };
        Assert.That(ScenarioCompatibility.MissingMods(early, onlyBase),
            Is.EqualTo(new[] { EarlyModNamespace }));

        var both = new Dictionary<string, bool>
        {
            [BaseMod] = true,
            [EarlyModNamespace] = true,
        };
        Assert.That(ScenarioCompatibility.MissingMods(early, both), Is.Empty);

        var notInstalled = new Dictionary<string, bool>
        {
            [BaseMod] = true,
        };
        Assert.That(ScenarioCompatibility.MissingMods(early, notInstalled),
            Is.EqualTo(new[] { EarlyModNamespace }));
    }

    [Test]
    public void ApplySelection_EarlyTechThenSolDefault_RestoresTheEarlyModToItsDefault()
    {
        var scenarios = DiscoverScenarios();
        var mods = InstalledMods();
        var enabled = new Dictionary<string, bool>();
        foreach (var mod in mods)
            enabled[mod.Namespace] = mod.EnabledByDefault;

        var sol = scenarios[SolDefault];
        var early = scenarios[EarlyTech];
        ScenarioCompatibility.ApplySelection(sol, early, enabled, mods);
        Assert.That(enabled[BaseMod], Is.True);
        Assert.That(enabled[EarlyModNamespace], Is.True);

        ScenarioCompatibility.ApplySelection(early, sol, enabled, mods);
        var earlyMod = mods.Single(m => m.Namespace == EarlyModNamespace);
        Assert.That(earlyMod.EnabledByDefault, Is.False);
        Assert.That(enabled[EarlyModNamespace], Is.EqualTo(earlyMod.EnabledByDefault));
        Assert.That(enabled[BaseMod], Is.True);
    }

    [Test]
    public void NotInstalledMods_EarlyTech_EmptyWhenTheEarlyModIsInstalledButOff()
    {
        var early = DiscoverScenarios()[EarlyTech];
        var mods = InstalledMods();
        var earlyMod = mods.Single(m => m.Namespace == EarlyModNamespace);
        Assert.That(earlyMod.EnabledByDefault, Is.False);
        Assert.That(ScenarioCompatibility.NotInstalledMods(early, mods), Is.Empty);

        var withoutEarly = mods.Where(m => m.Namespace != EarlyModNamespace).ToList();
        Assert.That(ScenarioCompatibility.NotInstalledMods(early, withoutEarly),
            Is.EqualTo(new[] { EarlyModNamespace }));
    }

    [Test]
    public void IsRequired_EarlyTechRequiresBoth_SolDefaultDoesNotRequireTheEarlyMod()
    {
        var scenarios = DiscoverScenarios();
        var early = scenarios[EarlyTech];
        var sol = scenarios[SolDefault];

        Assert.That(ScenarioCompatibility.IsRequired(early, BaseMod), Is.True);
        Assert.That(ScenarioCompatibility.IsRequired(early, EarlyModNamespace), Is.True);
        Assert.That(ScenarioCompatibility.IsRequired(sol, EarlyModNamespace), Is.False);
        Assert.That(ScenarioCompatibility.IsRequired(null, BaseMod), Is.False);
        Assert.That(ScenarioCompatibility.IsRequired(null, EarlyModNamespace), Is.False);
    }

    [Test]
    public void ApplySelection_SelectingEarlyTechFromDefault_NotInstalledAndMissingAreEmpty()
    {
        var scenarios = DiscoverScenarios();
        var mods = InstalledMods();
        var enabled = new Dictionary<string, bool>();
        foreach (var mod in mods)
            enabled[mod.Namespace] = mod.EnabledByDefault;

        var sol = scenarios[SolDefault];
        var early = scenarios[EarlyTech];
        Assert.That(enabled[EarlyModNamespace], Is.False);

        ScenarioCompatibility.ApplySelection(sol, early, enabled, mods);
        Assert.That(enabled[EarlyModNamespace], Is.True);
        Assert.That(ScenarioCompatibility.NotInstalledMods(early, mods), Is.Empty);
        Assert.That(ScenarioCompatibility.MissingMods(early, enabled), Is.Empty);
    }

    [Test]
    public void RefreshModsList_EnableStateIsKeyedByNamespace()
    {
        ModsState.RefreshModsList("Data");

        Assert.That(ModsState.IsModEnabled.ContainsKey(BaseMod), Is.True);
        Assert.That(ModsState.IsModEnabled.ContainsKey(EarlyModNamespace), Is.True);
        Assert.That(ModsState.IsModEnabled.ContainsKey(EarlyMod), Is.False);
        Assert.That(ModsState.IsModEnabled[BaseMod], Is.True);
        Assert.That(ModsState.IsModEnabled[EarlyModNamespace], Is.False);

        foreach (var mod in ModsState.AvailableMods)
        {
            Assert.That(ModsState.IsModEnabled.ContainsKey(mod.Mod.Namespace), Is.True, mod.Mod.Namespace);
            Assert.That(ModsState.IsModEnabled[mod.Mod.Namespace], Is.EqualTo(mod.Mod.DefaultEnabled), mod.Mod.Namespace);
            if (mod.Mod.ModName != mod.Mod.Namespace)
                Assert.That(ModsState.IsModEnabled.ContainsKey(mod.Mod.ModName), Is.False, mod.Mod.ModName);
        }
    }

    [Test]
    public void ExplicitSolDefault_HasFourColonyBlueprintsAndTheSolStartIds()
    {
        ModsState.RefreshModsList("Data");
        var available = ModsState.AvailableMods.Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(TickedPaths(BaseMod), available, SolDefault);

        Assert.That(store.Colonies, Has.Count.EqualTo(4));
        Assert.That(store.Colonies.Keys, Is.EquivalentTo(new[]
        {
            "colony-earth", "colony-luna-concord", "colony-mars-outpost", "colony-ceres-depot"
        }));
        Assert.That(store.SystemBodies, Does.ContainKey("planet-earth"));
        Assert.That(store.SystemBodies, Has.Count.EqualTo(534));

        var catalog = GameLifecycle.BuildCatalog(store);
        var systems = catalog.Systems.Where(s => s.StartingBodies.Count > 0).ToList();
        var request = new NewGameRequest(
            ModManifestPaths: TickedPaths(BaseMod),
            FactionName: "United Earth Corp",
            FactionAbbreviation: "UEC",
            SpeciesId: catalog.DefaultStart!.SpeciesId,
            ColonyId: catalog.DefaultStart.ColonyId,
            SystemId: catalog.DefaultStart.SystemId,
            BodyId: catalog.DefaultStart.BodyId,
            EnabledSystems: systems.Select(s => s.Id).ToList(),
            MaxSystems: 10,
            MasterSeed: 12345678,
            StartingFunds: 100_000_000,
            EleStart: true,
            SMPassword: "",
            PlayerPassword: "",
            ScenarioId: SolDefault);
        Assert.That(request.SpeciesId, Is.EqualTo("species-human"));
        Assert.That(request.ColonyId, Is.EqualTo("colony-earth"));
        Assert.That(request.SystemId, Is.EqualTo("system-sol"));
        Assert.That(request.BodyId, Is.EqualTo("planet-earth"));

        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null);

        var playerColonies = result!.Value.game.Systems
            .SelectMany(system => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .Where(c => c.FactionOwnerID == result.Value.faction.Id)
            .ToList();
        Assert.That(playerColonies, Has.Count.EqualTo(2));
        var names = playerColonies.Select(c => c.GetDataBlob<NameDB>().GetName(result.Value.faction.Id)).ToList();
        Assert.That(names.Any(n => n.Contains("Earth")), Is.True);
        Assert.That(names.Any(n => n.Contains("Mars")), Is.True);
    }

    [Test]
    public void ExplicitEarlyTech_HasOneEarthHqAndTwoShipDesigns()
    {
        ModsState.RefreshModsList("Data");
        var available = ModsState.AvailableMods.Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(TickedPaths(BaseMod, EarlyMod), available, EarlyTech);
        Assert.That(store.Systems.ContainsKey("system-sol"), Is.True);

        var catalog = GameLifecycle.BuildCatalog(store);
        var systems = catalog.Systems.Where(s => s.StartingBodies.Count > 0).ToList();
        var request = new NewGameRequest(
            ModManifestPaths: TickedPaths(BaseMod, EarlyMod),
            FactionName: "United Earth Corp",
            FactionAbbreviation: "UEC",
            SpeciesId: catalog.DefaultStart!.SpeciesId,
            ColonyId: catalog.DefaultStart.ColonyId,
            SystemId: catalog.DefaultStart.SystemId,
            BodyId: catalog.DefaultStart.BodyId,
            EnabledSystems: systems.Select(s => s.Id).ToList(),
            MaxSystems: 10,
            MasterSeed: 12345678,
            StartingFunds: 100_000_000,
            EleStart: true,
            SMPassword: "",
            PlayerPassword: "",
            ScenarioId: EarlyTech);
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null);

        var colonies = result!.Value.game.Systems
            .SelectMany(system => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .ToList();
        Assert.That(colonies, Has.Count.EqualTo(1));
        Assert.That(colonies[0].FactionOwnerID, Is.EqualTo(result.Value.faction.Id));
        Assert.That(colonies[0].GetDataBlob<NameDB>().GetName(result.Value.faction.Id),
            Is.EqualTo("Earth HQ"));

        var info = result.Value.faction.GetDataBlob<FactionInfoDB>();
        Assert.That(info.ShipDesigns.Keys,
            Is.EquivalentTo(new[] { "early-ship-design-starship", "early-ship-design-newton-tug" }));
        Assert.That(result.Value.game.Systems.Select(s => s.ID), Does.Contain("system-sol"));
    }
}
