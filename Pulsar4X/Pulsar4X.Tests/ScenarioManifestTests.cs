using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Modding;

namespace Pulsar4X.Tests;

[TestFixture]
public class ScenarioManifestTests
{
    [Test]
    public void ShippedScenarios_DataFilesExist_AndDefaultStartIdsAreInTheLoadedStore()
    {
        ModsState.RefreshModsList("Data");

        foreach (var scenarioDir in new[] { "Data/scenarios/warp-tech-start", "Data/scenarios/early-tech" })
        {
            var scenario = JsonConvert.DeserializeObject<ScenarioManifest>(
                File.ReadAllText(Path.Combine(scenarioDir, "scenarioInfo.json")));
            Assert.That(scenario, Is.Not.Null, scenarioDir);

            foreach (var file in scenario!.DataFiles)
                Assert.That(File.Exists(Path.Combine(scenarioDir, file)), Is.True, file);

            var paths = scenario.RequiredMods
                .Select(ns => ModsState.AvailableMods.Single(m => m.Mod.Namespace == ns).Path)
                .ToList();
            var store = GameLifecycle.LoadModStore(
                paths, ModsState.AvailableMods.Select(m => m.Path), scenario.Namespace);

            Assert.That(store.Species, Does.ContainKey(scenario.DefaultStart.Species), scenarioDir);
            Assert.That(store.Colonies, Does.ContainKey(scenario.DefaultStart.Colony), scenarioDir);
            Assert.That(store.Systems, Does.ContainKey(scenario.DefaultStart.System), scenarioDir);
            Assert.That(store.SystemBodies, Does.ContainKey(scenario.DefaultStart.Body), scenarioDir);
        }
    }

    [Test]
    public void ModManifests_DoNotListScenarioFiles()
    {
        ModsState.RefreshModsList("Data");

        foreach (var mod in ModsState.AvailableMods)
        {
            if (mod.Mod.DataFiles == null)
                continue;

            foreach (var file in mod.Mod.DataFiles)
            {
                Assert.That(file, Does.Not.Contain("../scenarios"), mod.Mod.ModName + " " + file);
                Assert.That(file, Does.Not.StartWith("scenarios/"), mod.Mod.ModName + " " + file);
            }
        }
    }

    [Test]
    public void BothStarts_CatalogHasSolEarthAndTheStartColony()
    {
        ModsState.RefreshModsList("Data");
        var available = ModsState.AvailableMods.Select(m => m.Path).ToList();

        var defaultPaths = ModsState.AvailableMods
            .Where(m => m.Mod.ModName == "Pulsar4x")
            .Select(m => m.Path)
            .ToList();
        var defaultCatalog = GameLifecycle.BuildCatalog(GameLifecycle.LoadModStore(defaultPaths, available));
        var defaultSol = defaultCatalog.Systems.Single(system => system.Id == "system-sol");
        Assert.That(defaultSol.StartingBodies.Select(body => body.Id), Does.Contain("planet-earth"));
        Assert.That(defaultCatalog.Colonies.Select(colony => colony.Id), Does.Contain("colony-earth"));

        var earlyCatalog = GameLifecycle.BuildCatalog(
            GameLifecycle.LoadModStore(defaultPaths, available, "Pulsar4x.Scenario.EarlyTech"));
        var earlySol = earlyCatalog.Systems.Single(system => system.Id == "system-sol");
        Assert.That(earlySol.StartingBodies.Select(body => body.Id), Does.Contain("planet-earth"));
        Assert.That(earlyCatalog.Colonies.Select(colony => colony.Id), Does.Contain("colony-early-earth"));
        Assert.That(earlyCatalog.Colonies.Select(colony => colony.Id), Does.Not.Contain("colony-earth"));
    }

    [Test]
    public void LoadScenario_OnTopOfEarlyTechStore_ReappliesTheSameData()
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods
            .Where(m => m.Mod.ModName == "Pulsar4x")
            .Select(m => m.Path)
            .ToList();
        var store = GameLifecycle.LoadModStore(
            paths, ModsState.AvailableMods.Select(m => m.Path), "Pulsar4x.Scenario.EarlyTech");

