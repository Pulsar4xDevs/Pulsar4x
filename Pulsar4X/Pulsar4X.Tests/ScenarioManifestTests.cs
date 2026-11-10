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

        foreach (var scenarioDir in new[] { "Data/scenarios/sol-default", "Data/scenarios/early-tech" })
        {
            var scenario = JsonConvert.DeserializeObject<ScenarioManifest>(
                File.ReadAllText(Path.Combine(scenarioDir, "scenarioInfo.json")));
            Assert.That(scenario, Is.Not.Null, scenarioDir);

            foreach (var file in scenario!.DataFiles)
                Assert.That(File.Exists(Path.Combine(scenarioDir, file)), Is.True, file);

            var paths = scenario.RequiredMods
                .Select(ns => ModsState.AvailableMods.Single(m => m.Mod.Namespace == ns).Path)
                .ToList();
            var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path));

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

        var earlyPaths = ModsState.AvailableMods
            .Where(m => m.Mod.ModName == "Early Tech Start")
            .Select(m => m.Path)
            .ToList();
        var earlyCatalog = GameLifecycle.BuildCatalog(GameLifecycle.LoadModStore(earlyPaths, available));
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
            .Where(m => m.Mod.ModName == "Early Tech Start")
            .Select(m => m.Path)
            .ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path));

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
    public void LoadScenario_EarlyTechWithoutItsMod_ThrowsAndLeavesColoniesUnchanged()
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods
            .Where(m => m.Mod.ModName == "Pulsar4x")
            .Select(m => m.Path)
            .ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path));
        var colonyKeys = store.Colonies.Keys.ToList();

        var loader = new ModLoader();
        var ex = Assert.Throws<System.InvalidOperationException>(
            () => loader.LoadScenario("Data/scenarios/early-tech/scenarioInfo.json", store));

        Assert.That(ex!.Message, Does.Contain("Pulsar4x.EarlyTechStart"));
        Assert.That(store.Colonies.Keys, Is.EquivalentTo(colonyKeys));
    }
}
