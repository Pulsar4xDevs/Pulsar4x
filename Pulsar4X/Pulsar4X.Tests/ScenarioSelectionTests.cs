using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Modding;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

[TestFixture]
public class ScenarioSelectionTests
{
    private const string BaseMod = "Pulsar4x";
    private const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";
    private const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";

    private static ModsState.ModMetaData Mod(string name)
        => ModsState.AvailableMods.Single(m => m.Mod.ModName == name);

    private static List<string> TickedPaths(params string[] names)
        => names.Select(n => Mod(n).Path).ToList();

    [Test]
    public void DiscoverScenarios_DataScenarios_FindsWarpTechStartAndEarlyTech()
    {
        ModsState.RefreshModsList("Data");
        var discovered = new ModLoader().DiscoverScenarios(new[] { "Data/scenarios" });

        Assert.That(discovered.Keys, Is.EquivalentTo(new[] { WarpTechStart, EarlyTech }));
        Assert.That(discovered[WarpTechStart].RequiredMods, Is.EqualTo(new[] { BaseMod }));
        Assert.That(discovered[EarlyTech].RequiredMods, Is.EqualTo(new[] { BaseMod }));
        Assert.That(discovered[EarlyTech].RequiredScenarios, Does.Contain(WarpTechStart));
    }

    [Test]
    public void DefaultChoice_Pulsar4xOnly_IsWarpTechStart()
    {
        ModsState.RefreshModsList("Data");
        var store = GameLifecycle.LoadModStore(TickedPaths(BaseMod), ModsState.AvailableMods.Select(m => m.Path));

        Assert.That(store.ScenarioNamespace, Is.EqualTo(WarpTechStart));
        Assert.That(store.Colonies, Does.ContainKey("colony-earth"));
        Assert.That(store.Colonies, Does.Not.ContainKey("colony-early-earth"));
    }

