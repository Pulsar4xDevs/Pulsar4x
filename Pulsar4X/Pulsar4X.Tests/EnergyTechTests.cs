using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Technology;

namespace Pulsar4X.Tests;

/// <summary>
/// Energy economy phase 4: the §7 tech slice. Thermal plant efficiency (capped, lifted by Combined
/// Cycle), the fission plant unlock behind Nuclear Power, reactor power density, reactor power
/// conversion (data only until phase 6), grid distribution, industrial energy efficiency and fuel
/// cells (unlock only). A tech-0 colony keeps every phase 3 number.
/// </summary>
[TestFixture]
public class EnergyTechTests
{
    const string BaseMod = "Pulsar4x";
    const string EarlyTech = "Pulsar4x.Scenario.EarlyTech";

    static readonly string[] NewTechs =
    {
        EnergyTech.ThermalEfficiency, EnergyTech.CombinedCycle, EnergyTech.FissionPowerPlant,
        EnergyTech.ReactorPowerDensity, EnergyTech.PowerConversion, EnergyTech.GridDistribution,
        EnergyTech.IndustrialEnergyEfficiency, EnergyTech.FuelCells,
    };

    ModDataStore _store = null!;
    Game _game = null!;
    StarSystem _sol = null!;
    Entity _player = null!;
    Entity _species = null!;
    Entity _earth = null!;
    int _designs;

    [SetUp]
    public void SetUp()
    {
        _store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", _store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", _store);

        _game = new Game(new NewGameSettings { MaxSystems = 1, CreatePlayerFaction = false }, _store);
        _game.Settings.EnforceSingleThread = true;
        StarSystemFactory.LoadFromBlueprint(_game, _store.Systems["system-sol"]);

        _player = FactionFactory.CreateBasicFaction(_game, "United Earth Corp", "UEC", 0);
        _player.FactionOwnerID = _player.Id;
        _sol = _game.Systems.Single(s => s.ID == "system-sol");
        _species = SpeciesFactory.CreateFromBlueprint(_sol, _store.Species["species-human"]);
        _species.FactionOwnerID = _player.Id;
        _player.GetDataBlob<FactionInfoDB>().Species.Add(_species);

