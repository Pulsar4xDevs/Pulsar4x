using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Interfaces;
using Pulsar4X.Ships;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;
using Pulsar4X.Technology;

namespace Pulsar4X.Tests;

/// <summary>
/// Energy economy phase 3: build energy. Industry points cost kWh by industry type (10 fabrication,
/// 5 assembly), refining is charged per recipe at the refinery, a job records what it spent, and a
/// production line draws its jobs' energy up to its nameplate. Scrapping a site-built facility in
/// an upgrade is a load too. Also TessaBot's phase 2 P3s.
/// </summary>
[TestFixture]
public class BuildEnergyTests
{
    ModDataStore _store = null!;
    Game _game = null!;
    StarSystem _sol = null!;
    Entity _player = null!;
    Entity _species = null!;
    Entity _earth = null!;

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

    FactionInfoDB Info => _player.GetDataBlob<FactionInfoDB>();
    ColonyPowerDB Power => _earth.GetDataBlob<ColonyPowerDB>();

    // ---- the default formula ----

    [Test]
    public void IndustryTypes_CarryJonsEnergyPerPoint()
    {
        Assert.That(Info.Data.EnergyPerPoint("component-construction"), Is.EqualTo(10));
        Assert.That(Info.Data.EnergyPerPoint("installation-construction"), Is.EqualTo(10));
        Assert.That(Info.Data.EnergyPerPoint("ordnance-construction"), Is.EqualTo(10));
        Assert.That(Info.Data.EnergyPerPoint("ship-assembly"), Is.EqualTo(5));
        Assert.That(Info.Data.EnergyPerPoint("refining"), Is.EqualTo(7), "the fallback for a recipe with no EnergyCost");
        Assert.That(Info.Data.EnergyPerPoint("no-such-type"), Is.EqualTo(0));
        Assert.That(Info.Data.EnergyPerPoint(null), Is.EqualTo(0));
    }

    [Test]
    public void ComponentDesigns_DefaultTo_PointsTimesEnergyPerPoint()
    {
        foreach (var design in Info.ComponentDesigns.Values)
        {
            double perPoint = Info.Data.EnergyPerPoint(design.IndustryTypeID);
            Assert.That(design.EnergyCost, Is.EqualTo(design.IndustryPointCosts * perPoint).Within(1e-9), design.UniqueID);
        }
        var factory = Info.ComponentDesigns["default-design-factory-site"];
        Assert.That(factory.IndustryTypeID, Is.EqualTo("installation-construction"));
        Assert.That(factory.EnergyCost, Is.EqualTo(8000 * 10), "8,000 IP x 10 kWh = 80 MWh");
        Assert.That(((IConstructableDesign)factory).EnergyCost, Is.EqualTo(factory.EnergyCost));
    }

    [Test]
    public void TemplateFormula_OverridesTheDefault()
    {
        var template = _store.ComponentTemplates["thermal-plant"];
        template.Formulas["EnergyCost"] = "[Mass] * 0.5";
        try
        {
            var design = TemplateDesign("thermal-plant", ("Size", 2));
            Assert.That(design.EnergyCost, Is.EqualTo(design.MassPerUnit * 0.5).Within(1e-6));
            Assert.That(design.EnergyCost, Is.Not.EqualTo(design.IndustryPointCosts * 10));

            template.Formulas["EnergyCost"] = "";
            var plain = TemplateDesign("thermal-plant", ("Size", 3));
            Assert.That(plain.EnergyCost, Is.EqualTo(plain.IndustryPointCosts * 10).Within(1e-6), "blank means the default");
        }
        finally
        {
            template.Formulas.Remove("EnergyCost");
        }
    }

    [Test]
    public void ShipDesign_IsAssemblyPlusItsComponents()
    {
        var tanker = Info.ShipDesigns["default-ship-design-tanker"];
        Assert.That(tanker.EnergyCost, Is.EqualTo(tanker.IndustryPointCosts * 5).Within(1e-9), "the hull job is assembly only");
        double parts = tanker.Components.Sum(c => c.design.EnergyCost * c.count);
        Assert.That(parts, Is.GreaterThan(0));
        Assert.That(tanker.TotalBuildEnergy, Is.EqualTo(tanker.EnergyCost + parts).Within(1e-9));
        Assert.That(ShipDesign.AssemblyEnergy(Info.Data, tanker.MassPerUnit), Is.EqualTo(tanker.EnergyCost).Within(1e-9));
    }

