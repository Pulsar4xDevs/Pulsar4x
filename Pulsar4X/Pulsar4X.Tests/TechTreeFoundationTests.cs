using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pulsar4X.Modding;

namespace Pulsar4X.Tests;

/// <summary>
/// The tech tree's shape (tech-tree-proposal Phase 1, data only): seven foundation roots, everything else under one of them,
/// level-gated edges for depth, warp deep and jump shallow. A tech's prerequisites are the techs whose Unlocks list it;
/// they are OR. Column 1 is a root; a tech's column is the longest chain of parents above it plus one.
/// </summary>
[TestFixture]
public class TechTreeFoundationTests
{
    static readonly string[] Foundations =
    {
        "tech-conventional-engine", "tech-electrical-engineering", "tech-industry-basics",
        "tech-life-sciences", "tech-nuclear-theory", "tech-electromagnetic-sensors", "tech-gravitic-theory",
    };
    // Weapons techs wait on the damage model rework and are not re-parented.
    static readonly string[] WeaponRoots = { "tech-high-explosives", "tech-fission-warheads", "tech-triggers", "tech-guidance" };

    static ModDataStore Store()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        return store;
    }

    /// <summary>Every edge parent -> (child, unlock level).</summary>
    static List<(string Parent, string Child, int Level)> Edges(ModDataStore store)
        => store.Techs.Values
            .SelectMany(t => t.Unlocks.SelectMany(kv => kv.Value.Where(store.Techs.ContainsKey).Select(c => (t.UniqueID, c, kv.Key))))
            .ToList();

    static Dictionary<string, int> Columns(ModDataStore store)
    {
        var edges = Edges(store);
        var column = new Dictionary<string, int>();
        int Col(string id, HashSet<string> path)
        {
            if (column.TryGetValue(id, out var known)) return known;
            Assert.That(path.Add(id), Is.True, "cycle through " + id);
            int c = 1 + edges.Where(e => e.Child == id).Select(e => Col(e.Parent, path)).DefaultIfEmpty(0).Max();
            path.Remove(id);
            return column[id] = c;
        }
        foreach (var id in store.Techs.Keys) Col(id, new HashSet<string>());
        return column;
    }

    static List<(string Parent, int Level)> Parents(ModDataStore store, string id)
        => Edges(store).Where(e => e.Child == id).Select(e => (e.Parent, e.Level)).ToList();

    [Test]
    public void TheRoots_AreTheSevenFoundations_AndTheDeferredWeaponTechs()
    {
        var store = Store();
        var children = Edges(store).Select(e => e.Child).ToHashSet();
        var roots = store.Techs.Keys.Where(id => !children.Contains(id)).OrderBy(id => id).ToList();
        Assert.That(roots, Is.EqualTo(Foundations.Concat(WeaponRoots).OrderBy(id => id)));

        Assert.That(store.Techs["tech-conventional-engine"].Name, Is.EqualTo("Chemical Rocketry"));
        Assert.That(store.Techs["tech-electrical-engineering"].Name, Is.EqualTo("Electrical Engineering"));
        Assert.That(store.Techs["tech-industry-basics"].Name, Is.EqualTo("Industrial Base"));
        Assert.That(store.Techs["tech-life-sciences"].Name, Is.EqualTo("Life Sciences"));
        Assert.That(store.Techs["tech-nuclear-theory"].Name, Is.EqualTo("Nuclear Theory"));
        Assert.That(store.Techs["tech-electromagnetic-sensors"].Name, Is.EqualTo("Electromagnetic Sensors"));
        Assert.That(store.Techs.Keys, Has.None.Contain("remote-sensing"));
        var gravitic = store.Techs["tech-gravitic-theory"];
        Assert.That(gravitic.Name, Is.EqualTo("Gravitic Theory"));
        Assert.That(gravitic.MaxLevel, Is.EqualTo(1));
        Assert.That(gravitic.CostFormula, Is.EqualTo("(1 + [Level]) * 500"));
        Assert.That(gravitic.Unlocks.Values.SelectMany(l => l), Is.EquivalentTo(new[] { "tech-gravitational-surveyor", "tech-spacetime-metric-theory" }));
        foreach (var id in new[] { "tech-electrical-engineering", "tech-industry-basics", "tech-life-sciences", "tech-electromagnetic-sensors" })
        {
            Assert.That(store.Techs[id].MaxLevel, Is.EqualTo(1), id);
            Assert.That(store.Techs[id].CostFormula, Is.EqualTo("(1 + [Level]) * 500"), id);
        }
        Assert.That(store.Techs["tech-electromagnetic-sensors"].Category, Is.EqualTo("tech-category-sensors"));
    }

    [Test]
    public void TheTree_HasNoCycle_AndEveryTechHangsUnderAFoundationOrAWeaponRoot()
    {
        var store = Store();
        var columns = Columns(store); // asserts there is no cycle
        var edges = Edges(store);
        var reached = new HashSet<string>(Foundations.Concat(WeaponRoots));
        var queue = new Queue<string>(reached);
        while (queue.Count > 0)
        {
            var parent = queue.Dequeue();
            foreach (var e in edges.Where(e => e.Parent == parent))
                if (reached.Add(e.Child)) queue.Enqueue(e.Child);
        }
        Assert.That(store.Techs.Keys.Except(reached), Is.Empty);
        Assert.That(columns.Values.Max(), Is.LessThanOrEqualTo(8), "the proposal's acceptance bound");
        Assert.That(columns.Values.Max(), Is.GreaterThanOrEqualTo(6), "the warp line reaches column 6");
    }

    [Test]
    public void EachBranch_HoldsItsTechs()
    {
        var store = Store();
        void Under(string foundation, params string[] ids)
        {
            foreach (var id in ids)
                Assert.That(Parents(store, id).Select(p => p.Parent), Is.EqualTo(new[] { foundation }), id);
        }
        Under("tech-conventional-engine", "tech-conventional-engine-max-power", "tech-conventional-engine-min-power",
            "tech-conventional-engine-max-size", "tech-conventional-engine-min-size", "tech-conventional-engine-exhaust-velocity",
            "tech-conventional-engine-fuel-consumption", "tech-solid-fuel");
        Under("tech-electrical-engineering", "tech-battery-capacity", "tech-capacitors", "tech-conductors", "tech-fuel-cells",
            "tech-grid-distribution", "tech-panel-efficiency", "tech-panel-bandwidth", "tech-panel-density", "tech-thermal-efficiency");
        Under("tech-industry-basics", "tech-academy-candidate-search", "tech-administration-level", "tech-academy-better-instruction",
            "tech-academy-larger-classes", "tech-construction-automation", "tech-factory-max-size", "tech-industrial-energy-efficiency",
            "tech-mining-production", "tech-shipyard-slip-max-size");
        Under("tech-life-sciences", "tech-life-support-efficiency", "tech-infra-pressure-max-extension", "tech-infra-gravity-range",
            "tech-infra-pressure-range", "tech-infra-gravity-max-extension", "tech-infra-gravity-min-extension");
        Under("tech-electromagnetic-sensors", "tech-antenna-sensitivity", "tech-antenna-efficiency", "tech-antenna-bandwidth", "tech-geo-surveyor");
        Under("tech-gravitic-theory", "tech-gravitational-surveyor", "tech-spacetime-metric-theory");
        foreach (var id in new[] { "tech-antenna-sensitivity", "tech-antenna-efficiency", "tech-antenna-bandwidth" })
            Assert.That(store.Techs[id].Category, Is.EqualTo("tech-category-sensors"), id);
        // Weapons techs stay where they were.
        Assert.That(Parents(store, "tech-early-directed-energy").Select(p => p.Parent), Is.EqualTo(new[] { "tech-early-nuclear-power" }));
        Assert.That(Parents(store, "tech-laser-warheads").Select(p => p.Parent), Is.EqualTo(new[] { "tech-early-directed-energy" }));
        Assert.That(Parents(store, "tech-fission-warheads"), Is.Empty);
    }

    [Test]
    public void TheLevelGates_AddDepth()
    {
        var store = Store();
        Assert.That(Parents(store, "tech-construction-bay"), Is.EqualTo(new[] { ("tech-construction-automation", 2) }));
        Assert.That(Parents(store, "tech-asteroid-mining"), Is.EqualTo(new[] { ("tech-mining-production", 3) }));
        Assert.That(store.Techs["tech-antenna-sensitivity"].Unlocks, Is.Empty, "Antenna Sensitivity no longer opens the surveyor");
        Assert.That(Parents(store, "tech-nuclear-pulse-engine"), Is.EqualTo(new[] { ("tech-nuclear-solid-engine", 3) }));
        Assert.That(Parents(store, "tech-combined-cycle"), Is.EqualTo(new[] { ("tech-thermal-efficiency", 4) }));
        // Every gate level can be reached.
        foreach (var e in Edges(store))
            Assert.That(e.Level, Is.InRange(1, store.Techs[e.Parent].MaxLevel), $"{e.Parent} -> {e.Child}");
    }

    [Test]
    public void Warp_SitsBehindSpacetimeMetricTheory_WhichIsExpensive()
    {
        var store = Store();
        var columns = Columns(store);
        Assert.That(Parents(store, "tech-early-warp-drive"), Is.EqualTo(new[] { ("tech-spacetime-metric-theory", 1) }),
            "warp no longer needs Reactor Power Conversion or any nuclear tech");
        Assert.That(Parents(store, "tech-spacetime-metric-theory"), Is.EqualTo(new[] { ("tech-gravitic-theory", 1) }));
        Assert.That(store.Techs["tech-spacetime-metric-theory"].CostFormula, Is.EqualTo("(1 + [Level]) * 8500"));
        Assert.That(store.Techs["tech-spacetime-metric-theory"].MaxLevel, Is.EqualTo(1));
        Assert.That(store.Techs["tech-power-conversion"].Unlocks, Is.Empty);
        Assert.That(store.Techs["tech-early-nuclear-power"].Unlocks.Values.SelectMany(l => l), Does.Not.Contain("tech-early-warp-drive"));
        Assert.That(Parents(store, "tech-power-conversion"), Is.EqualTo(new[] { ("tech-early-nuclear-power", 1) }));
        Assert.That(columns["tech-spacetime-metric-theory"], Is.EqualTo(2));
        Assert.That(columns["tech-early-warp-drive"], Is.EqualTo(3));
        Assert.That(Parents(store, "tech-alcubierre-bubble-creation-cost"), Is.EqualTo(new[] { ("tech-early-warp-drive", 1) }));
        Assert.That(Parents(store, "tech-alcubierre-sustain-cost"), Is.EqualTo(new[] { ("tech-early-warp-drive", 1) }));
        Assert.That(Parents(store, "tech-alcubierre-miniaturization"), Is.EqualTo(new[] { ("tech-alcubierre-bubble-creation-cost", 3) }));
        Assert.That(Parents(store, "tech-alcubierre-bleed"), Is.EqualTo(new[] { ("tech-alcubierre-sustain-cost", 4) }));
        Assert.That(Parents(store, "tech-alcubierre-gravitational-coupling"), Is.EqualTo(new[] { ("tech-alcubierre-sustain-cost", 4) }));
        Assert.That(Parents(store, "tech-alcubierre-collapse-efficiency"), Is.EqualTo(new[] { ("tech-alcubierre-gravitational-coupling", 3) }));
        Assert.That(columns["tech-alcubierre-collapse-efficiency"], Is.EqualTo(6));
        Assert.That(columns["tech-alcubierre-bleed"], Is.EqualTo(5));
    }

    [Test]
    public void Jump_StaysShallow_AndNeedsNeitherWarpNorNuclearNorSensors()
    {
        var store = Store();
        var columns = Columns(store);
        Assert.That(Parents(store, "tech-jumppoint-stabilization"), Is.EqualTo(new[] { ("tech-gravitational-surveyor", 1) }));
        Assert.That(Parents(store, "tech-jumppoint-stabilization-size-reduction"), Is.EqualTo(new[] { ("tech-jumppoint-stabilization", 1) }));
        Assert.That(Parents(store, "tech-jumppoint-stabilization-time-reduction"), Is.EqualTo(new[] { ("tech-jumppoint-stabilization", 1) }));
        Assert.That(columns["tech-gravitational-surveyor"], Is.EqualTo(2));
        Assert.That(columns["tech-jumppoint-stabilization"], Is.EqualTo(3));
        Assert.That(columns["tech-jumppoint-stabilization-size-reduction"], Is.EqualTo(4));
        Assert.That(store.Techs["tech-early-warp-drive"].Unlocks.Values.SelectMany(l => l),
            Has.None.StartsWith("tech-jumppoint").And.None.EqualTo("tech-gravitational-surveyor"));
        // The whole jump chain stays inside the gravitics branch.
        string id = "tech-jumppoint-stabilization-time-reduction";
        var chain = new List<string> { id };
        while (Parents(store, chain.Last()).FirstOrDefault().Parent is { } p) chain.Add(p);
        Assert.That(chain, Is.EqualTo(new[] { id, "tech-jumppoint-stabilization", "tech-gravitational-surveyor", "tech-gravitic-theory" }));
    }

    [Test]
    public void TheIonDrive_IsCheaper_AndTheBranchIsElectrical()
    {
        var store = Store();
        Assert.That(store.Techs["tech-ion-engine"].CostFormula, Is.EqualTo("(1 + [Level]) * 4000"));
        Assert.That(Parents(store, "tech-ion-engine"), Is.EqualTo(new[] { ("tech-electric-thrusters", 1) }));
        Assert.That(Parents(store, "tech-electric-thrusters"), Is.EqualTo(new[] { ("tech-conductors", 1) }));
        Assert.That(Columns(store)["tech-ion-engine"], Is.EqualTo(4));
    }

    [Test]
    public void EveryTech_HasADescription_AndTheScenariosStartTheFoundations()
    {
        var store = Store();
        foreach (var t in store.Techs.Values)
            Assert.That(t.Description, Is.Not.Empty, t.UniqueID);

        var foundations = new[]
        {
            "tech-conventional-engine", "tech-electrical-engineering", "tech-industry-basics",
            "tech-life-sciences", "tech-electromagnetic-sensors", "tech-nuclear-theory", "tech-gravitic-theory"
        };
        var researchedOnWarp = new[] { "tech-electrical-engineering", "tech-life-sciences", "tech-electromagnetic-sensors", "tech-conventional-engine" };
        foreach (var (scenario, colony, warpStart) in new[]
                 { ("warp-tech-start", "colony-earth", true), ("early-tech", "colony-early-earth", false) })
        {
            var colonies = JArray.Parse(System.IO.File.ReadAllText($"Data/scenarios/{scenario}/colonies.json")).Select(c => c["Payload"] ?? c);
            var payload = colonies.Single(c => (string?)c["UniqueID"] == colony);
            var items = payload["StartingItems"]!.Select(t => (string?)t).ToList();
            var open = payload["StartingResearchable"]!.Select(t => (string?)t).ToList();
            if (warpStart)
            {
                Assert.That(items, Is.SupersetOf(researchedOnWarp), scenario);
                Assert.That(items, Does.Contain("tech-industry-basics"), scenario);
                Assert.That(items, Does.Contain("tech-gravitic-theory"), scenario);
                Assert.That(open, Does.Not.Contain("tech-industry-basics"), scenario);
                Assert.That(open, Does.Not.Contain("tech-gravitic-theory"), scenario);
            }
            else
            {
                Assert.That(items.Where(id => id != null && id.StartsWith("tech-")), Is.Empty, scenario);
                Assert.That(open, Is.EquivalentTo(foundations), scenario);
            }
            Assert.That(open.Contains("tech-spacetime-metric-theory"), Is.False, scenario);
        }
    }
}
