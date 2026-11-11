using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Modding;

namespace Pulsar4X.Tests;

[TestFixture]
public class SystemPackTests
{
    private const string BaseMod = "Pulsar4x";
    private const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";
    private const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";
    private const string SolPack = "Pulsar4x.System.Sol";
    private const string AlphaPack = "Pulsar4x.System.AlphaCentauri";
    private const string AlphaSystem = "system-alpha-centauri";

    private static readonly string[] PackOrder = { AlphaPack, SolPack };

    private static List<string> TickedPaths(params string[] names)
        => names.Select(n => ModsState.AvailableMods.Single(m => m.Mod.ModName == n).Path).ToList();

    [Test]
    public void DiscoverSystems_FindsSolAndAlphaCentauri()
    {
        ModsState.RefreshModsList("Data");
        var available = ModsState.AvailableMods.Select(m => m.Path).ToList();
        var expected = new[] { SolPack, AlphaPack };

        var fromRoots = new ModLoader().DiscoverSystems(GameLifecycle.SystemRoots(available));
        var fromData = new ModLoader().DiscoverSystems(new[] { "Data/systems" });

        Assert.That(fromRoots.Keys, Is.EquivalentTo(expected));
        Assert.That(fromData.Keys, Is.EquivalentTo(expected));
    }

    [Test]
    public void DiscoveredPacks_ContainNoColonyOrFactionRecords()
    {
        var packs = new ModLoader().DiscoverSystems(new[] { "Data/systems" });

        foreach (var pack in packs.Values)
        {
            Assert.That(pack.DataFiles, Is.Not.Null, pack.Namespace);
            foreach (var file in pack.DataFiles)
            {
                var items = JArray.Parse(File.ReadAllText(Path.Combine(pack.SystemDirectory, file)));
                foreach (var item in items)
                {
                    string? type = item?["Type"]?.Value<string>();
                    Assert.That(type, Is.Not.EqualTo("Colony"), pack.Namespace + " " + file);
                    Assert.That(type, Is.Not.EqualTo("Faction"), pack.Namespace + " " + file);
                }
            }
        }
    }

    [Test]
    public void BaseModAlone_HasNoSystemsAndNoStars()
    {
        var loader = new ModLoader();
        var store = new ModDataStore();
        loader.LoadModManifest("Data/basemod/modInfo.json", store);

        Assert.That(store.Systems, Is.Empty);
        Assert.That(store.Stars, Is.Empty);

        var manifest = JsonConvert.DeserializeObject<ModManifest>(File.ReadAllText("Data/basemod/modInfo.json"));
        Assert.That(manifest, Is.Not.Null);
        foreach (var file in manifest!.DataFiles)
            Assert.That(file, Does.Not.Contain("systems/"));
    }

    [Test]
    public void LoadModStore_WarpTechStart_LoadsPacksThenScenarioOwnedData()
    {
        ModsState.RefreshModsList("Data");
        var available = ModsState.AvailableMods.Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(TickedPaths(BaseMod), available, WarpTechStart);

        Assert.That(store.LoadedSystems, Is.EqualTo(PackOrder));
        Assert.That(store.Systems, Does.ContainKey("system-sol"));
        Assert.That(store.Systems, Does.ContainKey(AlphaSystem));
        Assert.That(store.Systems["system-sol"].ModNamespace, Is.EqualTo(SolPack));
        Assert.That(store.Systems[AlphaSystem].ModNamespace, Is.EqualTo(AlphaPack));
        Assert.That(store.SystemBodies, Has.Count.EqualTo(534));
        Assert.That(store.SystemBodies, Does.ContainKey("planet-earth"));
        Assert.That(store.Colonies.Keys, Is.EquivalentTo(new[]
        {
            "colony-earth", "colony-luna-concord", "colony-mars-outpost", "colony-ceres-depot"
        }));
        Assert.That(store.Factions.Keys, Is.EquivalentTo(new[]
        {
            "faction-strata-survey", "faction-lode-mining"
        }));

        foreach (var colony in store.Colonies.Values)
            Assert.That(colony.ModNamespace, Is.EqualTo(WarpTechStart), colony.UniqueID);
        foreach (var faction in store.Factions.Values)
            Assert.That(faction.ModNamespace, Is.EqualTo(WarpTechStart), faction.UniqueID);
        foreach (var body in store.SystemBodies.Values)
            Assert.That(body.ModNamespace, Is.EqualTo(SolPack), body.UniqueID);
        Assert.That(store.Stars["sol"].ModNamespace, Is.EqualTo(SolPack));
        Assert.That(store.Stars["alpha-centauri-a"].ModNamespace, Is.EqualTo(AlphaPack));
        Assert.That(store.Stars["alpha-centauri-b"].ModNamespace, Is.EqualTo(AlphaPack));
    }

