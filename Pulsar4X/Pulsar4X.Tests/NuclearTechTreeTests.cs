using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Modding;

namespace Pulsar4X.Tests;

/// <summary>
/// The nuclear tech branch: Nuclear Theory opens Nuclear Power and Nuclear Engines; the nuclear engine techs sit behind Nuclear
/// Engines; the ion drive is not part of the nuclear branch. A tech's prerequisites are the techs whose Unlocks list it.
/// </summary>
[TestFixture]
public class NuclearTechTreeTests
{
    static ModDataStore Store()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        return store;
    }

    static List<string> Parents(ModDataStore store, string id)
        => store.Techs.Values.Where(t => t.Unlocks.Values.Any(ids => ids.Contains(id))).Select(t => t.UniqueID).ToList();

    static List<string> Chain(ModDataStore store, string id)
    {
        var chain = new List<string> { id };
        while (Parents(store, chain.Last()).FirstOrDefault(p => !chain.Contains(p)) is { } parent)
            chain.Add(parent);
        return chain;
    }

    [Test]
    public void NuclearTheory_IsARootTech_WithAModestCost()
    {
        var store = Store();
        var theory = store.Techs["tech-nuclear-theory"];
        Assert.That(theory.Name, Is.EqualTo("Nuclear Theory"));
        Assert.That(theory.MaxLevel, Is.EqualTo(1));
        Assert.That(Parents(store, "tech-nuclear-theory"), Is.Empty, "a root: each scenario lists it as open at the start");
        Assert.That(theory.CostFormula, Is.EqualTo("(1 + [Level]) * 1000"));
        Assert.That(store.Techs["tech-early-nuclear-power"].CostFormula, Is.EqualTo("(1 + [Level]) * 2000"), "Nuclear Power keeps its cost");
    }

    [Test]
    public void NuclearPower_AndNuclearEngines_SitBehindNuclearTheory()
    {
        var store = Store();
        Assert.That(Parents(store, "tech-early-nuclear-power"), Is.EqualTo(new[] { "tech-nuclear-theory" }));
        Assert.That(Parents(store, "tech-nuclear-engines"), Is.EqualTo(new[] { "tech-nuclear-theory" }));
        Assert.That(store.Techs["tech-nuclear-engines"].Name, Is.EqualTo("Nuclear Engines"));
        Assert.That(store.Techs["tech-nuclear-engines"].CostFormula, Is.EqualTo("(1 + [Level]) * 1500"));
    }

    [Test]
    public void TheNuclearEngineTechs_SitBehindNuclearEngines_NotConventionalEngine()
    {
        var store = Store();
        Assert.That(Parents(store, "tech-nuclear-solid-engine"), Is.EqualTo(new[] { "tech-nuclear-engines" }));
        Assert.That(Parents(store, "tech-nuclear-pulse-engine"), Is.EqualTo(new[] { "tech-nuclear-solid-engine" }));
        Assert.That(Chain(store, "tech-nuclear-pulse-engine"),
            Is.EqualTo(new[] { "tech-nuclear-pulse-engine", "tech-nuclear-solid-engine", "tech-nuclear-engines", "tech-nuclear-theory" }
                .Concat(Chain(store, "tech-nuclear-theory").Skip(1))));
        Assert.That(store.Techs["tech-conventional-engine"].Unlocks.Values.SelectMany(l => l), Does.Not.Contain("tech-nuclear-solid-engine"));
    }

    [Test]
    public void TheIonDrive_SitsUnderElectricThrusters_NotUnderTheNuclearBranch()
    {
        var store = Store();
        var thrusters = store.Techs["tech-electric-thrusters"];
        Assert.That(thrusters.Name, Is.EqualTo("Electric Thrusters"));
        Assert.That(thrusters.MaxLevel, Is.EqualTo(1));
        Assert.That(thrusters.Category, Is.EqualTo("tech-category-power-propulsion"));
        Assert.That(thrusters.CostFormula, Is.EqualTo("(1 + [Level]) * 2000"));
        Assert.That(store.Techs.Keys, Has.None.Contain("plasma"), "no plasma thrusters yet");
        Assert.That(Parents(store, "tech-ion-engine"), Is.EqualTo(new[] { "tech-electric-thrusters" }));
        Assert.That(Parents(store, "tech-electric-thrusters"), Is.EqualTo(new[] { "tech-conductors" }));
        Assert.That(Chain(store, "tech-ion-engine"),
            Is.EqualTo(new[] { "tech-ion-engine", "tech-electric-thrusters", "tech-conductors" }), "Conductors is a root");
        Assert.That(store.Techs["tech-conventional-engine"].Unlocks.Values.SelectMany(l => l), Does.Not.Contain("tech-ion-engine"));
        Assert.That(Chain(store, "tech-ion-engine"), Has.None.StartsWith("tech-nuclear-"));
        Assert.That(Chain(store, "tech-ion-engine"), Has.None.EqualTo("tech-early-nuclear-power"));
        Assert.That(store.Techs["tech-nuclear-pulse-engine"].Unlocks.Values.SelectMany(l => l), Is.Empty);
    }

    [Test]
    public void TheGateNodes_AreGone_AndEachStartListsTheNuclearTheoryRoot()
    {
        var store = Store();
        Assert.That(store.Techs.Keys, Does.Not.Contain("tech-modern-technology"));
        Assert.That(store.Techs.Keys, Does.Not.Contain("tech-early-technology"));
        foreach (var scenario in new[] { "warp-tech-start", "early-tech" })
        {
            var colonies = Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText($"Data/scenarios/{scenario}/colonies.json"));
            foreach (var colony in colonies.Select(c => c["Payload"] ?? c))
            {
                var items = ((colony["StartingItems"] ?? new Newtonsoft.Json.Linq.JArray()).Concat(colony["StartingResearchable"] ?? new Newtonsoft.Json.Linq.JArray()))
                    .Select(t => (string?)t).Where(t => t != null && t.StartsWith("tech-")).ToList();
                Assert.That(items, Is.All.Not.EqualTo("tech-modern-technology").And.Not.EqualTo("tech-early-technology"), scenario);
                foreach (var id in items)
                    Assert.That(store.Techs.ContainsKey(id), Is.True, id);
                // Nuclear Power is researched only where Nuclear Theory is too.
                if (items.Contains("tech-early-nuclear-power"))
                    Assert.That(items, Does.Contain("tech-nuclear-theory"), scenario);
            }
        }
    }

    [Test]
    public void TheWarheads_SitBehindHighExplosives()
    {
        var store = Store();
        Assert.That(Parents(store, "tech-high-explosives-warheads"), Is.EqualTo(new[] { "tech-high-explosives" }));
        Assert.That(Parents(store, "tech-fragmentation-warheads"), Is.EqualTo(new[] { "tech-high-explosives" }));
    }

    [Test]
    public void EveryTech_IsStillReachable_FromAStartingTech()
    {
        var store = Store();
        // The roots are the techs nothing unlocks; a scenario opens them (StartingResearchable) or researches them (StartingItems).
        var unlocked = store.Techs.Values.SelectMany(t => t.Unlocks.Values.SelectMany(l => l)).ToHashSet();
        var reached = store.Techs.Keys.Where(id => !unlocked.Contains(id)).ToHashSet();
        var queue = new Queue<string>(reached);
        while (queue.Count > 0)
            foreach (var id in store.Techs[queue.Dequeue()].Unlocks.Values.SelectMany(l => l).Where(i => store.Techs.ContainsKey(i)))
                if (reached.Add(id))
                    queue.Enqueue(id);
        foreach (var id in new[] { "tech-nuclear-theory", "tech-early-nuclear-power", "tech-nuclear-engines", "tech-nuclear-solid-engine",
                     "tech-nuclear-pulse-engine", "tech-electric-thrusters", "tech-ion-engine", "tech-fission-power-plant", "tech-early-warp-drive" })
            Assert.That(reached, Does.Contain(id), id);
    }
}
