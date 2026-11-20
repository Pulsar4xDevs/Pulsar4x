using System.Linq;
using NUnit.Framework;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Engine;
using Pulsar4X.Factions;

namespace Pulsar4X.Tests;

/// <summary>
/// A scenario colony's StartingResearchable opens techs (unlocked, level 0) without researching them; StartingItems researches.
/// No scenario uses a gate tech node any more.
/// </summary>
[TestFixture]
public class StartingResearchableTests
{
    [TestCase("Pulsar4x.Scenario.WarpTechStart")]
    [TestCase("Pulsar4x.Scenario.EarlyTech")]
    public void TheStart_ResearchesItsStartingItems_AndOnlyOpensItsStartingResearchable(string scenario)
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), scenario);
        var catalog = GameLifecycle.BuildCatalog(store);
        var created = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, catalog, 12345678));
        Assert.That(created, Is.Not.Null);
        var data = created!.Value.faction.GetDataBlob<FactionInfoDB>().Data;

        Assert.That(data.Techs["tech-conventional-engine"].Level, Is.EqualTo(1), "a StartingItems tech is researched");
        foreach (var open in new[] { "tech-mining-production", "tech-nuclear-theory", "tech-conductors" })
        {
            Assert.That(data.IsResearchable(open), Is.True, open + " is open");
            Assert.That(data.Techs[open].Level, Is.EqualTo(0), open + " is not researched");
            Assert.That(data.Techs[open].ResearchCost, Is.GreaterThan(0), open);
        }
        Assert.That(data.Techs.Keys.Concat(data.LockedTechs.Keys), Has.None.EqualTo("tech-modern-technology").And.None.EqualTo("tech-early-technology"));
        // What was only reachable through a deeper tech is still shut.
        Assert.That(data.Techs.ContainsKey("tech-nuclear-solid-engine"), Is.False);
    }
}