    [Test]
    public void LoadScenario_WarpTechStart_DefaultRootDiscoversTheSamePacks()
    {
        ModsState.RefreshModsList("Data");
        var available = ModsState.AvailableMods.Select(m => m.Path).ToList();
        var viaStore = GameLifecycle.LoadModStore(TickedPaths(BaseMod), available, WarpTechStart);

        var loader = new ModLoader();
        var direct = new ModDataStore();
        loader.LoadModManifest("Data/basemod/modInfo.json", direct);
        loader.LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", direct);

        Assert.That(direct.LoadedSystems, Is.EqualTo(PackOrder));
        Assert.That(direct.LoadedSystems, Is.EqualTo(viaStore.LoadedSystems));
        Assert.That(direct.Systems, Does.ContainKey("system-sol"));
        Assert.That(direct.Systems, Does.ContainKey(AlphaSystem));
        Assert.That(direct.Systems.Keys, Is.EquivalentTo(viaStore.Systems.Keys));
    }

    [Test]
    public void LoadModStore_EarlyTech_LoadsEachPackOnce()
    {
        ModsState.RefreshModsList("Data");
        var available = ModsState.AvailableMods.Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(TickedPaths(BaseMod), available, EarlyTech);

        Assert.That(store.LoadedSystems, Is.EqualTo(PackOrder));
        Assert.That(store.Systems, Does.ContainKey("system-sol"));
        Assert.That(store.SystemBodies, Does.ContainKey("planet-earth"));
        Assert.That(store.Colonies, Does.ContainKey("colony-early-earth"));
        Assert.That(store.Colonies, Does.Not.ContainKey("colony-earth"));
    }

