using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pulsar4X.Blueprints;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.Ships;

namespace Pulsar4X.Tests;

/// <summary>
/// Crew plan phase 1: crew quarters and bridges give berths, the designer warns when a design has
/// fewer berths than crew, and every stock design sleeps its crew.
/// </summary>
[TestFixture]
public class CrewBerthTests
{
    Game _game = null!;
    Entity _faction = null!;
    int _designs;

    FactionInfoDB Info => _faction.GetDataBlob<FactionInfoDB>();
    FactionDataStore Data => Info.Data;

    [SetUp]
    public void SetUp()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        _game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, store);
        _faction = FactionFactory.CreateBasicFaction(_game, "Berths", "BRT", 0);
        _faction.FactionOwnerID = _faction.Id;
        foreach (var good in Data.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID).ToList())
            Data.Unlock(good);
    }

    [TestCase(1, 250)]
    [TestCase(10, 1770)]
    [TestCase(25, 3856)]
    [TestCase(100, 12529)]
    public void Quarters_GiveTheirBerths_AndWeighLessPerBerthWhenBigger(int berths, int mass)
    {
        var quarters = Design("crew-quarters", ("Berths", berths));
        Assert.That(ShipCrew.BerthsPer(quarters), Is.EqualTo(berths));
        Assert.That(quarters.MassPerUnit, Is.EqualTo(mass).Within(1));
        Assert.That(quarters.CrewReq, Is.EqualTo(0), "small quarters need no stewards");
        Assert.That(quarters.CreditCost, Is.EqualTo((int)(quarters.MassPerUnit * 0.12)).Within(1));
        if (berths > 1)
            Assert.That(quarters.MassPerUnit / (double)berths, Is.LessThan(250), "sub-linear: a bigger block shares the galley");
    }

    [Test]
    public void BigQuarters_NeedAFewStewards()
    {
        Assert.That(Design("crew-quarters", ("Berths", 1000)).CrewReq, Is.EqualTo(2));
    }

    [TestCase(0, 5, 2)]
    [TestCase(1, 8, 4)]
    [TestCase(2, 12, 6)]
    [TestCase(0, 20, 10)]
    [TestCase(0, 1, 2)]
    public void Bridges_SleepAFew(int span, int console, int berths)
    {
        var bridge = Design("ship-command", ("Command Span", span), ("Console Space", console));
        Assert.That(ShipCrew.BerthsPer(bridge), Is.EqualTo(berths));
    }

    [Test]
    public void Berths_SumEveryCopy_AndOtherPartsGiveNone()
    {
        var quarters = Design("crew-quarters", ("Berths", 10));
        var bridge = Design("ship-command", ("Command Span", 1), ("Console Space", 8));
        var bay = Design("ship-construction", ("Size", 1));
        var parts = new List<(ComponentDesign, int)> { (quarters, 3), (bridge, 1), (bay, 2) };
        Assert.That(ShipCrew.BerthsPer(bay), Is.EqualTo(0));
        Assert.That(ShipCrew.Berths(parts), Is.EqualTo(34));
        Assert.That(ShipCrew.CrewRequired(parts), Is.EqualTo(bridge.CrewReq + 2 * bay.CrewReq));
    }

    [Test]
    public void Designer_WarnsWhenBerthsAreShort_AndNotOtherwise()
    {
        var bridge = Design("ship-command", ("Command Span", 1), ("Console Space", 8));
        var bay = Design("ship-construction", ("Size", 1));
        var quarters = Design("crew-quarters", ("Berths", 10));
        var shortParts = new List<(ComponentDesign, int)> { (bridge, 1), (bay, 1) };
        var warnings = ShipCrew.DesignCrewWarnings(shortParts);
        Assert.That(warnings, Has.Count.EqualTo(1));
        Assert.That(warnings[0], Does.Contain("Crew 22").And.Contain("only 4 berths").And.Contain("18 more"));

        var fullParts = new List<(ComponentDesign, int)> { (bridge, 1), (bay, 1), (quarters, 2) };
        Assert.That(ShipCrew.DesignCrewWarnings(fullParts), Is.Empty);
        Assert.That(ShipCrew.DesignCrewWarnings(new List<(ComponentDesign, int)>()), Is.Empty);
    }

    [Test]
    public void ShipDesign_CarriesItsBerths()
    {
        var bridge = Design("ship-command", ("Command Span", 0), ("Console Space", 5));
        var quarters = Design("crew-quarters", ("Berths", 10));
        Data.Unlock("plastic");
        Data.Unlock("plastic-armor");
        var design = new ShipDesign(Info, "Berthed", new List<(ComponentDesign, int)> { (bridge, 1), (quarters, 2) },
            (Data.Armor["plastic-armor"], 0f));
        Assert.That(design.CrewBerths, Is.EqualTo(22));
        Assert.That(design.CrewReq, Is.EqualTo(bridge.CrewReq));
    }

    [Test]
    public void EveryStockShipDesign_CarriesCrewQuarters()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        new ModLoader().LoadScenario("Data/scenarios/early-tech/scenarioInfo.json", store);
        Assert.That(store.ShipDesigns, Is.Not.Empty);
        foreach (var design in store.ShipDesigns.Values)
            Assert.That(design.Components.Any(c => c.Id.StartsWith("default-design-crew-quarters")), Is.True, design.UniqueID);
    }

    [TestCase("Pulsar4x.Scenario.WarpTechStart")]
    [TestCase("Pulsar4x.Scenario.EarlyTech")]
    [NonParallelizable]
    public void EveryStockDesign_SleepsItsCrew_IncludingTheNpcFactions(string scenario)
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), scenario);
        var game = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345))!.Value.game;

        var berths = new Dictionary<string, (int crew, int berths)>();
        foreach (var faction in game.Factions.Values.Where(f => f.HasDataBlob<FactionInfoDB>()))
        foreach (var design in faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values)
        {
            berths[design.Name] = (design.CrewReq, design.CrewBerths);
            TestContext.Out.WriteLine($"{design.Name} ({faction.GetDataBlob<NameDB>().DefaultName}): crew {design.CrewReq}, berths {design.CrewBerths}, mass {design.MassPerUnit}");
            Assert.That(design.CrewBerths, Is.EqualTo(ShipCrew.Berths(design.Components)), design.Name);
            Assert.That(design.CrewBerths, Is.GreaterThanOrEqualTo(design.CrewReq), design.Name);
            Assert.That(ShipCrew.DesignCrewWarnings(design.Components), Is.Empty, design.Name);
        }

        if (scenario.EndsWith("WarpTechStart"))
        {
            Assert.That(berths.Keys, Does.Contain("Geo Surveyor").And.Contain("Prospector"), "the NPC factions' designs");
            Assert.That(berths["Cargo Courier"], Is.EqualTo((14, 14)));
            Assert.That(berths["Colony Ship"], Is.EqualTo((44, 49)));
            Assert.That(berths["Ob'enn Dropship"], Is.EqualTo((52, 56)));
            Assert.That(berths["Sensor Sat"], Is.EqualTo((1, 1)));
        }
        else
        {
            Assert.That(berths["Newton Tug"], Is.EqualTo((2, 3)));
            Assert.That(berths["Starship"], Is.EqualTo((3, 3)));
        }
    }

    ComponentDesign Design(string templateId, params (string key, double value)[] properties)
    {
        Data.Unlock(templateId);
        var blueprint = new ComponentDesignBlueprint
        {
            UniqueID = templateId + "-berth-" + (++_designs),
            Name = templateId,
            TemplateId = templateId,
        };
        if (properties.Length > 0)
            blueprint.Properties = properties.Select(p => new ComponentDesignBlueprint.Property { Key = p.key, Value = new JValue(p.value) }).ToList();
        ComponentDesigner.StartResearched = true;
        try { return ComponentDesignFromJson.Create(_faction, Data, blueprint); }
        finally { ComponentDesigner.StartResearched = false; }
    }
}