    [Test]
    public void SampleBuildEnergies_ForTheReport()
    {
        foreach (var id in new[] { "default-ship-design-tanker", "default-ship-design-freighter" })
        {
            var ship = Info.ShipDesigns[id];
            TestContext.Out.WriteLine($"{ship.Name}: mass {ship.MassPerUnit:N0} kg, {ship.IndustryPointCosts:N0} assembly IP, " +
                $"assembly {Stringify.EnergyKwh(ship.EnergyCost)}, components {Stringify.EnergyKwh(ship.TotalBuildEnergy - ship.EnergyCost)}, " +
                $"total {Stringify.EnergyKwh(ship.TotalBuildEnergy)}");
            foreach (var (design, count) in ship.Components)
                TestContext.Out.WriteLine($"    {count} x {design.Name}: {design.IndustryPointCosts:N0} IP, {Stringify.EnergyKwh(design.EnergyCost)} each");
            Assert.That(ship.TotalBuildEnergy, Is.GreaterThan(0));
        }
        foreach (var id in new[] { "default-design-factory-site", "default-design-refinery-site", "default-design-shipyard", "default-design-research-lab" })
        {
            var d = Info.ComponentDesigns[id];
            TestContext.Out.WriteLine($"{d.Name} ({id}): {d.IndustryPointCosts:N0} IP, {Stringify.EnergyKwh(d.EnergyCost)}");
        }
        foreach (var id in new[] { "stainless-steel", "methalox", "hydrolox", "electronics", "fissile-fuels" })
        {
            var m = _store.ProcessedMaterials[id];
            TestContext.Out.WriteLine($"{m.Name}: {m.BatchEnergy} kWh per {m.OutputAmount} units, {m.BatchEnergy / m.IndustryPointCosts:0.##} kWh/IP");
        }

        new IndustryProcessor().ProcessEntity(_earth, 0);
        ColonyPowerProcessor.Recalc(_earth);
        foreach (var c in Power.Consumers.Where(c => c.Tier == PowerTier.Industry))
            TestContext.Out.WriteLine($"Earth day 1 {c.Name}: {Stringify.Power(c.Draw_kW)} of {Stringify.Power(c.Nameplate_kW)}");
        TestContext.Out.WriteLine($"Earth industry tier {Stringify.Power(Power.DemandByTier[PowerTier.Industry])}, total demand {Stringify.Power(Power.Demand_kW)}");
    }

    [Test]
    public void EnergyKwh_ScalesUnits()
    {
        Assert.That(Stringify.EnergyKwh(70), Is.EqualTo("70 kWh"));
        Assert.That(Stringify.EnergyKwh(80_000), Is.EqualTo("80 MWh"));
        Assert.That(Stringify.EnergyKwh(2_500_000), Is.EqualTo("2.5 GWh"));
        Assert.That(Stringify.EnergyKwh(3e9), Is.EqualTo("3 TWh"));
    }

    // ---- refining at the refinery ----

    [Test]
    public void Recipes_CarryTheirOwnEnergy_AndFallBackToTheType()
    {
        var steel = _store.ProcessedMaterials["stainless-steel"];
        Assert.That(((IConstructableDesign)steel).EnergyCost, Is.EqualTo(70), "0.7 kWh/kg x 100 kg");
        var expected = new Dictionary<string, double>
        {
            ["stainless-steel"] = 70, ["plastic"] = 12, ["electronics"] = 1000, ["methalox"] = 4250, ["rp-1"] = 2975,
            ["hydrolox"] = 19550, ["ntp"] = 1200, ["fissile-fuels"] = 400, ["space-crete"] = 50,
        };
        foreach (var (id, kWh) in expected)
            Assert.That(_store.ProcessedMaterials[id].BatchEnergy, Is.EqualTo(kWh), id);

        double? saved = steel.EnergyCost;
        steel.EnergyCost = null;
        try
        {
            Assert.That(steel.BatchEnergy, Is.EqualTo(20 * 7), "no recipe value: 20 IP x 7 kWh");
        }
        finally
        {
            steel.EnergyCost = saved;
        }
    }