        int manifestCount = store.ModManifests.Count;
        var colonyKeys = store.Colonies.Keys.ToList();
        var factionKeys = store.Factions.Keys.ToList();

        var loader = new ModLoader();
        loader.LoadScenario("Data/scenarios/early-tech/scenarioInfo.json", store);

        Assert.That(store.ModManifests, Has.Count.EqualTo(manifestCount));
        Assert.That(store.Colonies.Keys, Is.EquivalentTo(colonyKeys));
        Assert.That(store.Factions.Keys, Is.EquivalentTo(factionKeys));
    }

    [Test]
    public void LoadScenario_MissingRequiredMod_ThrowsAndLeavesColoniesUnchanged()
    {
        var root = Path.Combine(Path.GetTempPath(), "p4x-load-scenario-missing-mod-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, "scenarioInfo.json"),
                "{\"ScenarioName\":\"Needs Mod\",\"Namespace\":\"needs-missing-mod\",\"RequiredMods\":[\"not-installed-mod\"],\"DataFiles\":[]}");

            ModsState.RefreshModsList("Data");
            var paths = ModsState.AvailableMods
                .Where(m => m.Mod.ModName == "Pulsar4x")
                .Select(m => m.Path)
                .ToList();
            var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path));
            var colonyKeys = store.Colonies.Keys.ToList();

            var loader = new ModLoader();
            var ex = Assert.Throws<ModLoadException>(
                () => loader.LoadScenario(Path.Combine(root, "scenarioInfo.json"), store));

            Assert.That(ex!.Message, Does.Contain("not-installed-mod"));
            Assert.That(store.Colonies.Keys, Is.EquivalentTo(colonyKeys));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void WarpTechStartDesigns_AreOnTheScenario_NotTheBaseMod()
    {
        const string componentId = "default-design-alcubierre-2k";
        const string shipId = "default-ship-design-starship";
        const string warpNs = "Pulsar4x.Scenario.WarpTechStart";
        const string earlyNs = "Pulsar4x.Scenario.EarlyTech";

        var baseStore = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", baseStore);
        Assert.That(baseStore.ComponentDesigns, Is.Empty);
        Assert.That(baseStore.ShipDesigns, Is.Empty);
        Assert.That(baseStore.ComponentDesigns.ContainsKey(componentId), Is.False);
        Assert.That(baseStore.ShipDesigns.ContainsKey(shipId), Is.False);

        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods
            .Where(m => m.Mod.ModName == "Pulsar4x")
            .Select(m => m.Path)
            .ToList();
        var available = ModsState.AvailableMods.Select(m => m.Path).ToList();

        var warpStore = GameLifecycle.LoadModStore(paths, available, warpNs);
        Assert.That(warpStore.ComponentDesigns, Is.Not.Empty);
        Assert.That(warpStore.ShipDesigns, Is.Not.Empty);
        Assert.That(warpStore.ComponentDesigns, Does.ContainKey(componentId));
        Assert.That(warpStore.ShipDesigns, Does.ContainKey(shipId));
        Assert.That(warpStore.ComponentDesigns[componentId].ModNamespace, Is.EqualTo(warpNs));
        Assert.That(warpStore.ShipDesigns[shipId].ModNamespace, Is.EqualTo(warpNs));

        var earlyStore = GameLifecycle.LoadModStore(paths, available, earlyNs);
        Assert.That(earlyStore.ComponentDesigns, Is.Not.Empty);
        Assert.That(earlyStore.ShipDesigns, Is.Not.Empty);
        Assert.That(earlyStore.ComponentDesigns, Does.ContainKey(componentId));
        Assert.That(earlyStore.ShipDesigns, Does.ContainKey(shipId));
        Assert.That(earlyStore.ComponentDesigns[componentId].ModNamespace, Is.EqualTo(warpNs));
        Assert.That(earlyStore.ShipDesigns[shipId].ModNamespace, Is.EqualTo(warpNs));
    }
}
