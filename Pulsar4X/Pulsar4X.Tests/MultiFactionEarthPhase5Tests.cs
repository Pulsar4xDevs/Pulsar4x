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
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Logistics;
using Pulsar4X.Modding;
using Pulsar4X.Names;
using Pulsar4X.Energy;
using Pulsar4X.Storage;
using GameEngine.Engine.Orders;

namespace Pulsar4X.Tests;

/// <summary>
/// Multi-faction Earth phase 5: civilians vs employees, staffing by priority tier, civilians for the academy and
/// NPC crews, and commuter contracts with same-body blocs.
/// </summary>
[TestFixture]
public class MultiFactionEarthPhase5Tests
{
    const string WarpTechStart = "Pulsar4x.Scenario.WarpTechStart";

    Game _game = null!;
    Entity _player = null!;
    Entity _earthBody = null!;
    Entity _company = null!;

    [SetUp]
    public void SetUp()
    {
        ModsState.RefreshModsList("Data");
        var paths = ModsState.AvailableMods.Where(m => m.Mod.ModName == "Pulsar4x").Select(m => m.Path).ToList();
        var store = GameLifecycle.LoadModStore(paths, ModsState.AvailableMods.Select(m => m.Path), WarpTechStart);
        var request = GameLifecycle.BuildQuickstartRequest(paths, GameLifecycle.BuildCatalog(store), 12345678);
        var result = GameLifecycle.CreateGameCore(store, request);
        Assert.That(result, Is.Not.Null);
        (_game, _player) = (result!.Value.game, result.Value.faction);
        _game.Settings.EnforceSingleThread = true;
        _company = Colonies().Single(c => c.FactionOwnerID == _player.Id && BodyName(c) == "Earth");
        _earthBody = _company.GetDataBlob<ColonyInfoDB>().PlanetEntity;
    }

    List<Entity> Colonies() => _game.Systems.SelectMany(s => s.GetAllEntitiesWithDataBlob<ColonyInfoDB>()).ToList();
    static string BodyName(Entity c) => c.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<NameDB>().DefaultName;
    Entity Named(string name) => Colonies().Single(c => c.GetDataBlob<NameDB>().DefaultName == name);
    static FactionInfoDB Info(Entity colony) => colony.GetFactionOwner.GetDataBlob<FactionInfoDB>();
    static ICargoable Iron(Entity colony) => Info(colony).Data.CargoGoods["iron"];

    static long Stock(Entity colony, int mineralId) =>
        colony.GetDataBlob<CargoStorageDB>().TypeStores.Values
            .Sum(s => s.CurrentStoreInUnits.TryGetValue(mineralId, out var v) ? v : 0);

    static ColonyWorkforceDB Wf(Entity c) => c.GetDataBlob<ColonyWorkforceDB>();
    static long Pop(Entity c) => Workforce.PopulationOf(c);
    static long Above(ColonyWorkforceDB db, StaffTier tier) =>
        db.RequiredByTier.Where(kv => kv.Key < tier).Sum(kv => kv.Value);