    [Test]
    public void RefiningEnergy_IsChargedAtTheRefinery_NotTheMine()
    {
        var mineBefore = Power.Consumers.Single(c => c.DesignId == "default-design-mine").Draw_kW;
        var (lineId, line) = Refinery();
        var job = QueueOnly(line, "stainless-steel", 50);
        StockRecipe("stainless-steel", 50);

        ColonyPowerProcessor.Recalc(_earth);
        double infra = Pulsar4X.Industry.InfrastructureProcessor.GetEfficiency(_earth);
        var refinery = Power.Consumers.Single(c => c.DesignId == "default-design-refinery-site");
        double nameplate = refinery.Nameplate_kW;
        Assert.That(refinery.Draw_kW, Is.EqualTo(Math.Min(nameplate, 0.1 * nameplate + line.IndustryTypeRates["refining"] * infra * 3.5 / 24)).Within(1e-9));
        Assert.That(Power.Consumers.Single(c => c.DesignId == "default-design-mine").Draw_kW, Is.EqualTo(mineBefore), "the mine draws extraction only");

        long left = job.ProductionPointsLeft;
        new IndustryProcessor().ProcessEntity(_earth, 0);
        double points = job.NumberCompleted * (double)job.ProductionPointsCost + (job.ProductionPointsCost - job.ProductionPointsLeft) - (job.ProductionPointsCost - left);
        Assert.That(points, Is.GreaterThan(0));
        Assert.That(job.EnergyPerPoint, Is.EqualTo(3.5));
        Assert.That(job.EnergyUsed, Is.EqualTo(points * 3.5).Within(1e-6), "kWh = points x 70 kWh / 20 IP");
        Assert.That(line.EnergyUsedLastDay, Is.EqualTo(job.EnergyUsed).Within(1e-6));
        Assert.That(job.EnergyOrdered, Is.EqualTo(50 * 70).Within(1e-9));
    }

    // ---- job energy and the draw cap ----

    [Test]
    public void HungryRecipe_IsCappedAtTheNameplate_AndRunsSlower()
    {
        // Hydrolox: 19,550 kWh per 500 IP = 39.1 kWh/IP, against a refinery nameplate of
        // 145.8 kW (3,500 kWh a day): at most 89 points a day, not the line's ~330.
        var (lineId, line) = Refinery();
        var job = QueueOnly(line, "hydrolox", 5);
        StockRecipe("hydrolox", 5);

        ColonyPowerProcessor.Recalc(_earth);
        var refinery = Power.Consumers.Single(c => c.DesignId == "default-design-refinery-site");
        Assert.That(refinery.Draw_kW, Is.EqualTo(refinery.Nameplate_kW).Within(1e-9), "capped at the nameplate");
        double budget = refinery.Nameplate_kW * 24;

        new IndustryProcessor().ProcessEntity(_earth, 0);
        double points = job.ProductionPointsCost - job.ProductionPointsLeft;
        Assert.That(points, Is.EqualTo(Math.Floor(budget / 39.1 + 1e-9)));
        Assert.That(job.EnergyUsed, Is.EqualTo(points * 39.1).Within(1e-6));
        Assert.That(job.EnergyUsed, Is.LessThanOrEqualTo(budget + 1e-6));
        Assert.That(job.Status, Is.EqualTo(IndustryJobStatus.Processing));

        // At half power the line gets half the energy.
        Power.TierSatisfaction[PowerTier.Industry] = 0.5;
        double before = job.ProductionPointsLeft;
        new IndustryProcessor().ProcessEntity(_earth, 0);
        Assert.That(before - job.ProductionPointsLeft, Is.EqualTo(Math.Floor(budget * 0.5 / 39.1 + 1e-9)));
    }

