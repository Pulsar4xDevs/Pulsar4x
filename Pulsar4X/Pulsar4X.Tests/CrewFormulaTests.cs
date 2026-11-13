using System;
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
using Pulsar4X.Weapons;

namespace Pulsar4X.Tests;

/// <summary>
/// Crew plan phase 0: crew grows sub-linearly with a part's size (a bigger part needs less crew
/// per unit), the laser and beam fire control no longer need one crew per kg, the construction
/// bay stays linear in size with Construction Automation cutting it, and a design's credit cost
/// counts every copy of a part.
/// </summary>
[TestFixture]
public class CrewFormulaTests
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
        _faction = FactionFactory.CreateBasicFaction(_game, "Crew", "CRW", 0);
        _faction.FactionOwnerID = _faction.Id;
        foreach (var good in Data.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID).ToList())
            Data.Unlock(good);
    }

    // ---- sub-linear crew ----

    [TestCase("alcubierre-warp-drive", "Mass", new[] { 500.0, 2000, 8000, 32000 }, new[] { 2, 8, 23, 65 })]
    [TestCase("general-cargo-hold", "Storage Volume", new[] { 1000.0, 5000, 50000, 500000 }, new[] { 1, 5, 25, 128 })]
    [TestCase("passenger-hold", "Berths", new[] { 100.0, 1000, 10000, 100000 }, new[] { 1, 7, 37, 189 })]
    [TestCase("cargo-Shuttlebay", "Size", new[] { 5000.0, 100000, 1000000 }, new[] { 5, 31, 158 })]
    [TestCase("geo-surveyor", "Survey Speed", new[] { 1.0, 2, 5, 10 }, new[] { 2, 3, 6, 10 })]
    [TestCase("gravitational-surveyor", "Survey Speed", new[] { 1.0, 2, 5, 10 }, new[] { 3, 6, 12, 20 })]
    public void Crew_GrowsSubLinearly(string templateId, string property, double[] sizes, int[] crew)
    {
        var designs = sizes.Select(s => Design(templateId, (property, s))).ToList();
        Assert.That(designs.Select(d => d.CrewReq), Is.EqualTo(crew), templateId);
        for (int i = 1; i < sizes.Length; i++)
        {
            Assert.That(designs[i].CrewReq, Is.GreaterThanOrEqualTo(designs[i - 1].CrewReq), "a bigger part never needs less crew");
            Assert.That(designs[i].CrewReq / sizes[i], Is.LessThanOrEqualTo(designs[i - 1].CrewReq / sizes[i - 1]), "less crew per unit as it grows");
        }
    }

    [Test]
    public void Surveyors_AboutTwoCrewPerPointAtStockSize()
    {
        // Stock: geo speed 1, grav speed 10 (8 crew a point before, so 8 and 80).
        Assert.That(Design("geo-surveyor", ("Survey Speed", 1)).CrewReq, Is.EqualTo(2));
        Assert.That(Design("gravitational-surveyor", ("Survey Speed", 10)).CrewReq, Is.EqualTo(20));
    }

    [Test]
    public void StockLaserAndFireControl_NeedAFewCrew_NotOnePerKg()
    {
        // The stock designs (warp-tech componentDesigns.json): the laser takes the template defaults.
        var laser = Design("laser-weapon");
        var fireControl = Design("beam-fire-control", ("Range", 100), ("Tracking Speed", 5000), ("Size vs Range", 1));
        Assert.That(laser.MassPerUnit, Is.EqualTo(10100), "the stock laser's mass is unchanged");
        Assert.That(laser.CrewReq, Is.EqualTo(5), "Max(2, 0.02 x mass^0.6); was 10,100");
        Assert.That(fireControl.MassPerUnit, Is.EqualTo(150));
        Assert.That(fireControl.CrewReq, Is.EqualTo(4), "Max(1, 0.16 x mass^0.65); was 150");

        // Bigger guns and directors need more crew, but less per kg.
        var small = Design("laser-weapon", ("Power Input", 10000), ("Chamber Volume", 500), ("Radiator Volume", 500));
        var big = Design("laser-weapon", ("Power Input", 1000000), ("Chamber Volume", 10000), ("Radiator Volume", 10000));
        Assert.That(small.CrewReq, Is.EqualTo(2), "the floor");
        Assert.That(big.CrewReq, Is.InRange(laser.CrewReq, 10));
        Assert.That(big.CrewReq / (double)big.MassPerUnit, Is.LessThan(laser.CrewReq / (double)laser.MassPerUnit));
        var bigDirector = Design("beam-fire-control", ("Range", 175), ("Tracking Speed", 25000), ("Size vs Range", 4), ("Size vs TrackingSpeed", 4));
        Assert.That(bigDirector.CrewReq, Is.InRange(fireControl.CrewReq, 60));
        Assert.That(bigDirector.CrewReq / (double)bigDirector.MassPerUnit, Is.LessThan(fireControl.CrewReq / (double)fireControl.MassPerUnit));
    }

    // ---- construction bay ----

    [Test]
    public void ConstructionBay_IsLinearInSize_TwentyCrewPerSize()
    {
        for (int size = 1; size <= 20; size++)
            Assert.That(Design("ship-construction", ("Size", size)).CrewReq, Is.EqualTo(20 * size), "size " + size);
    }

    [TestCase(0, 1.0, 20, 400)]
    [TestCase(1, 0.85, 17, 340)]
    [TestCase(4, 0.52200625, 10, 208)]
    [TestCase(8, 0.2724905250390625, 5, 108)]
    public void ConstructionAutomation_CutsTheBaysCrew(int level, double factor, int size1Crew, int size20Crew)
    {
        if (level > 0)
        {
            Data.Unlock(CrewTech.ConstructionAutomation);
            Data.Techs[CrewTech.ConstructionAutomation].Level = level;
        }
        Assert.That(CrewTech.ConstructionCrewFactor(Data), Is.EqualTo(factor).Within(1e-12));
        Assert.That(Design("ship-construction", ("Size", 1)).CrewReq, Is.EqualTo(size1Crew));
        Assert.That(Design("ship-construction", ("Size", 20)).CrewReq, Is.EqualTo(size20Crew));
    }

    [Test]
    public void ConstructionAutomation_IsReachableFromBothStarts()
    {
        foreach (var root in new[] { "tech-modern-technology", "tech-early-technology" })
        {
            var unlocks = _game.StartingGameData.Techs[root].Unlocks.SelectMany(kv => kv.Value);
            Assert.That(unlocks, Does.Contain(CrewTech.ConstructionAutomation), root);
        }
    }

    // ---- credit cost counts copies ----

    [Test]
    public void ShipDesign_CreditCost_CountsEveryCopy()
    {
        var drive = Design("alcubierre-warp-drive", ("Mass", 500));
        var hold = Design("general-cargo-hold", ("Storage Volume", 1000));
        Assume.That(drive.CreditCost, Is.GreaterThan(0));
        var armor = (Data.Armor.Values.Concat(Data.LockedArmor.Values).First(), 0f);
        var one = new ShipDesign(Info, "one", new List<(ComponentDesign, int)> { (drive, 1), (hold, 1) }, armor, "credit-one-" + Guid.NewGuid().ToString("N"));
        var four = new ShipDesign(Info, "four", new List<(ComponentDesign, int)> { (drive, 4), (hold, 1) }, armor, "credit-four-" + Guid.NewGuid().ToString("N"));
        Assert.That(one.CreditCost, Is.EqualTo(drive.CreditCost + hold.CreditCost));
        Assert.That(four.CreditCost, Is.EqualTo(4 * drive.CreditCost + hold.CreditCost), "four drives cost four times one");
        Assert.That(four.CrewReq, Is.EqualTo(4 * drive.CrewReq + hold.CrewReq));
    }

    [Test]
    public void OrdnanceDesign_CreditCost_CountsEveryCopy()
    {
        // The missile booster is the ordnance part that still builds (the payload and electronics
        // suite name attribute types that no longer exist; ordnance is parked).
        var booster = Design("missile-srb");
        Assume.That(booster.CreditCost, Is.GreaterThan(0));
        var missile = new OrdnanceDesign(Info, "credit-missile", 0, new List<(ComponentDesign, int)> { (booster, 3) });
        Assert.That(missile.CreditCost, Is.EqualTo(3 * booster.CreditCost));
    }

    // ---- every stock design ----

    [TestCase("Pulsar4x.Scenario.WarpTechStart")]
    [TestCase("Pulsar4x.Scenario.EarlyTech")]
    [NonParallelizable]
    public void StockDesigns_HaveSaneCrew(string scenario)
    {
        ModsState.RefreshModsList("Data");
        var mods = ModsState.AvailableMods;
        var paths = new[] { mods.Single(m => m.Mod.ModName == "Pulsar4x").Path }.ToList();
        var store = GameLifecycle.LoadModStore(paths, mods.Select(m => m.Path), scenario);
        var game = GameLifecycle.CreateGameCore(store, GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345))!.Value.game;

        var seen = new HashSet<string>();
        var crew = new Dictionary<string, int>();
        foreach (var faction in game.Factions.Values.Where(f => f.HasDataBlob<FactionInfoDB>()))
        foreach (var design in faction.GetDataBlob<FactionInfoDB>().ShipDesigns.Values.OrderBy(d => d.Name))
        {
            if (!seen.Add(design.UniqueID))
                continue;
            crew[design.Name] = design.CrewReq;
            TestContext.Out.WriteLine($"{design.Name} ({faction.GetDataBlob<NameDB>().DefaultName}): crew {design.CrewReq}, credits {design.CreditCost} = "
                + string.Join(" + ", design.Components.Where(c => c.design.CrewReq > 0).Select(c => $"{c.design.Name} {c.design.CrewReq}x{c.count}")));
            Assert.That(design.CrewReq, Is.EqualTo(design.Components.Sum(c => c.design.CrewReq * c.count)), design.Name);
            Assert.That(design.CreditCost, Is.EqualTo(design.Components.Sum(c => c.design.CreditCost * c.count)), design.Name);
            Assert.That(design.CrewReq, Is.InRange(1, 60), design.Name + ": a stock design needs a handful to a few dozen crew");
            foreach (var (part, _) in design.Components)
                Assert.That(part.CrewReq, Is.InRange(0, 20), design.Name + ": " + part.Name);
        }
        Assert.That(crew, Is.Not.Empty);

        if (scenario.EndsWith("WarpTechStart"))
        {
            Assert.That(crew["Sanctum Adroit Gunship"], Is.EqualTo(29), "was 20,365");
            Assert.That(crew["Ob'enn Dropship"], Is.EqualTo(52), "was 20,454");
            Assert.That(crew["Surveyor"], Is.EqualTo(37), "was 103");
            Assert.That(crew["Colony Ship"], Is.EqualTo(44), "was 126");
        }
    }

    // ---- helpers ----

    ComponentDesign Design(string templateId, params (string key, double value)[] properties)
    {
        Data.Unlock(templateId);
        var blueprint = new ComponentDesignBlueprint
        {
            UniqueID = templateId + "-crew-" + (++_designs),
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