    [Test]
    public void ApplyScenario_MissingSystemPack_ThrowsAndAppliesNothing()
    {
        var root = Path.Combine(Path.GetTempPath(), "p4x-system-missing-" + Guid.NewGuid().ToString("N"));
        var scenarioDir = Path.Combine(root, "missing-pack");
        Directory.CreateDirectory(scenarioDir);
        try
        {
            const string scenarioNs = "test-missing-pack";
            File.WriteAllText(Path.Combine(scenarioDir, "scenarioInfo.json"),
                "{\"ScenarioName\":\"Missing Pack\",\"Namespace\":\"" + scenarioNs + "\",\"RequiredMods\":[\"Pulsar4x\"],\"IncludedSystems\":[\"does-not-exist\"],\"DataFiles\":[\"harmless.json\"]}");
            File.WriteAllText(Path.Combine(scenarioDir, "harmless.json"),
                "[{\"Type\":\"Colony\",\"Payload\":{\"UniqueID\":\"colony-should-not-load\",\"Name\":\"Nope\"}},{\"Type\":\"System\",\"Payload\":{\"UniqueID\":\"system-should-not-load\",\"Name\":\"Nope\"}}]");
            string manifestPath = Path.Combine(scenarioDir, "scenarioInfo.json");

            var loader = new ModLoader();
            var store = new ModDataStore();
            loader.LoadModManifest("Data/basemod/modInfo.json", store);
            int colonies = store.Colonies.Count;
            int systems = store.Systems.Count;
            var scenarios = loader.DiscoverScenarios(new[] { "Data/scenarios" });
            var packs = loader.DiscoverSystems(new[] { "Data/systems" });

            var ex = Assert.Throws<ModLoadException>(() =>
                loader.ApplyScenario(null, manifestPath, scenarios, store, packs));

            Assert.That(store.Colonies, Has.Count.EqualTo(colonies));
            Assert.That(store.Systems, Has.Count.EqualTo(systems));
            AssertIncludedSystemMissing(ex, scenarioNs);

            ModsState.RefreshModsList("Data");
            var loadEx = Assert.Throws<ModLoadException>(() =>
                GameLifecycle.LoadModStore(
                    new[] { "Data/basemod/modInfo.json" },
                    ModsState.AvailableMods.Select(m => m.Path),
                    scenarioManifestPath: manifestPath));
            AssertIncludedSystemMissing(loadEx, scenarioNs);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ApplyScenario_SharedPack_LoadsSolOnce()
    {
        var root = Path.Combine(Path.GetTempPath(), "p4x-system-shared-" + Guid.NewGuid().ToString("N"));
        var scenarioDir = Path.Combine(root, "shared-sol");
        Directory.CreateDirectory(scenarioDir);
        try
        {
            File.WriteAllText(Path.Combine(scenarioDir, "scenarioInfo.json"),
                "{\"ScenarioName\":\"Shared Sol\",\"Namespace\":\"test-shared-sol\",\"RequiredScenarios\":[\"" + WarpTechStart + "\"],\"IncludedSystems\":[\"" + SolPack + "\"],\"DataFiles\":[]}");

            var loader = new ModLoader();
            var store = new ModDataStore();
            loader.LoadModManifest("Data/basemod/modInfo.json", store);
            var scenarios = loader.DiscoverScenarios(new[] { "Data/scenarios" });
            var systems = loader.DiscoverSystems(new[] { "Data/systems" });

            loader.ApplyScenario(null, Path.Combine(scenarioDir, "scenarioInfo.json"), scenarios, store, systems);

            Assert.That(store.LoadedSystems, Is.EqualTo(PackOrder));
            Assert.That(store.LoadedSystems.Count(ns => ns == SolPack), Is.EqualTo(1));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void SystemManifest_RoundTrip_OmitsSystemDirectory()
    {
        var loaded = new ModLoader().DiscoverSystems(new[] { "Data/systems" })[SolPack];
        loaded.SystemDirectory = Path.GetFullPath("Data/systems/sol");

        var json = JsonConvert.SerializeObject(loaded, SystemManifest.SerializerSettings);
        Assert.That(json, Does.Not.Contain("SystemDirectory"));

        var round = JsonConvert.DeserializeObject<SystemManifest>(json, SystemManifest.SerializerSettings);
        Assert.That(round, Is.Not.Null);
        Assert.That(round!.Namespace, Is.EqualTo(loaded.Namespace));
        Assert.That(round.Name, Is.EqualTo(loaded.Name));
        Assert.That(round.Author, Is.EqualTo(loaded.Author));
        Assert.That(round.Version, Is.EqualTo(loaded.Version));
        Assert.That(round.Description, Is.EqualTo(loaded.Description));
        Assert.That(round.DataFiles, Is.EqualTo(loaded.DataFiles));
        Assert.That(round.SystemDirectory, Is.Null.Or.Empty);
    }

    private static void AssertIncludedSystemMissing(ModLoadException? ex, string scenarioNs)
    {
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Errors, Has.Count.EqualTo(1));
        var error = ex.Errors[0];
        Assert.That(error.ModNamespace, Is.EqualTo(scenarioNs));
        Assert.That(error.FileName, Is.EqualTo("scenarioInfo.json"));
        Assert.That(error.ObjectType, Is.EqualTo("ScenarioManifest"));
        Assert.That(error.ObjectId, Is.EqualTo(scenarioNs));
        Assert.That(error.Field, Is.EqualTo("IncludedSystems"));
        Assert.That(error.MissingId, Is.EqualTo("does-not-exist"));
    }
}