    [Test]
    public void IndustrialEnergyEfficiency_ScalesEveryPointsKwh_AndTheDraw()
    {
        // Phase 4: level 4 is x0.8 on every point's kWh, for jobs already queued too.
        for (int i = 0; i < 4; i++)
            Info.Data.IncrementTechLevel(EnergyTech.IndustrialEnergyEfficiency);
        Assert.That(EnergyTech.IndustrialEnergyFactor(_earth), Is.EqualTo(0.8).Within(1e-12));

        var (_, line) = Refinery();
        var steel = QueueOnly(line, "stainless-steel", 50);
        StockRecipe("stainless-steel", 50);
        ColonyPowerProcessor.Recalc(_earth);
        double infra = Pulsar4X.Industry.InfrastructureProcessor.GetEfficiency(_earth);
        var refinery = Power.Consumers.Single(c => c.DesignId == "default-design-refinery-site");
        Assert.That(refinery.Draw_kW, Is.EqualTo(Math.Min(refinery.Nameplate_kW,
            0.1 * refinery.Nameplate_kW + line.IndustryTypeRates["refining"] * infra * 3.5 * 0.8 / 24)).Within(1e-9));
        long left = steel.ProductionPointsLeft;
        new IndustryProcessor().ProcessEntity(_earth, 0);
        double points = steel.NumberCompleted * (double)steel.ProductionPointsCost + (steel.ProductionPointsCost - steel.ProductionPointsLeft) - (steel.ProductionPointsCost - left);
        Assert.That(points, Is.GreaterThan(0));
        Assert.That(steel.EnergyPerPoint, Is.EqualTo(3.5), "the job keeps the list kWh per point");
        Assert.That(steel.EnergyUsed, Is.EqualTo(points * 3.5 * 0.8).Within(1e-6));

        // A hungry recipe still runs at the nameplate, but gets 1/0.8 more points out of it.
        var hydrolox = QueueOnly(line, "hydrolox", 5);
        StockRecipe("hydrolox", 5);
        ColonyPowerProcessor.Recalc(_earth);
        refinery = Power.Consumers.Single(c => c.DesignId == "default-design-refinery-site");
        double budget = refinery.Nameplate_kW * 24;
        new IndustryProcessor().ProcessEntity(_earth, 0);
        double hPoints = hydrolox.ProductionPointsCost - hydrolox.ProductionPointsLeft;
        Assert.That(hPoints, Is.EqualTo(Math.Floor(budget / (39.1 * 0.8) + 1e-9)));
        Assert.That(hydrolox.EnergyUsed, Is.EqualTo(hPoints * 39.1 * 0.8).Within(1e-6));
    }

    [Test]
    public void IdleLines_DrawATenth_AndJobsFromOldSavesBuildFree()
    {
        var industry = _earth.GetDataBlob<IndustryAbilityDB>();
        foreach (var l in industry.ProductionLines.Values)
            l.Jobs.Clear();
        ColonyPowerProcessor.Recalc(_earth);
        foreach (var id in new[] { "default-design-refinery-site", "default-design-factory-site", "default-design-shipyard" })
        {
            var c = Power.Consumers.Single(c => c.DesignId == id);
            Assert.That(c.Draw_kW, Is.EqualTo(c.Nameplate_kW * ColonyPowerProcessor.IndustryIdleShare).Within(1e-9), id);
        }

        var (_, line) = Refinery();
        var job = QueueOnly(line, "stainless-steel", 5);
        job.EnergyPerPoint = 0; // as a job loaded from a save without build energy
        StockRecipe("stainless-steel", 5);
        new IndustryProcessor().ProcessEntity(_earth, 0);
        Assert.That(job.EnergyUsed, Is.EqualTo(0));
        Assert.That(job.NumberCompleted, Is.GreaterThan(0));
    }

    [Test]
    public void Projector_ShowsDrawAgainstNameplate()
    {
        var view = new GameProjector(_game).ProjectEntity(_earth, _player.Id).GetView<ColonyPowerView>()!;
        var refinery = view.Consumers.Single(c => c.DesignId == "default-design-refinery-site");
        var lab = view.Consumers.Single(c => c.DesignId == "default-design-research-lab");
        Assert.That(refinery.Nameplate_kW, Is.GreaterThanOrEqualTo(refinery.Draw_kW));
        Assert.That(refinery.Nameplate_kW, Is.EqualTo(500 * 7 / 24.0).Within(1e-6));
        Assert.That(lab.Nameplate_kW, Is.EqualTo(1 * 3000).Within(1e-6), "one 3 MW lab");
        Assert.That(lab.Draw_kW, Is.EqualTo(0.2 * lab.Nameplate_kW).Within(1e-6), "no project: 20 % of the nameplate");
    }

    // ---- scrapping ----

