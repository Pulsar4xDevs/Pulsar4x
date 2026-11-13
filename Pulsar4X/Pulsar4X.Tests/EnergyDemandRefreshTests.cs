using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pulsar4X.Api;
using Pulsar4X.Blueprints;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Energy;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Api;
using Pulsar4X.Engine.Factories;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Industry;
using Pulsar4X.Industry.Orders;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.People;
using Pulsar4X.Storage;

namespace Pulsar4X.Tests;

/// <summary>
/// Energy follow-up A: queuing, cancelling, reordering, editing or finishing an industry job, and
/// queuing, removing, reordering or finishing an installation job, refresh the colony's grid demand
/// the same tick (<see cref="ColonyPowerProcessor.RefreshDemand"/>). The daily plant run still burns
/// the fuel and raises the events, once.
/// </summary>
[TestFixture]
public class EnergyDemandRefreshTests
{
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
        foreach (var l in Industry.ProductionLines.Values)
            l.Jobs.Clear();
        ColonyPowerProcessor.Recalc(_earth);
    }

    FactionInfoDB Info => _player.GetDataBlob<FactionInfoDB>();
    ColonyPowerDB Power => _earth.GetDataBlob<ColonyPowerDB>();
    IndustryAbilityDB Industry => _earth.GetDataBlob<IndustryAbilityDB>();
    CommandTranslator Translator => new(_game);

    // ---- industry jobs ----

    [Test]
    public void QueueReorderCancel_ChangeTheLinesDraw_TheSameTick()
    {
        var (lineId, line) = Refinery();
        double idle = RefineryLine().Nameplate_kW * ColonyPowerProcessor.IndustryIdleShare;
        Assert.That(RefineryLine().Draw_kW, Is.EqualTo(idle).Within(1e-9), "an idle line draws its hotel share");
        double demand0 = Power.Demand_kW;
        var quiet = Snapshot();

        // Queue steel (3.5 kWh/point): the head job's energy counts at once.
        Accept(new QueueIndustryJobCommand(_earth.Id, lineId, "stainless-steel", 5, false, false));
        var steel = line.Jobs.Single();
        Assert.That(RefineryLine().Draw_kW, Is.EqualTo(Expected(lineId, steel)).Within(1e-9));
        Assert.That(RefineryLine().Draw_kW, Is.GreaterThan(idle));
        Assert.That(Power.Demand_kW, Is.EqualTo(demand0 - idle + RefineryLine().Draw_kW).Within(1e-6));

        // Queue hydrolox (39.1 kWh/point) behind it: steel is still the head, nothing changes.
        Accept(new QueueIndustryJobCommand(_earth.Id, lineId, "hydrolox", 5, false, false));
        var hydrolox = line.Jobs.Single(j => j.ItemGuid == "hydrolox");
        Assert.That(RefineryLine().Draw_kW, Is.EqualTo(Expected(lineId, steel)).Within(1e-9));

        // Move hydrolox to the front: the hungrier recipe draws now.
        Accept(new ChangeIndustryJobPriorityCommand(_earth.Id, lineId, hydrolox.JobID, -1));
        Assert.That(line.Jobs[0], Is.SameAs(hydrolox));
        Assert.That(RefineryLine().Draw_kW, Is.EqualTo(Expected(lineId, hydrolox)).Within(1e-9));
        Assert.That(RefineryLine().Draw_kW, Is.GreaterThan(Expected(lineId, steel)));

        // Cancel both: back to idle.
        Accept(new CancelIndustryJobCommand(_earth.Id, lineId, hydrolox.JobID));
        Assert.That(RefineryLine().Draw_kW, Is.EqualTo(Expected(lineId, steel)).Within(1e-9));
        Accept(new CancelIndustryJobCommand(_earth.Id, lineId, steel.JobID));
        Assert.That(RefineryLine().Draw_kW, Is.EqualTo(idle).Within(1e-9));
        Assert.That(Power.Demand_kW, Is.EqualTo(demand0).Within(1e-6));

        AssertNoDailyWork(quiet);
    }

    [Test]
    public void EditingAJobToDone_DropsItsDraw()
    {
        var (lineId, line) = Refinery();
        IndustryTools.AddJob(_earth, lineId, NewJob("stainless-steel", 5));
        var job = line.Jobs.Single();
        Assert.That(RefineryLine().Draw_kW, Is.EqualTo(Expected(lineId, job)).Within(1e-9));
        IndustryTools.EditExsistingJob(_earth, lineId, job.JobID, false, 0);
        Assert.That(RefineryLine().Draw_kW, Is.EqualTo(RefineryLine().Nameplate_kW * ColonyPowerProcessor.IndustryIdleShare).Within(1e-9),
            "nothing left to build on the line");
    }

    [Test]
    public void FinishingAJob_InTheIndustryPass_DropsTheDraw_WithoutARecalc()
    {
        var (lineId, line) = Refinery();
        IndustryTools.AddJob(_earth, lineId, NewJob("stainless-steel", 1));
        var job = line.Jobs.Single();
        StockRecipe("stainless-steel", 1);
        Assert.That(RefineryLine().Draw_kW, Is.GreaterThan(RefineryLine().Nameplate_kW * ColonyPowerProcessor.IndustryIdleShare));
        var quiet = Snapshot();

        for (int day = 0; day < 30 && job.NumberCompleted < job.NumberOrdered; day++)
            new IndustryProcessor().ProcessEntity(_earth, 0);
        Assert.That(job.NumberCompleted, Is.EqualTo(1), "one batch of steel finishes within a month");
        Assert.That(RefineryLine().Draw_kW, Is.EqualTo(RefineryLine().Nameplate_kW * ColonyPowerProcessor.IndustryIdleShare).Within(1e-9));
        AssertNoDailyWork(quiet);
    }

    [Test]
    public void AStalledJob_StopsDrawing_AfterTheIndustryPass()
    {
        var (lineId, line) = Refinery();
        IndustryTools.AddJob(_earth, lineId, NewJob("hydrolox", 5));
        var job = line.Jobs.Single();
        // No recipe in stock (the colony has none to spare): the job stalls on missing resources.
        foreach (var (id, _) in job.ResourcesCosts)
            Remove(id);
        Assert.That(RefineryLine().Draw_kW, Is.GreaterThan(RefineryLine().Nameplate_kW * ColonyPowerProcessor.IndustryIdleShare));
        new IndustryProcessor().ProcessEntity(_earth, 0);
        Assume.That(job.Status, Is.EqualTo(Pulsar4X.DataStructures.IndustryJobStatus.MissingResources));
        Assert.That(RefineryLine().Draw_kW, Is.EqualTo(RefineryLine().Nameplate_kW * ColonyPowerProcessor.IndustryIdleShare).Within(1e-9));
    }

    // ---- installation jobs ----

    [Test]
    public void QueueRemoveAndReorder_AnUpgrade_ChangeTheScrapLoad_TheSameTick()
    {
        var (lineId, _) = Refinery();
        var bigger = TemplateDesign("refinery-site", ("Size", 6000));
        StockAssemble(bigger);
        var construction = _earth.GetDataBlob<LocalConstructionDB>();
        construction.BuildQueue.Clear();
        ColonyPowerProcessor.Recalc(_earth);
        Assert.That(Power.Consumers.Any(c => c.DesignId == ColonyPowerProcessor.ScrappingLineId), Is.False);
        var quiet = Snapshot();

        Assert.That(Translator.Translate(_player, _earth, new UpgradeFacilityCommand(_earth.Id, lineId, bigger.UniqueID)).Accepted, Is.True);
        var upgrade = construction.BuildQueue.Single();
        Assert.That(ScrapLine(), Is.EqualTo(ColonyPowerProcessor.Scrapping_kW(_earth)).Within(1e-12));
        Assert.That(ScrapLine(), Is.GreaterThan(0), "scrapping draws from the moment it is queued");

        // A long job put ahead of it takes today's points: moving the upgrade up gives them back.
        var ahead = new LocalConstructionJob(bigger) { PointCost = construction.PointsPerDay * 10 };
        ahead.ResourcesRemaining.Clear();
        construction.BuildQueue = new Queue<LocalConstructionJob>(new[] { ahead, upgrade });
        ColonyPowerProcessor.Recalc(_earth);
        Assert.That(ScrapLine(), Is.EqualTo(0));
        // These three orders go through the colony's action lanes, so the queue (and the demand)
        // changes when the order executes; run them directly.
        MoveUpInConstructionQueueOrder.Create(_earth, upgrade).Execute(_earth.StarSysDateTime);
        Assert.That(construction.BuildQueue.Peek(), Is.SameAs(upgrade));
        Assert.That(ScrapLine(), Is.GreaterThan(0));
        MoveDownInConstructionQueueOrder.Create(_earth, upgrade).Execute(_earth.StarSysDateTime);
        Assert.That(ScrapLine(), Is.EqualTo(0));

        RemoveFromConstructionQueueOrder.Create(_earth, upgrade).Execute(_earth.StarSysDateTime);
        Assert.That(construction.BuildQueue, Has.No.Member(upgrade));
        Assert.That(ScrapLine(), Is.EqualTo(0));
        AssertNoDailyWork(quiet);
    }

    [Test]
    public void FinishingAnInstallationJob_HandsTodaysPointsOn_AndRefreshesTheScrapLoad()
    {
        var (lineId, _) = Refinery();
        var bigger = TemplateDesign("refinery-site", ("Size", 6000));
        StockAssemble(bigger);
        var construction = _earth.GetDataBlob<LocalConstructionDB>();
        construction.BuildQueue.Clear();
        Assert.That(Translator.Translate(_player, _earth, new UpgradeFacilityCommand(_earth.Id, lineId, bigger.UniqueID)).Accepted, Is.True);
        var upgrade = construction.BuildQueue.Single();

        // A small assembly ahead of it that finishes today and leaves the rest of the day's points.
        var small = Info.ComponentDesigns["default-design-factory-site"];
        var first = new LocalConstructionJob(small);
        first.ResourcesRemaining.Clear();
        first.PointCost = Math.Max(1, Math.Min(LocalConstructionJob.MountCost(small.MassPerUnit), construction.PointsPerDay / 2));
        construction.BuildQueue = new Queue<LocalConstructionJob>(new[] { first, upgrade });
        ColonyPowerProcessor.Recalc(_earth);
        double before = ScrapLine();
        Assume.That(upgrade.PointCost, Is.GreaterThan(construction.PointsPerDay), "the upgrade takes more than a day");

        new LocalConstructionProcessor().ProcessEntity(_earth, 0);
        Assert.That(construction.BuildQueue, Has.No.Member(first), "the small job finished");
        Assert.That(ScrapLine(), Is.EqualTo(ColonyPowerProcessor.Scrapping_kW(_earth)).Within(1e-12), "the grid shows today's load");
        Assert.That(ScrapLine(), Is.GreaterThan(before), "the upgrade now gets the whole day's points");
    }

    // ---- the daily run is unchanged ----

    [Test]
    public void Refreshes_BurnNoFuel_AndTheDailyRunBurnsOnce()
    {
        var (lineId, line) = Refinery();
        ColonyPowerProcessor.RunDay(_earth);
        var after1 = Snapshot();
        int history = Power.History.Count;

        // Many refreshes in one day.
        for (int i = 0; i < 5; i++)
        {
            Accept(new QueueIndustryJobCommand(_earth.Id, lineId, "stainless-steel", 5, false, false));
            Accept(new CancelIndustryJobCommand(_earth.Id, lineId, line.Jobs.Single().JobID));
        }
        AssertNoDailyWork(after1);
        Assert.That(Power.History.Count, Is.EqualTo(history));
        // Between dailies satisfaction is the capacity-based share Recalc already set.
        foreach (var (tier, share) in ColonyPowerProcessor.ShareByTier(Power.Delivered_kW, Power.DemandByTier))
            Assert.That(Power.TierSatisfaction[tier], Is.EqualTo(share).Within(1e-12), tier.ToString());

        // The next daily run burns one day's fuel for the demand it sees.
        var stock = FuelStock();
        ColonyPowerProcessor.RunDay(_earth);
        foreach (var (fuel, kg) in Power.FuelBurnt_kgLastDay)
        {
            var cargo = Info.Data.CargoGoods.GetAny(fuel)!;
            double taken = (stock[fuel] - Units(fuel)) * (double)cargo.MassPerUnit;
            Assert.That(taken, Is.EqualTo(kg).Within(cargo.MassPerUnit + 1e-9), fuel + ": one day's burn, whole units");
        }
        Assert.That(Power.History.Count, Is.LessThanOrEqualTo(history + 1));
    }

    [Test]
    public void ShortageEvents_ComeFromTheDailyRunOnly_AndOnce()
    {
        var events = new List<Pulsar4X.Events.Event>();
        void OnEvent(Pulsar4X.Events.Event e)
        {
            if (e.EntityId == _earth.Id)
                lock (events) events.Add(e);
        }
        Pulsar4X.Events.EventManager.Instance.Subscribe(Pulsar4X.Events.EventType.PowerShortage, OnEvent);
        try
        {
            // Switch most of the generation off so the grid is short.
            var instances = _earth.GetDataBlob<ComponentInstancesDB>();
            foreach (var c in instances.AllComponents.Values.Where(c => c.Design.HasAttribute<PowerPlantAtb>()))
                c.IsEnabled = false;
            ColonyPowerProcessor.RunDay(_earth);
            Assert.That(Power.Short, Is.True, "no fuel plants: life support and industry are short");
            Assert.That(events, Has.Count.EqualTo(1));

            var (lineId, line) = Refinery();
            Accept(new QueueIndustryJobCommand(_earth.Id, lineId, "hydrolox", 50, false, false));
            Accept(new CancelIndustryJobCommand(_earth.Id, lineId, line.Jobs.Single().JobID));
            Assert.That(events, Has.Count.EqualTo(1), "a refresh raises nothing");
            Assert.That(Power.Short, Is.True, "and keeps the daily flag");

            ColonyPowerProcessor.RunDay(_earth);
            Assert.That(events, Has.Count.EqualTo(1), "still short: the shortage is not raised again");
        }
        finally
        {
            Pulsar4X.Events.EventManager.Instance.Unsubscribe(Pulsar4X.Events.EventType.PowerShortage, OnEvent);
        }
    }

    [Test]
    public void AShipWithIndustry_GetsNoGrid()
    {
        var ship = Entity.Create(_player.Id);
        _sol.AddEntity(ship, Array.Empty<BaseDataBlob>());
        Assert.DoesNotThrow(() => ColonyPowerProcessor.RefreshDemand(ship));
        Assert.DoesNotThrow(() => ColonyPowerProcessor.RefreshDemand(null));
        Assert.That(ship.HasDataBlob<ColonyPowerDB>(), Is.False);
    }

    // ---- helpers ----

    record DailyState(DateTime LastDispatch, double Dispatched, string Burnt, int History, bool Short, int FuelOut, string Stock);

    DailyState Snapshot() => new(Power.LastDispatch, Power.Dispatched_kW,
        string.Join(",", Power.FuelBurnt_kgLastDay.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value.ToString("R"))),
        Power.History.Count, Power.Short, Power.FuelOut.Count,
        string.Join(",", FuelStock().OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value)));

    void AssertNoDailyWork(DailyState before)
    {
        Assert.That(Snapshot(), Is.EqualTo(before), "a refresh burns no fuel, records no day and raises nothing");
    }

    Dictionary<string, long> FuelStock()
        => Power.Plants.Where(p => p.FuelId != null).Select(p => p.FuelId!).Distinct().ToDictionary(id => id, Units);

    long Units(string id)
    {
        var cargo = Info.Data.CargoGoods.GetAny(id);
        return cargo == null ? 0 : _earth.GetDataBlob<CargoStorageDB>().GetUnitsStored(cargo, false);
    }

    void Accept(GameCommand command)
    {
        var result = Translator.Translate(_player, _earth, command);
        Assert.That(result.Accepted, Is.True, result.RejectionReason);
    }

    double Expected(string lineId, IndustryJob head)
    {
        var line = Industry.ProductionLines[lineId];
        double nameplate = RefineryLine().Nameplate_kW;
        double infra = InfrastructureProcessor.GetEfficiency(_earth);
        double kWhPerDay = line.IndustryTypeRates["refining"] * infra * head.EnergyPerPoint * EnergyTech.IndustrialEnergyFactor(_earth);
        return Math.Min(nameplate, nameplate * ColonyPowerProcessor.IndustryIdleShare + kWhPerDay / 24);
    }

    ColonyPowerConsumer RefineryLine() => Power.Consumers.Single(c => c.DesignId == "default-design-refinery-site");

    double ScrapLine() => Power.Consumers.Where(c => c.DesignId == ColonyPowerProcessor.ScrappingLineId).Sum(c => c.Draw_kW);

    (string lineId, IndustryAbilityDB.ProductionLine line) Refinery()
    {
        var instances = _earth.GetDataBlob<ComponentInstancesDB>();
        var pair = Industry.ProductionLines.First(l => instances.AllComponents.TryGetValue(l.Key, out var c)
            && c.Design.UniqueID == "default-design-refinery-site");
        return (pair.Key, pair.Value);
    }

    IndustryJob NewJob(string itemId, ushort count)
    {
        var job = new IndustryJob(Info, itemId);
        job.InitialiseJob(count, false);
        return job;
    }

    void StockRecipe(string itemId, int batches)
    {
        foreach (var (id, amount) in Info.IndustryDesigns[itemId].ResourceCosts)
            Add(id, amount * batches);
    }

    void StockAssemble(ComponentDesign design)
    {
        foreach (var (id, amount) in design.ResourceCosts)
            Add(id, amount);
    }

    void Add(string id, long units)
        => _earth.GetDataBlob<CargoStorageDB>().AddCargoByUnit(Info.Data.CargoGoods.GetAny(id)!, units);

    void Remove(string id)
    {
        var cargo = Info.Data.CargoGoods.GetAny(id);
        if (cargo == null)
            return;
        var storage = _earth.GetDataBlob<CargoStorageDB>();
        long units = storage.GetUnitsStored(cargo, false);
        if (units > 0)
            storage.RemoveCargoByUnit(cargo, units);
    }

    ComponentDesign TemplateDesign(string templateId, params (string key, double value)[] properties)
    {
        Info.Data.Unlock(templateId);
        var blueprint = new ComponentDesignBlueprint
        {
            UniqueID = templateId + "-refresh-" + Guid.NewGuid().ToString("N")[..6],
            Name = templateId + " " + string.Join(" ", properties.Select(p => p.value)),
            TemplateId = templateId,
            Properties = properties.Select(p => new ComponentDesignBlueprint.Property
            {
                Key = p.key,
                Value = new Newtonsoft.Json.Linq.JValue(p.value),
            }).ToList(),
        };
        return ComponentDesignFromJson.Create(_player, Info.Data, blueprint);
    }
}
