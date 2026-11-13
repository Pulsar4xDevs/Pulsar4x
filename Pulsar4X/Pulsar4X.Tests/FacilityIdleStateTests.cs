using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Industry.Orders;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;
using Pulsar4X.Technology;

namespace Pulsar4X.Tests;

/// <summary>
/// Power rebalance: labs, mines and construction offices have an idle state. A lab with no project
/// draws 20 % of its nameplate, a mine with nothing it can mine and store 10 %, a construction office
/// with an empty queue 10 %. Orders refresh the colony's demand the same tick; changes with no order
/// behind them (the stores filling, a deposit running out, research finishing) refresh only when a
/// facility's idle/working state actually flips.
/// </summary>
[TestFixture]
public class FacilityIdleStateTests
{
    // Independent oracles: the shares and nameplates from the brief and the data formulas.
    const double LabIdle = 0.2, MineIdle = 0.1, SiteIdle = 0.1;
    const double Lab_kW = 3000;
    const double Mine_kW = 4500 * 15 * 0.05 / 24;     // MiningAmount x 15 minerals x 0.05 kWh/kg / 24 h
    const double Site_kW = 8 * 10 / 24.0;             // Level 1 x 8 points x 10 kWh/point / 24 h

    ModDataStore _store = null!;
    Game _game = null!;
    StarSystem _sol = null!;
    Entity _player = null!;
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
        var species = SpeciesFactory.CreateFromBlueprint(_sol, _store.Species["species-human"]);
        species.FactionOwnerID = _player.Id;
        Info.Species.Add(species);
        var earth = NameLookup.GetFirstEntityWithName(_sol, "Earth");
        _earth = ColonyFactory.CreateFromBlueprint(_game, _player, species, _sol, earth, _store.Colonies["colony-earth"]);
        _earth.GetDataBlob<LocalConstructionDB>().BuildQueue.Clear();
        ColonyPowerProcessor.Recalc(_earth);
    }

    FactionInfoDB Info => _player.GetDataBlob<FactionInfoDB>();
    ColonyPowerDB Power => _earth.GetDataBlob<ColonyPowerDB>();
    ColonyPowerConsumer Line(string id) => Power.Consumers.Single(c => c.DesignId == id);
    ColonyPowerConsumer Labs => Line("default-design-research-lab");
    ColonyPowerConsumer Mine => Line("default-design-mine");
    ColonyPowerConsumer Sites => Line("default-design-local-construction-site");

    // ---- labs ----

    [Test]
    public void Labs_DrawTwentyPercent_WithNoProject_AndRefreshOnEachOrder()
    {
        Assert.That(Labs.Count, Is.EqualTo(2));
        Assert.That(Labs.Nameplate_kW, Is.EqualTo(2 * Lab_kW).Within(1e-9));
        Assert.That(Labs.Draw_kW, Is.EqualTo(2 * Lab_kW * LabIdle).Within(1e-9), "no project at the start");
        Assert.That(ColonyPowerProcessor.ResearchIdleShare, Is.EqualTo(LabIdle));

        var (lab, researcher) = LabEntities().First();
        string tech = AResearchableTech();
        AddTechToQueueOrder.Create(lab, tech).Execute(_earth.StarSysDateTime);
        Assert.That(researcher.TechQueue.Count, Is.EqualTo(1));
        Assert.That(Labs.Draw_kW, Is.EqualTo(Lab_kW + Lab_kW * LabIdle).Within(1e-9), "one lab working, the same tick");
        Assert.That(Power.Demand_kW, Is.EqualTo(Power.Consumers.Sum(c => c.Draw_kW)).Within(1e-6));

        FundingChangedOrder.Create(lab, 0).Execute(_earth.StarSysDateTime);
        Assert.That(Labs.Draw_kW, Is.EqualTo(2 * Lab_kW * LabIdle).Within(1e-9), "unfunded is idle");
        FundingChangedOrder.Create(lab, 1).Execute(_earth.StarSysDateTime);
        Assert.That(Labs.Draw_kW, Is.EqualTo(Lab_kW + Lab_kW * LabIdle).Within(1e-9));

        RemoveTechFromQueueOrder.Create(lab, tech).Execute(_earth.StarSysDateTime);
        Assert.That(researcher.TechQueue.Count, Is.EqualTo(0));
        Assert.That(Labs.Draw_kW, Is.EqualTo(2 * Lab_kW * LabIdle).Within(1e-9), "back to idle, the same tick");
    }

    [Test]
    public void FinishingResearch_IdlesTheLab_AndOnlyAFlipRecalcs()
    {
        var (lab, researcher) = LabEntities().First();
        var techs = Info.Data.Techs.Values.Where(t => Info.Data.IsResearchable(t.UniqueID)).Take(2).ToList();
        Assert.That(techs, Has.Count.EqualTo(2));
        AddTechToQueueOrder.Create(lab, techs[0].UniqueID).Execute(_earth.StarSysDateTime);
        AddTechToQueueOrder.Create(lab, techs[1].UniqueID).Execute(_earth.StarSysDateTime);
        Assert.That(Labs.Draw_kW, Is.EqualTo(Lab_kW + Lab_kW * LabIdle).Within(1e-9));

        // The head drops out of the queue (no longer researchable) but the next project takes over:
        // the lab is still working, so the pass does not recalc (the sentinel survives).
        techs[0].Level = techs[0].MaxLevel;
        Power.Demand_kW = -1;
        new ResearchProcessor().DoResearch(lab);
        Assert.That(researcher.TechQueue.Count, Is.EqualTo(1));
        Assert.That(Power.Demand_kW, Is.EqualTo(-1), "no flip, no recalc");

        // Now the last project goes: the lab idles and the colony's demand is refreshed at once.
        techs[1].Level = techs[1].MaxLevel;
        new ResearchProcessor().DoResearch(lab);
        Assert.That(researcher.TechQueue.Count, Is.EqualTo(0));
        Assert.That(Labs.Draw_kW, Is.EqualTo(2 * Lab_kW * LabIdle).Within(1e-9));
        Assert.That(Power.Demand_kW, Is.EqualTo(Power.Consumers.Sum(c => c.Draw_kW)).Within(1e-6));
    }

    // ---- mines ----

    [Test]
    public void Mines_IdleWhenTheStoresAreFull_AndResumeWhenRoomIsMade_OnFlipsOnly()
    {
        Assert.That(Mine.Draw_kW, Is.EqualTo(Mine_kW).Within(1e-6), "deposits and room: working");
        Assert.That(ColonyPowerProcessor.MiningIdleShare, Is.EqualTo(MineIdle));
        var mining = MiningPass();

        // A day's mining with room to spare changes nothing for the grid: no recalc.
        Power.Demand_kW = -1;
        mining.ProcessEntity(_earth, 0);
        Assert.That(Power.Demand_kW, Is.EqualTo(-1), "still working: no recalc");
        ColonyPowerProcessor.Recalc(_earth);

        // Fill the stores. Without an order the grid only notices on the next pass that looks: the mining pass.
        var filler = FillStores();
        Assert.That(Mine.Draw_kW, Is.EqualTo(Mine_kW).Within(1e-6), "not refreshed by a plain cargo change");
        mining.ProcessEntity(_earth, 0);
        Assert.That(Mine.Draw_kW, Is.EqualTo(Mine_kW * MineIdle).Within(1e-6), "nowhere to put the ore: idle");
        Assert.That(Power.Demand_kW, Is.EqualTo(Power.Consumers.Sum(c => c.Draw_kW)).Within(1e-6));

        // Still full: another pass is no flip.
        Power.Demand_kW = -1;
        mining.ProcessEntity(_earth, 0);
        Assert.That(Power.Demand_kW, Is.EqualTo(-1));
        ColonyPowerProcessor.Recalc(_earth);

        // Room again: the next look flips it back.
        var storage = _earth.GetDataBlob<CargoStorageDB>();
        storage.RemoveCargoByUnit(filler, storage.GetUnitsStored(filler, false));
        ColonyPowerProcessor.RefreshIfIdleStateChanged(_earth);
        Assert.That(Mine.Draw_kW, Is.EqualTo(Mine_kW).Within(1e-6));
    }

    [Test]
    public void Mines_IdleWhenEveryDepositIsGone()
    {
        var mining = MiningPass();
        var minerals = _earth.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<MineralsDB>();
        foreach (var deposit in minerals.Minerals.Values)
            MiningHelper.Deplete(deposit, deposit.Amount.Actual);
        Assert.That(Mine.Draw_kW, Is.EqualTo(Mine_kW).Within(1e-6), "not looked yet");
        mining.ProcessEntity(_earth, 0);
        Assert.That(Mine.Draw_kW, Is.EqualTo(Mine_kW * MineIdle).Within(1e-6), "nothing left to mine: idle");
    }

    // ---- construction offices ----

    [Test]
    public void ConstructionOffices_DrawTenPercent_WithAnEmptyQueue_AndRefreshOnQueueChanges()
    {
        Assert.That(Sites.Count, Is.EqualTo(5));
        Assert.That(Sites.Nameplate_kW, Is.EqualTo(5 * Site_kW).Within(1e-9));
        Assert.That(Sites.Draw_kW, Is.EqualTo(5 * Site_kW * SiteIdle).Within(1e-9), "empty queue");
        Assert.That(ColonyPowerProcessor.ConstructionIdleShare, Is.EqualTo(SiteIdle));

        var design = Info.ComponentDesigns["default-design-factory-site"];
        foreach (var (id, amount) in design.ResourceCosts)
            _earth.GetDataBlob<CargoStorageDB>().AddCargoByUnit(Info.Data.CargoGoods.GetAny(id)!, amount);
        var construction = _earth.GetDataBlob<LocalConstructionDB>();
        AddToConstructionQueueOrder.CreateAssemble(_earth, design).Execute(_earth.StarSysDateTime);
        var job = construction.BuildQueue.Single();
        Assert.That(Sites.Draw_kW, Is.EqualTo(5 * Site_kW).Within(1e-9), "queued: working, the same tick");

        RemoveFromConstructionQueueOrder.Create(_earth, job).Execute(_earth.StarSysDateTime);
        Assert.That(construction.BuildQueue, Is.Empty);
        Assert.That(Sites.Draw_kW, Is.EqualTo(5 * Site_kW * SiteIdle).Within(1e-9), "removed: idle again");

        // A job that finishes today: the daily pass empties the queue and the offices idle at once.
        var quick = new LocalConstructionJob(design);
        quick.ResourcesRemaining.Clear();
        quick.PointCost = 1;
        construction.BuildQueue.Enqueue(quick);
        ColonyPowerProcessor.Recalc(_earth);
        Assert.That(Sites.Draw_kW, Is.EqualTo(5 * Site_kW).Within(1e-9));
        new LocalConstructionProcessor().ProcessEntity(_earth, 0);
        Assert.That(construction.BuildQueue, Is.Empty, "finished");
        Assert.That(Sites.Draw_kW, Is.EqualTo(5 * Site_kW * SiteIdle).Within(1e-9));
    }

    [Test]
    public void IdleStates_AreNotSaved_AndAFreshKeyJustCostsOneRecalc()
    {
        var copy = (ColonyPowerDB)Power.Clone();
        Assert.That(copy.IdleStateKey, Is.EqualTo(Power.IdleStateKey));
        Assert.That(Power.IdleStateKey, Is.Not.Empty);
        Power.IdleStateKey = "";
        ColonyPowerProcessor.RefreshIfIdleStateChanged(_earth);
        Assert.That(Power.IdleStateKey, Is.EqualTo(ColonyPowerProcessor.IdleStateKey(_earth)));
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(Power);
        Assert.That(json, Does.Not.Contain("IdleStateKey"));
    }

    // ---- helpers ----

    IEnumerable<(Entity lab, ResearcherDB researcher)> LabEntities()
    {
        foreach (var c in _earth.GetDataBlob<ComponentInstancesDB>().GetComponentsBySpecificDesign("default-design-research-lab"))
        {
            Assert.That(_earth.Manager.TryGetEntityById(c.SpawnedEntityId, out var lab), Is.True);
            yield return (lab, lab.GetDataBlob<ResearcherDB>());
        }
    }

    string AResearchableTech() => Info.Data.Techs.Values.First(t => Info.Data.IsResearchable(t.UniqueID)).UniqueID;

    MineResourcesProcessor MiningPass()
    {
        var p = new MineResourcesProcessor();
        p.Init(_game);
        return p;
    }

    /// <summary>Fills every store the mine's minerals go to; returns the filler.</summary>
    ICargoable FillStores()
    {
        var storage = _earth.GetDataBlob<CargoStorageDB>();
        var mine = Info.ComponentDesigns["default-design-mine"].GetAttribute<MineResourcesAtbDB>();
        ICargoable? filler = null;
        foreach (var id in mine.ResourcesPerEconTick.Keys)
        {
            var mineral = Info.Data.CargoGoods.GetAny(id)!;
            filler ??= mineral;
            storage.AddCargoByUnit(mineral, 1_000_000_000_000);
        }
        foreach (var id in mine.ResourcesPerEconTick.Keys)
            Assert.That(storage.GetFreeUnitSpace(Info.Data.CargoGoods.GetAny(id)!), Is.LessThanOrEqualTo(0), id);
        return filler!;
    }
}