    [Test]
    public void LoadModStore_ExplicitWarpTechStart_HasFourColoniesAndEarthAmongTheBodies()
    {
        ModsState.RefreshModsList("Data");
        var available = ModsState.AvailableMods.Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(TickedPaths(BaseMod), available, WarpTechStart);

        var baseline = new ModDataStore();
        var loader = new ModLoader();
        loader.LoadModManifest("Data/basemod/modInfo.json", baseline);
        loader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", baseline);

        Assert.That(store.Colonies, Has.Count.EqualTo(4));
        Assert.That(store.Colonies.Keys, Is.EquivalentTo(new[]
        {
            "colony-earth", "colony-luna-concord", "colony-mars-outpost", "colony-ceres-depot"
        }));
        Assert.That(store.SystemBodies, Does.ContainKey("planet-earth"));
        Assert.That(store.SystemBodies, Has.Count.EqualTo(534));
        Assert.That(store.SystemBodies.Count, Is.EqualTo(baseline.SystemBodies.Count));
        Assert.That(store.Colonies.Count, Is.EqualTo(baseline.Colonies.Count));

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
            ScenarioId: WarpTechStart);
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
    public void LoadModStore_ExplicitEarlyTech_HasOnlyTheEarthHqAndTwoShipDesigns()
    {
        ModsState.RefreshModsList("Data");
        var store = GameLifecycle.LoadModStore(
            TickedPaths(BaseMod), ModsState.AvailableMods.Select(m => m.Path), EarlyTech);
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
            ScenarioId: EarlyTech);
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null);

        var colonies = result!.Value.game.Systems
            .SelectMany(system => system.GetAllEntitiesWithDataBlob<ColonyInfoDB>())
            .ToList();
        Assert.That(colonies, Has.Count.EqualTo(1));
        Assert.That(colonies[0].GetDataBlob<NameDB>().GetName(result.Value.faction.Id),
            Does.Contain("Earth"));

        var info = result.Value.faction.GetDataBlob<FactionInfoDB>();
        Assert.That(info.ShipDesigns.Keys,
            Is.EquivalentTo(new[] { "early-ship-design-starship", "early-ship-design-newton-tug" }));
    }

    [Test]
    public void ApplyScenario_MissingRequiredMod_ThrowsAndAppliesNothing()
    {
        var root = Path.Combine(Path.GetTempPath(), "p4x-scenario-missing-mod-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "needs-mod"));
        try
        {
            const string scenarioNs = "needs-missing-mod";
            File.WriteAllText(Path.Combine(root, "needs-mod", "scenarioInfo.json"),
                "{\"ScenarioName\":\"Needs Mod\",\"Namespace\":\"" + scenarioNs + "\",\"RequiredMods\":[\"not-installed-mod\"],\"DataFiles\":[]}");

            var loader = new ModLoader();
            var store = new ModDataStore();
            loader.LoadModManifest("Data/basemod/modInfo.json", store);
            var discovered = loader.DiscoverScenarios(new[] { root });
            int colonies = store.Colonies.Count;
            int systems = store.Systems.Count;

            var ex = Assert.Throws<ModLoadException>(
                () => loader.ApplyScenario(scenarioNs, null, discovered, store));

            Assert.That(store.Colonies, Has.Count.EqualTo(colonies));
            Assert.That(store.Systems, Has.Count.EqualTo(systems));
            Assert.That(ex!.Errors, Has.Count.EqualTo(1));
            var error = ex.Errors[0];
            Assert.That(error.ModNamespace, Is.EqualTo(scenarioNs));
            Assert.That(error.FileName, Is.EqualTo("scenarioInfo.json"));
            Assert.That(error.ObjectType, Is.EqualTo("ScenarioManifest"));
            Assert.That(error.ObjectId, Is.EqualTo(scenarioNs));
            Assert.That(error.Field, Is.EqualTo("RequiredMods"));
            Assert.That(error.MissingId, Is.EqualTo("not-installed-mod"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ApplyScenario_UnknownId_ThrowsModLoadException()
    {
        var loader = new ModLoader();
        var store = new ModDataStore();
        loader.LoadModManifest("Data/basemod/modInfo.json", store);
        var discovered = loader.DiscoverScenarios(new[] { "Data/scenarios" });

        var ex = Assert.Throws<ModLoadException>(
            () => loader.ApplyScenario("not-a-scenario", null, discovered, store));

        var error = ex!.Errors[0];
        Assert.That(error.ObjectType, Is.EqualTo("ScenarioManifest"));
        Assert.That(error.ObjectId, Is.EqualTo("not-a-scenario"));
        Assert.That(error.MissingId, Is.EqualTo("not-a-scenario"));
        Assert.That(error.Field, Is.EqualTo("ScenarioId"));
        Assert.That(store.Colonies, Is.Empty);
    }

    [Test]
    public void ApplyScenario_RequiredScenariosCycle_ThrowsModLoadException()
    {
        var root = Path.Combine(Path.GetTempPath(), "p4x-scenario-cycle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "a"));
        Directory.CreateDirectory(Path.Combine(root, "b"));
        try
        {
            File.WriteAllText(Path.Combine(root, "a", "scenarioInfo.json"),
                "{\"ScenarioName\":\"A\",\"Namespace\":\"cycle-a\",\"RequiredScenarios\":[\"cycle-b\"],\"DataFiles\":[]}");
            File.WriteAllText(Path.Combine(root, "b", "scenarioInfo.json"),
                "{\"ScenarioName\":\"B\",\"Namespace\":\"cycle-b\",\"RequiredScenarios\":[\"cycle-a\"],\"DataFiles\":[]}");

            var loader = new ModLoader();
            var store = new ModDataStore();
            var discovered = loader.DiscoverScenarios(new[] { root });

            var ex = Assert.Throws<ModLoadException>(
                () => loader.ApplyScenario("cycle-a", null, discovered, store));

            Assert.That(ex!.Errors.Any(e => e.Field == "RequiredScenarios"), Is.True);
            Assert.That(store.Colonies, Is.Empty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ApplyScenario_MissingRequiredScenario_ThrowsModLoadException()
    {
        var root = Path.Combine(Path.GetTempPath(), "p4x-scenario-missing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "orphan"));
        try
        {
            File.WriteAllText(Path.Combine(root, "orphan", "scenarioInfo.json"),
                "{\"ScenarioName\":\"Orphan\",\"Namespace\":\"orphan-scenario\",\"RequiredScenarios\":[\"does-not-exist\"],\"DataFiles\":[]}");

            var loader = new ModLoader();
            var store = new ModDataStore();
            var discovered = loader.DiscoverScenarios(new[] { root });

            var ex = Assert.Throws<ModLoadException>(
                () => loader.ApplyScenario("orphan-scenario", null, discovered, store));

            Assert.That(ex!.Errors, Has.Count.EqualTo(1));
            var error = ex.Errors[0];
            Assert.That(error.ModNamespace, Is.EqualTo("orphan-scenario"));
            Assert.That(error.FileName, Is.EqualTo("scenarioInfo.json"));
            Assert.That(error.ObjectType, Is.EqualTo("ScenarioManifest"));
            Assert.That(error.ObjectId, Is.EqualTo("orphan-scenario"));
            Assert.That(error.Field, Is.EqualTo("RequiredScenarios"));
            Assert.That(error.MissingId, Is.EqualTo("does-not-exist"));
            Assert.That(store.Colonies, Is.Empty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ScenarioManifest_RoundTrip_OmitsScenarioDirectoryAndKeepsFields()
    {
        var loaded = new ModLoader().DiscoverScenarios(new[] { "Data/scenarios" })[EarlyTech];
        loaded.ScenarioDirectory = Path.GetFullPath("Data/scenarios/early-tech");

        var json = JsonConvert.SerializeObject(loaded, ScenarioManifest.SerializerSettings);
        Assert.That(json, Does.Not.Contain("ScenarioDirectory"));

        var round = JsonConvert.DeserializeObject<ScenarioManifest>(json, ScenarioManifest.SerializerSettings);
        Assert.That(round, Is.Not.Null);
        Assert.That(round!.Namespace, Is.EqualTo(loaded.Namespace));
        Assert.That(round.ScenarioName, Is.EqualTo(loaded.ScenarioName));
        Assert.That(round.Author, Is.EqualTo(loaded.Author));
        Assert.That(round.Version, Is.EqualTo(loaded.Version));
        Assert.That(round.Description, Is.EqualTo(loaded.Description));
        Assert.That(round.DefaultEnabled, Is.EqualTo(loaded.DefaultEnabled));
        Assert.That(round.RequiredMods, Is.EqualTo(loaded.RequiredMods));
        Assert.That(round.RequiredScenarios, Is.EqualTo(loaded.RequiredScenarios));
        Assert.That(round.DataFiles, Is.EqualTo(loaded.DataFiles));
        Assert.That(round.DefaultStart.Species, Is.EqualTo(loaded.DefaultStart.Species));
        Assert.That(round.DefaultStart.Colony, Is.EqualTo(loaded.DefaultStart.Colony));
        Assert.That(round.DefaultStart.System, Is.EqualTo(loaded.DefaultStart.System));
        Assert.That(round.DefaultStart.Body, Is.EqualTo(loaded.DefaultStart.Body));
        Assert.That(round.ScenarioDirectory, Is.Null.Or.Empty);
    }

    [Test]
    public void BuildQuickstartRequest_DefaultEnabledMods_UsesWarpTechStartStart()
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.DefaultEnabled).Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path));
        var catalog = GameLifecycle.BuildCatalog(store);

        var request = GameLifecycle.BuildQuickstartRequest(paths, catalog, 1);
        Assert.That(request, Is.Not.Null);
        Assert.That(request.ScenarioId, Is.EqualTo(WarpTechStart));
        Assert.That(request.SpeciesId, Is.EqualTo("species-human"));
        Assert.That(request.ColonyId, Is.EqualTo("colony-earth"));
        Assert.That(request.SystemId, Is.EqualTo("system-sol"));
        Assert.That(request.BodyId, Is.EqualTo("planet-earth"));
    }

    [Test]
    public void BuildQuickstartRequest_EmptyCatalog_ThrowsNoPlayableSpecies()
    {
        var catalog = new NewGameCatalog(
            new List<CatalogOption>(),
            new List<CatalogOption>(),
            new List<CatalogOption>(),
            new List<CatalogSystemOption>());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            GameLifecycle.BuildQuickstartRequest(new List<string>(), catalog, 1));

        Assert.That(ex!.Message, Does.Contain("playable species"));
    }

    [Test]
    public void BuildQuickstartRequest_SpeciesButNoColonies_Throws()
    {
        var catalog = new NewGameCatalog(
            new List<CatalogOption> { new CatalogOption("species-human", "Human") },
            new List<CatalogOption>(),
            new List<CatalogOption>(),
            new List<CatalogSystemOption>
            {
                new CatalogSystemOption("system-sol", "Sol", new List<CatalogOption>
                {
                    new CatalogOption("planet-earth", "Earth")
                })
            });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            GameLifecycle.BuildQuickstartRequest(new List<string>(), catalog, 1));

        Assert.That(ex!.Message, Does.Contain("no colonies"));
    }

    [Test]
    public void BuildQuickstartRequest_NoStartableSystems_Throws()
    {
        var catalog = new NewGameCatalog(
            new List<CatalogOption> { new CatalogOption("species-human", "Human") },
            new List<CatalogOption>(),
            new List<CatalogOption> { new CatalogOption("colony-earth", "Earth") },
            new List<CatalogSystemOption>
            {
                new CatalogSystemOption("system-sol", "Sol", new List<CatalogOption>())
            });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            GameLifecycle.BuildQuickstartRequest(new List<string>(), catalog, 1));

        Assert.That(ex!.Message, Does.Contain("startable systems"));
    }
}
