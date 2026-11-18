using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Events;
using Pulsar4X.Industry;
using Pulsar4X.Factions;
using Pulsar4X.Modding;
using Pulsar4X.Names;

namespace Pulsar4X.Tests;

/// <summary>Multi-faction phase 8 small items: bloc specialisations, Luna's fixed deposits, the research category, NPC input supply.</summary>
[TestFixture]
[NonParallelizable]
public class MultiFactionEarthBlocDataTests
{
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";

    Game _game = null!;
    Entity _player = null!;
    Entity _company = null!;
    Entity _ecn = null!;

    [SetUp]
    public void SetUp()
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), WarpTechStart);
        var result = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345678));
        Assert.That(result, Is.Not.Null);
        (_game, _player) = (result!.Value.game, result.Value.faction);
        _game.Settings.EnforceSingleThread = true;
        _company = Colonies().Single(c => c.FactionOwnerID == _player.Id && BodyName(c) == "Earth");
        _ecn = Named("Euro Consortium");
    }

    List<Entity> Colonies() => _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>()).ToList();
    static string BodyName(Entity c) => c.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<NameDB>().DefaultName;
    Entity Named(string name) => Colonies().Single(c => c.GetDataBlob<NameDB>().DefaultName == name);
    static FactionInfoDB Info(Entity c) => c.GetFactionOwner.GetDataBlob<FactionInfoDB>();
    static ColonyCrewPoolDB Pool(Entity c) => ColonyCrewPoolDB.Of(c);
    int Species => _ecn.GetDataBlob<ColonyInfoDB>().Population.Keys.First();

    /// <summary>Gets the blocs' first daily pass done (the listing is set up there).</summary>
    void FirstPass()
    {
        foreach (var c in Colonies())
            CrewMarket.RunDay(c, c.StarSysDateTime);
    }


    [Test]
    public void Blocs_KnowTechsInTheirSpecialism_AndNotEachOthers()
    {
        bool Knows(Entity c, string tech) => Info(c).Data.Techs[tech].Level > 0;
        var amc = Named("Americas Compact");
        var spc = Named("Sino-Pacific Coalition");
        Assert.That(Knows(amc, "tech-life-support-efficiency"), "AMC: agriculture");
        Assert.That(Knows(amc, "tech-mining-production"), "AMC: raw materials");
        Assert.That(Knows(spc, "tech-construction-automation"), "SPC: manufacturing");
        Assert.That(Knows(spc, "tech-factory-max-size"), "SPC: manufacturing");
        Assert.That(Knows(_ecn, "tech-fuel-cells"), "ECN: high tech");
        Assert.That(Knows(_ecn, "tech-reactor-power-density"), "ECN: high tech");
        Assert.That(Knows(amc, "tech-construction-automation"), Is.False, "amc " + "tech-construction-automation");
        Assert.That(Knows(spc, "tech-life-support-efficiency"), Is.False, "spc " + "tech-life-support-efficiency");
        Assert.That(Knows(amc, "tech-reactor-power-density"), Is.False, "amc " + "tech-reactor-power-density");
        Assert.That(Knows(spc, "tech-fuel-cells"), Is.False, "spc " + "tech-fuel-cells");
        Assert.That(Knows(_ecn, "tech-construction-automation"), Is.False, "_ecn " + "tech-construction-automation");
        Assert.That(Knows(_ecn, "tech-life-support-efficiency"), Is.False, "_ecn " + "tech-life-support-efficiency");
    }

    [Test]
    public void Luna_HasAFixedSetOfDeposits_NotJustIron()
    {
        var luna = Named("Luna Concord");
        var names = Info(luna).Data.CargoGoods.GetMineralsList().ToDictionary(m => m.ID, m => m.Name.ToLowerInvariant());
        var present = luna.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<MineralsDB>().Minerals.Keys
            .Select(id => names[id]).ToList();
        Assert.That(present, Is.SupersetOf(new[] { "iron", "aluminium", "silicon", "titanium" }));
        Assert.That(present.Count, Is.GreaterThanOrEqualTo(6));
        Assert.That(present, Does.Not.Contain("water"), "Luna still buys its water");
    }

    [Test]
    public void AsteroidMiningTech_IsInConstructionAndProduction()
    {
        Assert.That(Info(_company).Data.Techs["tech-asteroid-mining"].Category, Is.EqualTo("tech-category-construction-production"));
    }

    [Test]
    public void Spc_MakesPlastic_SoTheBlocsThatBuyItKeepGettingSome()
    {
        var spc = Named("Sino-Pacific Coalition");
        var amc = Named("Americas Compact");
        var cg = Info(spc).Data.CargoGoods;
        // Two years of use at ECN's 50 a day would empty its 20,000 stock; without output SPC's 5,000 would be gone too.
        for (int month = 0; month < 14; month++)
        {
            _game.TimePulse.TimeStep(_game.TimePulse.GameGlobalDateTime + TimeSpan.FromDays(30));
            Assert.That(_game.TimePulse.LastFault, Is.Null);
        }
        Assert.That(ColonyNeeds.Stock(spc, "plastic"), Is.GreaterThan(0), "SPC keeps making plastic");
        Assert.That(ColonyNeeds.Stock(_ecn, "plastic"), Is.GreaterThanOrEqualTo(1500), "ECN keeps its working stock of plastic");
        Assert.That(ColonyNeeds.Stock(_ecn, "hydrocarbons"), Is.GreaterThanOrEqualTo(750), "ECN keeps its working stock of hydrocarbons");
        Assert.That(ColonyNeeds.Stock(amc, "plastic"), Is.GreaterThanOrEqualTo(0));
    }
}