        var earth = NameLookup.GetFirstEntityWithName(_sol, "Earth");
        _earth = ColonyFactory.CreateFromBlueprint(_game, _player, _species, _sol, earth, _store.Colonies["colony-earth"]);
    }

    FactionDataStore Data => _player.GetDataBlob<FactionInfoDB>().Data;
    ColonyPowerDB Power => _earth.GetDataBlob<ColonyPowerDB>();

    // ---- each tech's formulas ----

    [TestCase("tech-thermal-efficiency", "tech-category-power-propulsion", 9, 0.33, 0.36, 0.60, 500, 1000)]
    [TestCase("tech-combined-cycle", "tech-category-power-propulsion", 1, 1.0, 1.0, 1.0, 4000, 4000)]
    [TestCase("tech-fission-power-plant", "tech-category-power-propulsion", 1, 1.0, 1.0, 1.0, 3000, 3000)]
    [TestCase("tech-reactor-power-density", "tech-category-power-propulsion", 10, 1.0, 1.25, 3.5, 1000, 2000)]
    [TestCase("tech-power-conversion", "tech-category-power-propulsion", 10, 0.30, 0.315, 0.45, 1000, 2000)]
    [TestCase("tech-grid-distribution", "tech-category-power-propulsion", 8, 0.92, 0.9275, 0.98, 500, 1000)]
    [TestCase("tech-industrial-energy-efficiency", "tech-category-construction-production", 8, 1.0, 0.95, 0.60, 1000, 2000)]
    [TestCase("tech-fuel-cells", "tech-category-power-propulsion", 1, 1.0, 1.0, 1.0, 1500, 1500)]
    public void TechData_EvaluatesAtEachLevel(string id, string category, int maxLevel,
        double level0, double level1, double levelMax, int cost0, int cost1)
    {
        Assert.That(_store.Techs, Does.ContainKey(id));
        var tech = new Tech(_store.Techs[id]);
        Assert.That(tech.MaxLevel, Is.EqualTo(maxLevel));
        Assert.That(tech.Category, Is.EqualTo(category));
        Assert.That(_store.TechCategories, Does.ContainKey(category));
        Assert.That(tech.Name, Is.Not.Empty);
        Assert.That(tech.Description, Is.Not.Empty);

        Assert.That(tech.TechDataFormula(), Is.EqualTo(level0).Within(1e-12), "level 0");
        Assert.That(tech.TechCostFormula(), Is.EqualTo(cost0), "cost of the first level");
        tech.Level = 1;
        Assert.That(tech.TechDataFormula(), Is.EqualTo(level1).Within(1e-12), "level 1");
        Assert.That(tech.TechCostFormula(), Is.EqualTo(cost1), "cost of the second level");
        tech.Level = maxLevel;
        Assert.That(tech.TechDataFormula(), Is.EqualTo(levelMax).Within(1e-12), "max level");
    }

    // ---- the unlock chain from each scenario's starting techs ----

    [TestCase("colony-earth")]
    [TestCase("colony-early-earth")]
    public void EveryNewTech_IsReachable_FromTheScenariosStartingTechs(string colonyId)
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        new ModLoader().LoadScenario(colonyId == "colony-earth"
            ? "Data/scenarios/warp-tech-start/scenarioInfo.json"
            : "Data/scenarios/early-tech/scenarioInfo.json", store);
        var start = store.Colonies[colonyId].StartingItems!;

        // Every level of every tech can be researched, so whatever any level unlocks is reachable.
        var reachable = new HashSet<string>(start);
        var queue = new Queue<string>(start.Where(store.Techs.ContainsKey));
        while (queue.Count > 0)
            foreach (var item in store.Techs[queue.Dequeue()].Unlocks.Values.SelectMany(ids => ids))
                if (reachable.Add(item) && store.Techs.ContainsKey(item))
                    queue.Enqueue(item);

        foreach (var id in NewTechs)
            Assert.That(reachable, Does.Contain(id), id);
        Assert.That(reachable, Does.Contain("fission-plant"));
        Assert.That(reachable, Does.Contain("solar-farm"));
        Assert.That(reachable, Does.Contain("grid-battery"));
        // The solar farm and grid battery reuse the panel and battery techs.
        foreach (var id in new[] { "tech-panel-efficiency", "tech-panel-bandwidth", "tech-panel-density", "tech-battery-capacity" })
            Assert.That(reachable, Does.Contain(id), id);
    }

    [Test]
    public void WarpStart_OpensEveryEnergyTech_AndAlreadyHasTheFissionPlant()
    {
        foreach (var id in NewTechs.Where(id => id != EnergyTech.CombinedCycle))
            Assert.That(Data.Techs, Does.ContainKey(id), id);
        Assert.That(Data.Techs[EnergyTech.FissionPowerPlant].Level, Is.EqualTo(1), "nuclear is known at the warp start");
        Assert.That(Data.ComponentTemplates, Does.ContainKey("fission-plant"));
        foreach (var id in NewTechs.Where(id => id != EnergyTech.FissionPowerPlant && id != EnergyTech.CombinedCycle))
            Assert.That(Data.Techs[id].Level, Is.EqualTo(0), id);

        // Combined Cycle opens at Thermal Plant Efficiency 4, where simple cycle meets its 45% cap.
        Assert.That(Data.Techs, Does.Not.ContainKey(EnergyTech.CombinedCycle));
        for (int i = 0; i < 3; i++)
            Data.IncrementTechLevel(EnergyTech.ThermalEfficiency);
        Assert.That(Data.Techs, Does.Not.ContainKey(EnergyTech.CombinedCycle));
        Data.IncrementTechLevel(EnergyTech.ThermalEfficiency);
        Assert.That(Data.IsResearchable(EnergyTech.CombinedCycle), Is.True);
    }

    [Test]
    public void EarlyTech_ReachesTheFissionPlant_ThroughNuclearPower()
    {
        var (_, faction) = StartGameLikeTheMenu(EarlyTech);
        var data = faction.GetDataBlob<FactionInfoDB>().Data;

        foreach (var id in new[] { EnergyTech.ThermalEfficiency, EnergyTech.GridDistribution,
                     EnergyTech.IndustrialEnergyEfficiency, EnergyTech.FuelCells })
            Assert.That(data.IsResearchable(id), Is.True, id);
        foreach (var id in new[] { EnergyTech.FissionPowerPlant, EnergyTech.ReactorPowerDensity,
                     EnergyTech.PowerConversion, EnergyTech.CombinedCycle })
            Assert.That(data.Techs, Does.Not.ContainKey(id), id + " waits");
        Assert.That(data.ComponentTemplates, Does.Not.ContainKey("fission-plant"));

        data.IncrementTechLevel("tech-early-nuclear-power");
        foreach (var id in new[] { EnergyTech.FissionPowerPlant, EnergyTech.ReactorPowerDensity, EnergyTech.PowerConversion })
            Assert.That(data.IsResearchable(id), Is.True, id);
        Assert.That(data.ComponentTemplates, Does.Not.ContainKey("fission-plant"), "nuclear power alone is not a grid plant");

        data.IncrementTechLevel(EnergyTech.FissionPowerPlant);
        Assert.That(data.ComponentTemplates, Does.ContainKey("fission-plant"));
        Assert.That(data.IsResearchable(EnergyTech.FissionPowerPlant), Is.False, "an unlock is researched once");
    }

    // ---- grid efficiency and plant efficiency follow the tech level ----

    [Test]
    public void GridEfficiency_FollowsGridDistribution()
    {
        ColonyPowerProcessor.Recalc(_earth);
        Assert.That(Power.GridEfficiency, Is.EqualTo(0.92));

        Data.IncrementTechLevel(EnergyTech.GridDistribution);
        Data.IncrementTechLevel(EnergyTech.GridDistribution);
        ColonyPowerProcessor.Recalc(_earth);
        Assert.That(Power.GridEfficiency, Is.EqualTo(0.935).Within(1e-12));
        Assert.That(Power.Delivered_kW, Is.EqualTo(Power.Generation_kW * 0.935).Within(1e-6));

        // The daily pass serves demand from generation / grid efficiency, so better lines burn less fuel.
        double demandGen = Power.Demand_kW / Power.GridEfficiency;
        Assert.That(demandGen, Is.LessThan(Power.Demand_kW / 0.92));

        for (int i = 0; i < 6; i++)
            Data.IncrementTechLevel(EnergyTech.GridDistribution);
        Assert.That(Data.IsResearchable(EnergyTech.GridDistribution), Is.False);
        ColonyPowerProcessor.Recalc(_earth);
        Assert.That(Power.GridEfficiency, Is.EqualTo(0.98).Within(1e-12));
    }

    [Test]
    public void PlantEfficiency_FollowsThermalEfficiency_UpToTheCap_AndCombinedCycleLiftsIt()
    {
        double Eta(string templateId) => TemplateDesign(templateId, ("Size", 10)).GetAttribute<PowerPlantAtb>().Efficiency;
        var before = TemplateDesign("thermal-plant", ("Size", 10));

        Assert.That(Eta("thermal-plant"), Is.EqualTo(0.33).Within(1e-12));
        Assert.That(Eta("oxidiser-turbine"), Is.EqualTo(0.35).Within(1e-12));
        Assert.That(Eta("fission-plant"), Is.EqualTo(0.33).Within(1e-12));

        Data.IncrementTechLevel(EnergyTech.ThermalEfficiency);
        Assert.That(Eta("thermal-plant"), Is.EqualTo(0.36).Within(1e-12));
        Assert.That(Eta("oxidiser-turbine"), Is.EqualTo(0.38).Within(1e-12));
        Assert.That(Eta("fission-plant"), Is.EqualTo(0.36).Within(1e-12));
        Assert.That(before.GetAttribute<PowerPlantAtb>().Efficiency, Is.EqualTo(0.33).Within(1e-12),
            "a plant keeps the efficiency it was designed with");

        for (int i = 1; i < 9; i++)
            Data.IncrementTechLevel(EnergyTech.ThermalEfficiency);
        Assert.That(Data.Techs[EnergyTech.ThermalEfficiency].Level, Is.EqualTo(9));
        Assert.That(Eta("thermal-plant"), Is.EqualTo(0.45).Within(1e-12), "simple cycle stops at 45%");
        Assert.That(Eta("oxidiser-turbine"), Is.EqualTo(0.45).Within(1e-12));
        Assert.That(Eta("fission-plant"), Is.EqualTo(0.45).Within(1e-12));

        Data.IncrementTechLevel(EnergyTech.CombinedCycle);
        Assert.That(Eta("thermal-plant"), Is.EqualTo(0.60).Within(1e-12), "Combined Cycle: 0.45 -> 0.60");
        Assert.That(Eta("oxidiser-turbine"), Is.EqualTo(0.55).Within(1e-12));
        Assert.That(Eta("fission-plant"), Is.EqualTo(0.45).Within(1e-12), "a steam cycle; no combined cycle");

        // Fuel burn = output / (η · LHV): a 10 MW thermal plant at 60% burns 0.55 of what it did at 33%.
        var plant = TemplateDesign("thermal-plant", ("Size", 10)).GetAttribute<PowerPlantAtb>();
        Assert.That(plant.FuelAtFull_kgPerS, Is.EqualTo(before.GetAttribute<PowerPlantAtb>().FuelAtFull_kgPerS * 0.33 / 0.60).Within(1e-12));
    }

    [Test]
    public void ReactorPowerDensity_ScalesTheSteamTurbinesHeat()
    {
        var level0 = TemplateDesign("steam-turbine-reactor").GetAttribute<EnergyGenerationAtb>();
        Assert.That(level0.PowerOutputMax, Is.EqualTo(15).Within(1e-9));

        Data.IncrementTechLevel(EnergyTech.ReactorPowerDensity);
        Data.IncrementTechLevel(EnergyTech.ReactorPowerDensity);
        var level2 = TemplateDesign("steam-turbine-reactor").GetAttribute<EnergyGenerationAtb>();
        Assert.That(level2.PowerOutputMax, Is.EqualTo(15 * 1.5).Within(1e-9));
        Assert.That(level0.PowerOutputMax, Is.EqualTo(15).Within(1e-9), "built reactors keep their design");
    }

    // ---- tech 0 is phase 3 ----

    [Test]
    public void TechZero_GivesExactlyThePhase3Numbers()
    {
        // The warp start's Earth: every new tech open at level 0 (the fission plant unlock researched).
        AssertPhase3(Data, "warp start");
        ColonyPowerProcessor.Recalc(_earth);
        Assert.That(Power.GridEfficiency, Is.EqualTo(ColonyPowerDB.DefaultGridEfficiency));
        Assert.That(Power.Delivered_kW, Is.EqualTo(Power.Generation_kW * 0.92));
        var earthPlants = _earth.GetDataBlob<ComponentInstancesDB>().DesignsAndComponentCount.Keys
            .Select(d => d.TryGetAttribute<PowerPlantAtb>(out var p) ? (d.UniqueID, p.Efficiency) : default)
            .Where(p => p.UniqueID != null).ToList();
        Assert.That(earthPlants, Is.Not.Empty);
        foreach (var (id, eta) in earthPlants)
            Assert.That(eta, Is.EqualTo(id.Contains("oxidiser") || id.Contains("methalox") ? 0.35 : 0.33), id);

        // A faction that has opened none of them (they are all locked): level 0 all the same.
        var locked = new FactionDataStore(_store);
        foreach (var id in NewTechs)
            Assert.That(locked.Techs, Does.Not.ContainKey(id));
        AssertPhase3(locked, "all locked");
        // TechData on a tech the faction has not opened evaluates at level 0 instead of throwing.
        var basic = FactionFactory.CreateBasicFaction(_game, "Locked", "LCK", 0);
        var basicData = basic.GetDataBlob<FactionInfoDB>().Data;
        foreach (var good in basicData.LockedCargoGoods.GetAll().Values.Select(c => c.UniqueID).ToList())
            basicData.Unlock(good);
        foreach (var (templateId, eta) in new[] { ("thermal-plant", 0.33), ("oxidiser-turbine", 0.35), ("fission-plant", 0.33) })
        {
            basicData.Unlock(templateId);
            Assert.That(basicData.Techs, Does.Not.ContainKey(EnergyTech.ThermalEfficiency));
            Assert.That(TemplateDesign(basic, basicData, templateId).GetAttribute<PowerPlantAtb>().Efficiency, Is.EqualTo(eta), templateId);
        }
        Assert.That(TemplateDesign(basic, basicData, "steam-turbine-reactor").GetAttribute<EnergyGenerationAtb>().PowerOutputMax,
            Is.EqualTo(15).Within(1e-12));

        // Data from a save that predates the techs: the phase 3 constants.
        AssertPhase3(new FactionDataStore(), "no techs at all");
    }

    void AssertPhase3(FactionDataStore data, string why)
    {
        Assert.That(EnergyTech.GridEfficiency(data), Is.EqualTo(0.92), why);
        Assert.That(EnergyTech.IndustrialEnergyFactor(data), Is.EqualTo(1), why);
        if (data.IndustryTypes.Count + data.LockedIndustryTypes.Count > 0)
        {
            Assert.That(data.EnergyPerPoint("component-construction"), Is.EqualTo(10), why);
            Assert.That(data.EnergyPerPoint("ship-assembly"), Is.EqualTo(5), why);
            Assert.That(data.EnergyPerPoint("refining"), Is.EqualTo(7), why);
        }
    }

    [Test]
    public void TechZero_Designs_MatchPhase3()
    {
        Assert.That(TemplateDesign("thermal-plant", ("Size", 10)).GetAttribute<PowerPlantAtb>().Efficiency, Is.EqualTo(0.33));
        Assert.That(TemplateDesign("oxidiser-turbine", ("Size", 5)).GetAttribute<PowerPlantAtb>().Efficiency, Is.EqualTo(0.35));
        Assert.That(TemplateDesign("fission-plant", ("Size", 50)).GetAttribute<PowerPlantAtb>().Efficiency, Is.EqualTo(0.33));
        var reactor = TemplateDesign("steam-turbine-reactor");
        Assert.That(reactor.MassPerUnit, Is.EqualTo(2000));
        Assert.That(reactor.GetAttribute<EnergyGenerationAtb>().PowerOutputMax, Is.EqualTo(15).Within(1e-12));
        // Build energy is untouched by the techs at level 0.
        var factory = TemplateDesign("factory-site");
        Assert.That(factory.EnergyCost, Is.EqualTo(factory.IndustryPointCosts * 10.0));
    }

    // ---- helpers ----

    ComponentDesign TemplateDesign(string templateId, params (string key, double value)[] properties)
        => TemplateDesign(_player, Data, templateId, properties);

    ComponentDesign TemplateDesign(Entity faction, FactionDataStore data, string templateId, params (string key, double value)[] properties)
    {
        data.Unlock(templateId);
        var blueprint = new ComponentDesignBlueprint
        {
            UniqueID = templateId + "-tech-test-" + (++_designs),
            Name = templateId,
            TemplateId = templateId,
        };
        if (properties.Length > 0)
            blueprint.Properties = properties.Select(p => new ComponentDesignBlueprint.Property
            {
                Key = p.key,
                Value = new Newtonsoft.Json.Linq.JValue(p.value),
            }).ToList();
        ComponentDesigner.StartResearched = true;
        try { return ComponentDesignFromJson.Create(faction, data, blueprint); }
        finally { ComponentDesigner.StartResearched = false; }
    }

    static (Game game, Entity faction) StartGameLikeTheMenu(string? scenarioId)
    {
        ModsState.RefreshModsList("Data");
        var paths = new List<string> { ModsState.AvailableMods.Single(m => m.Mod.ModName == BaseMod).Path };
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), scenarioId);
        var catalog = GameLifecycle.BuildCatalog(store);
        var systems = catalog.Systems.Where(s => s.StartingBodies.Count > 0).ToList();
        var request = new NewGameRequest(
            ModManifestPaths: paths,
            FactionName: "United Earth Corp",
            FactionAbbreviation: "UEC",
            SpeciesId: catalog.Species.First().Id,
            ColonyId: catalog.Colonies.First().Id,
            SystemId: systems.First().Id,
            BodyId: systems.First().StartingBodies.First().Id,
            EnabledSystems: systems.Select(s => s.Id).ToList(),
            MaxSystems: 10,
            MasterSeed: 12345678,
            StartingFunds: 100_000_000,
            EleStart: true,
            SMPassword: "",
            PlayerPassword: "");
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null);
        return (result!.Value.game, result.Value.faction);
    }
}