    [Test]
    public void ScrappingASiteFacility_InAnUpgrade_IsALoadWhileTheJobRuns()
    {
        var (lineId, _) = Refinery();
        var old = _earth.GetDataBlob<ComponentInstancesDB>().AllComponents[lineId].Design;
        var bigger = TemplateDesign("refinery-site", ("Size", 6000));
        StockAssemble(bigger);

        var result = new CommandTranslator(_game).Translate(_player, _earth, new UpgradeFacilityCommand(_earth.Id, lineId, bigger.UniqueID));
        Assert.That(result.Accepted, Is.True, result.RejectionReason);

        var construction = _earth.GetDataBlob<LocalConstructionDB>();
        var job = construction.BuildQueue.Single(j => j.UpgradeLineId == lineId);
        Assert.That(FacilityUpgrade.ScrapEnergyFraction, Is.EqualTo(0.2));
        Assert.That(job.ScrapEnergy, Is.EqualTo(0.2 * old.EnergyCost).Within(1e-9), "a fifth of 80 MWh");
        Assert.That(job.ScrapEnergy, Is.EqualTo(16_000).Within(1e-9));

        ColonyPowerProcessor.Recalc(_earth);
        long ahead = construction.BuildQueue.TakeWhile(j => j != job).Sum(j => Math.Max(0, j.PointCost - j.PointsAccumulated));
        long today = Math.Max(0, Math.Min(construction.PointsPerDay - ahead, job.PointCost));
        var scrapping = Power.Consumers.Single(c => c.DesignId == ColonyPowerProcessor.ScrappingLineId);
        Assert.That(scrapping.Tier, Is.EqualTo(PowerTier.Industry));
        Assert.That(scrapping.Draw_kW, Is.EqualTo(job.ScrapEnergy * today / job.PointCost / 24).Within(1e-9));

        long before = job.PointsAccumulated;
        new LocalConstructionProcessor().ProcessEntity(_earth, 0);
        long spent = job.PointsAccumulated - before;
        Assert.That(spent, Is.GreaterThan(0));
        Assert.That(job.ScrapEnergyUsed, Is.EqualTo(job.ScrapEnergy * spent / job.PointCost).Within(1e-9));
    }

    [Test]
    public void CratesComeOffWhole_AndCostNoScrapEnergy()
    {
        Assert.That(FacilityUpgrade.ScrapEnergy(_earth, "no-such-line"), Is.EqualTo(0));
        var crate = _earth.GetDataBlob<ComponentInstancesDB>().AllComponents.Values
            .FirstOrDefault(c => LocalConstructionJob.IsCrate(c.Design) && c.Design.EnergyCost > 0);
        Assume.That(crate, Is.Not.Null, "Earth has an installed crate design");
        Assert.That(FacilityUpgrade.ScrapEnergy(_earth, crate!.UniqueID), Is.EqualTo(0));
    }

    // ---- TessaBot's phase 2 P3s ----

    [Test]
    public void Research_GoesThroughDoResearch_AtTheResearchShare()
    {
        var lab = _sol.GetAllEntitiesWithDataBlob<ResearcherDB>()
            .First(l => l.GetDataBlob<ResearcherDB>().LocationId == _earth.Id);
        var db = lab.GetDataBlob<ResearcherDB>();
        if (db.PointsPerDay.GetValue() <= 0)
            db.PointsPerDay = new ModifiableValue<int>(1000);
        db.CostPerDay = new ModifiableValue<decimal>(0);
        var tech = Info.Data.Techs.Values.Where(t => Info.Data.IsResearchable(t.UniqueID)).OrderByDescending(t => t.ResearchCost).First();
        Assume.That(tech.ResearchCost, Is.GreaterThan(db.PointsPerDay.GetValue() * 3), "the tech outlasts the test");
        while (db.TechQueue.TryDequeue(out _)) { }
        db.TechQueue.Enqueue(tech.UniqueID);
        int perDay = db.PointsPerDay.GetValue();
        var processor = new ResearchProcessor();

        int Day(double share)
        {
            Power.TierSatisfaction[PowerTier.Research] = share;
            int before = tech.ResearchProgress;
            processor.DoResearch(lab);
            return tech.ResearchProgress - before;
        }

        Assert.That(Day(1), Is.EqualTo(perDay));
        Assert.That(Day(0.25), Is.EqualTo((int)(perDay * 0.25)));
        Assert.That(Day(0), Is.EqualTo(0));
    }

