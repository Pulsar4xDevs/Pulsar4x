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
    public void ShippedScenarios_MatchTheFilesTheirModsStillLoad_AndDefaultStartIdsExist()
    {
        ModsState.RefreshModsList("Data");

        foreach (var scenarioDir in new[] { "Data/scenarios/sol-default", "Data/scenarios/early-tech" })
        {
            var scenario = JsonConvert.DeserializeObject<ScenarioManifest>(
                File.ReadAllText(Path.Combine(scenarioDir, "scenarioInfo.json")));
            Assert.That(scenario, Is.Not.Null, scenarioDir);

            string prefix = "../scenarios/" + Path.GetFileName(scenarioDir) + "/";
            var fromScenario = scenario!.DataFiles.Select(f => prefix + f).ToList();
            var owner = ModsState.AvailableMods.Single(m => m.Mod.DataFiles.Any(f => f.StartsWith(prefix)));
            var fromMod = owner.Mod.DataFiles.Where(f => f.StartsWith(prefix)).ToList();
            Assert.That(fromScenario, Is.EqualTo(fromMod), scenarioDir);

            foreach (var file in scenario.DataFiles)
                Assert.That(File.Exists(Path.Combine(scenarioDir, file)), Is.True, file);

            var paths = scenario.RequiredMods
                .Select(ns => ModsState.AvailableMods.Single(m => m.Mod.Namespace == ns).Path)
                .ToList();
            var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path));

            Assert.That(store.Species, Does.ContainKey(scenario.DefaultStart.Species));
            Assert.That(store.Colonies, Does.ContainKey(scenario.DefaultStart.Colony));
            Assert.That(store.Systems, Does.ContainKey(scenario.DefaultStart.System));
        }
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
