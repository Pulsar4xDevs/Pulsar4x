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
/// Crew plan phase 1 (crew rules phase 1): crew quarters give crew bunks, bridges give officer slots (2 / 4 / 6),
/// the designer warns when a bridged design has fewer bunks than crew, and every stock design bunks its crew.
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
    public void Bridges_GiveOfficerSlots_NotCrewBunks(int span, int console, int slots)
    {
        var bridge = Design("ship-command", ("Command Span", span), ("Console Space", console));
        Assert.That(ShipCrew.SlotsPer(bridge), Is.EqualTo(slots), "officer slots: half the console space, at least 2");
        Assert.That(ShipCrew.BerthsPer(bridge), Is.Zero, "a bridge sleeps no crew");
        Assert.That(bridge.HasAttribute<OfficerSlotsAtb>(), Is.True);
        Assert.That(bridge.HasAttribute<CrewQuartersAtb>(), Is.False, "the bridge no longer carries crew quarters");
    }

    [Test]
    public void OldSavedBridge_WithCrewQuarters_IsReadAsOfficerSlots()
    {
        // A bridge design saved before OfficerSlotsAtb carries CrewQuartersAtb: its berths are officer slots, not crew bunks.
        var legacy = Design("ship-command", ("Command Span", 1), ("Console Space", 8));
        legacy.AttributesByType.Remove(typeof(OfficerSlotsAtb));
        legacy.AttributesByType[typeof(CrewQuartersAtb)] = new CrewQuartersAtb(4);
        Assert.That(ShipCrew.SlotsPer(legacy), Is.EqualTo(4));
        Assert.That(ShipCrew.BerthsPer(legacy), Is.Zero);
    }

    [Test]
    public void Berths_SumEveryCopy_AndOtherPartsGiveNone()
    {
        var quarters = Design("crew-quarters", ("Berths", 10));
        var bridge = Design("ship-command", ("Command Span", 1), ("Console Space", 8));
        var bay = Design("ship-construction", ("Size", 1));
        var parts = new List<(ComponentDesign, int)> { (quarters, 3), (bridge, 1), (bay, 2) };
        Assert.That(ShipCrew.BerthsPer(bay), Is.EqualTo(0));
        Assert.That(ShipCrew.Berths(parts), Is.EqualTo(30), "crew quarters only; the bridge's 4 are officer slots");
        Assert.That(ShipCrew.OfficerSlots(parts), Is.EqualTo(4));
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
        Assert.That(warnings[0], Does.Contain("Crew 21").And.Contain("only 0 crew bunks").And.Contain("21 more"));

        var fullParts = new List<(ComponentDesign, int)> { (bridge, 1), (bay, 1), (quarters, 3) };
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
        Assert.That(design.CrewBerths, Is.EqualTo(20), "two 10-bunk quarters; the bridge's 2 are officer slots");
        Assert.That(design.OfficerSlots, Is.EqualTo(2));
        Assert.That(design.NeedsCaptain, Is.True);
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
        {
            bool bridged = design.Components.Any(c => c.Id is "default-design-Cockpit" or "default-design-bridge-well" or "default-design-bridge-system");
            // Crewed (bridged) designs carry crew quarters; the automated Sensor Sat and Target Drone carry none.
            Assert.That(design.Components.Any(c => c.Id.StartsWith("default-design-crew-quarters")), Is.EqualTo(bridged), design.UniqueID);
        }
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

        var stock = new Dictionary<string, (int crew, int bunks, int slots)>();
        foreach (var faction in game.Factions.Values.Where(f => f.HasDataBlob<FactionInfoDB>()))
        foreach (var design in faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values)
        {
            stock[design.Name] = (design.CrewReq, design.CrewBerths, design.OfficerSlots);
            TestContext.Out.WriteLine($"{design.Name} ({faction.GetDataBlob<NameDB>().DefaultName}): crew {design.CrewReq}, bunks {design.CrewBerths}, slots {design.OfficerSlots}, mass {design.MassPerUnit}");
            Assert.That(design.CrewBerths, Is.EqualTo(ShipCrew.Berths(design.Components)), design.Name);
            Assert.That(design.CrewBerths, Is.GreaterThanOrEqualTo(design.CrewReq), design.Name);
            Assert.That(ShipCrew.DesignCrewWarnings(design.Components), Is.Empty, design.Name);
            if (design.NeedsCaptain)
                Assert.That(design.OfficerSlots, Is.AnyOf(2, 4, 6), design.Name + ": officer slots 2 / 4 / 6");
            else
                Assert.That((design.CrewReq, design.CrewBerths, design.OfficerSlots), Is.EqualTo((0, 0, 0)), design.Name + ": automated");
        }

        // Every starting ship bunks its crew; a bridged ship also has an officer slot for its captain.
        var ships = game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ShipInfoDB>()).ToList();
        Assert.That(ships, Is.Not.Empty);
        foreach (var ship in ships)
        {
            var design = ship.GetDataBlob<ShipInfoDB>().Design;
            int captain = ShipCrew.HasCaptain(ship) ? 1 : 0;
            Assert.That(design.CrewBerths, Is.GreaterThanOrEqualTo(design.CrewReq), design.Name + ": a bunk for every crew member");
            Assert.That(design.OfficerSlots, Is.GreaterThanOrEqualTo(captain), design.Name + ": a slot for the captain");
            if (design.Name == "Sensor Sat")
                Assert.That(captain, Is.Zero, "the Sensor Sat has no bridge, so no captain");
        }

        if (scenario.EndsWith("WarpTechStart"))
        {
            Assert.That(stock.Keys, Does.Contain("Geo Surveyor").And.Contain("Prospector"), "the NPC factions' designs");
            // The crew plan table (crew, crew bunks, officer slots): bunks equal the part crew, slots 2 / 4 / 6.
            Assert.That(stock["Cargo Courier"], Is.EqualTo((14, 14, 2)));
            Assert.That(stock["Freighter"], Is.EqualTo((24, 24, 4)));
            Assert.That(stock["Colony Ship"], Is.EqualTo((43, 45, 4)), "two spare bunks");
            Assert.That(stock["Tanker"], Is.EqualTo((21, 21, 4)));
            Assert.That(stock["Sanctum Adroit Gunship"], Is.EqualTo((28, 28, 4)));
            Assert.That(stock["Ob'enn Dropship"], Is.EqualTo((51, 51, 6)));
            Assert.That(stock["Starship"], Is.EqualTo((6, 6, 4)));
            Assert.That(stock["Surveyor"], Is.EqualTo((36, 36, 6)));
            Assert.That(stock["Miner"], Is.EqualTo((16, 16, 6)));
            Assert.That(stock["Geo Surveyor"], Is.EqualTo((17, 17, 4)));
            Assert.That(stock["Prospector"], Is.EqualTo((15, 15, 4)));
            Assert.That(stock["Sensor Sat"], Is.EqualTo((0, 0, 0)), "automated: no bridge");
        }
        else
        {
            Assert.That(stock["Newton Tug"], Is.EqualTo((2, 2, 2)));
            // Mk0 geo surveyor (crew 4) plus four extra bunks: 7 crew, 7 bunks, still 2 officer slots.
            Assert.That(stock["Starship"], Is.EqualTo((7, 7, 2)));
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