    [Test]
    public void LunaKeepsItsFifteenInfrastructure_PlusThreeForItsBatteryBank()
    {
        // TessaBot P3: phase 2 raised it to 21 for the reactor's mass; no test needs that, so it is back to 15.
        // The scenario battery bank (3 x 1000 t) adds ~3000 to Luna's required capacity; three more
        // 400-capacity units hold its infrastructure efficiency at its pre-bank 0.40.
        var luna = _store.Colonies["colony-luna-concord"];
        Assert.That(luna.Installations!.Single(i => i.Id == "default-design-infrastructure-luna").Amount, Is.EqualTo(15 + 3));
    }

    [Test]
    public void EarlyTechEarth_HasOneHydrocarbonPile_CoveringTheReserve()
    {
        var store = new ModDataStore();
        new ModLoader().LoadModManifest("Data/basemod/modInfo.json", store);
        new ModLoader().LoadScenario("Data/scenarios/warp-tech-start/scenarioInfo.json", store);
        new ModLoader().LoadScenario("Data/scenarios/early-tech/scenarioInfo.json", store);
        var early = store.Colonies["colony-early-earth"];
        var piles = early.Cargo!.Where(c => c.Id == "hydrocarbons").ToList();
        Assert.That(piles, Has.Count.EqualTo(1));
        // 75 MW thermal, 730 days: ceil(75,000 kW x 86,400 s / (0.33 x 42 MJ/kg x 1000) x 730) = 341,298,702 kg,
        // plus the 50,000 kg working stock the colony always had.
        double reserve = Math.Ceiling(75_000.0 * 86_400 / (0.33 * 42 * 1000) * ColonyPowerProcessor.FuelReserveDays);
        Assert.That(reserve, Is.EqualTo(341_298_702));
        Assert.That(piles[0].Amount, Is.EqualTo(reserve + 50_000));
    }

    // ---- helpers ----

    (string lineId, IndustryAbilityDB.ProductionLine line) Refinery()
    {
        var instances = _earth.GetDataBlob<ComponentInstancesDB>();
        var industry = _earth.GetDataBlob<IndustryAbilityDB>();
        var pair = industry.ProductionLines.First(l => instances.AllComponents.TryGetValue(l.Key, out var c)
            && c.Design.UniqueID == "default-design-refinery-site");
        return (pair.Key, pair.Value);
    }

    IndustryJob QueueOnly(IndustryAbilityDB.ProductionLine line, string itemId, ushort count)
    {
        foreach (var l in _earth.GetDataBlob<IndustryAbilityDB>().ProductionLines.Values)
            l.Jobs.Clear();
        var job = new IndustryJob(Info, itemId);
        job.InitialiseJob(count, false);
        line.Jobs.Add(job);
        return job;
    }

    void StockRecipe(string itemId, int batches)
    {
        var design = Info.IndustryDesigns[itemId];
        foreach (var (id, amount) in design.ResourceCosts)
            Add(id, amount * batches);
    }

    void StockAssemble(ComponentDesign design)
    {
        foreach (var (id, amount) in design.ResourceCosts)
            Add(id, amount);
    }

    void Add(string id, long units)
    {
        var cargo = Info.Data.CargoGoods.GetAny(id)!;
        _earth.GetDataBlob<CargoStorageDB>().AddCargoByUnit(cargo, units);
    }

    ComponentDesign TemplateDesign(string templateId, params (string key, double value)[] properties)
    {
        var data = Info.Data;
        data.Unlock(templateId);
        var blueprint = new ComponentDesignBlueprint
        {
            UniqueID = templateId + "-p3-" + string.Join("-", properties.Select(p => p.value)) + "-" + Guid.NewGuid().ToString("N")[..6],
            Name = templateId + " " + string.Join(" ", properties.Select(p => p.value)),
            TemplateId = templateId,
            Properties = properties.Select(p => new ComponentDesignBlueprint.Property
            {
                Key = p.key,
                Value = new Newtonsoft.Json.Linq.JValue(p.value),
            }).ToList(),
        };
        return ComponentDesignFromJson.Create(_player, data, blueprint);
    }
}