    [Test]
    public void NewGame_StaffsEveryColonyAtMinOfPopulationAndRequired()
    {
        var wf = Wf(_company);
        TestContext.Out.WriteLine($"company pop {Pop(_company)} required {wf.Required} " +
            string.Join(", ", wf.RequiredByTier.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}")));
        foreach (var c in Colonies())
            TestContext.Out.WriteLine($"{c.GetDataBlob<NameDB>().DefaultName}: pop {Pop(c)} required {Wf(c).Required} employed {Wf(c).Employed}");
        Assert.That(wf.Required, Is.GreaterThan(0));
        Assert.That(wf.Employed, Is.EqualTo(Math.Min(Pop(_company), wf.Required)));
        Assert.That(Workforce.Civilians(_company), Is.EqualTo(Pop(_company) - wf.Employed));
        foreach (StaffTier tier in Enum.GetValues(typeof(StaffTier)))
            Assert.That(Workforce.Share(_company, tier), Is.EqualTo(1), tier.ToString());
        var amc = Named("Americas Compact");
        Assert.That(Wf(amc).Employed, Is.EqualTo(Wf(amc).Required), "a 1M bloc is fully staffed");
    }

    [Test]
    public void OldSave_LoadsWithEmployedAtMinOfPopAndRequired_AndUnchangedShares()
    {
        _company.RemoveDataBlob<ColonyWorkforceDB>();
        Assert.That(Workforce.Share(_company, StaffTier.Mining), Is.EqualTo(1), "no record: output unchanged");
        Assert.That(Workforce.Civilians(_company), Is.EqualTo(Pop(_company)));
        var db = Workforce.Ensure(_company)!;
        Assert.That(db.Employed, Is.EqualTo(Math.Min(Pop(_company), db.Required)));
        foreach (StaffTier tier in Enum.GetValues(typeof(StaffTier)))
            Assert.That(Workforce.Share(_company, tier), Is.EqualTo(1), tier.ToString());
    }

    [Test]
    public void ShortStaff_FillsHigherTiersFirst_AndHalfStaffedMiningYieldsHalf()
    {
        var wf = Wf(_company);
        Assert.That(wf.RequiredByTier.GetValueOrDefault(StaffTier.Mining), Is.GreaterThan(0), "the company mines");
        var processor = new MineResourcesProcessor();
        processor.Init(_game);
        var manager = _company.Manager!;
        int mineral = _company.GetDataBlob<MiningDB>().ActualMiningRate.Keys.First();
        long Stock() => _company.GetDataBlob<CargoStorageDB>().TypeStores.Values
            .Sum(s => s.CurrentStoreInUnits.TryGetValue(mineral, out var v) ? v : 0);

        long before = Stock();
        processor.ProcessManager(manager, 86400);
        long full = Stock() - before;
        Assert.That(full, Is.GreaterThan(0));

        long mining = wf.RequiredByTier[StaffTier.Mining];
        Workforce.SetTarget(_company, Above(wf, StaffTier.Mining) + mining / 2);
        foreach (var tier in new[] { StaffTier.Essential, StaffTier.Control, StaffTier.Storage, StaffTier.Industry })
            Assert.That(Workforce.Share(_company, tier), Is.EqualTo(1), tier.ToString());
        Assert.That(Workforce.Share(_company, StaffTier.Mining), Is.EqualTo((double)(mining / 2) / mining).Within(1e-9));
        if (wf.RequiredByTier.GetValueOrDefault(StaffTier.Research) > 0)
            Assert.That(Workforce.Share(_company, StaffTier.Research), Is.EqualTo(0), "lowest tier gets nothing left");

        before = Stock();
        processor.ProcessManager(manager, 86400);
        long half = Stock() - before;
        Assert.That((double)half / full, Is.EqualTo(0.5).Within(0.02));
    }

    [Test]
    public void Academy_RefusesWhenCiviliansAreShort_EvenWithPeopleAtWork()
    {
        var academy = _company.GetDataBlob<Pulsar4X.People.NavalAcademyDB>().Academies[0];
        var info = _company.GetDataBlob<ColonyInfoDB>();
        int species = info.Population.Keys.First();
        info.Population[species] = Wf(_company).Required + 50;
        Workforce.Recalc(_company);
        Assert.That(Workforce.Civilians(_company), Is.EqualTo(50));
        var refusal = Pulsar4X.People.NavalAcademyCourse.TryTrainCrew(_company, academy.ComponentId, 200);
        Assert.That(refusal, Does.Contain("civilians"));
        Assert.That(Pop(_company), Is.EqualTo(Wf(_company).Required + 50), "nobody left the population");
    }

    [Test]
    public void DisablingTheShipyard_FreesItsStaff()
    {
        var instances = _company.GetDataBlob<Pulsar4X.Datablobs.ComponentInstancesDB>();
        var yard = instances.DesignsAndComponentCount.Select(d => d.Key).Single(d => d.UniqueID == "default-design-shipyard");
        long crew = yard.CrewReq;
        Assert.That(crew, Is.EqualTo(2000), "the shrunk shipyard");
        long employed = Wf(_company).Employed, civilians = Workforce.Civilians(_company);
        foreach (var c in instances.GetComponentsBySpecificDesign(yard.UniqueID))
            c.IsEnabled = false;
        Workforce.Recalc(_company);
        Assert.That(Wf(_company).Employed, Is.EqualTo(employed - crew));
        Assert.That(Workforce.Civilians(_company), Is.EqualTo(civilians + crew));
    }

    [Test]
    public void CommuterContract_AddsStaffWithoutMovingPopulation_AndEndsBelowFriendly()
    {
        var amc = Named("Americas Compact");
        long companyPop = Pop(_company), amcPop = Pop(amc);
        var wf = Wf(_company);
        Workforce.SetTarget(_company, wf.Required + 500);
        Assert.That(Workforce.TryContract(_company, amc, 500), Is.Null);
        Assert.That(wf.Commuters[amc.FactionOwnerID], Is.EqualTo(500));
        Assert.That((Pop(_company), Pop(amc)), Is.EqualTo((companyPop, amcPop)), "commuters stay bloc residents");
        Assert.That(Workforce.TryContract(_company, amc, 1), Does.Contain("posts"), "no open posts left");

        var body = _company.GetDataBlob<ColonyInfoDB>().PlanetEntity;
        Assert.That(Workforce.LabourPool(body, amc.FactionOwnerID), Is.EqualTo((long)(amcPop * 0.05) - 500));

        Info(_company).Stances[amc.FactionOwnerID] = FactionStance.Neutral;
        Workforce.RunDay(_company, _company.StarSysDateTime);
        Assert.That(wf.CommuterTotal, Is.EqualTo(0), "a stance drop ends the contract");
    }

    [Test]
    public void RelocatingCommuters_MovesThemIn_AndPaysTheBlocTheSigningFee()
    {
        var amc = Named("Americas Compact");
        var wf = Wf(_company);
        Workforce.SetTarget(_company, wf.Required + 100);
        Assert.That(Workforce.TryContract(_company, amc, 100), Is.Null);
        long companyPop = Pop(_company), amcPop = Pop(amc), employed = wf.Employed;
        decimal companyFunds = Info(_company).Money.GetCurrentFunds(), amcFunds = Info(amc).Money.GetCurrentFunds();

        Assert.That(Workforce.TryRelocateCommuters(_company, amc, 100), Is.Null);
        Assert.That((Pop(_company), Pop(amc)), Is.EqualTo((companyPop + 100, amcPop - 100)));
        Assert.That((wf.Employed, wf.CommuterTotal), Is.EqualTo((employed + 100, 0L)));
        Assert.That(Info(_company).Money.GetCurrentFunds(), Is.EqualTo(companyFunds - 10_000m));
        Assert.That(Info(amc).Money.GetCurrentFunds(), Is.EqualTo(amcFunds + 10_000m));
    }

    [Test]
    public void Hiring_RampsByATenthOfTargetADay_FromCivilians()
    {
        var wf = Wf(_company);
        long required = wf.Required;
        Workforce.SetTarget(_company, 0);
        Assert.That(wf.Employed, Is.EqualTo(0), "lay-offs are instant");
        Workforce.SetTarget(_company, null);
        var day = _company.StarSysDateTime.Date.AddDays(1);
        Workforce.RunDay(_company, day);
        long step = (long)Math.Ceiling(required * 0.1);
        Assert.That(wf.Employed, Is.EqualTo(Math.Min(step, Pop(_company))));
        Workforce.RunDay(_company, day);
        Assert.That(wf.Employed, Is.EqualTo(Math.Min(step, Pop(_company))), "once a day");
        Workforce.RunDay(_company, day.AddDays(1));
        Assert.That(wf.Employed, Is.EqualTo(Math.Min(2 * step, Pop(_company))));
    }

    [Test]
    public void NpcShipCrews_DrawOnlyCivilians()
    {
        var amc = Named("Americas Compact");
        long civ = Workforce.Civilians(amc);
        Assert.That(Pulsar4X.Ships.ShipCrew.Available(amc, int.MaxValue), Is.EqualTo((int)Math.Min(int.MaxValue, civ)));
    }

    [Test]
    public void WorkforceView_ProjectsTiersFacilitiesAndBlocContracts()
    {
        var view = new GameProjector(_game).ProjectEntity(_company, _player.Id).GetView<WorkforceView>();
        Assert.That(view, Is.Not.Null);
        Assert.That(view!.Required, Is.EqualTo(Wf(_company).Required));
        Assert.That(view.Employed + view.Civilians, Is.EqualTo(Pop(_company)));
        Assert.That(view.Tiers.Select(t => t.TierRank), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(view.Facilities.Sum(f => f.Required), Is.EqualTo(view.Required));
        Assert.That(view.Facilities.Single(f => f.DesignId == "default-design-shipyard").Tier, Is.EqualTo("Industry"));
        Assert.That(view.Contracts.Select(c => c.FactionName), Is.SupersetOf(new[] { "Americas Compact", "Sino-Pacific Coalition", "Euro Consortium" }));
        Assert.That(view.Contracts.All(c => c.CanContract && c.LabourPool == 50_000));
    }
}
